using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using Windows.Media.FaceAnalysis;

namespace Shuriken
{
    public static class SoftwareBitmapExtension
    {
        public static async Task<IList<DetectedFace>> FaceDetectAsync(this SoftwareBitmap source)
        {
            var dest = source;
            var detector = await FaceDetector.CreateAsync();

            if (!FaceDetector.IsBitmapPixelFormatSupported(dest.BitmapPixelFormat))
            {
                dest = SoftwareBitmap.Convert(dest, BitmapPixelFormat.Gray8);
            }

            return await detector.DetectFacesAsync(dest);
        }


        public static async Task<WriteableBitmap> ToWriteableBitmapAsync(this SoftwareBitmap source, Guid EndcorderID)
        {
            if (source == null) return null;

            using (var memory = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(EndcorderID, memory);
                encoder.SetSoftwareBitmap(source);
                await encoder.FlushAsync();

                var writeableBitmap = new WriteableBitmap(source.PixelWidth, source.PixelHeight);
                await writeableBitmap.SetSourceAsync(memory);

                return writeableBitmap;
            }
        }

        public static async Task<SoftwareBitmap> RotateRightAsync(this SoftwareBitmap source)
        {
            if (source == null) return null;

            using (var memory = new InMemoryRandomAccessStream())
            {
                // BitmapEncoder を用いメモリ上で source をリサイズ
                var id = BitmapEncoder.PngEncoderId;
                var encoder = await BitmapEncoder.CreateAsync(id, memory);
                encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise90Degrees;
                // encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.SetSoftwareBitmap(source);
                await encoder.FlushAsync();

                // リサイズしたメモリを WriteableBitmap に複写
                var writeableBitmap = new WriteableBitmap(source.PixelHeight, source.PixelWidth);
                await writeableBitmap.SetSourceAsync(memory);

                // dest（XAML の Image コントロール互換）を作成し、WriteableBitmap から複写
                var dest = new SoftwareBitmap(BitmapPixelFormat.Bgra8, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, BitmapAlphaMode.Premultiplied);
                dest.CopyFromBuffer(writeableBitmap.PixelBuffer);

                return dest;
            }
        }

        public static async Task<SoftwareBitmap> RotateLeftAsync(this SoftwareBitmap source)
        {
            if (source == null) return null;

            using (var memory = new InMemoryRandomAccessStream())
            {
                // BitmapEncoder を用いメモリ上で source をリサイズ
                var id = BitmapEncoder.PngEncoderId;
                var encoder = await BitmapEncoder.CreateAsync(id, memory);
                encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise270Degrees;
                // encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.SetSoftwareBitmap(source);
                await encoder.FlushAsync();

                // リサイズしたメモリを WriteableBitmap に複写
                var writeableBitmap = new WriteableBitmap(source.PixelHeight, source.PixelWidth);
                await writeableBitmap.SetSourceAsync(memory);

                // dest（XAML の Image コントロール互換）を作成し、WriteableBitmap から複写
                var dest = new SoftwareBitmap(BitmapPixelFormat.Bgra8, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, BitmapAlphaMode.Premultiplied);
                dest.CopyFromBuffer(writeableBitmap.PixelBuffer);

                return dest;
            }
        }

        //http://c5d5e5.asablo.jp/blog/2017/08/08/8642588
        // 2 よりこっちのほうが速い
        public static async Task<SoftwareBitmap> ResizeAsync(this SoftwareBitmap source, float newWidth, float newHeight)
        {
            if (source == null) return null;

            using (var memory = new InMemoryRandomAccessStream())
            {
                // BitmapEncoder を用いメモリ上で source をリサイズ
                var id = BitmapEncoder.PngEncoderId;
                var encoder = await BitmapEncoder.CreateAsync(id, memory);
                encoder.BitmapTransform.ScaledHeight = (uint)newHeight;
                encoder.BitmapTransform.ScaledWidth = (uint)newWidth;
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.SetSoftwareBitmap(source);
                await encoder.FlushAsync();

                // リサイズしたメモリを WriteableBitmap に複写
                var writeableBitmap = new WriteableBitmap((int)newWidth, (int)newHeight);
                await writeableBitmap.SetSourceAsync(memory);

                // dest（XAML の Image コントロール互換）を作成し、WriteableBitmap から複写
                var dest = new SoftwareBitmap(BitmapPixelFormat.Bgra8, (int)newWidth, (int)newHeight, BitmapAlphaMode.Premultiplied);
                dest.CopyFromBuffer(writeableBitmap.PixelBuffer);

                return dest;
            }
        }

