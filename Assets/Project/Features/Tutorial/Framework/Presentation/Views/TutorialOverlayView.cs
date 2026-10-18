using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Project.Features.Tutorial.Framework.Presentation.Views
{
    /// <summary>
    /// Overlay view component (on the prefab under <c>UIRoot.TutorialRoot</c>): a fullscreen dimmer
    /// using the <c>Project/UI/TutorialCutout</c> shader with cutouts over targets and
    /// <see cref="ContentRoot"/> (above the dim) where steps spawn their panels. Implements
    /// <see cref="ICanvasRaycastFilter"/>: a click inside a cutout passes through to the target,
    /// outside — blocked (and counted as "advance"). Does NOT render the message/panel itself — that
    /// is done by the step.
    /// <para>Cutout shape: the first <c>Image</c> element with a sprite becomes an alpha mask of that
    /// sprite (non-rectangular); the rest are rectangular (up to 8). Dim strength = opacity/color of
    /// the Image itself (the material is neutral).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialOverlayView : MonoBehaviour, ICanvasRaycastFilter, IPointerClickHandler
    {
        private const int MAX_HOLES = 8;
        private static readonly int HolesId = Shader.PropertyToID("_Holes");
        private static readonly int HoleCountId = Shader.PropertyToID("_HoleCount");
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
        private static readonly int MaskRectId = Shader.PropertyToID("_MaskRect");
        private static readonly int MaskUvId = Shader.PropertyToID("_MaskUv");
        private static readonly int MaskEnabledId = Shader.PropertyToID("_MaskEnabled");

        [SerializeField]
        [Tooltip("Fullscreen dimmer (Image with Project/UI/TutorialCutout material, raycastTarget=1).")]
        private Image _dimmer;

        [SerializeField]
        [Tooltip("Content root above the dim: steps spawn their panels/messages here.")]
        private RectTransform _contentRoot;

        // Sorting order of elements raised above the dimmer = dimmer order + this offset.
        private const int RAISED_ORDER_OFFSET = 1;
        // Fallback sort if the dimmer canvas is not found (dimmer ~50, loading 100 — fit in between).
        private const int RAISED_ORDER_FALLBACK = 60;

        private Material _material;
        private readonly Vector4[] _holes = new Vector4[MAX_HOLES];
        private int _holeCount;
        private readonly List<RectTransform> _rectTargets = new List<RectTransform>();
        private readonly List<RaisedElement> _raised = new List<RaisedElement>();
        private RectTransform _maskRegion;
        private Sprite _maskSprite;
        private Vector4 _maskRectUv;
        private readonly Vector3[] _worldCorners = new Vector3[4];
        private UniTaskCompletionSource _advanceSource;
        private float _defaultDimAlpha;
        private bool _hasDefaultDimAlpha;

        /// <summary>Root for spawning step panels/messages (above the dim).</summary>
        public RectTransform ContentRoot => _contentRoot;

        private void Awake()
        {
            // Clone the material (MPB is unavailable on CanvasRenderer): hole edits do not touch the asset.
            _material = new Material(_dimmer.material);
            _dimmer.material = _material;
            RememberDefaultDimAlpha();
            ApplyHoles();
            ApplyMaskDisabled();
        }

        private void OnDestroy()
        {
            _advanceSource?.TrySetResult();
            RestoreRaised();
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        // One raised-above element record — what we added and what to restore on release.
        private struct RaisedElement
        {
            public Canvas Canvas;
            public GraphicRaycaster Raycaster;
            public bool AddedCanvas;
            public bool AddedRaycaster;
            public bool PreviousOverride;
            public int PreviousOrder;
        }

        // While highlights are active — keep the holes aligned (the target may shift due to layout/resolution).
        private void LateUpdate()
        {
            if (_rectTargets.Count > 0 || _maskRegion != null)
            {
                Recompute();
            }
        }

        /// <summary>Activates the overlay.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>Hides the overlay, clears holes and restores dim alpha to the prefab default.</summary>
        public void Hide()
        {
            ClearHighlight();
            RememberDefaultDimAlpha();
            SetDimAlpha(_defaultDimAlpha);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the dimmer alpha (0..1). A transparent dimmer still catches clicks outside cutouts —
        /// input blocking does not depend on dim strength.
        /// </summary>
        /// <param name="alpha">Dimmer alpha.</param>
        public void SetDimAlpha(float alpha)
        {
            RememberDefaultDimAlpha();
            Color color = _dimmer.color;
            color.a = Mathf.Clamp01(alpha);
            _dimmer.color = color;
        }

        /// <summary>
        /// Caches the prefab-authored dimmer alpha before the first edit. Not in <c>Awake</c>: the
        /// service creates the overlay under a disabled root, so <c>Awake</c> only runs on the first
        /// show — after <see cref="Hide"/> and <see cref="SetDimAlpha"/> of the step would have
        /// overwritten the prefab value.
        /// </summary>
        private void RememberDefaultDimAlpha()
        {
            if (_hasDefaultDimAlpha)
            {
                return;
            }

            _defaultDimAlpha = _dimmer.color.a;
            _hasDefaultDimAlpha = true;
        }

        /// <summary>
        /// Highlights targets: the first <c>Image</c> with a sprite becomes a shaped cutout (mask),
        /// the rest become rectangular cutouts. Empty list — no cutouts.
        /// </summary>
        /// <param name="targets">Highlight targets.</param>
        public void Highlight(IReadOnlyList<Graphic> targets)
        {
            _rectTargets.Clear();
            _maskRegion = null;
            _maskSprite = null;

            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    Graphic graphic = targets[i];
                    if (graphic == null)
                    {
                        continue;
                    }

                    // First Image with a sprite -> shaped mask; the rest -> rectangular cutouts.
                    if (_maskRegion == null && graphic is Image image && image.sprite != null)
                    {
                        _maskRegion = image.rectTransform;
                        _maskSprite = image.sprite;
                        ApplyMaskTexture();
                    }
                    else if (_rectTargets.Count < MAX_HOLES)
                    {
                        _rectTargets.Add(graphic.rectTransform);
                    }
                }
            }

            Recompute();
        }

        /// <summary>Highlights regions as rectangular cutouts by their <see cref="RectTransform"/> (no shaped mask).</summary>
        /// <param name="regions">Rectangular regions for cutouts.</param>
        public void Highlight(IReadOnlyList<RectTransform> regions)
        {
            _rectTargets.Clear();
            _maskRegion = null;
            _maskSprite = null;

            if (regions != null)
            {
                for (int i = 0; i < regions.Count; i++)
                {
                    if (regions[i] != null && _rectTargets.Count < MAX_HOLES)
                    {
                        _rectTargets.Add(regions[i]);
                    }
                }
            }

            Recompute();
        }

        /// <summary>
        /// Raises elements above the dim (without cutouts): each element gets a <c>Canvas</c>
        /// (<c>overrideSorting</c>, order above the dimmer) + <c>GraphicRaycaster</c>. The element
        /// renders fully above the dim and takes clicks directly. Replaces the previous raised set.
        /// </summary>
        /// <param name="elements">Elements to raise above the dim.</param>
        public void RaiseAbove(IReadOnlyList<RectTransform> elements)
        {
            RestoreRaised();

            if (elements == null)
            {
                return;
            }

            int order = _dimmer != null && _dimmer.canvas != null
                ? _dimmer.canvas.sortingOrder + RAISED_ORDER_OFFSET
                : RAISED_ORDER_FALLBACK;

            for (int i = 0; i < elements.Count; i++)
            {
                RectTransform element = elements[i];
                if (element == null)
                {
                    continue;
                }

                Canvas canvas = element.GetComponent<Canvas>();
                bool addedCanvas = canvas == null;
                if (addedCanvas)
                {
                    canvas = element.gameObject.AddComponent<Canvas>();
                }

                bool prevOverride = canvas.overrideSorting;
                int prevOrder = canvas.sortingOrder;
                canvas.overrideSorting = true;
                canvas.sortingOrder = order;

                GraphicRaycaster raycaster = element.GetComponent<GraphicRaycaster>();
                bool addedRaycaster = raycaster == null;
                if (addedRaycaster)
                {
                    raycaster = element.gameObject.AddComponent<GraphicRaycaster>();
                }

                _raised.Add(new RaisedElement
                {
                    Canvas = canvas,
                    Raycaster = raycaster,
                    AddedCanvas = addedCanvas,
                    AddedRaycaster = addedRaycaster,
                    PreviousOverride = prevOverride,
                    PreviousOrder = prevOrder,
                });
            }
        }

        // Lowers raised elements back under the dim: removes added components (raycaster first since
        // GraphicRaycaster requires Canvas, then canvas) and restores overrideSorting / order on
        // pre-existing canvases.
        private void RestoreRaised()
        {
            for (int i = 0; i < _raised.Count; i++)
            {
                RaisedElement raised = _raised[i];
                if (raised.AddedRaycaster && raised.Raycaster != null)
                {
                    Destroy(raised.Raycaster);
                }

                if (raised.Canvas == null)
                {
                    continue;
                }

                if (raised.AddedCanvas)
                {
                    Destroy(raised.Canvas);
                }
                else
                {
                    raised.Canvas.overrideSorting = raised.PreviousOverride;
                    raised.Canvas.sortingOrder = raised.PreviousOrder;
                }
            }

            _raised.Clear();
        }

        /// <summary>Clears highlights (all cutouts, mask, and raised elements).</summary>
        public void ClearHighlight()
        {
            RestoreRaised();
            _rectTargets.Clear();
            _maskRegion = null;
            _maskSprite = null;
            _holeCount = 0;
            ApplyHoles();
            ApplyMaskDisabled();
        }

        /// <summary>Waits for a click on the dim (outside cutouts) — "advance".</summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task that completes on click.</returns>
        public UniTask WaitForAdvanceClick(CancellationToken token)
        {
            _advanceSource = new UniTaskCompletionSource();
            return _advanceSource.Task.AttachExternalCancellation(token);
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            _advanceSource?.TrySetResult();
        }

        /// <inheritdoc />
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            bool hasMask = _maskRegion != null && _maskSprite != null;
            if (_holeCount == 0 && !hasMask)
            {
                return true;
            }

            RectTransform rect = _dimmer.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out Vector2 local))
            {
                return true;
            }

            Vector2 uv = ToUv(rect, local);
            for (int i = 0; i < _holeCount; i++)
            {
                Vector4 hole = _holes[i];
                if (uv.x >= hole.x && uv.x <= hole.z && uv.y >= hole.y && uv.y <= hole.w)
                {
                    return false;
                }
            }

            if (hasMask &&
                uv.x >= _maskRectUv.x && uv.x <= _maskRectUv.z &&
                uv.y >= _maskRectUv.y && uv.y <= _maskRectUv.w)
            {
                return false;
            }

            return true;
        }

        private void Recompute()
        {
            RectTransform dimRect = _dimmer.rectTransform;

            _holeCount = 0;
            for (int i = 0; i < _rectTargets.Count && _holeCount < MAX_HOLES; i++)
            {
                RectTransform target = _rectTargets[i];
                if (target == null)
                {
                    continue;
                }

                // AABB over ALL 4 corners — otherwise a rotated (fan-arranged) card produces a skewed /
                // partial cutout (2 corners do not cover a tilted rectangle). This way the cutout is
                // an axis-aligned rectangle slightly larger than the card, fully covering it.
                target.GetWorldCorners(_worldCorners);
                float minX = float.PositiveInfinity;
                float minY = float.PositiveInfinity;
                float maxX = float.NegativeInfinity;
                float maxY = float.NegativeInfinity;
                for (int c = 0; c < 4; c++)
                {
                    Vector2 uv = ToUv(dimRect, dimRect.InverseTransformPoint(_worldCorners[c]));
                    minX = Mathf.Min(minX, uv.x);
                    minY = Mathf.Min(minY, uv.y);
                    maxX = Mathf.Max(maxX, uv.x);
                    maxY = Mathf.Max(maxY, uv.y);
                }

                _holes[_holeCount] = new Vector4(minX, minY, maxX, maxY);
                _holeCount++;
            }

            ApplyHoles();

            if (_maskRegion != null && _material != null)
            {
                _maskRegion.GetWorldCorners(_worldCorners);
                Vector2 min = ToUv(dimRect, dimRect.InverseTransformPoint(_worldCorners[0]));
                Vector2 max = ToUv(dimRect, dimRect.InverseTransformPoint(_worldCorners[2]));
                _maskRectUv = new Vector4(
                    Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
                    Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
                _material.SetVector(MaskRectId, _maskRectUv);
                _material.SetInt(MaskEnabledId, 1);
            }
        }

        private void ApplyMaskTexture()
        {
            if (_material == null || _maskSprite == null)
            {
                return;
            }

            Texture texture = _maskSprite.texture;
            Rect textureRect = _maskSprite.textureRect;
            _material.SetTexture(MaskTexId, texture);
            _material.SetVector(MaskUvId, new Vector4(
                textureRect.xMin / texture.width, textureRect.yMin / texture.height,
                textureRect.xMax / texture.width, textureRect.yMax / texture.height));
        }

        private void ApplyMaskDisabled()
        {
            if (_material != null)
            {
                _material.SetInt(MaskEnabledId, 0);
            }
        }

        private static Vector2 ToUv(RectTransform rect, Vector2 localPoint)
        {
            Rect r = rect.rect;
            return new Vector2(
                Mathf.InverseLerp(r.xMin, r.xMax, localPoint.x),
                Mathf.InverseLerp(r.yMin, r.yMax, localPoint.y));
        }

        private void ApplyHoles()
        {
            if (_material == null)
            {
                return;
            }

            _material.SetVectorArray(HolesId, _holes);
            _material.SetInt(HoleCountId, _holeCount);
        }
    }
}
