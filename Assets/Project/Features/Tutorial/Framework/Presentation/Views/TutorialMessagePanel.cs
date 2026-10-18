using TMPro;
using UnityEngine;

namespace Assets.Project.Features.Tutorial.Framework.Presentation.Views
{
    /// <summary>
    /// Simple tutorial message panel: background + text. A step spawns its prefab via
    /// <c>SpawnPanel</c> into <c>Overlay.ContentRoot</c> and sets the text through <see cref="SetText"/>.
    /// A richer common panel can replace it later — this component is intentionally minimal.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialMessagePanel : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Message text label.")]
        private TextMeshProUGUI _label;

        /// <summary>Sets the message text.</summary>
        /// <param name="text">Localized text ready for display.</param>
        public void SetText(string text)
        {
            if (_label != null)
            {
                _label.SetText(text);
            }
        }
    }
}
