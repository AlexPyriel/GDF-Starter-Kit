using System;
using Assets.Project.Core.Infrastructure.UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Features.Scenes.Game
{
    /// <summary>
    /// Confirmation panel shown before leaving the gameplay scene.
    /// </summary>
    public sealed class GameExitConfirmPanel : PanelView
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _confirmButton;

        private Action _backAction;
        private Action _confirmAction;

        /// <summary>
        /// Assigns the actions invoked by the back and confirm buttons.
        /// </summary>
        /// <param name="backAction">The action invoked when the user cancels the exit flow.</param>
        /// <param name="confirmAction">The action invoked when the user confirms the exit flow.</param>
        public void Configure(Action backAction, Action confirmAction)
        {
            _backAction = backAction;
            _confirmAction = confirmAction;
        }

        private void Awake()
        {
            _backButton.onClick.AddListener(HandleBackClicked);
            _confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        private void OnDestroy()
        {
            _backButton.onClick.RemoveListener(HandleBackClicked);
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        private void HandleBackClicked()
        {
            _backAction?.Invoke();
        }

        private void HandleConfirmClicked()
        {
            _confirmAction?.Invoke();
        }
    }
}
