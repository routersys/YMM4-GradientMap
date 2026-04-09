using GradientMap.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace GradientMap.ViewModels;

public sealed class GradientColorStopViewModel : INotifyPropertyChanged
{
    private float _position;
    private Color _color;

    public GradientColorStopViewModel(float position, Color color)
    {
        _position = Math.Clamp(position, 0f, 1f);
        _color = color;
    }

    public float Position
    {
        get => _position;
        set
        {
            var v = Math.Clamp(value, 0f, 1f);
            if (MathF.Abs(_position - v) < float.Epsilon) return;
            _position = v;
            Raise();
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = value;
            Raise();
        }
    }

    public GradientColorStop ToModel() =>
        new(_position, _color.R, _color.G, _color.B, _color.A);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise([CallerMemberName] string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
