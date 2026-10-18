using System.Collections.Generic;
using Assets.Project.Features.Tutorial.Framework.Enums;
using UnityEngine.UI;

namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// Highlight source: a named group of UI elements the tutorial spotlights with a cutout. Authored
    /// by designers in the editor (a component on the element) — key (enum dropdown) + target list.
    /// A step references the same key.
    /// </summary>
    public interface ITutorialHighlightSource
    {
        /// <summary>Group key (the step uses this to find the highlight).</summary>
        public ETutorialHighlightKey HighlightKey { get; }

        /// <summary>Elements to highlight (cutout shape follows the element sprite, otherwise rectangular).</summary>
        public IReadOnlyList<Graphic> HighlightTargets { get; }
    }
}
