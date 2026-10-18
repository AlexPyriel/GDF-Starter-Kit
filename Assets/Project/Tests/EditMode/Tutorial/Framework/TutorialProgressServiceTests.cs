using System.Collections.Generic;
using Assets.Project.Features.Tutorial.Progress;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Project.Tests.EditMode.Tutorial.Framework
{
    /// <summary>
    /// Exercises <see cref="ITutorialProgressService"/> on <see cref="PlayerPrefsTutorialProgressService"/>:
    /// completion, resume cursor, single-tutorial reset and reset of all known keys. The real
    /// <see cref="TutorialKeys.All"/> values are snapshotted in <see cref="SetUp"/> and restored in
    /// <see cref="TearDown"/> so tests do not disturb project PlayerPrefs.
    /// </summary>
    public sealed class TutorialProgressServiceTests
    {
        private const string KEY_A = "test.tutorial.alpha";
        private const string KEY_B = "test.tutorial.beta";

        private PlayerPrefsTutorialProgressService _service;
        private Dictionary<string, (bool hasCompleted, int completed, bool hasCursor, int cursor)> _snapshot;

        [SetUp]
        public void SetUp()
        {
            _service = new PlayerPrefsTutorialProgressService();

            _snapshot = new Dictionary<string, (bool, int, bool, int)>();
            foreach (string key in TutorialKeys.All)
            {
                string completedKey = PlayerPrefsTutorialProgressService.ToPrefsKey(key);
                string cursorKey = PlayerPrefsTutorialProgressService.ToCursorKey(key);
                _snapshot[key] = (
                    PlayerPrefs.HasKey(completedKey), PlayerPrefs.GetInt(completedKey, 0),
                    PlayerPrefs.HasKey(cursorKey), PlayerPrefs.GetInt(cursorKey, 0));
            }

            ClearTestKeys();
        }

        [TearDown]
        public void TearDown()
        {
            ClearTestKeys();

            foreach (KeyValuePair<string, (bool hasCompleted, int completed, bool hasCursor, int cursor)> entry in _snapshot)
            {
                string completedKey = PlayerPrefsTutorialProgressService.ToPrefsKey(entry.Key);
                string cursorKey = PlayerPrefsTutorialProgressService.ToCursorKey(entry.Key);

                if (entry.Value.hasCompleted)
                    PlayerPrefs.SetInt(completedKey, entry.Value.completed);
                else
                    PlayerPrefs.DeleteKey(completedKey);

                if (entry.Value.hasCursor)
                    PlayerPrefs.SetInt(cursorKey, entry.Value.cursor);
                else
                    PlayerPrefs.DeleteKey(cursorKey);
            }

            PlayerPrefs.Save();
        }

        private void ClearTestKeys()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsTutorialProgressService.ToPrefsKey(KEY_A));
            PlayerPrefs.DeleteKey(PlayerPrefsTutorialProgressService.ToCursorKey(KEY_A));
            PlayerPrefs.DeleteKey(PlayerPrefsTutorialProgressService.ToPrefsKey(KEY_B));
            PlayerPrefs.DeleteKey(PlayerPrefsTutorialProgressService.ToCursorKey(KEY_B));
            PlayerPrefs.Save();
        }

        [Test]
        public void IsCompleted_FreshKey_False()
        {
            Assert.IsFalse(_service.IsCompleted(KEY_A));
        }

        [Test]
        public void MarkCompleted_ThenIsCompleted_True()
        {
            _service.MarkCompleted(KEY_A);

            Assert.IsTrue(_service.IsCompleted(KEY_A));
            Assert.IsFalse(_service.IsCompleted(KEY_B), "Flag must not leak across keys.");
        }

        [Test]
        public void GetCursor_FreshKey_Zero()
        {
            Assert.AreEqual(0, _service.GetCursor(KEY_A));
        }

        [Test]
        public void SetCursor_ThenGetCursor_ReturnsValue()
        {
            _service.SetCursor(KEY_A, 3);

            Assert.AreEqual(3, _service.GetCursor(KEY_A));
            Assert.AreEqual(0, _service.GetCursor(KEY_B), "Cursor must not leak across keys.");
        }

        [Test]
        public void SetCursor_Overwrite_KeepsLatest()
        {
            _service.SetCursor(KEY_A, 3);
            _service.SetCursor(KEY_A, 5);

            Assert.AreEqual(5, _service.GetCursor(KEY_A));
        }

        [Test]
        public void Reset_ClearsCompletionAndCursor()
        {
            _service.MarkCompleted(KEY_A);
            _service.SetCursor(KEY_A, 4);

            _service.Reset(KEY_A);

            Assert.IsFalse(_service.IsCompleted(KEY_A));
            Assert.AreEqual(0, _service.GetCursor(KEY_A));
        }

        [Test]
        public void ResetAll_ClearsEveryKnownKey()
        {
            foreach (string key in TutorialKeys.All)
            {
                _service.MarkCompleted(key);
                _service.SetCursor(key, 2);
            }

            _service.ResetAll();

            foreach (string key in TutorialKeys.All)
            {
                Assert.IsFalse(_service.IsCompleted(key), "Key not reset: " + key);
                Assert.AreEqual(0, _service.GetCursor(key), "Cursor not reset: " + key);
            }
        }
    }
}
