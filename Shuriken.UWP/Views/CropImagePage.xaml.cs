using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Shuriken.UWP.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class CropImagePage : Page
    {
        public CropImagePage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var picture = e.Parameter as Models.Picture;

            ImageCropper.Source = await picture.SoftwareBitmap.ToWriteableBitmapAsync(BitmapEncoder.PngEncoderId);
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var filename = $"{Guid.NewGuid()}.png"; // ToDo: PNG 決め打ちを直す
            var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);

            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.None))
            {
                await ImageCropper.SaveAsync(fileStream, BitmapFileFormat.Png);
            }

            var picture = await Models.Picture.FromFileAsync(file);

            Frame.Navigate(typeof(MainPage), picture); // ToDo: MainPage で受け取る
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), null);
        }
    }
}
