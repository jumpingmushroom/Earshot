using System;
using System.Collections.Generic;
using Earshot.Core;
using Earshot.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Earshot.UI
{
    /// <summary>
    /// PLAN.md §2.6. The caption list: bottom centre, stacking up, one row per board line (oldest at the
    /// top, newest at the bottom). Lives under Hud.m_rootObject, so F3 and photo mode hide it with the HUD.
    /// Rows are left-aligned inside a plate sized to the widest row, so the arrows form a column.
    /// </summary>
    internal sealed class CaptionHud : MonoBehaviour
    {
        /// <summary>Matches MaxLines' upper bound (10).</summary>
        private const int PoolSize = 10;
        private const float LineHeight = 30f;
        private const float Pad = 10f;
        private const float Icon = 20f;
        private const float TextX = Icon + 4f + Icon + 8f;
        /// <summary>Default height above the bottom edge: clear of the stamina and Eitr bars. Tuned on the rig.</summary>
        private const float BaseY = 150f;

        private static CaptionHud _instance;

        private sealed class Row
        {
            public RectTransform Rect;
            public Image Warn;
            public Image Arrow;
            public TextMeshProUGUI Text;
            public TextMeshProUGUI Near;
            // What the text was last built from, and its measured width; rebuilt only when one changes.
            public string Source;
            public int Count;
            public string Action;
            public bool Bold;
            public float TextWidth;
            public string NearText;
            public float NearWidth;
        }

        private readonly List<Row> _rows = new List<Row>();
        private RectTransform _root;
        private Image _plate;
        private CanvasGroup _group;

        public static void Ensure()
        {
            if (_instance != null)
                return;
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null || UiUtil.Font == null)
                return;
            RectTransform holder = UiUtil.Rect("EarshotCaptions", hud.m_rootObject.transform);
            holder.anchorMin = new Vector2(0.5f, 0f);
            holder.anchorMax = new Vector2(0.5f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            _instance = holder.gameObject.AddComponent<CaptionHud>();
            _instance.Build(holder);
        }

        private void Build(RectTransform root)
        {
            _root = root;
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _group.alpha = 0f;
            _plate = root.gameObject.AddComponent<Image>();
            _plate.sprite = Sprites.Plate;
            _plate.type = Image.Type.Sliced;
            _plate.raycastTarget = false;
            for (int i = 0; i < PoolSize; i++)
                _rows.Add(MakeRow(i));
        }

        private Row MakeRow(int i)
        {
            RectTransform rt = UiUtil.Rect("Line" + i, _root);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            var row = new Row { Rect = rt };
            row.Warn = MakeIcon(rt, "Warn", Sprites.Warning, Icon / 2f);
            row.Arrow = MakeIcon(rt, "Arrow", Sprites.Arrow, Icon + 4f + Icon / 2f);
            row.Text = UiUtil.Text(rt, "Text", 22f);
            Place(row.Text.rectTransform, TextX);
            row.Near = UiUtil.Text(rt, "Near", 16f);
            Place(row.Near.rectTransform, 0f);
            rt.gameObject.SetActive(false);
            return row;
        }

        private static Image MakeIcon(RectTransform parent, string name, Sprite sprite, float centreX)
        {
            RectTransform rt = UiUtil.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Icon, Icon);
            rt.anchoredPosition = new Vector2(centreX, 0f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static void Place(RectTransform rt, float x)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(10f, LineHeight);
        }

        private void LateUpdate()
        {
            try
            {
                Draw();
            }
            catch (Exception e)
            {
                EarshotPlugin.WarnOnce("CaptionHud", e);
                _group.alpha = 0f;
            }
        }

        private void Draw()
        {
            CaptionBoard board = Runtime.Board;
            if (!PluginConfig.Enabled.Value || board.Lines.Count == 0 || Hud.IsUserHidden())
            {
                _group.alpha = 0f;
                return;
            }

            float now = Time.time;
            Vector3 forward = WorldQuery.CameraForward();
            Vector3 listener = WorldQuery.ListenerPosition();
            int n = Math.Min(board.Lines.Count, _rows.Count);
            float widest = 0f;

            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                if (i >= n)
                {
                    if (row.Rect.gameObject.activeSelf)
                        row.Rect.gameObject.SetActive(false);
                    continue;
                }
                CaptionLine line = board.Lines[i];
                if (!row.Rect.gameObject.activeSelf)
                    row.Rect.gameObject.SetActive(true);

                bool threat = Categories.IsThreat(line.Category);
                if (row.Source != line.Source || row.Count != line.Count ||
                    row.Action != line.Action || row.Bold != threat)
                {
                    string text = Runtime.Tr.Format(LineStyle.SourceWithCount(line.Source, line.Count), line.Action);
                    row.Text.text = text;
                    row.Text.fontStyle = threat ? FontStyles.Bold : FontStyles.Normal;
                    row.TextWidth = row.Text.GetPreferredValues(text).x;
                    row.Text.rectTransform.sizeDelta = new Vector2(row.TextWidth, LineHeight);
                    row.Source = line.Source;
                    row.Count = line.Count;
                    row.Action = line.Action;
                    row.Bold = threat;
                }

                Color c = PluginConfig.ColorFor(line.Category);
                c.a = LineStyle.Opacity(line.Distance, line.MaxDistance, line.OnScreen) * line.Fade(now, board.Settings);
                row.Text.color = c;
                row.Arrow.color = c;
                row.Warn.color = c;
                row.Near.color = c;
                row.Warn.enabled = threat;

                float bearing = Bearing.Degrees(forward.x, forward.z, listener.x, listener.z, line.X, line.Z);
                row.Arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -Bearing.ArrowDegrees(bearing, PluginConfig.SnapArrows.Value));

                float width = TextX + row.TextWidth;

                bool near = LineStyle.ShowNear(line.Category, line.Distance, PluginConfig.NearDistance.Value, line.NearFlag);
                if (row.Near.gameObject.activeSelf != near)
                    row.Near.gameObject.SetActive(near);
                if (near)
                {
                    string nearText = Runtime.Tr.Get("near") ?? "near";
                    if (row.NearText != nearText)
                    {
                        row.Near.text = nearText;
                        row.NearText = nearText;
                        row.NearWidth = row.Near.GetPreferredValues(nearText).x;
                        row.Near.rectTransform.sizeDelta = new Vector2(row.NearWidth, LineHeight);
                    }
                    row.Near.rectTransform.anchoredPosition = new Vector2(width + 8f, 0f);
                    width += 8f + row.NearWidth;
                }

                row.Rect.sizeDelta = new Vector2(width, LineHeight);
                row.Rect.anchoredPosition = new Vector2(Pad, Pad + (n - 1 - i) * LineHeight);
                widest = Math.Max(widest, width);
            }

            _root.sizeDelta = new Vector2(widest + 2f * Pad, n * LineHeight + 2f * Pad);
            _root.anchoredPosition = new Vector2(PluginConfig.OffsetX.Value, BaseY + PluginConfig.OffsetY.Value);
            _root.localScale = Vector3.one * PluginConfig.Scale.Value;
            _plate.color = new Color(0f, 0f, 0f, PluginConfig.BackgroundOpacity.Value);
            _group.alpha = 1f;
        }
    }
}
