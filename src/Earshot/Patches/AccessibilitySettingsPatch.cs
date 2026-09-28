using System;
using System.Collections.Generic;
using Earshot.Core;
using Earshot.Core.Model;
using HarmonyLib;
using TMPro;
using Valheim.SettingsGui;

namespace Earshot.Patches
{
    /// <summary>
    /// PLAN.md §2.7. Iron Gate's "Closed Captions" toggle exists but is hidden (activeSelf = false).
    /// Show it and make it Earshot's master switch. Its state comes from and goes to our config, not
    /// PlatformPrefs("ClosedCaptions"), whose default of 0 would turn captions off for new players.
    /// Its label and its UITooltip's topic/text carry Iron Gate's unlocalised keys, since the game
    /// has no strings for them; replace those with Earshot's own text too.
    /// </summary>
    [HarmonyPatch(typeof(AccessibilitySettings))]
    internal static class AccessibilitySettingsPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(AccessibilitySettings.Initialize))]
        private static void AfterInitialize(AccessibilitySettings __instance)
        {
            try
            {
                var toggle = __instance.m_closedCaptionsToggle;
                if (toggle == null)
                    return;
                toggle.gameObject.SetActive(true);
                toggle.isOn = PluginConfig.Enabled.Value;
                foreach (TMP_Text label in toggle.GetComponentsInChildren<TMP_Text>(true))
                    if (!TextCheck.IsClean(label.text))
                        label.text = Runtime.Tr.Get("settings_toggle") ?? "Closed captions (Earshot)";

                var tooltips = new List<UITooltip>(toggle.GetComponentsInChildren<UITooltip>(true));
                UITooltip parentTip = toggle.GetComponentInParent<UITooltip>();
                if (parentTip != null && !tooltips.Contains(parentTip))
                    tooltips.Add(parentTip);
                foreach (UITooltip tip in tooltips)
                {
                    if (tip == null)
                        continue;
                    if (!TextCheck.IsClean(Localization.instance.Localize(tip.m_topic)))
                        tip.m_topic = Runtime.Tr.Get("settings_toggle") ?? "Closed captions (Earshot)";
                    if (!TextCheck.IsClean(Localization.instance.Localize(tip.m_text)))
                        tip.m_text = Runtime.Tr.Get("settings_toggle_descr") ?? "Show a caption for important sounds, with an arrow pointing where each one came from. More options in the mod settings (F1).";
                }
            }
            catch (Exception e)
            {
                EarshotPlugin.WarnOnce("AccessibilitySettings.Initialize", e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(AccessibilitySettings.OnOkAsync))]
        private static void AfterOk(AccessibilitySettings __instance)
        {
            try
            {
                if (__instance.m_closedCaptionsToggle != null)
                    PluginConfig.Enabled.Value = __instance.m_closedCaptionsToggle.isOn;
            }
            catch (Exception e)
            {
                EarshotPlugin.WarnOnce("AccessibilitySettings.OnOkAsync", e);
            }
        }
    }
}
