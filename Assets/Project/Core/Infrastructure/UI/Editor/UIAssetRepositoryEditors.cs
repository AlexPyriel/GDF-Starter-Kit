#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ConfigRepository;
using Assets.Project.Core.Infrastructure.UI.Configuration;
using UnityEditor;
using UnityEngine;

namespace Assets.Project.Core.Infrastructure.UI.Editor
{
    [CustomEditor(typeof(UIScreenPrefabConfigRepository))]
    public class UIScreenPrefabConfigRepositoryEditor : UIAssetRepositoryEditorBase<UIScreenPrefabConfigRepository, UIScreenPrefabConfig> { }

    [CustomEditor(typeof(UIPanelPrefabConfigRepository))]
    public class UIPanelPrefabConfigRepositoryEditor : UIAssetRepositoryEditorBase<UIPanelPrefabConfigRepository, UIPanelPrefabConfig> { }

    /// <summary>
    /// Adds explicit repository sync for UI prefab configs so type-based runtime keys are generated from script references,
    /// not typed manually in the asset.
    /// </summary>
    public abstract class UIAssetRepositoryEditorBase<TRepository, TConfig> : UnityEditor.Editor
        where TRepository : ScriptableObject, IConfigProvider<string, TConfig>
        where TConfig : Config<string>
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Assign a view script and addressable prefab in each config entry, then press Sync Keys. " +
                "The repository key is generated from the script type FullName, and validation checks that the prefab contains that component.",
                MessageType.Info);

            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Sync Keys"))
            {
                SyncRepository((TRepository)target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void SyncRepository(TRepository repository)
        {
            if (repository == null)
                throw new ArgumentNullException(nameof(repository));

            List<TConfig> snapshot = CollectConfigs(repository);
            List<string> issues = new();
            Dictionary<string, TConfig> rebuilt = new();

            foreach (TConfig config in snapshot)
            {
                if (!TryBuildEntry(config, out string key, out string issue))
                {
                    issues.Add(issue);
                    continue;
                }

                if (!rebuilt.TryAdd(key, config))
                {
                    issues.Add($"Duplicate UI type key '{key}' detected. Keep only one config per UI component type.");
                }
            }

            if (issues.Count > 0)
            {
                Debug.LogWarning(
                    $"[{typeof(TRepository).Name}] Sync aborted because {issues.Count} issue(s) were found. Repository entries were left unchanged:\n- {string.Join("\n- ", issues)}",
                    repository);
                return;
            }

            repository.Clear();

            foreach (KeyValuePair<string, TConfig> pair in rebuilt)
            {
                repository.TrySet(pair.Key, pair.Value);
            }

            EditorUtility.SetDirty(repository);
            AssetDatabase.SaveAssets();

            Debug.Log($"[{typeof(TRepository).Name}] Synced {rebuilt.Count} UI prefab configs.", repository);
        }

        private static List<TConfig> CollectConfigs(TRepository repository)
        {
            return repository.Enumerate()
                .Select(pair => pair.Value)
                .Where(config => config != null)
                .ToList();
        }

        private static bool TryBuildEntry(TConfig config, out string key, out string issue)
        {
            key = null;
            issue = null;

            MonoScript viewScript = GetViewScript(config);
            if (viewScript == null)
            {
                issue = "A UI prefab config is missing its view script reference.";
                return false;
            }

            Type viewType = viewScript.GetClass();
            if (viewType == null)
            {
                issue = $"Script '{viewScript.name}' does not resolve to a valid class.";
                return false;
            }

            key = UIAssetTypeKey.Get(viewType);

            if (!TryLoadPrefab(config, out GameObject prefab))
            {
                issue = $"Config '{key}' has no valid addressable prefab assigned.";
                return false;
            }

            if (prefab.GetComponent(viewType) == null)
            {
                issue = $"Prefab '{prefab.name}' does not contain component '{key}'.";
                return false;
            }

            return true;
        }

        private static MonoScript GetViewScript(TConfig config)
        {
            return config switch
            {
                UIScreenPrefabConfig screenConfig => screenConfig.ViewScript,
                UIPanelPrefabConfig panelConfig => panelConfig.ViewScript,
                _ => null
            };
        }

        private static bool TryLoadPrefab(TConfig config, out GameObject prefab)
        {
            prefab = null;

            string assetGuid = config switch
            {
                UIScreenPrefabConfig screenConfig => screenConfig.PrefabReference.AssetGUID,
                UIPanelPrefabConfig panelConfig => panelConfig.PrefabReference.AssetGUID,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(assetGuid))
                return false;

            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return prefab != null;
        }
    }
}
#endif
