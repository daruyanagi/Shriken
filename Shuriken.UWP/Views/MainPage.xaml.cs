using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace Shuriken.UWP
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ViewModels.MainViewModel ViewModel = new ViewModels.MainViewModel();

        public MainPage()
        {
            this.InitializeComponent();

            ViewModel = new ViewModels.MainViewModel();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // データの要求があったときのイベントを購読
            var dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequestedAsync;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            // イベントの購読解除
            var dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested -= this.DataTransferManager_DataRequestedAsync;
        }

        private async void DataTransferManager_DataRequestedAsync(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var file = await Windows.Storage.ApplicationData.Current.TemporaryFolder.GetFileAsync(ViewModel.Resized.FileName);
            DataPackage dataPackage = args.Request.Data;
            dataPackage.Properties.Title = Windows.ApplicationModel.Package.Current.DisplayName;
            dataPackage.Properties.Description = "Shrink a image for Blog/SNS posting - https://daruyanagi.jp";

            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));
        }

        internal async Task ShareTargetActivateAsync(ShareTargetActivatedEventArgs args)
        {
            ViewModel = new ViewModels.MainViewModel();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = ViewModel;

            // ビットマップファイルの場合
            if (args.ShareOperation.Data.Contains(StandardDataFormats.StorageItems))
            {
                var items = await args.ShareOperation.Data.GetStorageItemsAsync();

                // ToDo: 複数ファイルへの対応
                var file = items.FirstOrDefault() as Windows.Storage.StorageFile;

                await ViewModel.OpenFileAsync(file);
            }

            // ビットマップデータの場合
            else if (args.ShareOperation.Data.Contains(StandardDataFormats.Bitmap))
            {
                var reference = await args.ShareOperation.Data.GetBitmapAsync();

                using (var stream = await reference.OpenReadAsync())
                {
                    await ViewModel.OpenStreamAsync(stream);
                }
            }

            Window.Current.Content = this;
            Window.Current.Activate();

            // ［共有］で呼ばれたときは、コマンド実行後にウィンドウを閉じるようにする。［共有］ボタンは無効化
            // ViewModel.FromShare = true; バグのため無効化
            ViewModel.Window = Window.Current;
            ShareButton1.IsEnabled = false;
            ShareButton2.IsEnabled = false;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tag = (ResolutionsComboBox.SelectedItem as ComboBoxItem).Tag as string;
            ViewModel.Limit = int.Parse(tag);
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var tag = (sender as RadioButton).Tag as string;
            ViewModel.ResizeMode = (ViewModels.ResizeMode)Enum.Parse(typeof (ViewModels.ResizeMode), tag);
        }
    }
}
