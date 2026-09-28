using System;
using Earshot.Core.Model;
using UnityEngine;

namespace Earshot.Core
{
    /// <summary>The Unity-side facts the model needs: where the ears are, how loud a source is there, who is nearby.</summary>
    internal static class WorldQuery
    {
        public static string CleanName(string name)
        {
            if (name == null)
                return "";
            int i = name.IndexOf("(Clone)", StringComparison.Ordinal);
            return (i >= 0 ? name.Substring(0, i) : name).Trim();
        }

        public static Vector3 ListenerPosition()
        {
            AudioMan am = AudioMan.instance;
            AudioListener listener = am != null ? am.GetActiveAudioListener() : null;
            if (listener != null)
                return listener.transform.position;
            Camera cam = Utils.GetMainCamera();
            return cam != null ? cam.transform.position : Vector3.zero;
        }

        public static Vector3 CameraForward()
        {
            Camera cam = Utils.GetMainCamera();
            return cam != null ? cam.transform.forward : Vector3.forward;
        }

        public static float Loudness(AudioSource a, float baseVolume, float distance)
        {
            Func<float, float> curve = null;
            if (a.rolloffMode == AudioRolloffMode.Custom)
            {
                AnimationCurve c = a.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
                if (c != null)
                    curve = x => c.Evaluate(x);
            }
            float attenuation = Audibility.Attenuation((Rolloff)(int)a.rolloffMode, distance, a.minDistance, a.maxDistance, curve);
            return Audibility.Loudness(baseVolume, attenuation);
        }

        /// <summary>The nearest character (players included) within radius of pos, or null.</summary>
        public static Character NearestCharacter(Vector3 pos, float radius)
        {
            Character best = null;
            float bestSq = radius * radius;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null)
                    continue;
                float d = (c.transform.position - pos).sqrMagnitude;
                if (d <= bestSq)
                {
                    bestSq = d;
                    best = c;
                }
            }
            return best;
        }

        public static bool AnyPlayerWithin(Vector3 pos, float radius)
        {
            float r2 = radius * radius;
            foreach (Player p in Player.GetAllPlayers())
                if (p != null && (p.transform.position - pos).sqrMagnitude <= r2)
                    return true;
            return false;
        }

        /// <summary>In front of the camera, inside the viewport, and no further than `within` metres along the view.</summary>
        public static bool OnScreen(Vector3 pos, float within)
        {
            Camera cam = Utils.GetMainCamera();
            if (cam == null)
                return false;
            Vector3 v = cam.WorldToViewportPoint(pos);
            return v.z > 0f && v.z <= within && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
        }
    }
}
