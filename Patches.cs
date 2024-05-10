using System;
using HarmonyLib;
using SFS.UI;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Local
// ReSharper disable InconsistentNaming

namespace UITools
{
    static class GameHook
    {
        internal static readonly GameObject_Local settingsMenu = new() { Value = null };
        internal static event Action OnSettingsMenuClosed;
        internal static event Action OnSettingsMenuOpened;

        [HarmonyPatch(typeof(BasicMenu), nameof(BasicMenu.OnOpen))]
        class BaseMenu_Open
        {
            [HarmonyPrefix]
            static void Prefix(BasicMenu __instance)
            {
                if (settingsMenu.Value is null && __instance.gameObject.name == "Settings Menu")
                    settingsMenu.Value = __instance.gameObject;
            }

            [HarmonyPostfix]
            static void Postfix(BasicMenu __instance)
            {
                if (__instance.gameObject.name == "Settings Menu")
                    OnSettingsMenuOpened?.Invoke();
            }
        }

        [HarmonyPatch(typeof(BasicMenu), nameof(BasicMenu.OnClose))]
        class BaseMenu_Close
        {
            [HarmonyPostfix]
            static void Postfix(BasicMenu __instance)
            {
                if (__instance.gameObject.name == "Settings Menu")
                    OnSettingsMenuClosed?.Invoke();
            }
        }
    }
}