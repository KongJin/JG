using UnityEngine;

namespace Features.Garage.Presentation.Theme
{
    public enum ButtonPreset
    {
        Primary,
        Secondary,
        Ghost,
    }

    public static class ButtonStyles
    {
        public static readonly ButtonPreset Primary = ButtonPreset.Primary;
        public static readonly ButtonPreset Secondary = ButtonPreset.Secondary;
        public static readonly ButtonPreset Ghost = ButtonPreset.Ghost;

        public static Color ResolveColor(ButtonPreset preset)
        {
            return preset switch
            {
                ButtonPreset.Primary => ThemeColors.AccentOrange,
                ButtonPreset.Secondary => ThemeColors.BackgroundCard,
                ButtonPreset.Ghost => Color.clear,
                _ => ThemeColors.BackgroundCard,
            };
        }
    }
}
