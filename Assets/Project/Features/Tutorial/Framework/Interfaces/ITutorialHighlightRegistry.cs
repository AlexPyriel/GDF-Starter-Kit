using System.Collections.Generic;
using Assets.Project.Features.Tutorial.Framework.Enums;
using UnityEngine.UI;

namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// Highlight-group registry: tag components register themselves by key on load, a step finds
    /// targets by key. Lives in the root scope (survives scene transitions); scene-local groups come
    /// and go with their scenes.
    /// </summary>
    public interface ITutorialHighlightRegistry
    {
        /// <summary>Registers a highlight source (called by the tag component in OnEnable).</summary>
        /// <param name="source">Highlight source.</param>
        public void Register(ITutorialHighlightSource source);

        /// <summary>Unregisters a highlight source (called by the tag component in OnDisable).</summary>
        /// <param name="source">Highlight source.</param>
        public void Unregister(ITutorialHighlightSource source);

        /// <summary>Returns highlight targets by key.</summary>
        /// <param name="key">Group key.</param>
        /// <param name="targets">Matched targets (empty otherwise).</param>
        /// <returns><see langword="true"/> if a group with that key is registered.</returns>
        public bool TryGetTargets(ETutorialHighlightKey key, out IReadOnlyList<Graphic> targets);
    }
}
