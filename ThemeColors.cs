using System.Drawing;

namespace frm_winget_upgrade
{
    public static class ThemeColors
    {
        public static readonly Color DeepCharcoal     = Color.FromArgb(30, 30, 30);
        public static readonly Color DarkCharcoal     = Color.FromArgb(37, 37, 37);
        public static readonly Color DarkerBackground = Color.FromArgb(26, 26, 26);
        public static readonly Color GridRowAlt        = Color.FromArgb(36, 36, 36);

        public static readonly Color ElectricBlue     = Color.FromArgb(0, 217, 255);
        public static readonly Color ElectricBlueDark = Color.FromArgb(0, 168, 204);
        public static readonly Color NeonGreen         = Color.FromArgb(57, 255, 20);
        public static readonly Color WarningOrange     = Color.FromArgb(255, 149, 0);
        public static readonly Color ErrorRed          = Color.FromArgb(255, 58, 58);

        public static readonly Color PrimaryText   = Color.White;
        public static readonly Color SecondaryText = Color.FromArgb(160, 160, 160);

        public static readonly Color HoverBlue    = Color.FromArgb(45, 45, 45);
        public static readonly Color PressedBlue  = Color.FromArgb(0, 168, 204);
        public static readonly Color BorderGray   = Color.FromArgb(45, 45, 45);
        public static readonly Color DisabledGray = Color.FromArgb(96, 96, 96);

        public static readonly Color SuccessGreen  = NeonGreen;
        public static readonly Color InfoBlue      = ElectricBlue;
        public static readonly Color CautionOrange = WarningOrange;
        public static readonly Color DangerRed     = ErrorRed;
        public static readonly Color TerminalGreen = Color.FromArgb(57, 255, 20);
    }

    public static class ThemeConstants
    {
        public const int BaseUnit  = 8;
        public const int Spacing8  = 8;
        public const int Spacing12 = 12;
        public const int Spacing16 = 16;
        public const int Spacing24 = 24;

        public const int CornerSmall  = 4;
        public const int CornerMedium = 6;
        public const int CornerLarge  = 8;

        public const int SidebarWidthExpanded  = 200;
        public const int SidebarWidthCollapsed = 52;

        public const int TopBarHeight = 40;
        public const int HeaderHeight = 56;

        public const int DataGridRowHeight    = 30;
        public const int DataGridHeaderHeight = 34;

        public const int ButtonHeight    = 30;
        public const int SearchBoxHeight = 34;

        public const int DefaultWindowWidth  = 1100;
        public const int DefaultWindowHeight = 680;
        public const int MinimumWindowWidth  = 900;
        public const int MinimumWindowHeight = 520;

        public const int ResponsiveBreakpointSmall = 1000;
        public const int ResponsiveBreakpointLarge = 1400;
    }
}
