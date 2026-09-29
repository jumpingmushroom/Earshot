namespace Earshot.Core
{
    /// <summary>
    /// PLAN.md §2.7. In 1.0.16 vanilla's ClosedCaptions.RegisterCaption is empty. If a patch ever fills it
    /// in, its panel gains child lines; Earshot then hides that panel so captions don't show twice.
    /// </summary>
    internal static class VanillaCaptions
    {
        private static float _next;
        /// <summary>The panel already hidden; a new instance (after logging out and back in) is checked again.</summary>
        private static ClosedCaptions _hidden;

        public static void Tick(float now)
        {
            if (now < _next || !PluginConfig.Enabled.Value)
                return;
            _next = now + 1f;
            ClosedCaptions vanilla = ClosedCaptions.Instance;
            if (vanilla == null || vanilla == _hidden || !vanilla.gameObject.activeSelf || vanilla.transform.childCount == 0)
                return;
            vanilla.gameObject.SetActive(false);
            _hidden = vanilla;
            EarshotPlugin.Log.LogInfo("Vanilla closed captions detected; hiding them in favour of Earshot.");
        }
    }
}
