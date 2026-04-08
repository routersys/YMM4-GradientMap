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
    private readonly Dictionary<(string FilePath, int Index), WeakReference<ID2D1Bitmap>> _cache =
        new(GradientCacheKeyComparer.Instance);

    public ID2D1Bitmap? CreateGradientBitmap(
        ID2D1DeviceContext deviceContext,
        string filePath,
        int gradientIndex)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        var key = (filePath, gradientIndex);

        lock (_cache)
        {
            if (_cache.TryGetValue(key, out var weakRef) &&
                weakRef.TryGetTarget(out var cached))
                return cached;
        }

        try
        {
            var bitmap = string.Equals(
                Path.GetExtension(filePath), ".grd", StringComparison.OrdinalIgnoreCase)
                ? CreateFromGrd(deviceContext, filePath, gradientIndex)
                : CreateFromImage(deviceContext, filePath);

            if (bitmap is not null)
            {
                lock (_cache)
                {
                    _cache[key] = new WeakReference<ID2D1Bitmap>(bitmap);
                }
            }

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static ID2D1Bitmap? CreateFromGrd(
        ID2D1DeviceContext deviceContext,
        string filePath,
        int gradientIndex)
    {
        var pixels = GrdParser.ParseToPixels(filePath, gradientIndex);
        if (pixels is null) return null;

        const int width = GrdParser.Resolution;
        const int stride = width * 4;

        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            return deviceContext.CreateBitmap(
                new SizeI(width, 1),
                handle.AddrOfPinnedObject(),
                stride,
                new BitmapProperties
                {
                    PixelFormat = new()
                    {
                        Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                        AlphaMode = AlphaMode.Premultiplied,
                    },
                    DpiX = 96f,
                    DpiY = 96f,
                });
        }
        finally
        {
            handle.Free();
        }
    }

    private static ID2D1Bitmap? CreateFromImage(ID2D1DeviceContext deviceContext, string filePath)
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
            return deviceContext.CreateBitmap(
                new SizeI(width, height),
                handle.AddrOfPinnedObject(),
                stride,
                new BitmapProperties
                {
                    PixelFormat = new()
                    {
                        Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                        AlphaMode = AlphaMode.Premultiplied,
                    },
                    DpiX = 96f,
                    DpiY = 96f,
                });
        }
        finally
        {
            handle.Free();
            ArrayPool<byte>.Shared.Return(buffer);
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

    private sealed class GradientCacheKeyComparer
        : IEqualityComparer<(string FilePath, int Index)>
    {
        public static readonly GradientCacheKeyComparer Instance = new();

        public bool Equals((string FilePath, int Index) x, (string FilePath, int Index) y) =>
            string.Equals(x.FilePath, y.FilePath, StringComparison.OrdinalIgnoreCase) &&
            x.Index == y.Index;

        public int GetHashCode((string FilePath, int Index) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FilePath),
                obj.Index);
    }
}
