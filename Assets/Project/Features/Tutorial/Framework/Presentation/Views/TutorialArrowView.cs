using DG.Tweening;
using UnityEngine;

namespace Assets.Project.Features.Tutorial.Framework.Presentation.Views
{
    /// <summary>
    /// Tutorial pointer arrow: a step spawns it (via <c>SpawnPanel</c> into <c>Overlay.ContentRoot</c>)
    /// and points it at a UI element with <see cref="PointAt"/>. The root (this object) is placed at
    /// the target rect center plus an offset and rotated by an angle; the visual (icon inside)
    /// smoothly bobs on its local Y via DOTween — the sprite looks down, so the bob always goes toward
    /// the target at any angle. Attachment uses WORLD coordinates of the target rect, so the arrow
    /// points correctly at any resolution — not tied to scene anchors; the root follows the target
    /// each frame while the arrow is alive (the target may shift due to layout or animation after
    /// spawn). The target is found by the step itself — the arrow only needs its <see cref="RectTransform"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TutorialArrowView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Bobbing arrow visual (icon inside the root). Bobs on Y relative to its base prefab position.")]
        private RectTransform _icon;

        [SerializeField]
        [Tooltip("Bob amplitude on Y, canvas units (reference 3840x2160).")]
        private float _bobDistance = 30f;

        [SerializeField]
        [Tooltip("Duration of one bob phase (down or back), seconds.")]
        [Min(0.05f)]
        private float _bobDuration = 0.6f;

        [SerializeField]
        [Tooltip("Bob easing curve.")]
        private Ease _bobEase = Ease.InOutSine;

        [SerializeField]
        [Tooltip("Arrow canvas sorting order (overrideSorting): ABOVE tutorial-raised elements (dimmer ~50 + raised 51), below the loading screen (100). Otherwise raised cards would occlude the arrow.")]
        private int _sortingOrder = 70;

        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _target;
        private Vector2 _offset;
        private Vector2 _iconBasePosition;
        private bool _basePositionCaptured;
        private Tween _bob;

        private void Awake()
        {
            CaptureBasePosition();
            EnsureSorting();
        }

        // Own Canvas with overrideSorting — arrow renders above tutorial-raised cards (whose canvas is
        // on dimmer+1), otherwise they occlude it. Order is set by _sortingOrder.
        private void EnsureSorting()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = _sortingOrder;
        }

        /// <summary>
        /// Points the arrow at an element: the root is placed at the target rect center plus
        /// <paramref name="offset"/>, rotated by <paramref name="angle"/>, and the bob starts. The
        /// position is derived from world coordinates — resolution- and anchor-independent; from
        /// there the arrow follows the target while alive. A <see langword="null"/> target is a no-op.
        /// </summary>
        /// <param name="target">Pointed element.</param>
        /// <param name="offset">Offset from the target center, canvas units (e.g. (0, 140) — above the target).</param>
        /// <param name="angle">Rotation in degrees counter-clockwise: 0 — down (as in the prefab), -90 — left, 90 — right, 180 — up.</param>
        public void PointAt(RectTransform target, Vector2 offset, float angle = 0f)
        {
            if (target == null)
            {
                return;
            }

            _target = target;
            _offset = offset;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            Follow();

            PlayBob();
        }

        /// <summary>Starts the looped bob of the visual on Y (base down and back, yoyo).</summary>
        public void PlayBob()
        {
            StopBob();
            if (_icon == null)
            {
                return;
            }

            CaptureBasePosition();
            _icon.anchoredPosition = _iconBasePosition;

            // DOAnchorPos is unavailable without the DOTween UI module — tween anchoredPosition.y manually.
            _bob = DOTween.To(
                    () => _icon.anchoredPosition.y,
                    y =>
                    {
                        Vector2 p = _icon.anchoredPosition;
                        p.y = y;
                        _icon.anchoredPosition = p;
                    },
                    _iconBasePosition.y - _bobDistance,
                    Mathf.Max(0.05f, _bobDuration))
                .SetEase(_bobEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this);
        }

        /// <summary>Stops the bob and returns the visual to its base position.</summary>
        public void StopBob()
        {
            _bob?.Kill();
            _bob = null;
            if (_icon != null && _basePositionCaptured)
            {
                _icon.anchoredPosition = _iconBasePosition;
            }
        }

        private void LateUpdate()
        {
            Follow();
        }

        // Place the root at the target world-space center (resolution-independent), then shift by the offset in canvas units.
        private void Follow()
        {
            if (_target == null)
            {
                return;
            }

            RectTransform self = (RectTransform)transform;
            _target.GetWorldCorners(_corners);
            Vector3 worldCenter = (_corners[0] + _corners[2]) * 0.5f;

            self.position = worldCenter;
            self.anchoredPosition += _offset;
        }

        private void CaptureBasePosition()
        {
            if (!_basePositionCaptured && _icon != null)
            {
                _iconBasePosition = _icon.anchoredPosition;
                _basePositionCaptured = true;
            }
        }

        private void OnDestroy()
        {
            StopBob();
        }
    }
}
