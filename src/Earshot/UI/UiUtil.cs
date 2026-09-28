using TMPro;
using UnityEngine;

namespace Earshot.UI
{
    internal static class UiUtil
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>The HUD's own font, so captions match the game and cover its languages.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                Hud hud = Hud.instance;
                return hud != null && hud.m_healthText != null ? hud.m_healthText.font : null;
            }
        }

        private static Material _outlined;
        private static TMP_FontAsset _outlinedFor;

        /// <summary>One shared outlined copy of the font's material, so text reads against snow and sky.</summary>
        private static Material Outlined(TMP_FontAsset font)
        {
            if (_outlined != null && _outlinedFor == font)
                return _outlined;
            _outlined = new Material(font.material);
            _outlined.EnableKeyword(ShaderUtilities.Keyword_Outline);
            _outlined.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
            _outlined.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 1f));
            _outlined.hideFlags = HideFlags.HideAndDontSave;
            _outlinedFor = font;
            return _outlined;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, float size)
        {
            RectTransform rt = Rect(name, parent);
            // Added while inactive so TMP's Awake runs after the font is set; otherwise it looks
            // for its default LiberationSans (not shipped with the game) and logs a warning.
            rt.gameObject.SetActive(false);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = Font;
            if (font != null)
            {
                t.font = font;
                t.fontSharedMaterial = Outlined(font);
            }
            rt.gameObject.SetActive(true);
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
