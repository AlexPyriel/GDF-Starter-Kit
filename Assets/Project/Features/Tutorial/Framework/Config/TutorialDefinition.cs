using System.Collections.Generic;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using UnityEngine;

namespace Assets.Project.Features.Tutorial.Framework.Config
{
    /// <summary>
    /// Config of one tutorial (MVP — a single tutorial per game): progress identifier, ordered list
    /// of entries (<see cref="TutorialStepEntry"/>: SO step reference + positional skip/checkpoint
    /// flags), and the "do not run" flag. Step behavior lives in its own asset file (so edits do
    /// not conflict between authors); here you set ORDER and positional orchestration only. A
    /// multi-tutorial catalog (<c>ConfigRepository&lt;string, …&gt;</c>) is deferred.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/Tutorial/" + nameof(TutorialDefinition), fileName = nameof(TutorialDefinition), order = 0)]
    public sealed class TutorialDefinition : ScriptableObject, ITutorialDefinition
    {
        [Tooltip("Tutorial identifier — completion key and resume cursor key in ITutorialProgressService (PlayerPrefs).")]
        [SerializeField] private string _tutorialId = "main";

        [Tooltip("Ordered entries. Order = list position, resume cursor = index. Each entry: SO step reference + " +
                 "skip/checkpoint flags (set here next to the step, not in the step asset).")]
        [SerializeField] private List<TutorialStepEntry> _steps = new List<TutorialStepEntry>();

        [Tooltip("Do not run the tutorial: IsCompleted is computed true from this flag WITHOUT writing to PlayerPrefs. " +
                 "Clear the flag to re-enable the tutorial. Editor-only.")]
        [SerializeField] private bool _skipTutorial;

        /// <inheritdoc />
        public string TutorialId => _tutorialId;

        /// <inheritdoc />
        public IReadOnlyList<ITutorialStepEntry> Steps => _steps;

        /// <inheritdoc />
        public bool SkipTutorial => _skipTutorial;
    }
}
