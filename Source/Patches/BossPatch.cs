using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using SkarrQueen.Behaviours;
using TeamCherry.Localization;
using UnityEngine;
namespace SkarrQueen.Patches;
internal static class BossPatches {

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    private static void ModifyQueen(PlayMakerFSM __instance) {
        
        if (__instance.name == "Hunter Queen Boss" && __instance.FsmName == "Control" &&
            __instance.gameObject.layer == LayerMask.NameToLayer("Enemies")) {
            __instance.gameObject.AddComponent<SkarrQueenKarmelita>();

        }
        
    }

    /// Change boss title.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Language), nameof(Language.Get), typeof(string), typeof(string))]
    private static void ChangeSkarrTitle(string key, string sheetTitle, ref string __result) {
        __result = key switch {
            "HUNTER_QUEEN_SUPER" => Language.CurrentLanguage() switch {
                LanguageCode.EN => "SkarrQueen",
                LanguageCode.KO => "스카르여왕",
                _ => __result
            },
            _ => __result
        };
    }
}