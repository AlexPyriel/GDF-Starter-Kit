using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// Tutorial overlay: a blocking dim with cutouts over highlighted targets (clicks inside the
    /// cutout pass through to the target), plus <see cref="ContentRoot"/> on top of the dim where
    /// a step spawns its own panels/messages. Lives as a singleton under the global
    /// <c>UIRoot.TutorialRoot</c> and survives scene transitions. Does not render the message itself.
    /// </summary>
    public interface ITutorialOverlay
    {
        /// <summary>Root for spawning step content (panel/message) above the dim.</summary>
        public RectTransform ContentRoot { get; }

        /// <summary>Shows the overlay (dim over the scene, blocks interaction outside cutouts).</summary>
        public void Show();

        /// <summary>Hides the overlay (clears the dim and cutouts).</summary>
        public void Hide();

        /// <summary>
        /// Highlights targets: the first <c>Image</c> with a sprite becomes a cutout shaped like the
        /// sprite (non-rectangular), the rest become rectangular cutouts. Clicks inside a cutout pass
        /// through to the target.
        /// </summary>
        /// <param name="targets">Highlight targets (typically from a <c>TutorialHighlightGroup</c>).</param>
        public void Highlight(IReadOnlyList<Graphic> targets);

        /// <summary>
        /// Highlights arbitrary regions as rectangular cutouts using their <see cref="RectTransform"/>
        /// (no shaped mask) — for targets without their own graphic (hand zones, containers), or where
        /// a plain rectangular cutout is preferable to a sprite-shaped one.
        /// </summary>
        /// <param name="regions">Rectangular regions to cut out.</param>
        public void Highlight(IReadOnlyList<RectTransform> regions);

        /// <summary>
        /// Raises the given elements ABOVE the dim without cutting a hole: each element's root receives
        /// a <c>Canvas</c> (<c>overrideSorting</c>, order above the dimmer) + <c>GraphicRaycaster</c>,
        /// so the element renders in full on top of the dim (keeping its shape, lift, hover) and
        /// receives clicks directly. Useful for lively highlights of hand cards where a rectangular
        /// cutout over a rotated/raised card looks crude. Cleared by
        /// <see cref="ClearHighlight"/>/<see cref="Hide"/>.
        /// </summary>
        /// <param name="elements">Elements to lift above the dim.</param>
        public void RaiseAbove(IReadOnlyList<RectTransform> elements);

        /// <summary>Clears highlights (all cutouts and raised elements); the dim stays solid.</summary>
        public void ClearHighlight();

        /// <summary>
        /// Sets dim strength — the dimmer alpha (0 — transparent, but clicks outside cutouts are still
        /// blocked). <see cref="Hide"/> restores the prefab's authored value.
        /// </summary>
        /// <param name="alpha">Dimmer alpha in [0, 1].</param>
        public void SetDimAlpha(float alpha);

        /// <summary>Waits for a click on the dim (outside cutouts) — "advance" for message steps.</summary>
        /// <param name="token">Cancellation token for the wait.</param>
        /// <returns>Task that completes on a click into the dim.</returns>
        public UniTask WaitForAdvanceClick(CancellationToken token);
    }
}
