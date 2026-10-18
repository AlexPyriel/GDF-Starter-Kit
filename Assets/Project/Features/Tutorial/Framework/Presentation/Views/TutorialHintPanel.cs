using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Features.Tutorial.Framework.Presentation.Views
{
    /// <summary>
    /// Tutorial hint over the dim: text, "Continue" button, and screen position. A step spawns the
    /// prefab via <c>SpawnPanel</c> into <c>Overlay.ContentRoot</c>, sets text, button visibility and
    /// position from its asset. Position is the hint pivot point from screen center in canvas units
    /// (reference 3840x2160) — Pos X / Pos Y as if anchors were centered. Code only sets text, button,
    /// position, and click transparency; look, arrow, and spawn animation live in the prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TutorialHintPanel : MonoBehaviour
    {
        private static readonly Vector2 ScreenCenter = new Vector2(0.5f, 0.5f);

        [SerializeField]
        [Tooltip("Hint text label.")]
        private TextMeshProUGUI _label;

        [SerializeField]
        [Tooltip("Continue button.")]
        private Button _buttonContinue;

        [SerializeField]
        [Tooltip("Panel layout root (with layout groups / Content Size Fitter): used to force immediate layout rebuild after text change so the panel adapts to the new content size. Null — rebuild is skipped.")]
        private RectTransform _layoutRoot;

        /// <summary>
        /// "Continue" button — a step awaits its click where the hint is closed by confirmation
        /// (<see langword="null"/> if not assigned on the prefab).
        /// </summary>
        public Button ContinueButton => _buttonContinue;

        /// <summary>Sets the hint text.</summary>
        /// <param name="text">Localized text ready for display.</param>
        public void SetText(string text)
        {
            if (_label != null)
            {
                _label.SetText(text);
            }

            RebuildLayout();
        }

        /// <summary>
        /// Shows or hides the "Continue" button. Useful where a step waits for confirmation rather
        /// than an action on the target (click on an element, button).
        /// </summary>
        /// <param name="visible"><see langword="true"/> — the button is visible.</param>
        public void SetContinueVisible(bool visible)
        {
            if (_buttonContinue != null)
            {
                _buttonContinue.gameObject.SetActive(visible);
            }

            RebuildLayout();
        }

        /// <summary>
        /// Whether the hint catches clicks. Disable it when a window beneath the hint expects clicks
        /// anywhere on the screen (dialog) — clicks on the hint pass through as off-hint clicks.
        /// </summary>
        /// <param name="blocks"><see langword="false"/> — the hint lets clicks pass through.</param>
        public void SetBlocksRaycasts(bool blocks)
        {
            if (!TryGetComponent(out CanvasGroup group))
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.blocksRaycasts = blocks;
        }

        /// <summary>
        /// Forces an immediate layout rebuild on <see cref="_layoutRoot"/>, so the panel adapts to a
        /// new content size in the same frame (after a text change or a Continue-button visibility
        /// change — both affect height). No-op if the root is not assigned.
        /// </summary>
        private void RebuildLayout()
        {
            if (_layoutRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutRoot);
            }
        }

        /// <summary>Places the hint at a screen point.</summary>
        /// <param name="position">
        /// Hint pivot offset from screen center in canvas units (reference 3840x2160): (0, 0) — center,
        /// (0, 540) — a quarter of the height above center.
        /// </param>
        public void PlaceAt(Vector2 position)
        {
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = ScreenCenter;
            rect.anchoredPosition = position;
        }
    }
}
