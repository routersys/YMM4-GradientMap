using System.IO;
using System.Text;

namespace GradientMap.Services;

internal static class GrdParser
{
    internal const int Resolution = 256;

    internal static byte[]? ParseToPixels(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);

        var sig = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (sig != "8BGR") return null;

        var version = ReadU16(reader);
        var count = ReadU16(reader);
        if (count == 0) return null;

        return ReadFirstSolidGradientPixels(reader, version);
    }

    private static byte[]? ReadFirstSolidGradientPixels(BinaryReader reader, ushort version)
    {
        SkipName(reader, version);

        var gradType = ReadU16(reader);
        if (gradType != 0) return null;

        ReadU32(reader);

        var colorStopCount = ReadU16(reader);
        var colorStops = new (float Location, float Midpoint, byte R, byte G, byte B)[colorStopCount];
        for (var i = 0; i < colorStopCount; i++)
        {
            var location = ReadU32(reader) / 4096f;
            var midpoint = ReadU32(reader) / 100f;
            var model = ReadU16(reader);
            var c0 = ReadU16(reader);
            var c1 = ReadU16(reader);
            var c2 = ReadU16(reader);
            var c3 = ReadU16(reader);
            var (r, g, b) = ColorToRgb(model, c0, c1, c2, c3);
            colorStops[i] = (location, midpoint, r, g, b);
        }

        var transStopCount = ReadU16(reader);
        var transStops = new (float Location, float Midpoint, float Opacity)[transStopCount];
        for (var i = 0; i < transStopCount; i++)
        {
            var location = ReadU32(reader) / 4096f;
            var midpoint = ReadU32(reader) / 100f;
            var opacity = ReadU16(reader) / 65535f;
            transStops[i] = (location, midpoint, opacity);
        }

        return SampleToPixels(colorStops, transStops);
    }

    private static void SkipName(BinaryReader reader, ushort version)
    {
        if (version >= 5)
        {
            var charCount = ReadU32(reader);
            reader.BaseStream.Seek(charCount * 2, SeekOrigin.Current);
        }
        else
        {
            var byteCount = ReadU16(reader);
            reader.BaseStream.Seek(byteCount, SeekOrigin.Current);
        }
    }

    private static byte[] SampleToPixels(
        (float Location, float Midpoint, byte R, byte G, byte B)[] colorStops,
        (float Location, float Midpoint, float Opacity)[] transStops)
    {
        var pixels = new byte[Resolution * 4];
        for (var i = 0; i < Resolution; i++)
        {
            var t = i / (Resolution - 1f);
            var (r, g, b) = SampleColor(colorStops, t);
            var a = SampleOpacity(transStops, t);

            pixels[i * 4 + 0] = (byte)Math.Round(b * a * 255f);
            pixels[i * 4 + 1] = (byte)Math.Round(g * a * 255f);
            pixels[i * 4 + 2] = (byte)Math.Round(r * a * 255f);
            pixels[i * 4 + 3] = (byte)Math.Round(a * 255f);
        }
        return pixels;
    }

    private static (float R, float G, float B) SampleColor(
        (float Location, float Midpoint, byte R, byte G, byte B)[] stops, float t)
    {
        if (stops.Length == 0) return (0f, 0f, 0f);
        if (stops.Length == 1) return (stops[0].R / 255f, stops[0].G / 255f, stops[0].B / 255f);
        if (t <= stops[0].Location) return (stops[0].R / 255f, stops[0].G / 255f, stops[0].B / 255f);
        if (t >= stops[^1].Location) return (stops[^1].R / 255f, stops[^1].G / 255f, stops[^1].B / 255f);

        for (var i = 0; i < stops.Length - 1; i++)
        {
            var left = stops[i];
            var right = stops[i + 1];
            if (t < left.Location || t > right.Location) continue;

            var span = right.Location - left.Location;
            if (span < 1e-6f) return (right.R / 255f, right.G / 255f, right.B / 255f);

            var local = (t - left.Location) / span;
            var adj = AdjustMidpoint(local, left.Midpoint);
            return (
                Lerp(left.R / 255f, right.R / 255f, adj),
                Lerp(left.G / 255f, right.G / 255f, adj),
                Lerp(left.B / 255f, right.B / 255f, adj));
        }

        return (stops[^1].R / 255f, stops[^1].G / 255f, stops[^1].B / 255f);
    }

    private static float SampleOpacity(
        (float Location, float Midpoint, float Opacity)[] stops, float t)
    {
        if (stops.Length == 0) return 1f;
        if (stops.Length == 1) return stops[0].Opacity;
        if (t <= stops[0].Location) return stops[0].Opacity;
        if (t >= stops[^1].Location) return stops[^1].Opacity;

        for (var i = 0; i < stops.Length - 1; i++)
        {
            var left = stops[i];
            var right = stops[i + 1];
            if (t < left.Location || t > right.Location) continue;

            var span = right.Location - left.Location;
            if (span < 1e-6f) return right.Opacity;

            var local = (t - left.Location) / span;
            return Lerp(left.Opacity, right.Opacity, AdjustMidpoint(local, left.Midpoint));
        }

        return stops[^1].Opacity;
    }

    private static float AdjustMidpoint(float local, float mid)
    {
        if (local <= mid)
            return mid > 0f ? 0.5f * local / mid : 0.5f;
        return (1f - mid) > 0f ? 0.5f + 0.5f * (local - mid) / (1f - mid) : 1f;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static (byte R, byte G, byte B) ColorToRgb(ushort model, ushort c0, ushort c1, ushort c2, ushort c3)
    {
        return model switch
        {
            0 => ((byte)(c0 >> 8), (byte)(c1 >> 8), (byte)(c2 >> 8)),
            1 => HsbToRgb(c0, c1, c2),
            2 => CmykToRgb(c0, c1, c2, c3),
            3 => LabToRgb(c0, c1, c2),
            7 => ((byte)(c0 >> 8), (byte)(c0 >> 8), (byte)(c0 >> 8)),
            _ => (0, 0, 0)
        };
    }

    private static (byte R, byte G, byte B) HsbToRgb(ushort h, ushort s, ushort b)
    {
        var hf = h / 65535f * 360f;
        var sf = s / 65535f;
        var bf = b / 65535f;

        if (sf < 1e-6f)
        {
            var v = (byte)(bf * 255f);
            return (v, v, v);
        }

        var sector = (int)(hf / 60f) % 6;
        var f = hf / 60f - MathF.Floor(hf / 60f);
        var p = bf * (1f - sf);
        var q = bf * (1f - f * sf);
        var tv = bf * (1f - (1f - f) * sf);

        var (r, g, bv) = sector switch
        {
            0 => (bf, tv, p),
            1 => (q, bf, p),
            2 => (p, bf, tv),
            3 => (p, q, bf),
            4 => (tv, p, bf),
            _ => (bf, p, q)
        };

        return ((byte)(r * 255f), (byte)(g * 255f), (byte)(bv * 255f));
    }

    private static (byte R, byte G, byte B) CmykToRgb(ushort c, ushort m, ushort y, ushort k)
    {
        var cf = c / 65535f;
        var mf = m / 65535f;
        var yf = y / 65535f;
        var kf = k / 65535f;
        return (
            (byte)((1f - cf) * (1f - kf) * 255f),
            (byte)((1f - mf) * (1f - kf) * 255f),
            (byte)((1f - yf) * (1f - kf) * 255f));
    }

    private static (byte R, byte G, byte B) LabToRgb(ushort l, ushort a, ushort b)
    {
        var lf = l / 65535f * 100f;
        var af = a / 65535f * 255f - 128f;
        var bf = b / 65535f * 255f - 128f;

        var fy = (lf + 16f) / 116f;
        var fx = af / 500f + fy;
        var fz = fy - bf / 200f;

        static float F(float t) => t > 0.206897f ? t * t * t : (t - 16f / 116f) / 7.787f;

        var x = 0.95047f * F(fx);
        var y = 1.00000f * F(fy);
        var z = 1.08883f * F(fz);

        var r = x * 3.2406f + y * -1.5372f + z * -0.4986f;
        var g = x * -0.9689f + y * 1.8758f + z * 0.0415f;
        var bv = x * 0.0557f + y * -0.2040f + z * 1.0570f;

        static float Gamma(float v) =>
            v > 0.0031308f ? 1.055f * MathF.Pow(v, 1f / 2.4f) - 0.055f : 12.92f * v;

        return (
            (byte)(Math.Clamp(Gamma(r), 0f, 1f) * 255f),
            (byte)(Math.Clamp(Gamma(g), 0f, 1f) * 255f),
            (byte)(Math.Clamp(Gamma(bv), 0f, 1f) * 255f));
    }

    private static ushort ReadU16(BinaryReader r)
    {
        var buf = r.ReadBytes(2);
        return (ushort)((buf[0] << 8) | buf[1]);
    }

    private static uint ReadU32(BinaryReader r)
    {
        var buf = r.ReadBytes(4);
        return (uint)((buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | buf[3]);
    }
}
