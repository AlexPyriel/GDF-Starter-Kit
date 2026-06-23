using Assets.Project.Core.Infrastructure.UI.Enums;

namespace Assets.Project.Core.Infrastructure.UI.Interfaces
{
    /// <summary>
    /// Marks a view as a panel managed by the panel service layer.
    /// </summary>
    public interface IUIPanel : IView
    {
        /// <summary>
        /// Gets how the panel should be layered and dimmed relative to the active screen.
        /// </summary>
        EUIPanelMode Mode { get; }
    }
}
