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
        private Canvas _canvas;
        private IOverlayUIService _overlayUIService;
        private readonly Vector3[] _focusCorners = new Vector3[4];

        [SerializeField] private RectTransform _screenRoot;
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private RectTransform _overlayRoot;
        [SerializeField] private RectTransform _tutorialRoot;
        [SerializeField] private Image _backdrop;
        [SerializeField] private Button _backdropButton;
        [SerializeField] private Image _tutorialDimmer;
        [SerializeField] private Image _focusFrame;
        [SerializeField] private Outline _focusFrameOutline;

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
        /// Gets the transform that hosts tutorial dimming and focus visuals.
        /// </summary>
        public RectTransform TutorialRoot => _tutorialRoot;

        private void Awake()
        {
            ValidateSerializedReferences();
            _canvas = GetComponent<Canvas>();
        }

        private void OnDestroy()
        {
            if (_overlayUIService == null)
                return;

            _overlayUIService.OverlayAlphaRequested -= HandleOverlayAlphaRequested;
            _overlayUIService.TutorialFocusRequested -= HandleTutorialFocusRequested;
            _overlayUIService.TutorialFocusCleared -= HandleTutorialFocusCleared;
        }

        /// <summary>
        /// Binds the UI host to an overlay-capable service that drives backdrop and tutorial visuals.
        /// </summary>
        /// <param name="overlayUIService">The overlay service that owns panel dismissal and focus state.</param>
        public void Initialize(IOverlayUIService overlayUIService)
        {
            if (_overlayUIService != null)
            {
                _overlayUIService.OverlayAlphaRequested -= HandleOverlayAlphaRequested;
                _overlayUIService.TutorialFocusRequested -= HandleTutorialFocusRequested;
                _overlayUIService.TutorialFocusCleared -= HandleTutorialFocusCleared;
            }

            _overlayUIService = overlayUIService;

            if (_overlayUIService == null)
                return;

            _overlayUIService.OverlayAlphaRequested += HandleOverlayAlphaRequested;
            _overlayUIService.TutorialFocusRequested += HandleTutorialFocusRequested;
            _overlayUIService.TutorialFocusCleared += HandleTutorialFocusCleared;
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

        private void HandleOverlayAlphaRequested(float alpha)
        {
            _tutorialRoot.gameObject.SetActive(alpha > 0f || _focusFrame.gameObject.activeSelf);

            Color color = _tutorialDimmer.color;
            color.a = Mathf.Clamp01(alpha);
            _tutorialDimmer.color = color;
        }

        private void HandleTutorialFocusRequested(RectTransform target)
        {
            if (target == null)
            {
                HandleTutorialFocusCleared();
                return;
            }

            _tutorialRoot.gameObject.SetActive(true);
            _focusFrame.gameObject.SetActive(true);
            MatchRectTransform(_focusFrame.rectTransform, target, _tutorialRoot, _canvas);
            _focusFrame.transform.SetAsLastSibling();
        }

        private void HandleTutorialFocusCleared()
        {
            _focusFrame.gameObject.SetActive(false);

            if (_tutorialDimmer.color.a <= 0f)
                _tutorialRoot.gameObject.SetActive(false);
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

            if (_tutorialDimmer == null)
                throw new MissingReferenceException($"{nameof(UIRoot)} requires {nameof(_tutorialDimmer)} to be assigned.");

            if (_focusFrame == null)
                throw new MissingReferenceException($"{nameof(UIRoot)} requires {nameof(_focusFrame)} to be assigned.");

            if (_focusFrameOutline == null)
                throw new MissingReferenceException($"{nameof(UIRoot)} requires {nameof(_focusFrameOutline)} to be assigned.");

            _backdropButton.onClick.AddListener(OnBackdropClicked);
            _backdrop.gameObject.SetActive(false);
            _backdrop.transform.SetAsFirstSibling();
            _focusFrame.gameObject.SetActive(false);
            _panelRoot.SetAsLastSibling();
            _overlayRoot.SetAsLastSibling();
            _tutorialRoot.SetAsLastSibling();
            _tutorialRoot.gameObject.SetActive(false);
        }

        private void OnBackdropClicked()
        {
            _overlayUIService?.DismissTopPanelAsync().Forget();
        }

        private void MatchRectTransform(RectTransform destination, RectTransform source, RectTransform parent, Canvas canvas)
        {
            source.GetWorldCorners(_focusCorners);

            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = canvas.worldCamera;

            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, _focusCorners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(eventCamera, _focusCorners[2]);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, min, eventCamera, out Vector2 localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, max, eventCamera, out Vector2 localMax);

            destination.anchorMin = destination.anchorMax = new Vector2(0.5f, 0.5f);
            destination.pivot = new Vector2(0.5f, 0.5f);
            destination.anchoredPosition = (localMin + localMax) * 0.5f;
            destination.sizeDelta = new Vector2(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y));
            destination.localScale = Vector3.one;
        }
    }
}
