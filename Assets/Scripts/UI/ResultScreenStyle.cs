using UnityEngine;

namespace TapBrawl.UI
{
    /// <summary>Палитра и размеры экрана Result (согласовано с <see cref="UiModalStyle"/>).</summary>
    public static class ResultScreenStyle
    {
        public static readonly Vector2 ReferenceResolution = MatchHudStyle.ReferenceResolution;
        public const float MatchWidthOrHeight = MatchHudStyle.MatchWidthOrHeight;

        public static readonly Color ScreenBackground = new(0.12f, 0.06f, 0.22f, 1f);
        public static readonly Color PanelColor = UiModalStyle.PanelColor;
        public static readonly Color PrimaryText = UiModalStyle.ProfilePrimaryTextColor;
        public static readonly Color AccentText = UiModalStyle.ProfileAccentTextColor;
        public static readonly Color BadgeText = new(1f, 0.84f, 0.2f, 1f);
        public static readonly Color BadgeChipBackground = new(1f, 0.84f, 0.2f, 0.15f);

        public const int HeadlineFontSize = 72;
        public const int BadgeFontSize = 38;
        public const int DetailsFontSize = 34;
        public const int ScoreAccentFontSize = 46;
        public const int StatusFontSize = 32;

        public const float PanelPadding = 32f;
        public const float SectionSpacing = 20f;
        public const float BadgeChipHeight = 72f;
        public const float HeadlineMinHeight = 96f;
        public const float DetailsMaxHeight = 960f;
        public const float BottomButtonHeight = 96f;
        public const float BottomButtonMargin = 48f;
        public const float PanelHorizontalInset = 0.06f;
        public const float PanelTopAnchor = 0.82f;
        public const float PanelBottomAnchor = 0.18f;
    }
}
