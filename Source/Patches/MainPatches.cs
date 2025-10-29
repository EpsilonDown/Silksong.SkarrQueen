using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using SkarrQueen.Behaviours;

namespace SkarrQueen.Patches;
internal static class MainPatches {
    private static Harmony _harmony = null!;
    internal static void Initialize() {
        _harmony = new Harmony(nameof(MainPatches));
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
    private static void InitAssetManager(GameManager __instance)
    {
        __instance.StartCoroutine(AssetManager.Init());
    }


    /// <summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.SetLoadedGameData), typeof(SaveGameData), typeof(int))]
    private static void CheckPatchSkarr(GameManager __instance) {
        
        __instance.GetSaveStatsForSlot(PlayerData.instance.profileID, (saveStats, _) => {
            if (saveStats.IsAct3) {
                _harmony.PatchAll(typeof(BossPatches));
                __instance.gameObject.AddComponent<TrapLoader>();
            }
        });
    }
    
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnToMainMenu))]
    private static IEnumerator Unpatch(IEnumerator result) {
        while (result.MoveNext()) {
            yield return result.Current;
        }
        
        _harmony.UnpatchSelf();
    }
}