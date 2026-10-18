using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Project.Core.Infrastructure.UI.Hosts;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Presentation.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <summary>
    /// Singleton tutorial overlay (root scope). Lazily instantiates <see cref="TutorialOverlayView"/>
    /// under the global <see cref="UIRoot.TutorialRoot"/> (survives scene transitions) and delegates
    /// <see cref="ITutorialOverlay"/> calls to it. Keeps the tutorial layer active via
    /// <see cref="UIRoot.SetTutorialPanelsVisible"/> while the overlay is shown.
    /// </summary>
    public sealed class TutorialOverlay : ITutorialOverlay, IDisposable
    {
        private readonly UIRoot _uiRoot;
        private readonly IObjectResolver _resolver;
        private readonly TutorialOverlayView _viewPrefab;

        private TutorialOverlayView _view;

        /// <summary>Creates the overlay service.</summary>
        /// <param name="uiRoot">Global UI host (its <c>TutorialRoot</c> is the overlay parent).</param>
        /// <param name="resolver">Resolver used to instantiate the prefab with root-scope injection.</param>
        /// <param name="viewPrefab">Overlay view prefab.</param>
        /// <exception cref="ArgumentNullException">Any dependency is <see langword="null"/>.</exception>
        public TutorialOverlay(UIRoot uiRoot, IObjectResolver resolver, TutorialOverlayView viewPrefab)
        {
            _uiRoot = uiRoot != null ? uiRoot : throw new ArgumentNullException(nameof(uiRoot));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _viewPrefab = viewPrefab != null ? viewPrefab : throw new ArgumentNullException(nameof(viewPrefab));
        }

        /// <inheritdoc />
        public RectTransform ContentRoot => EnsureView().ContentRoot;

        /// <inheritdoc />
        public void Show()
        {
            TutorialOverlayView view = EnsureView();
            _uiRoot.SetTutorialPanelsVisible(true);
            view.Show();
            view.transform.SetAsLastSibling();
        }

        /// <inheritdoc />
        public void Hide()
        {
            if (_view != null)
            {
                _view.Hide();
            }

            _uiRoot.SetTutorialPanelsVisible(false);
        }

        /// <inheritdoc />
        public void Highlight(IReadOnlyList<Graphic> targets)
        {
            EnsureView().Highlight(targets);
        }

        /// <inheritdoc />
        public void Highlight(IReadOnlyList<RectTransform> regions)
        {
            EnsureView().Highlight(regions);
        }

        /// <inheritdoc />
        public void RaiseAbove(IReadOnlyList<RectTransform> elements)
        {
            EnsureView().RaiseAbove(elements);
        }

        /// <inheritdoc />
        public void ClearHighlight()
        {
            if (_view != null)
            {
                _view.ClearHighlight();
            }
        }

        /// <inheritdoc />
        public void SetDimAlpha(float alpha)
        {
            EnsureView().SetDimAlpha(alpha);
        }

        /// <inheritdoc />
        public UniTask WaitForAdvanceClick(CancellationToken token)
        {
            return EnsureView().WaitForAdvanceClick(token);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_view != null)
            {
                UnityEngine.Object.Destroy(_view.gameObject);
                _view = null;
            }
        }

        private TutorialOverlayView EnsureView()
        {
            if (_view == null)
            {
                _view = _resolver.Instantiate(_viewPrefab, _uiRoot.TutorialRoot);
                _view.Hide();
            }

            return _view;
        }
    }
}
