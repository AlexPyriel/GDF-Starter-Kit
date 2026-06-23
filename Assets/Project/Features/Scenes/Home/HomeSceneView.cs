using Assets.Project.Core.Infrastructure.UI.Views;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Features.Scenes.Home
{
    /// <summary>
    /// Authored home screen view that exposes the primary action button.
    /// </summary>
    public sealed class HomeSceneView : ScreenView
    {
        [SerializeField]
        private Button _button;

        /// <summary>
        /// Gets the reactive stream that fires when the main home button is clicked.
        /// </summary>
        public Observable<Unit> ButtonClicked => _button.OnClickAsObservable();

        private void Awake()
        {
            if (_button == null)
                throw new MissingReferenceException($"{nameof(HomeSceneView)} requires {nameof(_button)} to be assigned.");
        }
    }
}
