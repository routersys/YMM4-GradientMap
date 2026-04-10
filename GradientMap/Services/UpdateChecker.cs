using GradientMap.Interfaces;
using System.Reflection;

namespace GradientMap.Services;

internal sealed class UpdateChecker(IVersionFetcher versionFetcher, IUpdateNotifier notifier)
    : IUpdateChecker
{
    private static int _checked;

    public async Task CheckAndNotifyAsync()
    {
        if (Interlocked.CompareExchange(ref _checked, 1, 0) != 0)
            return;

        try
        {
            var latestVersion = await versionFetcher
                .FetchLatestVersionAsync()
                .ConfigureAwait(false);

            if (latestVersion is null)
                return;

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (currentVersion is null || latestVersion <= currentVersion)
                return;

            notifier.Notify(currentVersion, latestVersion);
        }
        catch
        {
        }
    }
}
