using System;
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
