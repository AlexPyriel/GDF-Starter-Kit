using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Assets.Project.Core.Infrastructure.UI.Enums;
using Assets.Project.Core.Infrastructure.UI.Hosts;
using Assets.Project.Core.Infrastructure.UI.Interfaces;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Project.Core.Infrastructure.UI.Services
{
    /// <summary>
    /// Root-scoped app-level overlay service for global panels that must work across scenes.
    /// </summary>
    public sealed class GlobalOverlayService : IOverlayUIService, IDisposable
    {
        private readonly IUIAssetProvider _uiAssetProvider;
        private readonly IObjectResolver _objectResolver;
        private readonly Dictionary<Type, IUIPanel> _panels = new();
        private readonly List<IUIPanel> _panelStack = new();
        private readonly UIRoot _uiRoot;

        /// <summary>
        /// Creates the app-level overlay service bound to the global overlay UI host.
        /// </summary>
        /// <param name="uiAssetProvider">Provides authored global panel prefabs.</param>
        /// <param name="uiRoot">The global overlay root created by the root container.</param>
        /// <param name="objectResolver">Instantiates prefabs with root-scope dependency injection.</param>
        public GlobalOverlayService(IUIAssetProvider uiAssetProvider, UIRoot uiRoot, IObjectResolver objectResolver)
        {
            _uiAssetProvider = uiAssetProvider ?? throw new ArgumentNullException(nameof(uiAssetProvider));
            _uiRoot = uiRoot ?? throw new ArgumentNullException(nameof(uiRoot));
            _objectResolver = objectResolver ?? throw new ArgumentNullException(nameof(objectResolver));
            _uiRoot.Initialize(this);
        }

        /// <summary>
        /// Gets the active global panel stack ordered from bottom to top.
        /// </summary>
        public IReadOnlyList<IUIPanel> PanelStack => _panelStack;

        /// <summary>
        /// Shows the requested global panel, creating it from the authored prefab on first use.
        /// </summary>
        /// <typeparam name="TPanel">The global panel component type to show.</typeparam>
        /// <returns>A task that returns the visible panel instance.</returns>
        public async UniTask<TPanel> ShowPanelAsync<TPanel>() where TPanel : Component, IUIPanel
        {
            TPanel prefab = await _uiAssetProvider.LoadPanelPrefabAsync<TPanel>();
            return await GetOrCreatePanelAsync(prefab, show: true);
        }

        /// <summary>
        /// Hides the requested global panel if it has already been created.
        /// </summary>
        /// <typeparam name="TPanel">The global panel component type to dismiss.</typeparam>
        /// <returns>A completed task once the panel has been dismissed.</returns>
        public UniTask DismissPanelAsync<TPanel>() where TPanel : Component, IUIPanel
        {
            if (_panels.TryGetValue(typeof(TPanel), out IUIPanel panel))
                ReleasePanel(panel, typeof(TPanel));

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Dismisses every active global panel.
        /// </summary>
        /// <returns>A completed task once all global panels have been dismissed.</returns>
        public UniTask DismissAllPanelsAsync()
        {
            for (int i = _panelStack.Count - 1; i >= 0; i--)
            {
                IUIPanel panel = _panelStack[i];
                ReleasePanel(panel, panel.GetType());
            }

            RefreshPanelPresentation();
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask DismissTopPanelAsync()
        {
            if (_panelStack.Count == 0)
                return UniTask.CompletedTask;

            int lastIndex = _panelStack.Count - 1;
            IUIPanel panel = _panelStack[lastIndex];
            ReleasePanel(panel, panel.GetType());
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            for (int i = _panelStack.Count - 1; i >= 0; i--)
            {
                IUIPanel panel = _panelStack[i];
                ReleasePanel(panel, panel.GetType());
            }
        }

        private async UniTask<TPanel> GetOrCreatePanelAsync<TPanel>(TPanel prefab, bool show = true)
            where TPanel : Component, IUIPanel
        {
            return await GetOrCreatePanelAsync(prefab, GetPanelParent(prefab.Mode), show);
        }

        private void PromotePanel(IUIPanel panel)
        {
            _panelStack.Remove(panel);
            _panelStack.Add(panel);
            MovePanelToLayer(panel);

            if (panel is Component component)
                component.transform.SetAsLastSibling();
        }

        private void MovePanelToLayer(IUIPanel panel)
        {
            if (panel is not Component component)
                return;

            Transform parent = GetPanelParent(panel.Mode);
            if (component.transform.parent != parent)
                component.transform.SetParent(parent, false);
        }

        private Transform GetPanelParent(EUIPanelMode mode)
        {
            EnsureRootRegistered();
            return mode == EUIPanelMode.Overlay ? _uiRoot.OverlayRoot : _uiRoot.PanelRoot;
        }

        private void RefreshPanelPresentation()
        {
            if (_uiRoot == null)
                return;

            bool showBackdrop = false;
            float backdropAlpha = 0f;

            for (int i = _panelStack.Count - 1; i >= 0; i--)
            {
                IUIPanel panel = _panelStack[i];
                if (panel is not Component component || !component.gameObject.activeSelf)
                    continue;

                if (panel.Mode == EUIPanelMode.Inline)
                    continue;

                showBackdrop = true;
                backdropAlpha = panel.Mode == EUIPanelMode.Overlay ? 0.78f : 0.56f;
                break;
            }

            _uiRoot.SetBackdropVisible(showBackdrop, backdropAlpha);
        }

        private void EnsureRootRegistered()
        {
            if (_uiRoot == null)
                throw new InvalidOperationException("Global overlay UIRoot is not available from the root scope.");
        }

        private void RegisterPanel<TPanel>(TPanel panel, bool hideOnRegister = true)
            where TPanel : Component, IUIPanel
        {
            if (panel == null)
                throw new ArgumentNullException(nameof(panel));

            _panels[typeof(TPanel)] = panel;
            MovePanelToLayer(panel);

            if (hideOnRegister)
                panel.Hide();

            RefreshPanelPresentation();
        }

        private UniTask<TPanel> ShowRegisteredPanelAsync<TPanel>() where TPanel : Component, IUIPanel
        {
            if (!_panels.TryGetValue(typeof(TPanel), out IUIPanel panel))
                throw new InvalidOperationException($"Global panel of type '{typeof(TPanel).Name}' is not registered.");

            PromotePanel(panel);
            panel.Show();
            RefreshPanelPresentation();
            return UniTask.FromResult((TPanel)panel);
        }

        private async UniTask<TPanel> GetOrCreatePanelAsync<TPanel>(TPanel prefab, Transform parent, bool show = true)
            where TPanel : Component, IUIPanel
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (_panels.TryGetValue(typeof(TPanel), out IUIPanel existingPanel))
            {
                if (show)
                    await ShowRegisteredPanelAsync<TPanel>();

                return (TPanel)existingPanel;
            }

            TPanel createdPanel = _objectResolver.Instantiate(prefab, parent);
            RegisterPanel(createdPanel, hideOnRegister: !show);

            if (show)
            {
                PromotePanel(createdPanel);
                createdPanel.Show();
            }

            return createdPanel;
        }

        private void ReleasePanel(IUIPanel panel, Type panelType)
        {
            if (panel == null)
                return;

            panel.Hide();
            _panelStack.Remove(panel);
            _panels.Remove(panelType);

            if (panel is Component component && component != null && component.gameObject != null)
                UnityEngine.Object.Destroy(component.gameObject);

            _uiAssetProvider.UnloadPanelPrefab(panelType);
            RefreshPanelPresentation();
        }
    }
}
