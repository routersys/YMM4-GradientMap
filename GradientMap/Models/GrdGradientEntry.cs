using GradientMap.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

public sealed class GrdGradientEntry : INotifyPropertyChanged
{
    private BitmapSource? _thumbnail;
    private bool _thumbnailLoaded;
    private bool _thumbnailLoading;

    public GrdGradientEntry(int index, string name, string filePath)
    {
        Index = index;
        Name = name;
        FilePath = filePath;
    }

    public int Index { get; }
    public string Name { get; }
    public string FilePath { get; }

    public BitmapSource? Thumbnail
    {
        get
        {
            if (!_thumbnailLoaded && !_thumbnailLoading)
                _ = LoadAsync();
            return _thumbnail;
        }
        private set
        {
            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    private async Task LoadAsync()
    {
        _thumbnailLoading = true;
        var bmp = await ThumbnailService.CreateThumbnailAsync(FilePath, Index)
            .ConfigureAwait(false);
        _thumbnailLoaded = true;
        _thumbnailLoading = false;
        Thumbnail = bmp;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
