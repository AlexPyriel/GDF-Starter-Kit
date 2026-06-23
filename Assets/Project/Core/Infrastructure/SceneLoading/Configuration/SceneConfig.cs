using System;
using ConfigRepository;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Assets.Project.Core.Infrastructure.SceneLoading.Configuration
{
    /// <summary>
    /// Holds scene loading data and a hidden scene identity synchronized by the config wrapper.
    /// </summary>
    [Serializable]
    public class SceneConfig : Config<ESceneType>
    {
        [SerializeField] private AssetReference _sceneReference;

        /// <summary>
        /// Gets the Addressables reference for the scene.
        /// </summary>
        public AssetReference SceneReference => _sceneReference;
    }
}
