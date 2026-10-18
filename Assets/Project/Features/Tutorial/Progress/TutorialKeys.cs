using System.Collections.Generic;

namespace Assets.Project.Features.Tutorial.Progress
{
    /// <summary>
    /// Tutorial keys for <see cref="ITutorialProgressService"/>. A new tutorial adds a constant here
    /// and an entry in <see cref="All"/> — the editor save-reset menu uses this list to clear flags.
    /// </summary>
    public static class TutorialKeys
    {
        /// <summary>Main step-by-step gameplay tutorial. Also the key for its resume cursor.</summary>
        public const string MAIN = "main";

        private static readonly string[] AllKeys = { MAIN };

        /// <summary>All known tutorial keys.</summary>
        public static IReadOnlyList<string> All => AllKeys;
    }
}
