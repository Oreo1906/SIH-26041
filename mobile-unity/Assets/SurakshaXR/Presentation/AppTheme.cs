using UnityEngine;
using UnityEngine.UIElements;

namespace SurakshaXR.Presentation
{
    // Shared semantic colours for existing portrait pages and camera overlays.
    public static class AppTheme
    {
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var color); return color; }
        public static readonly Color Background = Hex("#F4F6F8"), Surface = Color.white,
            Ink = Hex("#152A35"), Muted = Hex("#52636D"), Primary = Hex("#087F74"), OnPrimary = Color.white,
            Border = Hex("#D5DFE3"), Amber = Hex("#81530C"), AmberSurface = Hex("#FFF3DB"),
            DarkBackground = Hex("#101E26"), DarkSurface = Hex("#162A35"), DarkInk = Hex("#F5F8FA"),
            DarkMuted = Hex("#BACBD3"), DarkBorder = Hex("#455D69");
        public static void Round(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = radius;
        }
        public static void Outline(VisualElement element, Color color, float width = 1)
        {
            element.style.borderTopWidth = element.style.borderBottomWidth = element.style.borderLeftWidth = element.style.borderRightWidth = width;
            element.style.borderTopColor = element.style.borderBottomColor = element.style.borderLeftColor = element.style.borderRightColor = color;
        }
        public static void ApplyCard(VisualElement element, bool dark = false)
        {
            element.style.backgroundColor = dark ? DarkSurface : Surface;
            element.style.color = dark ? DarkInk : Ink; Round(element, 20); Outline(element, dark ? DarkBorder : Border);
        }
        public static void ApplyButton(Button button, bool primary = true, bool dark = false, float minHeight = 72)
        {
            button.style.minHeight = minHeight; button.style.color = primary ? OnPrimary : dark ? DarkInk : Ink;
            button.style.backgroundColor = primary ? Primary : dark ? DarkSurface : Surface;
            button.style.unityTextAlign = TextAnchor.MiddleCenter; button.style.backgroundImage = StyleKeyword.None;
            Round(button, 14); Outline(button, primary ? Primary : dark ? DarkBorder : Border);
            button.RegisterCallback<PointerDownEvent>(_ => { if (button.enabledInHierarchy) button.style.opacity = .72f; });
            button.RegisterCallback<PointerUpEvent>(_ => button.style.opacity = 1);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.opacity = 1);
        }
    }
}
