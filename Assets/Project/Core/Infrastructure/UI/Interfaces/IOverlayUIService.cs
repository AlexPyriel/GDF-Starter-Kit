using Cysharp.Threading.Tasks;

namespace Assets.Project.Core.Infrastructure.UI.Interfaces
{
    /// <summary>
    /// Exposes the minimal overlay host contract shared by scene-local and global UI services.
    /// </summary>
    public interface IOverlayUIService
    {
        /// <summary>
        /// Dismisses the top-most visible panel managed by the overlay host.
        /// </summary>
        /// <returns>A task that completes when the top panel has been dismissed.</returns>
        public UniTask DismissTopPanelAsync();
    }
}
