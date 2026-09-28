using System;
using UnityEngine;

namespace Earshot.UI
{
    /// <summary>
    /// White sprites drawn in code and tinted per line: an up-pointing arrowhead (rotated for direction),
    /// a warning triangle with a cut-out "!", and a rounded plate for 9-slicing. Drawn rather than taken
    /// from a font: Valheim's fonts may not have ↖ or ⚠.
    /// </summary>
    internal static class Sprites
    {
        private static Sprite _arrow;
        private static Sprite _warning;
        private static Sprite _plate;

        private static readonly Vector2[] ArrowShape = { new Vector2(16f, 30f), new Vector2(29f, 3f), new Vector2(16f, 10f), new Vector2(3f, 3f) };
        private static readonly Vector2[] TriangleShape = { new Vector2(16f, 30f), new Vector2(31f, 2f), new Vector2(1f, 2f) };

        public static Sprite Arrow => _arrow ?? (_arrow = Make(32, (x, y) => Inside(x, y, ArrowShape), Vector4.zero));
        public static Sprite Warning => _warning ?? (_warning = Make(32, WarningPixel, Vector4.zero));
        public static Sprite Plate => _plate ?? (_plate = Make(32, PlatePixel, new Vector4(12f, 12f, 12f, 12f)));

        private static bool WarningPixel(float x, float y)
        {
            if (!Inside(x, y, TriangleShape))
                return false;
            bool bar = x >= 14.5f && x <= 17.5f && y >= 12f && y <= 23f;
            bool dot = x >= 14.5f && x <= 17.5f && y >= 6f && y <= 9f;
            return !(bar || dot);
        }

        private static bool PlatePixel(float x, float y)
        {
            const float r = 10f;
            float dx = Math.Max(Math.Max(r - x, 0f), x - (32f - r));
            float dy = Math.Max(Math.Max(r - y, 0f), y - (32f - r));
            return dx * dx + dy * dy <= r * r;
        }

        /// <summary>Even-odd point-in-polygon, in texture pixels (y up).</summary>
        private static bool Inside(float x, float y, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        private static Sprite Make(int size, Func<float, float, bool> inside, Vector4 border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                            if (inside(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss))
                                hits++;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (ss * ss)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }
}
