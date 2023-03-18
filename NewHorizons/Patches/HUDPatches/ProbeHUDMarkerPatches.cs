using HarmonyLib;
using NewHorizons.Handlers;

namespace NewHorizons.Patches.HUDPatches
{
    [HarmonyPatch(typeof(ProbeHUDMarker))]
    public static class ProbeHUDMarkerPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProbeHUDMarker.Awake))]
        public static void ProbeHUDMarker_Awake(ProbeHUDMarker __instance)
        {
            GlobalMessenger.AddListener("ProbeEnterCloakField", __instance.RefreshOwnVisibility);
            GlobalMessenger.AddListener("ProbeExitCloakField", __instance.RefreshOwnVisibility);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProbeHUDMarker.OnDestroy))]
        public static void ProbeHUDMarker_OnDestroy(ProbeHUDMarker __instance)
        {
            GlobalMessenger.RemoveListener("ProbeEnterCloakField", __instance.RefreshOwnVisibility);
            GlobalMessenger.RemoveListener("ProbeExitCloakField", __instance.RefreshOwnVisibility);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProbeHUDMarker.RefreshOwnVisibility))]
        public static bool ProbeHUDMarker_RefreshOwnVisibility(ProbeHUDMarker __instance)
        {
            bool insideEYE = Locator.GetEyeStateManager() != null && Locator.GetEyeStateManager().IsInsideTheEye();
            bool insideQM = __instance._quantumMoon != null && (__instance._quantumMoon.IsPlayerInside() || __instance._quantumMoon.IsProbeInside());
            bool insideRW = Locator.GetRingWorldController() != null && Locator.GetRingWorldController().isPlayerInside == Locator.GetRingWorldController().isProbeInside;
            bool insideIP = Locator.GetCloakFieldController() != null && Locator.GetCloakFieldController().isPlayerInsideCloak == Locator.GetCloakFieldController().isProbeInsideCloak;
            bool insideCloak = Components.CloakSectorController.isPlayerInside == Components.CloakSectorController.isProbeInside;
            bool sameInterference = InterferenceHandler.IsPlayerSameAsProbe();
            bool isActive = __instance.gameObject.activeInHierarchy || __instance._isTLCDuplicate;

            __instance._isVisible = isActive && !insideEYE && !insideQM && !__instance._translatorEquipped && !__instance._inConversation && __instance._launched && (__instance._isWearingHelmet || __instance._atFlightConsole) && insideRW && insideIP && insideCloak && sameInterference;

            if (__instance._canvasMarker != null) __instance._canvasMarker.SetVisibility(__instance._isVisible);

            return false;
        }
    }
}
