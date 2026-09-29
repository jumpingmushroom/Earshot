using Earshot.Core.Model;
using UnityEngine;

namespace Earshot.Core
{
    /// <summary>PLAN.md §2.9: `earshot demo` offers a fixed set of sample captions to the board so they stay
    /// on screen for screenshots and settings preview. Uses Board.Offer directly (not Runtime.Offer), so the
    /// samples don't pollute the `earshot` recent log.</summary>
    internal static class DemoCaptions
    {
        private struct Sample
        {
            public readonly string SourceKey;
            public readonly string ActionKey;
            public readonly Category Category;
            public readonly float Angle;
            public readonly float Distance;
            public readonly float MaxDistance;
            public readonly int[] SourceIds;

            public Sample(string sourceKey, string actionKey, Category category, float angle, float distance, float maxDistance, params int[] sourceIds)
            {
                SourceKey = sourceKey;
                ActionKey = actionKey;
                Category = category;
                Angle = angle;
                Distance = distance;
                MaxDistance = maxDistance;
                SourceIds = sourceIds;
            }
        }

        private const float TickInterval = 0.5f;

        private static readonly Sample[] Samples =
        {
            new Sample("$enemy_eikthyr", "alerted", Category.Boss, 0f, 60f, 100f, 9001),
            new Sample("$enemy_greydwarf", "attacking", Category.Enemy, -45f, 6f, 50f, 9002, 9003, 9004),
            new Sample("$enemy_troll", "stomping", Category.Enemy, 90f, 40f, 100f, 9005),
            new Sample("$enemy_deer", "alerted", Category.Wildlife, 180f, 25f, 50f, 9006),
            new Sample("$piece_smelter", "done", Category.World, -100f, 15f, 50f, 9007),
        };

        private static float _until;
        private static float _next;
        private static float _cameraYaw;

        public static void Start(float seconds)
        {
            _until = Time.time + seconds;
            _next = 0f;
            Camera cam = Utils.GetMainCamera();
            _cameraYaw = cam != null ? cam.transform.eulerAngles.y : 0f;
        }

        public static void Tick(float now)
        {
            if (now >= _until || now < _next)
                return;
            _next = now + TickInterval;

            Player me = Player.m_localPlayer;
            if (me == null)
                return;

            foreach (Sample sample in Samples)
            {
                Vector3 pos = me.transform.position + Quaternion.Euler(0f, _cameraYaw + sample.Angle, 0f) * Vector3.forward * sample.Distance;
                var label = new Label
                {
                    Source = Localization.instance.Localize(sample.SourceKey),
                    Action = Runtime.Tr.Get(sample.ActionKey),
                    Category = sample.Category
                };
                float distance = Vector3.Distance(WorldQuery.ListenerPosition(), pos);
                foreach (int sourceId in sample.SourceIds)
                {
                    var e = new SoundEvent
                    {
                        X = pos.x,
                        Z = pos.z,
                        Distance = distance,
                        MaxDistance = sample.MaxDistance,
                        Loudness = 1f,
                        OnScreen = false,
                        SourceId = sourceId
                    };
                    Runtime.Board.Offer(e, label, now);
                }
            }
        }
    }
}
