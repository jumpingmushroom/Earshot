using System;

namespace Earshot.Core.Model
{
    /// <summary>Same values as UnityEngine.AudioRolloffMode, so the adapter can cast.</summary>
    public enum Rolloff
    {
        Logarithmic = 0,
        Linear = 1,
        Custom = 2
    }

    /// <summary>
    /// How loud a sound is at the listener, from its AudioSource's rolloff settings. Beyond
    /// maxDistance a sound counts as silent (Unity keeps a logarithmic tail there, but for captions
    /// "out of range" should mean "not heard"). The player's volume sliders are deliberately not
    /// part of this: a deaf player may run with sound at 0.
    /// </summary>
    public static class Audibility
    {
        public static float Attenuation(Rolloff mode, float distance, float minDistance, float maxDistance, Func<float, float> customCurve)
        {
            if (maxDistance <= 0f || distance > maxDistance)
                return 0f;
            if (distance < 0f)
                distance = 0f;
            switch (mode)
            {
                case Rolloff.Linear:
                    if (distance <= minDistance || maxDistance <= minDistance)
                        return 1f;
                    return Clamp01(1f - (distance - minDistance) / (maxDistance - minDistance));
                case Rolloff.Custom:
                    return customCurve == null ? 1f : Clamp01(customCurve(distance / maxDistance));
                default:
                    if (distance <= minDistance)
                        return 1f;
                    return Clamp01(minDistance / distance);
            }
        }

        public static float Loudness(float baseVolume, float attenuation)
        {
            return Clamp01(baseVolume) * Clamp01(attenuation);
        }

        private static float Clamp01(float v)
        {
            return v < 0f ? 0f : v > 1f ? 1f : v;
        }
    }
}
