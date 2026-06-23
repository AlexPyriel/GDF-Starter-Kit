using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Project.Core.Infrastructure.UI.Interfaces
{
    /// <summary>
    /// Exposes the minimal overlay host contract shared by scene-local and global UI services.
    /// </summary>
    public interface IOverlayUIService
    {
        /// <summary>
        /// Requests tutorial focus framing for the specified UI target.
        /// </summary>
        event Action<RectTransform> TutorialFocusRequested;

        /// <summary>
        /// Requests clearing the currently highlighted tutorial focus target.
        /// </summary>
        event Action TutorialFocusCleared;

        /// <summary>
        /// Requests updating the overlay dimmer alpha.
        /// </summary>
        event Action<float> OverlayAlphaRequested;

        /// <summary>
        /// Dismisses the top-most visible panel managed by the overlay host.
        /// </summary>
        /// <returns>A task that completes when the top panel has been dismissed.</returns>
        UniTask DismissTopPanelAsync();
    }
}
