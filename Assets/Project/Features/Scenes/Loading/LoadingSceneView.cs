using Assets.Project.Core.Infrastructure.UI.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Features.Scenes.Loading
{
    /// <summary>
    /// Authored loading screen view that displays progress feedback for scene transitions.
    /// </summary>
    public sealed class LoadingSceneView : ScreenView
    {
        [SerializeField] private Image _progressBar;
        [SerializeField] private TMP_Text _progressLabel;

        private void Awake()
        {
            if (_progressBar == null)
                throw new MissingReferenceException($"{nameof(LoadingSceneView)} requires {nameof(_progressBar)} to be assigned.");

            if (_progressLabel == null)
                throw new MissingReferenceException($"{nameof(LoadingSceneView)} requires {nameof(_progressLabel)} to be assigned.");
        }

        /// <summary>
        /// Resets the loading progress UI back to its initial empty state.
        /// </summary>
        public void ResetState()
        {
            SetProgress(0f);
        }

        /// <summary>
        /// Updates the loading progress UI using a normalized progress value.
        /// </summary>
        /// <param name="progress">The normalized loading progress in 0..1 range.</param>
        public void SetProgress(float progress)
        {
            float normalizedProgress = Mathf.Clamp01(progress);
            _progressBar.fillAmount = normalizedProgress;
            _progressLabel.text = $"{Mathf.RoundToInt(normalizedProgress * 100f)}%";
        }
    }
}
