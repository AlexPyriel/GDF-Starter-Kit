#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    /// Custom inspector for the UI prefab repositories. Each entry's key is the FullName of the
    /// view component type: the "Sync Keys" button fills the keys from the assigned view scripts
    /// and verifies that every addressable prefab contains the matching component.
    /// </summary>
    public abstract class UIAssetRepositoryEditorBase<TRepository, TConfig> : UnityEditor.Editor
        where TRepository : ScriptableObject, IConfigProvider<string, TConfig>
        where TConfig : Config<string>
    {
        private const string LIST_PATH = "_configs._serializedList";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Add an entry with '+', assign its view script and an addressable prefab, then press 'Sync Keys'. " +
                "The repository key is generated from the script type FullName; validation checks that the prefab contains that component.",
                MessageType.Info);

            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Sync Keys"))
            {
                SyncKeys();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SyncKeys()
        {
            SerializedProperty list = serializedObject.FindProperty(LIST_PATH);
            if (list == null || !list.isArray)
            {
                Debug.LogWarning($"[{target.GetType().Name}] Could not find the serialized config list.", target);
                return;
            }

            string[] keys = new string[list.arraySize];
            List<string> issues = new();
            HashSet<string> seen = new();

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty value = list.GetArrayElementAtIndex(i).FindPropertyRelative("Value");

                MonoScript viewScript = value.FindPropertyRelative("_viewScript").objectReferenceValue as MonoScript;
                if (viewScript == null)
                {
                    issues.Add($"Entry {i}: view script is not assigned.");
                    continue;
                }

                Type viewType = viewScript.GetClass();
                if (viewType == null)
                {
                    issues.Add($"Entry {i}: script '{viewScript.name}' does not resolve to a class.");
                    continue;
                }

                string key = UIAssetTypeKey.Get(viewType);

                string prefabGuid = value.FindPropertyRelative("_prefabReference.m_AssetGUID").stringValue;
                if (string.IsNullOrWhiteSpace(prefabGuid))
                {
                    issues.Add($"Config '{key}': addressable prefab is not assigned.");
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(prefabGuid));
                if (prefab == null)
                {
                    issues.Add($"Config '{key}': prefab guid does not resolve to a prefab.");
                    continue;
                }

                if (prefab.GetComponent(viewType) == null)
                {
                    issues.Add($"Prefab '{prefab.name}' does not contain component '{key}'.");
                    continue;
                }

                if (!seen.Add(key))
                {
                    issues.Add($"Duplicate UI type key '{key}'. Keep only one config per UI component type.");
                    continue;
                }

                keys[i] = key;
            }

            if (issues.Count > 0)
            {
                Debug.LogWarning(
                    $"[{target.GetType().Name}] Sync aborted because {issues.Count} issue(s) were found. Entries were left unchanged:\n- {string.Join("\n- ", issues)}",
                    target);
                return;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Key").stringValue = keys[i];

                SerializedProperty idProperty = entry.FindPropertyRelative("Value").FindPropertyRelative("_id");
                if (idProperty != null)
                {
                    idProperty.stringValue = keys[i];
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            Debug.Log($"[{target.GetType().Name}] Synced {list.arraySize} UI prefab config key(s).", target);
        }
    }
}
#endif
