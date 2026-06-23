namespace Assets.Project.Core.Infrastructure.UI.Enums
{
    /// <summary>
    /// Defines how a panel should behave relative to the active screen.
    /// </summary>
    public enum EUIPanelMode
    {
        /// <summary>
        /// Keeps the panel inline without forcing a blocking backdrop.
        /// </summary>
        Inline = 0,

        /// <summary>
        /// Shows the panel as a standard modal above the active screen.
        /// </summary>
        Modal = 1,

        /// <summary>
        /// Shows the panel in the overlay layer above regular modal content.
        /// </summary>
        Overlay = 2
    }
}
