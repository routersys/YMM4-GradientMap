using Vortice.Direct2D1;

namespace GradientMap.Interfaces;

public interface IGradientTextureFactory
{
    ID2D1Bitmap? CreateGradientBitmap(ID2D1DeviceContext deviceContext, string filePath);
}
