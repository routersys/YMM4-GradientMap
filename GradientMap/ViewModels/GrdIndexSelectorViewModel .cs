using GradientMap.Interfaces;
using GradientMap.Models;
using GradientMap.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace GradientMap.ViewModels;

public sealed class GrdIndexSelectorViewModel : INotifyPropertyChanged
{
    private readonly IGrdManifestReader _manifestReader;

    private string _filePath = string.Empty;
    private int _selectedIndex;
    private bool _suppressSync;
    private GrdManifest _manifest = GrdManifest.Empty;

    public GrdIndexSelectorViewModel()
    {
        _manifestReader = GradientMapServices.Container.Resolve<IGrdManifestReader>();
    }

    public ObservableCollection<GrdGradientEntry> Entries { get; } = [];

    public bool IsVisible => _manifest.IsMultiple;

    public GrdGradientEntry? SelectedEntry
    {
        get => Entries.FirstOrDefault(e => e.Index == _selectedIndex);
        set
        {
            if (value is null) return;
            if (_selectedIndex == value.Index) return;
            _selectedIndex = value.Index;
            OnPropertyChanged();
            if (!_suppressSync)
                OnPropertyChanged(nameof(GradientIndex));
        }
    }

    public int GradientIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex == value) return;
            _selectedIndex = value;
            SyncSelection();
            OnPropertyChanged();
        }
    }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (string.Equals(_filePath, value, StringComparison.OrdinalIgnoreCase)) return;
            _filePath = value;
            RefreshManifest();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RefreshManifest()
    {
        _manifest = string.IsNullOrWhiteSpace(_filePath) ||
                    !File.Exists(_filePath) ||
                    !string.Equals(Path.GetExtension(_filePath), ".grd", StringComparison.OrdinalIgnoreCase)
            ? GrdManifest.Empty
            : _manifestReader.Read(_filePath);

        Entries.Clear();
        foreach (var entry in _manifest.Gradients)
            Entries.Add(entry);

        _suppressSync = true;
        try
        {
            var clamped = _manifest.Count > 0
                ? Math.Clamp(_selectedIndex, 0, _manifest.Count - 1)
                : 0;
            _selectedIndex = clamped;
            SyncSelection();
        }
        finally
        {
            _suppressSync = false;
        }

        OnPropertyChanged(nameof(IsVisible));
        OnPropertyChanged(nameof(GradientIndex));
    }

    private void SyncSelection()
    {
        OnPropertyChanged(nameof(SelectedEntry));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
