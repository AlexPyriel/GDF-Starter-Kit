using System;
using ConfigRepository;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Project.Core.Infrastructure.UI.Configuration
{
    /// <summary>
    /// Stores the addressable prefab reference for a panel identified by its component type key.
    /// </summary>
    [Serializable]
    public class UIPanelPrefabConfig : Config<string>
    {
        [SerializeField] private AssetReferenceGameObject _prefabReference;

#if UNITY_EDITOR
        [SerializeField] private MonoScript _viewScript;

        /// <summary>
        /// Gets the authoring script used to derive the runtime repository key during editor sync.
        /// </summary>
        public MonoScript ViewScript => _viewScript;
#endif

        /// <summary>
        /// Gets the addressable reference for the configured panel prefab.
        /// </summary>
        public AssetReferenceGameObject PrefabReference => _prefabReference;
    }
}
