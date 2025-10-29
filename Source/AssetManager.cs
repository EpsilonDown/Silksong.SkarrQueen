using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SkarrQueen
{

    /// <summary>
    /// Manages all loaded assets in the mod.
    /// </summary>
    internal static class AssetManager
    {
        private static readonly Dictionary<string, string[]> ScenePrefabs = new()
        {
            ["Bone_East_18b"] = ["Boss Scene"],
        };

        private static readonly Dictionary<string, string[]> BundleAssets = new()
        {
        };
        internal static string GetAssetRoot(this string assetPath) =>
        assetPath.Split("/").Last().Replace(".asset", "").Replace(".prefab", "").Replace(".wav", "");

        private static List<AssetBundle> _manuallyLoadedBundles = new();

        private static readonly Dictionary<Type, Dictionary<string, Object>> Assets = new();

        /// <summary>
        /// Manually load asset bundles.
        /// </summary>
        internal static IEnumerator Init()
        {
            yield return LoadScenePrefabs();
        }

        /// <summary>
        /// Load all prefabs located in scenes.
        /// </summary>
        private static IEnumerator LoadScenePrefabs()
        {
            AudioManager.BlockAudioChange = true;
            foreach (var (sceneName, prefabNames) in ScenePrefabs)
            {
                string loadScenePath = $"Scenes/{sceneName}";

                var loadSceneHandle = Addressables.LoadSceneAsync(loadScenePath, LoadSceneMode.Additive);
                yield return loadSceneHandle;

                if (loadSceneHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var sceneInstance = loadSceneHandle.Result;
                    var scene = sceneInstance.Scene;
                    foreach (var rootObj in scene.GetRootGameObjects())
                    {
                        foreach (string prefabName in prefabNames)
                        {
                            GameObject? prefab = rootObj.GetComponentsInChildren<Transform>(true)
                                .FirstOrDefault(obj => obj.name == prefabName)?.gameObject;
                            if (prefab)
                            {
                                prefab.SetActive(false);
                                var prefabCopy = Object.Instantiate(prefab);
                                prefabCopy.name = prefabName;
                                Object.DontDestroyOnLoad(prefabCopy);
                                TryAdd(prefabCopy);
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log(loadSceneHandle.OperationException);
                }

                var unloadSceneHandle =
                    Addressables.UnloadSceneAsync(loadSceneHandle);
                yield return unloadSceneHandle;
            }

            AudioManager.BlockAudioChange = false;
        }

        /// <summary>
        /// Load all required assets located within loaded<see cref="AssetBundle">asset bundles</see>.
        /// </summary>
        internal static IEnumerator LoadBundleAssets()
        {
            string platformFolder = Application.platform switch
            {
                RuntimePlatform.WindowsPlayer => "StandaloneWindows64",
                RuntimePlatform.OSXPlayer => "StandaloneOSX",
                RuntimePlatform.LinuxPlayer => "StandaloneLinux64",
                _ => ""
            };
            string bundlesPath = Path.Combine(Addressables.RuntimePath, platformFolder);
            foreach (var (bundleName, assetNames) in BundleAssets)
            {
                bool bundleAlreadyLoaded = false;
                foreach (var loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
                {
                    foreach (string assetPath in loadedBundle.GetAllAssetNames())
                    {
                        foreach (string assetName in assetNames)
                        {
                            if (assetPath.GetAssetRoot() == assetName)
                            {
                                bundleAlreadyLoaded = true;
                                var loadAssetRequest = loadedBundle.LoadAssetAsync(assetPath);
                                yield return loadAssetRequest;

                                var loadedAsset = loadAssetRequest.asset;
                                TryAdd(loadedAsset);
                                Debug.Log(loadedAsset.name);

                                break;
                            }
                        }

                        if (bundleAlreadyLoaded)
                        {
                            break;
                        }
                    }

                    if (bundleAlreadyLoaded)
                    {
                        break;
                    }
                }

                if (bundleAlreadyLoaded)
                {
                    Debug.Log($"Bundle {bundleName} already loaded!");
                    continue;
                }

                string bundlePath = Path.Combine(bundlesPath, $"{bundleName}.bundle");
                var bundleLoadRequest = AssetBundle.LoadFromFileAsync(bundlePath);
                yield return bundleLoadRequest;

                AssetBundle bundle = bundleLoadRequest.assetBundle;
                _manuallyLoadedBundles.Add(bundle);
                foreach (string assetPath in bundle.GetAllAssetNames())
                {
                    foreach (string assetName in assetNames)
                    {
                        if (assetPath.GetAssetRoot() == assetName)
                        {
                            var assetLoadRequest = bundle.LoadAssetAsync(assetPath);
                            yield return assetLoadRequest;

                            var loadedAsset = assetLoadRequest.asset;
                            TryAdd(loadedAsset);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Whether an asset with a specified name is already loaded.
        /// </summary>
        /// <param name="assetName">The name of the asset to check.</param>
        /// <returns></returns>
        private static bool Has(string assetName)
        {
            foreach (var (_, subDict) in Assets)
            {
                foreach (var (name, existingAsset) in subDict)
                {
                    if (assetName == name && existingAsset)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Try adding a new asset.
        /// </summary>
        /// <param name="asset">The asset to add.</param>
        private static void TryAdd<T>(T asset) where T : Object
        {
            var assetName = asset.name;
            if (Has(assetName))
            {
                Debug.Log($"Asset \"{assetName}\" has already been loaded!");
                return;
            }

            var assetType = asset.GetType();
            if (Assets.ContainsKey(assetType))
            {
                var existingAssetSubDict = Assets[assetType];
                if (existingAssetSubDict != null)
                {
                    if (existingAssetSubDict.ContainsKey(assetName))
                    {
                        var existingAsset = existingAssetSubDict[assetName];
                        if (existingAsset != null)
                        {
                            Debug.Log($"There is already an asset \"{assetName}\" of type \"{assetType}\"!");
                        }
                        else
                        {
                            Debug.Log(
                                $"Key \"{assetName}\" for sub-dictionary of type \"{assetType}\" exists, but its value is null; Replacing with new asset...");
                            Assets[assetType][assetName] = asset;
                        }
                    }
                    else
                    {
                        Debug.Log($"Adding asset \"{assetName}\" of type \"{assetType}\".");
                        Assets[assetType].Add(assetName, asset);
                    }
                }
                else
                {
                    Debug.Log($"Failed to get sub-dictionary of type \"{assetType}\"!");
                    Assets.Add(assetType, new Dictionary<string, Object>());
                }
            }
            else
            {
                Assets.Add(assetType, new Dictionary<string, Object> { [assetName] = asset });
                Debug.Log(
                    $"Added new sub-dictionary of type \"{assetType}\" with initial asset \"{assetName}\".");
            }
        }
        /// <summary>
        /// Unload all saved assets.
        /// </summary>
        internal static void UnloadAll()
        {
            foreach (var assetDict in Assets.Values)
            {
                foreach (var asset in assetDict.Values)
                {
                    Object.DestroyImmediate(asset);
                }
            }

            Assets.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Unload bundles that were manually loaded for this mod.
        /// </summary>
        internal static void UnloadManualBundles()
        {
            foreach (var bundle in _manuallyLoadedBundles)
            {
                string bundleName = bundle.name;
                var unloadBundleHandle = bundle.UnloadAsync(true);
                unloadBundleHandle.completed += _ => { Debug.Log($"Successfully unloaded bundle \"{bundleName}\""); };
            }

            _manuallyLoadedBundles.Clear();

            foreach (var (_, obj) in Assets[typeof(GameObject)])
            {
                if (obj is GameObject gameObject && gameObject.activeSelf)
                {
                    Debug.Log($"Recycling all instances of prefab \"{gameObject.name}\"");
                    gameObject.RecycleAll();
                }
            }
        }

        /// <summary>
        /// Fetch an asset.
        /// </summary>
        /// <param name="assetName">The name of the asset to fetch.</param>
        /// <typeparam name="T">The type of asset to fetch.</typeparam>
        /// <returns>The fetched object if it exists, otherwise returns null.</returns>
        internal static T? Get<T>(string assetName) where T : Object
        {
            Type assetType = typeof(T);
            if (Assets.ContainsKey(assetType))
            {
                var subDict = Assets[assetType];
                if (subDict != null)
                {
                    if (subDict.ContainsKey(assetName))
                    {
                        var assetObj = subDict[assetName];
                        if (assetObj != null)
                        {
                            return assetObj as T;
                        }

                        Debug.Log($"Failed to get asset \"{assetName}\"; asset is null!");
                        return null;
                    }

                    Debug.Log($"Sub-dictionary for type \"{assetType}\" does not contain key \"{assetName}\"!");
                    return null;
                }

                Debug.Log($"Failed to get asset \"{assetName}\"; sub-dictionary of key \"{assetType}\" is null!");
                return null;
            }

            Debug.Log($"Could not find a sub-dictionary of type \"{assetType}\"!");
            return null;
        }
    }
}