using System.Text.Json.Serialization;

namespace GradientMap.Models;

internal sealed record GitHubReleaseResponse(
    [property: JsonPropertyName("tag_name")] string TagName);
