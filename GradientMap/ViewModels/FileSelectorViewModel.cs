using GradientMap.Models;
using GradientMap.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YukkuriMovieMaker.Commons;

namespace GradientMap.ViewModels;

public sealed class FileSelectorViewModel : INotifyPropertyChanged
{
    private readonly string[] _extensions;
    private readonly string _filter;
    private string _filePath = string.Empty;
    private string _currentDirectory = string.Empty;
    private FileEntry? _selectedFile;
    private bool _suppressSync;

    public FileSelectorViewModel(string extensions, string filter)
    {
        _extensions = extensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : $".{e.ToLowerInvariant()}")
            .ToArray();
        _filter = BuildDialogFilter(filter);
        ToggleFavoriteCommand = new ActionCommand(_ => true, OnToggleFavorite);
        BrowseCommand = new ActionCommand(_ => true, _ => Browse());
    }

    public ObservableCollection<FileEntry> Files { get; } = [];

    public FileEntry? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (_selectedFile == value) return;
            _selectedFile = value;
            OnPropertyChanged();
            if (!_suppressSync && value is not null)
                FilePath = value.FilePath;
        }
    }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (string.Equals(_filePath, value, StringComparison.OrdinalIgnoreCase)) return;
            _filePath = value;
            OnPropertyChanged();
            OnFilePathChanged();
        }
    }

    public ICommand ToggleFavoriteCommand { get; }
    public ICommand BrowseCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnFilePathChanged()
    {
        var dir = string.IsNullOrWhiteSpace(_filePath)
            ? string.Empty
            : Path.GetDirectoryName(_filePath) ?? string.Empty;

        if (!string.Equals(dir, _currentDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _currentDirectory = dir;
            RefreshFiles();
        }

        SyncSelection();
    }

    private void SyncSelection()
    {
        _suppressSync = true;
        try
        {
            SelectedFile = Files.FirstOrDefault(
                f => string.Equals(f.FilePath, _filePath, StringComparison.OrdinalIgnoreCase));

            if (SelectedFile is null && !string.IsNullOrWhiteSpace(_filePath) && File.Exists(_filePath))
            {
                var entry = CreateEntry(_filePath);
                Files.Add(entry);
                SelectedFile = entry;
            }
        }
        finally
        {
            _suppressSync = false;
        }
    }

    private void RefreshFiles()
    {
        Files.Clear();

        var settings = GradientMapSettings.Instance;

        var favorites = settings.FavoritePaths
            .Where(p => File.Exists(p) && IsSupported(p))
            .Select(CreateEntry)
            .ToList();

        foreach (var fav in favorites)
        {
            fav.IsFavorite = true;
            Files.Add(fav);
        }

        if (!string.IsNullOrWhiteSpace(_currentDirectory) && Directory.Exists(_currentDirectory))
        {
            var dirFiles = Directory.EnumerateFiles(_currentDirectory)
                .Where(IsSupported)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .Select(CreateEntry);

            foreach (var entry in dirFiles)
            {
                if (Files.Any(f => string.Equals(f.FilePath, entry.FilePath, StringComparison.OrdinalIgnoreCase)))
                    continue;
                entry.IsFavorite = settings.FavoritePaths
                    .Contains(entry.FilePath, StringComparer.OrdinalIgnoreCase);
                Files.Add(entry);
            }
        }
    }

    private bool IsSupported(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return _extensions.Contains(ext);
    }

    private static FileEntry CreateEntry(string path) => new(path);

    private void OnToggleFavorite(object? parameter)
    {
        if (parameter is not FileEntry entry) return;

        entry.IsFavorite = !entry.IsFavorite;
        var settings = GradientMapSettings.Instance;
        var updated = new List<string>(settings.FavoritePaths);

        if (entry.IsFavorite)
        {
            if (!updated.Contains(entry.FilePath, StringComparer.OrdinalIgnoreCase))
                updated.Add(entry.FilePath);
        }
        else
        {
            updated.RemoveAll(
                p => string.Equals(p, entry.FilePath, StringComparison.OrdinalIgnoreCase));
        }

        settings.FavoritePaths = updated;
        settings.Save();
        RefreshFiles();
        SyncSelection();
    }

    private void Browse()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = _filter,
            InitialDirectory = string.IsNullOrWhiteSpace(_currentDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : _currentDirectory
        };

        if (dialog.ShowDialog() == true)
            FilePath = dialog.FileName;
    }

    private static string BuildDialogFilter(string filter)
    {
        return filter.Replace(',', '|');
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
