using GradientMap.Interfaces;
using GradientMap.Services;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace GradientMap.Effect;

public sealed class GradientMapEffectProcessor : IVideoEffectProcessor
{
    private readonly IGraphicsDevicesAndContext _devices;
    private readonly GradientMapEffect _item;
    private readonly IResourceRegistry _registry;
    private readonly IGradientTextureFactory _textureFactory;

    private GradientMapCustomEffect? _effect;
    private ID2D1Image? _effectOutput;
    private ID2D1Bitmap? _gradientBitmap;
    private ID2D1Image? _sourceInput;

    private string _loadedPath = string.Empty;
    private bool _isFirst = true;
    private float _opacity;
    private int _blendMode;
    private int _isHorizontal;

    public ID2D1Image Output =>
        _effectOutput ?? _sourceInput
        ?? throw new InvalidOperationException(
            "SetInput must be called before accessing Output.");

    public GradientMapEffectProcessor(
        IGraphicsDevicesAndContext devices,
        GradientMapEffect item)
    {
        _devices = devices;
        _item = item;
        _registry = GradientMapServices.Container.Resolve<IResourceRegistry>();
        _textureFactory = GradientMapServices.Container.Resolve<IGradientTextureFactory>();

        InitializeEffect();
    }

    private void InitializeEffect()
    {
        var effect = new GradientMapCustomEffect(_devices);
        if (!effect.IsEnabled)
        {
            effect.Dispose();
            return;
        }
        _effect = effect;
        _effectOutput = _registry.Track(effect.Output);
    }

    public void SetInput(ID2D1Image? input)
    {
        _sourceInput = input;
        _effect?.SetSourceInput(input);
    }

    public void ClearInput()
    {
        _sourceInput = null;
        _effect?.ClearAllInputs();
    }

    public DrawDescription Update(EffectDescription effectDescription)
    {
        if (_effect is null)
            return effectDescription.DrawDescription;

        var frame = effectDescription.ItemPosition.Frame;
        var length = effectDescription.ItemDuration.Frame;
        var fps = effectDescription.FPS;

        var opacity = (float)(_item.Opacity.GetValue(frame, length, fps) / 100d);
        var blendMode = (int)_item.BlendMode;
        var isHorizontal = _item.IsHorizontal ? 1 : 0;
        var path = _item.GradientFilePath;

        if (path != _loadedPath)
            RefreshGradientBitmap(path);

        if (_isFirst || _opacity != opacity)
            _effect.Opacity = opacity;

        if (_isFirst || _blendMode != blendMode)
            _effect.BlendMode = blendMode;

        if (_isFirst || _isHorizontal != isHorizontal)
            _effect.IsHorizontal = isHorizontal;

        _isFirst = false;
        _opacity = opacity;
        _blendMode = blendMode;
        _isHorizontal = isHorizontal;

        return effectDescription.DrawDescription;
    }

    private void RefreshGradientBitmap(string path)
    {
        if (_gradientBitmap is not null)
        {
            _effect?.SetGradientInput(null);
            _registry.Untrack(_gradientBitmap);
            _gradientBitmap.Dispose();
            _gradientBitmap = null;
        }

        _loadedPath = path;

        var bitmap = _textureFactory.CreateGradientBitmap(_devices.DeviceContext, path);
        if (bitmap is null) return;

        _gradientBitmap = _registry.Track(bitmap);
        _effect?.SetGradientInput(bitmap);
    }

    public void Dispose()
    {
        _effect?.ClearAllInputs();
        _registry.Dispose();
    }
}
