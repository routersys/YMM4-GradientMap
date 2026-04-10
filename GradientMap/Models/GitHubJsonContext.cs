using System.Text.Json.Serialization;

namespace GradientMap.Models;

[JsonSerializable(typeof(GitHubReleaseResponse))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
internal sealed partial class GitHubJsonContext : JsonSerializerContext
{
}
