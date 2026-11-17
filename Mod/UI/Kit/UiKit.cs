using System;
using SFS.UI.ModGUI;
using TMPro;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.Kit
{
    internal static class UiKit
    {
        public static Label Label(Transform parent, int width, int height, int x, int y, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            Label label = Builder.CreateLabel(parent, width, height, x, y, text);
            TextMeshProUGUI textMesh = label.gameObject.GetComponent<TextMeshProUGUI>();
            if (textMesh != null) textMesh.richText = false;
            label.AutoFontResize = false;
            label.FontSize = size;
            label.Color = color;
            label.TextAlignment = align;
            return label;
        }

        public static Label Heading(Transform parent, string text, int y) =>
            Label(parent, Theme.ContentWidth, 34, 0, y, text, Theme.TitleSize, Theme.TextPrimary);

        public static Label Section(Transform parent, string text, int y) =>
            Label(parent, Theme.ContentWidth, 18, 0, y, text, Theme.SectionSize, Theme.TextSecondary);

        public static Label Body(Transform parent, string text, int y) =>
            Label(parent, Theme.ContentWidth, 24, 0, y, text, Theme.BodySize, Theme.TextPrimary);

        public static Label Small(Transform parent, int width, int x, int y, string text, Color color) =>
            Label(parent, width, 20, x, y, text, Theme.SmallSize, color);

        public static Button Primary(Transform parent, int width, int height, int x, int y, string text, Action onClick)
        {
            Button button = Builder.CreateButton(parent, width, height, x, y, onClick, text);
            button.TextColor = Theme.TextPrimary;
            return button;
        }

        public static Button Secondary(Transform parent, int width, int height, int x, int y, string text, Action onClick)
        {
            Button button = Builder.CreateButton(parent, width, height, x, y, onClick, text);
            button.TextColor = Theme.TextSecondary;
            return button;
        }

        public static Button Danger(Transform parent, int width, int height, int x, int y, string text, Action onClick)
        {
            Button button = Builder.CreateButton(parent, width, height, x, y, onClick, text);
            button.TextColor = Theme.Danger;
            return button;
        }

        public static Box Card(Transform parent, int width, int height, int x, int y, Color color, float opacity = 0.55f)
        {
            Box box = Builder.CreateBox(parent, width, height, x, y, 0.2f);
            box.Color = color;
            box.Opacity = opacity;
            return box;
        }

        public static void Divider(Transform parent, int width, int y) => Builder.CreateSeparator(parent, width, 0, y);

        public static void ListRow(Transform parent, int y, string title, string subtitle, string action, Action onAction)
        {
            Card(parent, Theme.ContentWidth, Theme.RowHeight, 0, y, Theme.Row, 0.5f);
            Label(parent, 430, 20, -155, y + 8, title, Theme.BodySize, Theme.TextPrimary);
            if (!string.IsNullOrEmpty(subtitle))
                Label(parent, 430, 18, -155, y - 10, subtitle, Theme.SmallSize, Theme.TextSecondary);
            if (!string.IsNullOrEmpty(action) && onAction != null)
                Secondary(parent, 110, 30, 288, y, action, onAction);
        }

        public static Label Stat(Transform parent, int x, int y, string caption, string value)
        {
            Card(parent, 236, 76, x, y, Theme.Surface, 0.6f);
            Label(parent, 212, 16, x, y + 18, caption.ToUpperInvariant(), 11f, Theme.TextMuted);
            return Label(parent, 212, 26, x, y - 10, value, Theme.StatSize, Theme.TextPrimary);
        }

        public static Transform Region(Transform parent, int x, int y, int height)
        {
            Container container = Builder.CreateContainer(parent, x, y);
            container.Size = new Vector2(Theme.ContentWidth, height);
            return container.rectTransform;
        }

        public static void Clear(Transform region)
        {
            if (region == null) return;
            for (int i = region.childCount - 1; i >= 0; i--)
            {
                GameObject child = region.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        public static void EmptyState(Transform parent, int y, string title, string body)
        {
            Card(parent, Theme.ContentWidth, 92, 0, y, Theme.Surface, 0.45f);
            Label(parent, Theme.ContentWidth - 48, 22, 0, y + 14, title, 16f, Theme.TextSecondary, TextAlignmentOptions.Center);
            Label(parent, Theme.ContentWidth - 48, 40, 0, y - 16, body, Theme.SmallSize, Theme.TextMuted, TextAlignmentOptions.Center);
        }

        public static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "-";
            return id.Length <= 8 ? id : id.Substring(0, 8);
        }

        public static string FriendlyDate(DateTime utc)
        {
            if (utc.Year < 2000) return "unknown";
            return utc.ToString("yyyy-MM-dd");
        }
    }
}
