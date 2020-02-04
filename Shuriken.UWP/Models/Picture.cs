using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Shuriken.UWP.Models
{
    public enum LongSide
    {
        Width,
        Height,
    }

    public class Picture
    {
        private static readonly Guid ENCODER_ID = BitmapEncoder.PngEncoderId;

        static public async Task<Picture> FromSoftwareBitmapAsync(SoftwareBitmap softwareBitmap)
        {
            if (softwareBitmap == null) throw new NullReferenceException();

            var filename = $"{Guid.NewGuid()}.png"; // ToDo: PNG 決め打ちを直す
            var original_file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(filename);

            using (var s = await original_file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(ENCODER_ID, s);
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();
            }

            var property = await original_file.GetBasicPropertiesAsync();

            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);

            return new Picture()
            {
                SoftwareBitmap = softwareBitmap,
                Source = source,
                FileName = filename,
                FileSize = property.Size.ToReadableSize(),
                Height = softwareBitmap.PixelHeight,
                Width = softwareBitmap.PixelWidth,
            };
        }

        static public async Task<Picture> FromFileAsync(StorageFile file)
        {
            if (file == null) throw new NullReferenceException();

            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                return await FromStreamAsync(stream);
            }
        }

        static public async Task<Picture> FromStreamAsync(IRandomAccessStream stream)
        {
            if (stream == null) throw new NullReferenceException();

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            return await FromSoftwareBitmapAsync(softwareBitmap);
        }

        public string FileName { get; private set; }
        public string FileSize { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        public SoftwareBitmap SoftwareBitmap { get; private set; }
        public SoftwareBitmapSource Source { get; private set; }
        public LongSide LongSide { get { return Width >= Height ? LongSide.Width : LongSide.Height;  } }

        private Picture()
        {

        }
    }
}
