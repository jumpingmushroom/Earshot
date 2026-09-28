using System;
using Earshot.Core;
using HarmonyLib;

namespace Earshot.Patches
{
    /// <summary>ZSFX.Play is the one choke point for every sound effect (PLAN.md §1.1). Postfix: the sound has started (or was refused).</summary>
    [HarmonyPatch(typeof(ZSFX), nameof(ZSFX.Play))]
    internal static class ZsfxPatch
    {
        private static void Postfix(ZSFX __instance)
        {
            try
            {
                SoundCapture.OnPlay(__instance);
            }
            catch (Exception e)
            {
                EarshotPlugin.WarnOnce("ZSFX.Play", e);
            }
        }
    }
}
