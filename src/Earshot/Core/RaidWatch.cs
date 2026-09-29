using Earshot.Core.Model;
using UnityEngine;

namespace Earshot.Core
{
    /// <summary>
    /// PLAN.md §2.5. While a raid is active around the player, keep a "Raid" line pointing at the event
    /// centre. Just the word: vanilla already shows the event's own message in the middle of the screen.
    /// </summary>
    internal static class RaidWatch
    {
        private const float Interval = 0.5f;
        private static float _next;

        public static void Tick(float now)
        {
            if (now < _next)
                return;
            _next = now + Interval;
            if (!PluginConfig.CategoryOn(Category.Raid))
                return;
            RandEventSystem system = RandEventSystem.instance;
            RandomEvent ev = system != null ? system.GetActiveEvent() : null;
            if (ev == null)
                return;

            Vector3 listener = WorldQuery.ListenerPosition();
            var e = new SoundEvent
            {
                PrefabName = "raid:" + ev.m_name,
                X = ev.m_pos.x,
                Z = ev.m_pos.z,
                Distance = Vector3.Distance(listener, ev.m_pos),
                MaxDistance = ev.m_eventRange,
                Loudness = 1f
            };
            var label = new Label { Source = Runtime.Tr.Get("raid") ?? "Raid", Category = Category.Raid, Origin = LabelOrigin.Special };
            // Straight to the board: a refresh every 0.5 s must not flood the console's recent log.
            Runtime.Board.Offer(e, label, now);
        }
    }
}
