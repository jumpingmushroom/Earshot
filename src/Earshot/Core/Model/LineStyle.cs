using System;

namespace Earshot.Core.Model
{
    public static class LineStyle
    {
        public const float FarOpacity = 0.55f;
        public const float OnScreenOpacity = 0.5f;

        /// <summary>1.0 up close, easing linearly to FarOpacity at the sound's range; capped when its source is on screen.</summary>
        public static float Opacity(float distance, float maxDistance, bool onScreen)
        {
            float k = maxDistance > 0f ? Math.Min(1f, Math.Max(0f, distance / maxDistance)) : 0f;
            float o = 1f - (1f - FarOpacity) * k;
            return onScreen ? Math.Min(o, OnScreenOpacity) : o;
        }

        public static bool ShowNear(Category category, float distance, float nearDistance, bool nearFlag)
        {
            return (nearFlag || Categories.IsThreat(category)) && distance <= nearDistance;
        }

        /// <summary>"Greydwarf ×3": the count sits on the source, so no language needs a plural form.</summary>
        public static string SourceWithCount(string source, int count)
        {
            return count >= 2 ? source + " ×" + count : source;
        }
    }
}
