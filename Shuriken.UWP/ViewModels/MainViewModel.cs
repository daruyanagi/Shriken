using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Shuriken.UWP.ViewModels
{
    public enum ResizeMode
    {
        Auto = 0,
        Width = 1,
        Height = 2,
    }

    public class MainViewModel : BindableBase
    {
        public bool FromShare { get; set; } = false;
        public Windows.UI.Xaml.Window Window { get; set; } = null;

        public RelayCommand OpenCommand { get; private set; }
        public RelayCommand PasteCommand { get; private set; }
        public RelayCommand RestoreDefaultCommand { get; private set; }
        public RelayCommand ResizeCommand { get; private set; }
        public RelayCommand RotateCommand { get; private set; }
        public RelayCommand<MainPage> CropCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand CopyCommand { get; private set; }
        public RelayCommand ShareCommand { get; private set; }

        public Models.Picture Original { get { return original; } private set { SetProperty(ref original, value); } }
        private Models.Picture original = null;

        public Models.Picture Resized { get { return PictureManipurationHistory.LastOrDefault(); } set { PictureManipurationHistory.Add(value); OnPropertyChanged(nameof(Resized)); } }
        public Models.PictureManipurationHistory PictureManipurationHistory { get; } = new Models.PictureManipurationHistory();

        public int Limit { get { return limit; } set { SetProperty(ref limit, value); } }
        private int limit = 1024;

        public ResizeMode ResizeMode { get { return resizeMode; } set { SetProperty(ref resizeMode, value); } }
        private ResizeMode resizeMode = ResizeMode.Auto;

        public MainViewModel()
        {
            OpenCommand = new RelayCommand(async () =>
            {
                var picker = new FileOpenPicker();

                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");

                var file = await picker.PickSingleFileAsync();

                await OpenFileAsync(file);
            });

            PasteCommand = new RelayCommand(async () =>
            {
                var view = Clipboard.GetContent();
                if (!view.Contains(StandardDataFormats.Bitmap)) return;

                var reference = await view.GetBitmapAsync();
                if (reference == null) return;

                using (var stream = await reference.OpenReadAsync())
                {
                    await OpenStreamAsync(stream);
                }
            });

            RestoreDefaultCommand = new RelayCommand(() =>
            {
                PictureManipurationHistory.Clear();
                Resized = Original;
            });

            ResizeCommand = new RelayCommand(async () =>
            {
                if (Resized == null) return;

                SoftwareBitmap softwareBitmap = null;

                switch (ResizeMode)
                {
                    case ResizeMode.Auto:
                        switch (Resized.LongSide)
                        {
                            case Models.LongSide.Height:
                                softwareBitmap = await ResizeByHeightAsync();
                                break;
                            case Models.LongSide.Width:
                                softwareBitmap = await ResizeByWidthAsync();
                                break;
                        }
                        break;
                    case ResizeMode.Height:
                        softwareBitmap = await ResizeByHeightAsync();
                        break;

                    case ResizeMode.Width:
                        softwareBitmap = await ResizeByWidthAsync();
                        break;
                }

                // XAML の Image コントロールは BGRA8/AlphaMode=Straight しかサポートしない
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                var picture = await Models.Picture.FromSoftwareBitmapAsync(softwareBitmap);
                Resized = picture;
            });

            RotateCommand = new RelayCommand(async () =>
            {
                var softwareBitmap = await Resized.SoftwareBitmap.RotateRightAsync();
                var picture = await Models.Picture.FromSoftwareBitmapAsync(softwareBitmap);
                Resized = picture;
            });

            SaveCommand = new RelayCommand(async () =>
            {
                var picker = new FileSavePicker();

                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.SuggestedFileName = $"{DateTime.Now:yyyMMdd-hhmmss}";
                picker.FileTypeChoices.Add("Portable Network Graphics", new List<string>() { ".png" });

                var file = await picker.PickSaveFileAsync();

                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    using (var s = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var id = BitmapEncoder.PngEncoderId; // ToDo: PNG 以外の対応
                        var encoder = await BitmapEncoder.CreateAsync(id, s);
                        encoder.SetSoftwareBitmap(Resized.SoftwareBitmap);
                        await encoder.FlushAsync();
                    }
                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                }

                if (FromShare) Window.Close();
            });

            CopyCommand = new RelayCommand(async () =>
            {
                var file = await ApplicationData.Current.TemporaryFolder.GetFileAsync(Resized.FileName); // ToDo: プロパティで持つ
                var dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(file)); // Caution: PNG 決め打ち
                Clipboard.SetContent(dataPackage);

                if (FromShare) Window.Close();
            });

            ShareCommand = new RelayCommand(() =>
            {
                DataTransferManager.ShowShareUI();

                if (FromShare) Window.Close();
            });

            CropCommand = new RelayCommand<MainPage>(_ =>
            {
                if (Resized == null) return;

                _.Frame.Navigate(typeof(Views.CropImagePage), Resized);
            });
        }

        private async Task<SoftwareBitmap> ResizeByHeightAsync()
        {
            float width = Resized.Width * ((float)Limit / Resized.Height);
            return await Resized.SoftwareBitmap.ResizeAsync(width, Limit);
        }

        private async Task<SoftwareBitmap> ResizeByWidthAsync()
        {
            float height = Resized.Height * ((float)Limit / Resized.Width);
            return await Resized.SoftwareBitmap.ResizeAsync(Limit, height);
        }

        public async Task OpenFileAsync(StorageFile file)
        {
            if (file == null) return;

            Original = await Models.Picture.FromFileAsync(file);

            PictureManipurationHistory.Clear();
            Resized = Original;
        }

        public async Task OpenStreamAsync(IRandomAccessStream stream)
        {
            if (stream == null) return;

            Original = await Models.Picture.FromStreamAsync(stream);

            PictureManipurationHistory.Clear();
            Resized = Original;
        }
    }
}
