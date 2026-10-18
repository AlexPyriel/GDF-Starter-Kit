namespace Assets.Project.Features.Tutorial.Framework.Enums
{
    /// <summary>
    /// Highlight group keys. The <c>TutorialHighlightGroup</c> tag component picks a key through an
    /// editor dropdown; a step references the same key — no magic strings, matches guaranteed by the
    /// compiler. A new highlight target = a new value here (one line), then designers place tags in
    /// the editor.
    /// </summary>
    public enum ETutorialHighlightKey
    {
        /// <summary>No group (neutral value; highlights nothing).</summary>
        None = 0,

        // TODO (per-game): add highlight-group values here, for example:
        //   PlayButton = 1,
        //   InventorySlot = 2,
    }
}
