using GradientMap.Interfaces;
using GradientMap.Models;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;

namespace GradientMap.Services;

public sealed class GrdManifestReader : IGrdManifestReader
{
    private readonly ConcurrentDictionary<string, GrdManifest> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public GrdManifest Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return GrdManifest.Empty;

        if (_cache.TryGetValue(filePath, out var cached))
            return cached;

        var manifest = GrdParser.ReadManifest(filePath);

        if (manifest == GrdManifest.Empty)
            return manifest;

        var entries = manifest.Gradients
            .Select(e => new GrdGradientEntry(e.Index, e.Name, filePath))
            .ToImmutableArray();

        var hydrated = new GrdManifest(filePath, entries);
        _cache[filePath] = hydrated;
        return hydrated;
    }
}
