using Assets.Project.Core.Infrastructure.UI.Interfaces;
using UnityEngine;

namespace Assets.Project.Core.Infrastructure.UI.Views
{
    /// <summary>
    /// Base MonoBehaviour-backed implementation of the shared UI view contract.
    /// </summary>
    public abstract class View : MonoBehaviour, IView
    {
        /// <summary>
        /// Makes the view visible by activating its GameObject.
        /// </summary>
        public virtual void Show() => gameObject.SetActive(true);

        /// <summary>
        /// Hides the view by deactivating its GameObject.
        /// </summary>
        public virtual void Hide() => gameObject.SetActive(false);
    }
}
