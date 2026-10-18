using System;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Steps.Base;
using UnityEngine;

namespace Assets.Project.Features.Tutorial.Framework.Config
{
    /// <summary>
    /// Tutorial list entry: SO step reference + positional flags (skip/checkpoint). Serialized inline
    /// in <c>TutorialDefinition._steps</c> — designers set the flags right next to the step in the
    /// list, without opening the step asset. Holds only an object reference and two bools, so there
    /// are no <c>[SerializeReference]</c>-level conflicts.
    /// </summary>
    [Serializable]
    public sealed class TutorialStepEntry : ITutorialStepEntry
    {
        [Tooltip("SO step asset (its behavior and target scene). Created via Create/Project/Tutorial/Steps/… and dragged here.")]
        [SerializeField] private TutorialStepAsset _step;

        [Tooltip("Debug skip: runner skips this step. For running only the steps you need during development.")]
        [SerializeField] private bool _isDisabled;

        [Tooltip("Checkpoint: after a successful step the resume cursor is saved — the step is not replayed on re-entry. " +
                 "Set ONLY on clean boundaries without transient state (for example, scene transitions).")]
        [SerializeField] private bool _shouldSaveProgress;

        /// <inheritdoc />
        public ITutorialStep Step => _step;

        /// <inheritdoc />
        public bool IsDisabled => _isDisabled;

        /// <inheritdoc />
        public bool ShouldSaveProgress => _shouldSaveProgress;
    }
}
