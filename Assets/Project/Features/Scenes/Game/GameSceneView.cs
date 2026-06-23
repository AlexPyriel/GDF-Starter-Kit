using Assets.Project.Core.Infrastructure.UI.Views;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Features.Scenes.Game
{
    /// <summary>
    /// Authored gameplay screen view that exposes the pause action button.
    /// </summary>
    public sealed class GameSceneView : ScreenView
    {
        [SerializeField]
        private Button _button;

        /// <summary>
        /// Gets the reactive stream that fires when the pause button is clicked.
        /// </summary>
        public Observable<Unit> PauseButtonClicked => _button.OnClickAsObservable();

        private void Awake()
        {
            if (_button == null)
                throw new MissingReferenceException($"{nameof(GameSceneView)} requires {nameof(_button)} to be assigned.");
        }
    }
}
