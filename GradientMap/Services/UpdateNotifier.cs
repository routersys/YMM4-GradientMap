using GradientMap.Interfaces;
using GradientMap.Localization;
using System.Windows;

namespace GradientMap.Services;

internal sealed class UpdateNotifier : IUpdateNotifier
{
    public void Notify(Version currentVersion, Version latestVersion)
    {
        Application.Current?.Dispatcher.Invoke(() =>
            MessageBox.Show(
                string.Format(Texts.UpdateAvailableMessage, latestVersion, currentVersion),
                Texts.UpdateAvailableTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Information));
    }
}
