using UnityEngine;

namespace TapBrawl.UI
{
    /// <summary>Палитра и размеры HUD сцены Match (согласовано с <see cref="UiModalStyle"/>).</summary>
    public static class MatchHudStyle
    {
        public static readonly Color ScreenBackground = new(0.078f, 0.078f, 0.094f, 1f);
        public static readonly Color CameraBackground = ScreenBackground;

        public const float TopBarHeight = 112f;
        public const float NoticeBarHeight = 48f;
        public const float SkillBarHeight = 280f;
        public const float SkillSlotSize = 160f;
        public const float SkillSlotGap = 48f;
        public const float SkillSlotVerticalOffset = -44f;
        public static float SkillSlotCenterStep => SkillSlotSize + SkillSlotGap;
        public const float EnergyBarHeight = 20f;
        public const float PlayAreaBottomInset = SkillBarHeight + 16f;
        public const float PlayAreaTopInset = TopBarHeight + NoticeBarHeight + 8f;

        public const int ScoreFontSize = 36;
        public const int TimerFontSize = 52;
        public const int SecondaryFontSize = 28;
        public const int NoticeFontSize = 24;
        public const int SkillStatusFontSize = 22;
        public const int EnergyFontSize = 22;

        public static readonly Color PrimaryText = UiModalStyle.ProfilePrimaryTextColor;
        public static readonly Color AccentText = UiModalStyle.ProfileAccentTextColor;
        public static readonly Color NoticeText = new(1f, 0.88f, 0.25f, 1f);
        public static readonly Color SkillBarPanel = UiModalStyle.SkillsSectionColor;
        public static readonly Color PinPickerPanel = UiModalStyle.PanelColor;
        public static readonly Color PinPickerBackdrop = UiModalStyle.BackdropColor;

        public static readonly Vector2 ReferenceResolution = new(1080f, 1920f);
        public const float MatchWidthOrHeight = 0.5f;
    }
}
