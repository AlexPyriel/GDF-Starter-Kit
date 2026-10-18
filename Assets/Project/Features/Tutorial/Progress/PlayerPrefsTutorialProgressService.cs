using System;
using UnityEngine;

namespace Assets.Project.Features.Tutorial.Progress
{
    /// <summary>
    /// PlayerPrefs implementation of <see cref="ITutorialProgressService"/>: completion is stored as
    /// an <c>int 1</c> under <see cref="ToPrefsKey"/>. Default for the starter kit; swap for a
    /// game-save-backed implementation when a concrete game is ready.
    /// </summary>
    public sealed class PlayerPrefsTutorialProgressService : ITutorialProgressService
    {
        /// <summary>PlayerPrefs key prefix for tutorial completion flags.</summary>
        public const string PREFS_KEY_PREFIX = "tutorial.completed.";

        /// <summary>PlayerPrefs key prefix for step-by-step tutorial resume cursors.</summary>
        public const string CURSOR_KEY_PREFIX = "tutorial.cursor.";

        /// <summary>PlayerPrefs key holding the tutorial completion flag.</summary>
        /// <param name="tutorialKey">Tutorial key from <see cref="TutorialKeys"/>.</param>
        /// <returns>PlayerPrefs key.</returns>
        /// <exception cref="ArgumentException">Key is empty.</exception>
        public static string ToPrefsKey(string tutorialKey)
        {
            if (string.IsNullOrWhiteSpace(tutorialKey))
                throw new ArgumentException("Tutorial key must not be empty.", nameof(tutorialKey));

            return PREFS_KEY_PREFIX + tutorialKey;
        }

        /// <summary>PlayerPrefs key holding the tutorial resume cursor.</summary>
        /// <param name="tutorialKey">Tutorial key from <see cref="TutorialKeys"/>.</param>
        /// <returns>PlayerPrefs key.</returns>
        /// <exception cref="ArgumentException">Key is empty.</exception>
        public static string ToCursorKey(string tutorialKey)
        {
            if (string.IsNullOrWhiteSpace(tutorialKey))
                throw new ArgumentException("Tutorial key must not be empty.", nameof(tutorialKey));

            return CURSOR_KEY_PREFIX + tutorialKey;
        }

        /// <inheritdoc />
        public bool IsCompleted(string tutorialKey)
        {
            return PlayerPrefs.GetInt(ToPrefsKey(tutorialKey), 0) == 1;
        }

        /// <inheritdoc />
        public void MarkCompleted(string tutorialKey)
        {
            PlayerPrefs.SetInt(ToPrefsKey(tutorialKey), 1);
            PlayerPrefs.Save();
        }

        /// <inheritdoc />
        public int GetCursor(string tutorialKey)
        {
            return PlayerPrefs.GetInt(ToCursorKey(tutorialKey), 0);
        }

        /// <inheritdoc />
        public void SetCursor(string tutorialKey, int stepIndex)
        {
            PlayerPrefs.SetInt(ToCursorKey(tutorialKey), stepIndex);
            PlayerPrefs.Save();
        }

        /// <inheritdoc />
        public void Reset(string tutorialKey)
        {
            PlayerPrefs.DeleteKey(ToPrefsKey(tutorialKey));
            PlayerPrefs.DeleteKey(ToCursorKey(tutorialKey));
            PlayerPrefs.Save();
        }

        /// <inheritdoc />
        public void ResetAll()
        {
            foreach (string key in TutorialKeys.All)
            {
                PlayerPrefs.DeleteKey(ToPrefsKey(key));
                PlayerPrefs.DeleteKey(ToCursorKey(key));
            }

            PlayerPrefs.Save();
        }
    }
}
