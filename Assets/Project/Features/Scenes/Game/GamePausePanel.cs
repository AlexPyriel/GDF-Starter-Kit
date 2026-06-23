using System;
using Assets.Project.Core.Infrastructure.UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Features.Scenes.Game
{
    /// <summary>
    /// Minimal pause panel used as the primary in-game modal flow.
    /// </summary>
    public sealed class GamePausePanel : PanelView
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _exitButton;

        private Action _resumeAction;
        private Action _exitAction;

        /// <summary>
        /// Assigns the actions invoked by the resume and exit buttons.
        /// </summary>
        /// <param name="resumeAction">The action invoked when the resume button is clicked.</param>
        /// <param name="exitAction">The action invoked when the exit button is clicked.</param>
        public void Configure(Action resumeAction, Action exitAction)
        {
            _resumeAction = resumeAction;
            _exitAction = exitAction;
        }

        private void Awake()
        {
            _resumeButton.onClick.AddListener(HandleResumeClicked);
            _exitButton.onClick.AddListener(HandleExitClicked);
        }

        private void OnDestroy()
        {
            _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            _exitButton.onClick.RemoveListener(HandleExitClicked);
        }

        private void HandleResumeClicked()
        {
            if (_resumeAction != null)
            {
                _resumeAction.Invoke();
            }
        }

        private void HandleExitClicked()
        {
            _exitAction?.Invoke();
        }
    }
}
