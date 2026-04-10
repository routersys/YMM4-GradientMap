using GradientMap.Core;
using GradientMap.Interfaces;

namespace GradientMap.Services;

internal static class GradientMapServices
{
    internal static readonly IServiceRegistry Container = BuildContainer();

    private static IServiceRegistry BuildContainer()
    {
        var registry = new ServiceRegistry();
        registry.RegisterSingleton<IGradientTextureFactory>(new GradientTextureFactory());
        registry.RegisterSingleton<IGrdManifestReader>(new GrdManifestReader());
        registry.RegisterFactory<IResourceRegistry>(() => new ResourceRegistry());
        registry.RegisterSingleton<IVersionFetcher>(new VersionFetcher());
        registry.RegisterSingleton<IUpdateNotifier>(new UpdateNotifier());
        registry.RegisterSingleton<IUpdateChecker>(
            new UpdateChecker(
                registry.Resolve<IVersionFetcher>(),
                registry.Resolve<IUpdateNotifier>()));
        return registry;
    }
}
