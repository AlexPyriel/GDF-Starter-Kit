using Assets.Project.Core.Infrastructure.UI.Enums;
using Assets.Project.Core.Infrastructure.UI.Interfaces;
using UnityEngine;

namespace Assets.Project.Core.Infrastructure.UI.Views
{
    /// <summary>
    /// Base class for reusable panels and popup-like views.
    /// </summary>
    public abstract class PanelView : View, IUIPanel
    {
        [SerializeField] private EUIPanelMode _mode = EUIPanelMode.Modal;

        /// <inheritdoc />
        public EUIPanelMode Mode => _mode;
    }
}
