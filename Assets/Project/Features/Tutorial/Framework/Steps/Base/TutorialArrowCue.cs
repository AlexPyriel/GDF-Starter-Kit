using System;
using Assets.Project.Features.Tutorial.Framework.Presentation.Views;
using UnityEngine;

namespace Assets.Project.Features.Tutorial.Framework.Steps.Base
{
    /// <summary>
    /// Tutorial-step arrow as a single editable block: prefab, offset from the target center, and angle.
    /// Kept together so a designer edits the arrow as one unit on the step inspector. The arrow follows
    /// the target (<see cref="TutorialArrowView.PointAt"/>), so the offset is from the target rather
    /// than from the screen center.
    /// </summary>
    [Serializable]
    public struct TutorialArrowCue
    {
        [Tooltip("Arrow prefab (TutorialArrowView). Empty — no arrow (warning in the console).")]
        [SerializeField] private TutorialArrowView _prefab;

        [Tooltip("Arrow offset from the target center, in canvas units (reference 3840x2160). The arrow stays near the target at any aspect ratio.")]
        [SerializeField] private Vector2 _offset;

        [Tooltip("Arrow rotation in degrees counter-clockwise: 0 — down, -90 — left, 90 — right, 180 — up.")]
        [SerializeField] private float _angle;

        /// <summary>Creates an arrow block — for step default values.</summary>
        /// <param name="prefab">Arrow prefab.</param>
        /// <param name="offset">Offset from the target center, in canvas units.</param>
        /// <param name="angle">Rotation in degrees counter-clockwise (0 — down).</param>
        public TutorialArrowCue(TutorialArrowView prefab, Vector2 offset, float angle)
        {
            _prefab = prefab;
            _offset = offset;
            _angle = angle;
        }

        /// <summary>Arrow prefab; <see langword="null"/> — no arrow.</summary>
        public TutorialArrowView Prefab => _prefab;

        /// <summary>Arrow offset from the target center, in canvas units.</summary>
        public Vector2 Offset => _offset;

        /// <summary>Arrow rotation in degrees counter-clockwise (0 — down).</summary>
        public float Angle => _angle;
    }
}
