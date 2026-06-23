using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Project.Core.Infrastructure.UI.Interfaces
{
    /// <summary>
    /// Loads authored UI prefabs for screens and panels without exposing the underlying asset system.
    /// </summary>
    public interface IUIAssetProvider
    {
        /// <summary>
        /// Loads the prefab that backs the requested screen type.
        /// </summary>
        /// <typeparam name="TScreen">The screen component type to load.</typeparam>
        /// <returns>A task that returns the authored screen prefab.</returns>
        UniTask<TScreen> LoadScreenPrefabAsync<TScreen>() where TScreen : Component, IUIScreen;

        /// <summary>
        /// Loads the prefab that backs the requested panel type.
        /// </summary>
        /// <typeparam name="TPanel">The panel component type to load.</typeparam>
        /// <returns>A task that returns the authored panel prefab.</returns>
        UniTask<TPanel> LoadPanelPrefabAsync<TPanel>() where TPanel : Component, IUIPanel;

        /// <summary>
        /// Releases the loaded prefab handle for the specified screen type if it is currently cached.
        /// </summary>
        /// <param name="screenType">Concrete screen component type.</param>
        void UnloadScreenPrefab(Type screenType);

        /// <summary>
        /// Releases the loaded prefab handle for the specified panel type if it is currently cached.
        /// </summary>
        /// <param name="panelType">Concrete panel component type.</param>
        void UnloadPanelPrefab(Type panelType);
    }
}
