namespace GradientMap.Interfaces;

public interface IVersionFetcher
{
    Task<Version?> FetchLatestVersionAsync(CancellationToken cancellationToken = default);
}
