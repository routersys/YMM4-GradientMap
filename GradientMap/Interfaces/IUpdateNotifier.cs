namespace GradientMap.Interfaces;

public interface IUpdateNotifier
{
    void Notify(Version currentVersion, Version latestVersion);
}
