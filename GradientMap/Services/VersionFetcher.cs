using GradientMap.Interfaces;
using GradientMap.Models;
using System.Net.Http;
using System.Text.Json;

namespace GradientMap.Services;

internal sealed class VersionFetcher : IVersionFetcher
{
    private static readonly HttpClient _httpClient = CreateHttpClient();

    private const string PrimaryUrl =
        "https://api.github.com/repos/routersys/YMM4-GradientMap/releases/latest";

    private const string FallbackUrl =
        "http://manjubox.net/api/ymm4plugins/github/detail/routersys/YMM4-GradientMap";

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GradientMap-Plugin/1.0");
        return client;
    }

    public async Task<Version?> FetchLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryFetchAsync(PrimaryUrl, cancellationToken).ConfigureAwait(false);
        if (result is not null)
            return result;

        return await TryFetchAsync(FallbackUrl, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Version?> TryFetchAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _httpClient
                .GetStringAsync(url, cancellationToken)
                .ConfigureAwait(false);

            var response = JsonSerializer.Deserialize(
                json,
                GitHubJsonContext.Default.GitHubReleaseResponse);

            if (response is null)
                return null;

            var tag = response.TagName.TrimStart('v', 'V');
            return Version.TryParse(tag, out var version) ? version : null;
        }
        catch
        {
            return null;
        }
    }
}
