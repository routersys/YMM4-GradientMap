using System.Reflection;

namespace GradientMap.Services;

internal static class GradientMapShaderLoader
{
    private static readonly byte[] ShaderBytes = LoadShaderBytes();

    public static ReadOnlySpan<byte> GetShaderBytes() => ShaderBytes;

    private static byte[] LoadShaderBytes()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(static n =>
                n.EndsWith(".GradientMap.cso", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                "Compiled shader 'GradientMap.cso' was not found as an embedded resource. " +
                "Ensure the project has been built so that fxc.exe compiled the HLSL.");

        using var stream = asm.GetManifestResourceStream(name)!;
        var bytes = new byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);
        return bytes;
    }
}
