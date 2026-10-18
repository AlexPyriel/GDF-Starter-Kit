using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Project.Core.Infrastructure.UI.Views
{
    /// <summary>
    /// Reusable modal yes/no confirmation panel with configurable title, body,
    /// and button labels. Texts and actions are supplied by the caller through
    /// <see cref="Configure"/>, so a single prefab can serve every confirmation
    /// use case in a game (quit, overwrite save, restart, etc.).
    /// </summary>
    public sealed class ConfirmDialogPanel : PanelView
    {
        [SerializeField]
        [Tooltip("Dialog title.")]
        private TMP_Text _title;

        [SerializeField]
        [Tooltip("Dialog body (the explanatory question).")]
        private TMP_Text _body;

        [SerializeField]
        [Tooltip("Confirm button label.")]
        private TMP_Text _confirmLabel;

        [SerializeField]
        [Tooltip("Cancel button label.")]
        private TMP_Text _cancelLabel;

        [SerializeField]
        [Tooltip("Cancel button (close without action).")]
        private Button _backButton;

        [SerializeField]
        [Tooltip("Confirm button (perform the action).")]
        private Button _confirmButton;

        private Action _backAction;
        private Action _confirmAction;
        private Action _dismissedAction;
        private bool _answered;

        /// <summary>
        /// Configures the dialog: texts and button actions. Null/unassigned text
        /// fields are skipped, preserving the prefab's authored layout. Calling
        /// this on an already-visible panel reconfigures it for a new question
        /// and resets the visual button state.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="body">Explanatory question.</param>
        /// <param name="confirmLabel">Confirm button label.</param>
        /// <param name="cancelLabel">Cancel button label.</param>
        /// <param name="onConfirm">Action invoked when confirm is pressed.</param>
        /// <param name="onCancel">Action invoked when cancel is pressed.</param>
        /// <param name="onDismissed">
        /// Action invoked if the panel is hidden without a button press (click
        /// outside, scene unload). Pass <see langword="null"/> if not needed.
        /// </param>
        public void Configure(
            string title,
            string body,
            string confirmLabel,
            string cancelLabel,
            Action onConfirm,
            Action onCancel,
            Action onDismissed = null)
        {
            SetText(_title, title);
            SetText(_body, body);
            SetText(_confirmLabel, confirmLabel);
            SetText(_cancelLabel, cancelLabel);

            _confirmAction = onConfirm;
            _backAction = onCancel;
            _dismissedAction = onDismissed;
            _answered = false;

            ResetButtonState(_confirmButton);
            ResetButtonState(_backButton);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null && value != null)
            {
                target.text = value;
            }
        }

        private static void ResetButtonState(Selectable selectable)
        {
            if (selectable == null)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject == selectable.gameObject)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void Awake()
        {
            _backButton.onClick.AddListener(HandleBackClicked);
            _confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        private void OnDisable()
        {
            if (_answered)
            {
                return;
            }

            Action dismissed = _dismissedAction;
            _dismissedAction = null;
            dismissed?.Invoke();
        }

        private void OnDestroy()
        {
            _backButton.onClick.RemoveListener(HandleBackClicked);
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        private void HandleBackClicked()
        {
            _answered = true;
            _backAction?.Invoke();
        }

        private void HandleConfirmClicked()
        {
            _answered = true;
            _confirmAction?.Invoke();
        }
    }
}
