using GradientMap.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GradientMap.Services;

internal static class GradientStopSerializer
{
    private static readonly JsonSerializerOptions Options = new();

    public static GradientColorStop[] Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var dtos = JsonSerializer.Deserialize<StopDto[]>(json, Options);
            return dtos?.Select(d => new GradientColorStop(d.P, d.R, d.G, d.B, d.A)).ToArray() ?? [];
        }
        catch { return []; }
    }

    public static string Serialize(IEnumerable<GradientColorStop> stops)
    {
        var dtos = stops
            .Select(s => new StopDto { P = s.Position, R = s.R, G = s.G, B = s.B, A = s.A })
            .ToArray();
        return JsonSerializer.Serialize(dtos, Options);
    }

    private sealed class StopDto
    {
        [JsonPropertyName("p")] public float P { get; set; }
        [JsonPropertyName("r")] public byte R { get; set; }
        [JsonPropertyName("g")] public byte G { get; set; }
        [JsonPropertyName("b")] public byte B { get; set; }
        [JsonPropertyName("a")] public byte A { get; set; }
    }
}
