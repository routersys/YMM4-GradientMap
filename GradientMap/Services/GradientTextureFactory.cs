using GradientMap.Interfaces;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.Mathematics;

namespace GradientMap.Services;

public sealed class GradientTextureFactory : IGradientTextureFactory
{
    public ID2D1Bitmap? CreateGradientBitmap(ID2D1DeviceContext deviceContext, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        try
        {
            var source = LoadBitmapSource(filePath);
            var width = source.PixelWidth;
            var height = source.PixelHeight;
            var stride = width * 4;
            var byteLen = stride * height;

            var buffer = ArrayPool<byte>.Shared.Rent(byteLen);
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                source.CopyPixels(buffer, stride, 0);

                var props = new BitmapProperties
                {
                    PixelFormat = new()
                    {
                        Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                        AlphaMode = AlphaMode.Premultiplied,
                    },
                    DpiX = 96f,
                    DpiY = 96f,
                };

                return deviceContext.CreateBitmap(
                    new SizeI(width, height),
                    handle.AddrOfPinnedObject(),
                    stride,
                    props);
            }
            finally
            {
                handle.Free();
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch
        {
            return null;
        }
    }

    private static BitmapSource LoadBitmapSource(string filePath)
    {
        BitmapSource? result = null;

        void Load()
        {
            using var stream = File.OpenRead(filePath);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.None,
                BitmapCacheOption.OnLoad);
            var converted = new FormatConvertedBitmap(
                decoder.Frames[0],
                PixelFormats.Pbgra32,
                null,
                0.0);
            converted.Freeze();
            result = converted;
        }

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
            dispatcher.Invoke(Load);
        else
            Load();

        return result ?? throw new InvalidOperationException("Bitmap load returned null.");
    }
}
