using System;
using Cysharp.Threading.Tasks;
using Assets.Project.Core.Infrastructure.UI.Configuration;
using Assets.Project.Core.Infrastructure.UI.Interfaces;
using Assets.Project.Core.Infrastructure.UI.Services;
using Assets.Project.Core.Infrastructure.UI.Views;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Assets.Project.Core.Infrastructure.UI.Providers
{
    /// <summary>
    /// Loads authored UI screen and panel prefabs from addressable references stored in project config repositories.
    /// Repository entries are keyed directly by the concrete UI component type name.
    /// </summary>
    public sealed class AddressableUIAssetProvider : IUIAssetProvider
    {
        private readonly UIPanelPrefabConfigRepository _panelPrefabConfigRepository;
        private readonly UIScreenPrefabConfigRepository _screenPrefabConfigRepository;
        private readonly AddressableUIViewLoaderService _viewLoaderService;

        /// <summary>
        /// Creates a UI asset provider backed by addressable prefab repositories.
        /// </summary>
        public AddressableUIAssetProvider(
            UIScreenPrefabConfigRepository screenPrefabConfigRepository,
            UIPanelPrefabConfigRepository panelPrefabConfigRepository,
            AddressableUIViewLoaderService viewLoaderService)
        {
            _screenPrefabConfigRepository = screenPrefabConfigRepository ?? throw new ArgumentNullException(nameof(screenPrefabConfigRepository));
            _panelPrefabConfigRepository = panelPrefabConfigRepository ?? throw new ArgumentNullException(nameof(panelPrefabConfigRepository));
            _viewLoaderService = viewLoaderService ?? throw new ArgumentNullException(nameof(viewLoaderService));
        }

        /// <inheritdoc />
        public async UniTask<TScreen> LoadScreenPrefabAsync<TScreen>() where TScreen : Component, IUIScreen
        {
            string key = UIAssetTypeKey.Get<TScreen>();

            if (!_screenPrefabConfigRepository.TryGet(key, out UIScreenPrefabConfig config) || config == null)
                throw new InvalidOperationException($"Screen prefab config was not found for key '{key}'.");

            View view = await LoadViewAsync(config.PrefabReference, $"screen '{key}'");
            return GetRequiredView<TScreen>(view, $"screen '{key}'");
        }

        /// <inheritdoc />
        public async UniTask<TPanel> LoadPanelPrefabAsync<TPanel>() where TPanel : Component, IUIPanel
        {
            string key = UIAssetTypeKey.Get<TPanel>();

            if (!_panelPrefabConfigRepository.TryGet(key, out UIPanelPrefabConfig config) || config == null)
                throw new InvalidOperationException($"Panel prefab config was not found for key '{key}'.");

            View view = await LoadViewAsync(config.PrefabReference, $"panel '{key}'");
            return GetRequiredView<TPanel>(view, $"panel '{key}'");
        }

        /// <inheritdoc />
        public void UnloadScreenPrefab(Type screenType)
        {
            UIScreenPrefabConfig config = GetScreenConfig(screenType);
            if (config?.PrefabReference != null)
                _viewLoaderService.Unload(config.PrefabReference);
        }

        /// <inheritdoc />
        public void UnloadPanelPrefab(Type panelType)
        {
            UIPanelPrefabConfig config = GetPanelConfig(panelType);
            if (config?.PrefabReference != null)
                _viewLoaderService.Unload(config.PrefabReference);
        }

        private async UniTask<View> LoadViewAsync(AssetReferenceGameObject reference, string label)
        {
            if (reference == null)
                throw new InvalidOperationException($"Addressable reference for {label} is not assigned.");

            View view = await _viewLoaderService.LoadPrefabAsync(reference);
            if (view == null)
                throw new InvalidOperationException($"Addressable view for {label} failed to load.");

            return view;
        }

        private static TComponent GetRequiredView<TComponent>(View view, string label)
            where TComponent : Component
        {
            if (view is TComponent component)
                return component;

            throw new InvalidOperationException($"Addressable view for {label} does not match required component '{typeof(TComponent).Name}'.");
        }

        private UIScreenPrefabConfig GetScreenConfig(Type screenType)
        {
            string key = UIAssetTypeKey.Get(screenType);
            if (_screenPrefabConfigRepository.TryGet(key, out UIScreenPrefabConfig config) && config != null)
                return config;

            throw new InvalidOperationException($"Screen prefab config was not found for key '{key}'.");
        }

        private UIPanelPrefabConfig GetPanelConfig(Type panelType)
        {
            string key = UIAssetTypeKey.Get(panelType);
            if (_panelPrefabConfigRepository.TryGet(key, out UIPanelPrefabConfig config) && config != null)
                return config;

            throw new InvalidOperationException($"Panel prefab config was not found for key '{key}'.");
        }
    }
}