        // https://docs.microsoft.com/ja-jp/windows/uwp/audio-video-camera/detect-and-track-faces-in-an-image
        public static async Task<SoftwareBitmap> ResizeAsync2(this SoftwareBitmap source, float newWidth, float newHeight)
        {
            if (source == null) return null;

            using (var memory = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memory);
                encoder.SetSoftwareBitmap(source);
                await encoder.FlushAsync();

                var decoder = await BitmapDecoder.CreateAsync(memory);

                var transform = new BitmapTransform()
                {
                    ScaledHeight = (uint)newHeight,
                    ScaledWidth = (uint)newWidth,
                    InterpolationMode = BitmapInterpolationMode.Fant,
                };

                var dest = await decoder.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, 
                    transform, 
                    ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage
                );

                return dest;
            }
        }

        // https://stackoverflow.com/questions/41251716/how-to-resize-a-softwarebitmap
        // Win2D が必要
        // public static SoftwareBitmap Resize(this SoftwareBitmap softwareBitmap, float newWidth, float newHeight)
        // {
        //     using (var resourceCreator = CanvasDevice.GetSharedDevice())
        //     using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(resourceCreator, softwareBitmap))
        //     using (var canvasRenderTarget = new CanvasRenderTarget(resourceCreator, newWidth, newHeight, canvasBitmap.Dpi))
        //     using (var drawingSession = canvasRenderTarget.CreateDrawingSession())
        //     using (var scaleEffect = new ScaleEffect())
        //     {
        //         scaleEffect.Source = canvasBitmap;
        //         scaleEffect.Scale = new System.Numerics.Vector2(newWidth / softwareBitmap.PixelWidth, newHeight / softwareBitmap.PixelHeight);
        //         drawingSession.DrawImage(scaleEffect);
        //         drawingSession.Flush();
        //         return SoftwareBitmap.CreateCopyFromBuffer(canvasRenderTarget.GetPixelBytes().AsBuffer(), BitmapPixelFormat.Bgra8, (int)newWidth, (int)newHeight, BitmapAlphaMode.Premultiplied);
        //     }
        // }
        // 
        // public static async Task<SoftwareBitmap> ResizeAsync(this SoftwareBitmap softwareBitmap, float newWidth, float newHeight)
        // {
        //     using (var resourceCreator = CanvasDevice.GetSharedDevice())
        //     using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(resourceCreator, softwareBitmap))
        //     using (var canvasRenderTarget = new CanvasRenderTarget(resourceCreator, (int)newWidth, (int)newHeight, 96))
        //     using (var drawingSession = canvasRenderTarget.CreateDrawingSession())
        //     {
        //         drawingSession.DrawImage(canvasBitmap, canvasRenderTarget.Bounds);
        // 
        //         var pixelBytes = canvasRenderTarget.GetPixelBytes();
        // 
        //         var writeableBitmap = new WriteableBitmap((int)newWidth, (int)newHeight);
        //         using (var stream = writeableBitmap.PixelBuffer.AsStream())
        //         {
        //             await stream.WriteAsync(pixelBytes, 0, pixelBytes.Length);
        //         }
        // 
        //         var scaledSoftwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, (int)newWidth, (int)newHeight);
        //         scaledSoftwareBitmap.CopyFromBuffer(writeableBitmap.PixelBuffer);
        // 
        //         return scaledSoftwareBitmap;
        //     }
        // }
    }
}
