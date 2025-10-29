using BepInEx;
using HarmonyLib;
using SkarrQueen.Patches;
using UnityEngine;

namespace SkarrQueen;

/// <summary>
/// The main plugin class.
/// </summary>
[BepInPlugin("SkarrQueen", "SkarrQueen", "1.0.0")]
public class Plugin : BaseUnityPlugin {
    private static Harmony _harmony = null!;

    private void Awake() {
        Debug.Log($"Plugin SkarrQueen (1.0.0) has loaded!");

        _harmony = new Harmony("1.0.0");
        #if DEBUG
        _harmony.PatchAll(typeof(DebugPatches));
        #endif

        MainPatches.Initialize();
        _harmony.PatchAll(typeof(MainPatches));

       
    }

    private void OnDestroy() {
        _harmony.UnpatchSelf();
        AssetManager.UnloadAll();
    }
}