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
    /// Minimal screen and panel orchestration service with DI-backed panel creation.
    /// </summary>
    public sealed class SceneUIService : IOverlayUIService, IDisposable
    {
        private readonly IUIAssetProvider _uiAssetProvider;
        private readonly IObjectResolver _objectResolver;
        private readonly Dictionary<Type, IUIPanel> _panels = new();
        private readonly List<IUIPanel> _panelStack = new();
        private readonly UIRoot _uiRoot;

        private IUIScreen _currentScreen;

        /// <summary>
        /// Creates a scene-local UI orchestration service bound to the authored scene UI root.
        /// </summary>
        /// <param name="uiAssetProvider">Provides authored screen and panel prefabs.</param>
        /// <param name="uiRoot">The synchronously created scene UI root for this scope.</param>
        /// <param name="objectResolver">Instantiates prefabs with scene-scope dependency injection.</param>
        public SceneUIService(IUIAssetProvider uiAssetProvider, UIRoot uiRoot, IObjectResolver objectResolver)
        {
            _uiAssetProvider = uiAssetProvider ?? throw new ArgumentNullException(nameof(uiAssetProvider));
            _uiRoot = uiRoot ?? throw new ArgumentNullException(nameof(uiRoot));
            _objectResolver = objectResolver ?? throw new ArgumentNullException(nameof(objectResolver));
            _uiRoot.Initialize(this);
        }

        /// <inheritdoc />
        public event Action<RectTransform> TutorialFocusRequested;
        /// <inheritdoc />
        public event Action TutorialFocusCleared;
        /// <inheritdoc />
        public event Action<float> OverlayAlphaRequested;

        /// <summary>
        /// Gets the currently visible screen managed by the scene UI service.
        /// </summary>
        public IUIScreen CurrentScreen => _currentScreen;

        /// <summary>
        /// Gets the active panel stack ordered from bottom to top.
        /// </summary>
        public IReadOnlyList<IUIPanel> PanelStack => _panelStack;

        /// <summary>
        /// Shows the requested screen, creating it from the authored prefab on first use.
        /// </summary>
        /// <typeparam name="TScreen">The screen component type to show.</typeparam>
        /// <returns>A task that returns the visible screen instance.</returns>
        public async UniTask<TScreen> ShowScreenAsync<TScreen>() where TScreen : Component, IUIScreen
        {
            if (_currentScreen is TScreen currentScreen && currentScreen is Component currentComponent && currentComponent.gameObject != null)
                return currentScreen;

            IUIScreen previousScreen = _currentScreen;
            TScreen prefab = await _uiAssetProvider.LoadScreenPrefabAsync<TScreen>();
            TScreen createdScreen = CreateScreen(prefab, show: true, previousScreen);

            if (previousScreen != null && !ReferenceEquals(previousScreen, createdScreen))
                ReleaseScreen(previousScreen, previousScreen.GetType());

            return createdScreen;
        }

        /// <summary>
        /// Hides the requested screen if it has already been created for the current scene scope.
        /// </summary>
        /// <typeparam name="TScreen">The screen component type to hide.</typeparam>
        /// <returns>A completed task once the screen has been hidden.</returns>
        public UniTask HideScreenAsync<TScreen>() where TScreen : Component, IUIScreen
        {
            if (_currentScreen is TScreen screen)
                ReleaseScreen(screen, typeof(TScreen));

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Shows the requested panel, creating it from the authored prefab on first use.
        /// </summary>
        /// <typeparam name="TPanel">The panel component type to show.</typeparam>
        /// <returns>A task that returns the visible panel instance.</returns>
        public async UniTask<TPanel> ShowPanelAsync<TPanel>() where TPanel : Component, IUIPanel
        {
            TPanel prefab = await _uiAssetProvider.LoadPanelPrefabAsync<TPanel>();
            return await GetOrCreatePanelAsync(prefab, show: true);
        }

        /// <summary>
        /// Hides the requested panel if it has already been created for the current scene scope.
        /// </summary>
        /// <typeparam name="TPanel">The panel component type to dismiss.</typeparam>
        /// <returns>A completed task once the panel has been dismissed.</returns>
        public UniTask DismissPanelAsync<TPanel>() where TPanel : Component, IUIPanel
        {
            if (_panels.TryGetValue(typeof(TPanel), out IUIPanel panel))
                ReleasePanel(panel, typeof(TPanel));

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Dismisses every active panel in the current scene scope.
        /// </summary>
        /// <returns>A completed task once all panels have been dismissed.</returns>
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
            ReleaseCurrentScreen();

            for (int i = _panelStack.Count - 1; i >= 0; i--)
            {
                IUIPanel panel = _panelStack[i];
                ReleasePanel(panel, panel.GetType());
            }
        }

        internal void FocusTarget(RectTransform target)
        {
            TutorialFocusRequested?.Invoke(target);
        }

        internal void ClearFocusTarget()
        {
            TutorialFocusCleared?.Invoke();
        }

        internal void SetOverlayAlpha(float alpha)
        {
            OverlayAlphaRequested?.Invoke(Mathf.Clamp01(alpha));
        }

        private TScreen CreateScreen<TScreen>(TScreen prefab, bool show = true, IUIScreen previousScreen = null)
            where TScreen : Component, IUIScreen
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            EnsureRootRegistered();

            TScreen createdScreen = _objectResolver.Instantiate(prefab, _uiRoot.ScreenRoot);
            RegisterScreen(createdScreen, previousScreen);

            if (show)
                createdScreen.Show();

            return createdScreen;
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

        private void MoveScreenToRoot(IUIScreen screen)
        {
            if (screen is not Component component || _uiRoot == null)
                return;

            Transform parent = _uiRoot.ScreenRoot;
            if (component.transform.parent != parent)
                component.transform.SetParent(parent, false);
        }

        private void MovePanelToLayer(IUIPanel panel)
        {
            if (panel is not Component component || _uiRoot == null)
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
                throw new InvalidOperationException("Scene UIRoot is not available from the current scene scope.");
        }

        private void RegisterScreen<TScreen>(TScreen screen, IUIScreen previousScreen = null)
            where TScreen : Component, IUIScreen
        {
            if (screen == null)
                throw new ArgumentNullException(nameof(screen));

            if (previousScreen != null && !ReferenceEquals(previousScreen, screen))
                previousScreen.Hide();

            _currentScreen = screen;
            MoveScreenToRoot(screen);
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
                throw new InvalidOperationException($"Panel of type '{typeof(TPanel).Name}' is not registered.");

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

        private void ReleaseCurrentScreen()
        {
            if (_currentScreen == null)
                return;

            ReleaseScreen(_currentScreen, _currentScreen.GetType());
        }

        private void ReleaseScreen(IUIScreen screen, Type screenType)
        {
            if (screen == null)
                return;

            screen.Hide();
            if (ReferenceEquals(_currentScreen, screen))
                _currentScreen = null;

            if (screen is Component component && component != null && component.gameObject != null)
                UnityEngine.Object.Destroy(component.gameObject);

            _uiAssetProvider.UnloadScreenPrefab(screenType);
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
