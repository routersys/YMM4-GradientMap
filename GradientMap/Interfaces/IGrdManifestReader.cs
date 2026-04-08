using GradientMap.Models;

namespace GradientMap.Interfaces;

public interface IGrdManifestReader
{
    GrdManifest Read(string filePath);
}
