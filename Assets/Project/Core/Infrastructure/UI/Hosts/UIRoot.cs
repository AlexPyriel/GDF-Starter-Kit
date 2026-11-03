using Cysharp.Threading.Tasks;
using Assets.Project.Core.Infrastructure.UI.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Core.Infrastructure.UI.Hosts
{
    /// <summary>
    /// Authored UI host used by either GlobalOverlayCanvas.prefab or SceneUICanvas.prefab.
    /// Provides screen, panel, overlay and tutorial roots for all prefab-backed UI.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIRoot : MonoBehaviour
    {
        private IOverlayUIService _overlayUIService;

        [SerializeField] private RectTransform _screenRoot;
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private RectTransform _overlayRoot;
        [SerializeField] private RectTransform _tutorialRoot;
        [SerializeField] private Image _backdrop;
        [SerializeField] private Button _backdropButton;

        /// <summary>
        /// Gets the transform that hosts active scene screens.
        /// </summary>
        public RectTransform ScreenRoot => _screenRoot;

        /// <summary>
        /// Gets the transform that hosts regular modal panels.
        /// </summary>
        public RectTransform PanelRoot => _panelRoot;

        /// <summary>
        /// Gets the transform that hosts overlay-level panels.
        /// </summary>
        public RectTransform OverlayRoot => _overlayRoot;

        /// <summary>
        /// Gets the transform that hosts the tutorial overlay.
        /// </summary>
        public RectTransform TutorialRoot => _tutorialRoot;

        private void Awake()
        {
            ValidateSerializedReferences();
        }

        /// <summary>
        /// Binds the UI host to the overlay-capable service that owns panel dismissal.
        /// </summary>
        /// <param name="overlayUIService">The overlay service that dismisses the top panel on backdrop clicks.</param>
        public void Initialize(IOverlayUIService overlayUIService)
        {
            _overlayUIService = overlayUIService;
        }

        /// <summary>
        /// Shows or hides the authored backdrop and updates its alpha when visible.
        /// </summary>
        /// <param name="isVisible">Whether the backdrop should be visible.</param>
        /// <param name="alpha">The backdrop alpha in normalized 0..1 range.</param>
        public void SetBackdropVisible(bool isVisible, float alpha)
        {
            _backdrop.gameObject.SetActive(isVisible);

            if (isVisible)
            {
                Color color = _backdrop.color;
                color.a = Mathf.Clamp01(alpha);
                _backdrop.color = color;
            }
        }

        /// <summary>
        /// Signals whether the tutorial feature currently has visible panels under <see cref="TutorialRoot"/>.
        /// The tutorial root is active only while <paramref name="hasTutorialPanels"/> is <see langword="true"/>.
        /// </summary>
        /// <param name="hasTutorialPanels">Whether active panels exist in the tutorial layer.</param>
        public void SetTutorialPanelsVisible(bool hasTutorialPanels)
        {
            _tutorialRoot.gameObject.SetActive(hasTutorialPanels);
        }

        private void ValidateSerializedReferences()
        {
            if (_screenRoot == null)
                throw new MissingReferenceException($"{nameof(UIRoot)} requires {nameof(_screenRoot)} to be assigned.");

            if (_panelRoot == null)
                throw new MissingReferenceException($"{nameof(UIRoot)} requires {nameof(_panelRoot)} to be assigned.");

            if (_overlayRoot == null)
                throw new MissingReferenceException($"{nameof(UIRoot)} requires {nameof(_overlayRoot)} to be assigned.");

            if (_tutorialRoot == null)
                throw new MissingReferenceException($"{nameof(UIRoot)} requires {nameof(_tutorialRoot)} to be assigned.");

            if (_backdrop == null)
                throw new MissingReferenceException($"{nameof(UIRoot)} requires {nameof(_backdrop)} to be assigned.");

            if (_backdropButton == null)
                throw new MissingReferenceException($"{nameof(UIRoot)} requires {nameof(_backdropButton)} to be assigned.");

            _backdropButton.onClick.AddListener(OnBackdropClicked);
            _backdrop.gameObject.SetActive(false);
            _backdrop.transform.SetAsFirstSibling();
            _panelRoot.SetAsLastSibling();
            _overlayRoot.SetAsLastSibling();
            _tutorialRoot.SetAsLastSibling();
            _tutorialRoot.gameObject.SetActive(false);
        }

        private void OnBackdropClicked()
        {
            _overlayUIService?.DismissTopPanelAsync().Forget();
        }
    }
}
