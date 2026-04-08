using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace GradientMap.Models;

public sealed class FileEntry : INotifyPropertyChanged
{
    private bool _isFavorite;
    private BitmapSource? _thumbnail;
    private bool _thumbnailLoaded;
    private bool _thumbnailLoading;

    public FileEntry(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        Extension = Path.GetExtension(filePath).ToLowerInvariant();
    }

    public string FilePath { get; }
    public string FileName { get; }
    public string Extension { get; }
    public bool IsGrd => Extension == ".grd";

    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetField(ref _isFavorite, value);
    }

    public BitmapSource? Thumbnail
    {
        get
        {
            if (!_thumbnailLoaded && !_thumbnailLoading)
                _ = LoadThumbnailAsync();
            return _thumbnail;
        }
        private set => SetField(ref _thumbnail, value);
    }

    private async Task LoadThumbnailAsync()
    {
        _thumbnailLoading = true;
        var bitmap = await Services.ThumbnailService.CreateThumbnailAsync(FilePath)
            .ConfigureAwait(false);
        _thumbnailLoaded = true;
        _thumbnailLoading = false;
        Thumbnail = bitmap;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public override bool Equals(object? obj) =>
        obj is FileEntry other &&
        string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath);
}
