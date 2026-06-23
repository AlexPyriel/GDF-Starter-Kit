namespace Assets.Project.Core.Infrastructure.UI.Interfaces
{
    /// <summary>
    /// Defines the minimal contract for a visible UI view.
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// Makes the view visible.
        /// </summary>
        public void Show();

        /// <summary>
        /// Hides the view.
        /// </summary>
        public void Hide();
    }
}
