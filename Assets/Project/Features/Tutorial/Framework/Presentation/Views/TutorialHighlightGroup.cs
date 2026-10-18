using System.Collections.Generic;
using Assets.Project.Features.Tutorial.Framework.Enums;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Features.Tutorial.Framework.Presentation.Views
{
    /// <summary>
    /// Designer-facing highlight tag component: placed on any UI object in the editor, assigns a key
    /// and a list of elements to highlight. Registers in <see cref="ITutorialHighlightRegistry"/> on
    /// enable. A tutorial step finds this group by key and cuts holes over its targets (shape — from
    /// the sprite, otherwise rectangle).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialHighlightGroup : MonoBehaviour, ITutorialHighlightSource
    {
        [Tooltip("Group key (dropdown). The step references the same enum value to find this highlight.")]
        [SerializeField] private ETutorialHighlightKey _key;

        [Tooltip("Elements to highlight. The cutout follows the element sprite shape; without a sprite — rectangular.")]
        [SerializeField] private List<Graphic> _targets = new List<Graphic>();

        /// <inheritdoc />
        public ETutorialHighlightKey HighlightKey => _key;

        /// <inheritdoc />
        public IReadOnlyList<Graphic> HighlightTargets => _targets;

        private void OnEnable()
        {
            TutorialHighlightRegistry.Current?.Register(this);
        }

        private void OnDisable()
        {
            TutorialHighlightRegistry.Current?.Unregister(this);
        }
    }
}
