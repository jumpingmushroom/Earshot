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
    /// Its label and its SettingsTooltip's topic/text carry Iron Gate's unlocalised keys, since the
    /// game has no strings for them; replace those with Earshot's own text too. The settings screen
    /// uses its own tooltip class, Valheim.SettingsGui.SettingsTooltip, not the general UITooltip.
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

                var candidates = new List<SettingsTooltip>();
                var listParent = toggle.transform.parent;
                if (listParent != null)
                    candidates.AddRange(listParent.GetComponentsInChildren<SettingsTooltip>(true));
                SettingsTooltip ancestorTip = toggle.GetComponentInParent<SettingsTooltip>();
                if (ancestorTip != null && !candidates.Contains(ancestorTip))
                    candidates.Add(ancestorTip);

                var tooltips = new List<SettingsTooltip>();
                foreach (SettingsTooltip candidate in candidates)
                {
                    if (candidate == null || tooltips.Contains(candidate))
                        continue;
                    bool belongsToToggle = candidate.transform.IsChildOf(toggle.transform)
                        || toggle.transform.IsChildOf(candidate.transform)
                        || candidate.m_selectableOverride == toggle;
                    if (belongsToToggle)
                        tooltips.Add(candidate);
                }

                int fixedCount = 0;
                foreach (SettingsTooltip tip in tooltips)
                {
                    bool changed = false;
                    bool topicClean = TextCheck.IsClean(Localization.instance.Localize(tip.m_topicId ?? ""));
                    bool textClean = TextCheck.IsClean(Localization.instance.Localize(tip.m_textId ?? ""));
                    if (!topicClean || !textClean)
                    {
                        string topic = topicClean ? tip.m_topicId : (Runtime.Tr.Get("settings_toggle") ?? "Closed captions (Earshot)");
                        string text = textClean ? tip.m_textId : (Runtime.Tr.Get("settings_toggle_descr") ?? "Show a caption for important sounds, with an arrow pointing where each one came from. More options in the mod settings (F1).");
                        tip.SetTexts(topic, text);
                        changed = true;
                    }
                    if (!string.IsNullOrEmpty(tip.m_textIdSwitch) && !TextCheck.IsClean(Localization.instance.Localize(tip.m_textIdSwitch)))
                    {
                        tip.m_textIdSwitch = "";
                        changed = true;
                    }
                    if (!string.IsNullOrEmpty(tip.m_textIdPlayStation) && !TextCheck.IsClean(Localization.instance.Localize(tip.m_textIdPlayStation)))
                    {
                        tip.m_textIdPlayStation = "";
                        changed = true;
                    }
                    if (!string.IsNullOrEmpty(tip.m_textIdXbox) && !TextCheck.IsClean(Localization.instance.Localize(tip.m_textIdXbox)))
                    {
                        tip.m_textIdXbox = "";
                        changed = true;
                    }
                    if (changed)
                        fixedCount++;
                }

                if (tooltips.Count == 0)
                    EarshotPlugin.Log.LogInfo("Earshot: no settings tooltip found for the Closed captions toggle");
                else
                    EarshotPlugin.Log.LogInfo($"Earshot: fixed {fixedCount} settings tooltip(s) for the Closed captions toggle");
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
