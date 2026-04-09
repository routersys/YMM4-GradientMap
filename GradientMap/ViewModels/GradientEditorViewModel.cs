using GradientMap.Localization;
using GradientMap.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GradientMap.ViewModels;

public sealed class GradientEditorViewModel : INotifyPropertyChanged, IDisposable
{
    private sealed class DelegateCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            ArgumentNullException.ThrowIfNull(execute);
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _execute(parameter);
            }
        }
    }

    private readonly ObservableCollection<GradientColorStopViewModel> _stops = [];
    private LinearGradientBrush _gradientBrush;
    private bool _serializationSuspended;
    private bool _disposed;

    private readonly DelegateCommand _deleteStopCommand;
    private readonly DelegateCommand _exportAsGrdCommand;
    private readonly DelegateCommand _exportAsPngCommand;

    public GradientEditorViewModel()
    {
        Stops = new ReadOnlyObservableCollection<GradientColorStopViewModel>(_stops);
        _gradientBrush = BuildDefaultBrush();

        _deleteStopCommand = new DelegateCommand(
            p => { if (p is GradientColorStopViewModel vm) RemoveStop(vm); },
            p => p is GradientColorStopViewModel vm && CanDeleteStop(vm));

        _exportAsGrdCommand = new DelegateCommand(_ => ExportAsGrd(), _ => CanExport);
        _exportAsPngCommand = new DelegateCommand(_ => ExportAsPng(), _ => CanExport);

        DeleteStopCommand = _deleteStopCommand;
        ExportAsGrdCommand = _exportAsGrdCommand;
        ExportAsPngCommand = _exportAsPngCommand;
    }

    public ReadOnlyObservableCollection<GradientColorStopViewModel> Stops { get; }
    public LinearGradientBrush GradientBrush => _gradientBrush;
    public bool CanExport => _stops.Count >= 2;

    public bool CanDeleteStop(GradientColorStopViewModel stop) => _stops.Count > 2;

    public ICommand DeleteStopCommand { get; }
    public ICommand ExportAsGrdCommand { get; }
    public ICommand ExportAsPngCommand { get; }

    public event Action<string>? GradientJsonChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void LoadFromJson(string json)
    {
        DetachAll();
        _stops.Clear();

        var models = GradientStopSerializer.Deserialize(json);
        IEnumerable<GradientColorStopViewModel> vms = models.Length >= 2
            ? models.OrderBy(m => m.Position)
                    .Select(m => new GradientColorStopViewModel(
                        m.Position, Color.FromArgb(m.A, m.R, m.G, m.B)))
            : [
                new GradientColorStopViewModel(0f, Colors.Black),
                new GradientColorStopViewModel(1f, Colors.White)
              ];

        foreach (var vm in vms)
        {
            vm.PropertyChanged += OnStopChanged;
            _stops.Add(vm);
        }

        RefreshBrush();
        Raise(nameof(CanExport));
        _deleteStopCommand.RaiseCanExecuteChanged();
        _exportAsGrdCommand.RaiseCanExecuteChanged();
        _exportAsPngCommand.RaiseCanExecuteChanged();
    }

    public void AddStopAt(float position, Color color)
    {
        var sorted = _stops.OrderBy(s => s.Position).ToList();
        if (sorted.Any(s => Math.Abs(position - s.Position) < 1e-3f))
            return;

        var vm = new GradientColorStopViewModel(position, color);
        vm.PropertyChanged += OnStopChanged;
        var idx = _stops.Count(s => s.Position <= position);
        _stops.Insert(idx, vm);
        RefreshBrush();
        Raise(nameof(CanExport));
        Commit();
        _deleteStopCommand.RaiseCanExecuteChanged();
        _exportAsGrdCommand.RaiseCanExecuteChanged();
        _exportAsPngCommand.RaiseCanExecuteChanged();
    }

    public void RemoveStop(GradientColorStopViewModel stop)
    {
        if (!CanDeleteStop(stop)) return;
        stop.PropertyChanged -= OnStopChanged;
        _stops.Remove(stop);
        RefreshBrush();
        Raise(nameof(CanExport));
        Commit();
        _deleteStopCommand.RaiseCanExecuteChanged();
        _exportAsGrdCommand.RaiseCanExecuteChanged();
        _exportAsPngCommand.RaiseCanExecuteChanged();
    }

    public void SuspendSerialization() => _serializationSuspended = true;

    public void ResumeAndFinalizeDrag()
    {
        _serializationSuspended = false;
        var sorted = _stops.OrderBy(s => s.Position).ToList();
        DetachAll();
        _stops.Clear();
        foreach (var s in sorted)
        {
            s.PropertyChanged += OnStopChanged;
            _stops.Add(s);
        }
        RefreshBrush();
        Commit();
        _deleteStopCommand.RaiseCanExecuteChanged();
        _exportAsGrdCommand.RaiseCanExecuteChanged();
        _exportAsPngCommand.RaiseCanExecuteChanged();
    }

    public Color SampleColorAt(float position)
    {
        var sorted = _stops.OrderBy(s => s.Position).ToArray();
        if (sorted.Length == 0) return Colors.Black;
        if (position <= sorted[0].Position) return sorted[0].Color;
        if (position >= sorted[^1].Position) return sorted[^1].Color;

        for (var i = 0; i < sorted.Length - 1; i++)
        {
            var l = sorted[i];
            var r = sorted[i + 1];
            if (position < l.Position || position > r.Position) continue;
            var span = r.Position - l.Position;
            if (span < 1e-6f) return r.Color;
            var t = (position - l.Position) / span;
            return Color.FromArgb(
                LerpByte(l.Color.A, r.Color.A, t),
                LerpByte(l.Color.R, r.Color.R, t),
                LerpByte(l.Color.G, r.Color.G, t),
                LerpByte(l.Color.B, r.Color.B, t));
        }
        return sorted[^1].Color;
    }

    private void OnStopChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshBrush();
        if (!_serializationSuspended) Commit();
    }

    private void RefreshBrush()
    {
        var stops = new GradientStopCollection(
            _stops.OrderBy(s => s.Position)
                  .Select(s => new GradientStop(s.Color, s.Position)));
        var brush = new LinearGradientBrush(stops, new Point(0, 0), new Point(1, 0));
        brush.Freeze();
        _gradientBrush = brush;
        Raise(nameof(GradientBrush));
    }

    private void Commit()
    {
        var json = GradientStopSerializer.Serialize(
            _stops.OrderBy(s => s.Position).Select(s => s.ToModel()));
        GradientJsonChanged?.Invoke(json);
    }

    private void DetachAll()
    {
        foreach (var s in _stops) s.PropertyChanged -= OnStopChanged;
    }

    private void ExportAsGrd()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = Texts.ExportGrdFilterName,
            DefaultExt = ".grd"
        };
        if (dialog.ShowDialog() != true) return;
        var stops = _stops.OrderBy(s => s.Position).Select(s => s.ToModel()).ToArray();
        GradientExportService.ExportAsGrd(dialog.FileName, "Custom Gradient", stops);
    }

    private void ExportAsPng()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = Texts.ExportPngFilterName,
            DefaultExt = ".png"
        };
        if (dialog.ShowDialog() != true) return;
        var stops = _stops.OrderBy(s => s.Position).Select(s => s.ToModel()).ToArray();
        GradientExportService.ExportAsPng(dialog.FileName, stops);
    }

    private static byte LerpByte(byte a, byte b, float t) => (byte)(a + (b - a) * t);

    private static LinearGradientBrush BuildDefaultBrush()
    {
        var stops = new GradientStopCollection([
            new GradientStop(Colors.Black, 0),
            new GradientStop(Colors.White, 1)]);
        var brush = new LinearGradientBrush(stops, new Point(0, 0), new Point(1, 0));
        brush.Freeze();
        return brush;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DetachAll();
    }

    private void Raise([CallerMemberName] string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
