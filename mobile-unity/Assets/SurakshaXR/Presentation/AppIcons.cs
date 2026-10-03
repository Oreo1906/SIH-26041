using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurakshaXR.Presentation
{
    public static class AppIcons
    {
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static Image Create(string name, float size = 28, Color? tint = null)
        {
            if (!textures.TryGetValue(name, out var texture)) {
                texture = Resources.Load<Texture2D>("Icons/" + name); textures[name] = texture;
            }
            var image = new Image { image = texture, tintColor = tint ?? AppTheme.Primary, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.width = image.style.height = size; image.style.flexShrink = 0; return image;
        }
    }
}
