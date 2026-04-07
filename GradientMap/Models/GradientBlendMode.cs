using GradientMap.Localization;
using System.ComponentModel.DataAnnotations;

namespace GradientMap.Models;

public enum GradientBlendMode
{
    [Display(Name = nameof(Texts.BlendModeNormalName),
             Description = nameof(Texts.BlendModeNormalDesc),
             ResourceType = typeof(Texts))]
    Normal = 0,

    [Display(Name = nameof(Texts.BlendModeLuminosityName),
             Description = nameof(Texts.BlendModeLuminosityDesc),
             ResourceType = typeof(Texts))]
    Luminosity = 1,

    [Display(Name = nameof(Texts.BlendModeHueName),
             Description = nameof(Texts.BlendModeHueDesc),
             ResourceType = typeof(Texts))]
    Hue = 2,
}
