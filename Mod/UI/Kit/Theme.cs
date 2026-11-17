using UnityEngine;

namespace SFSEnhanced.Mod.UI.Kit
{
    internal static class Theme
    {
        public const string DefaultAccentHex = "#4FA8FF";

        public static readonly Color Accent = Hex(0x4F, 0xA8, 0xFF);
        public static readonly Color TextPrimary = Hex(0xEC, 0xF2, 0xF8);
        public static readonly Color TextSecondary = Hex(0x9C, 0xAD, 0xBF);
        public static readonly Color TextMuted = Hex(0x6F, 0x80, 0x92);
        public static readonly Color Positive = Hex(0x53, 0xD1, 0x8C);
        public static readonly Color Warning = Hex(0xE8, 0xB3, 0x4A);
        public static readonly Color Danger = Hex(0xE5, 0x6B, 0x6B);
        public static readonly Color Surface = Hex(0x0C, 0x13, 0x1B);
        public static readonly Color Row = Hex(0x16, 0x20, 0x2C);

        public const int ContentWidth = 740;
        public const int RowHeight = 44;
        public const int RowStep = 52;
        public const int ContentTop = 200;

        public const float TitleSize = 24f;
        public const float SectionSize = 12f;
        public const float BodySize = 15f;
        public const float SmallSize = 12f;
        public const float StatSize = 18f;

        public static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

        public static Color Parse(string hex)
        {
            Color parsed;
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out parsed)) return parsed;
            return Accent;
        }

        public static Color State(bool ok) => ok ? Positive : TextMuted;
    }
}
