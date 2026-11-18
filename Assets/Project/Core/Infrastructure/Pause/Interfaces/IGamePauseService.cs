using System;

namespace Assets.Project.Core.Infrastructure.Pause.Interfaces
{
    /// <summary>
    /// Freezes and resumes local game time for the whole application. Callers take a pause handle and
    /// dispose it when their reason to pause ends; the game stays paused while at least one handle is
    /// held, so a pause menu, a rewarded ad and a dialog can overlap without resuming each other.
    /// </summary>
    /// <remarks>
    /// The first handle sets <c>Time.timeScale = 0</c> and <c>AudioListener.pause = true</c>, then publishes
    /// <see cref="Messages.GamePausedMessage"/>. Releasing the last handle restores the previous time scale,
    /// unpauses the listener, then publishes <see cref="Messages.GameResumedMessage"/>. Online games that
    /// must not freeze locally simply do not call <see cref="Pause"/> and only open their menu.
    /// </remarks>
    public interface IGamePauseService
    {
        /// <summary><see langword="true"/> while at least one pause handle is held.</summary>
        public bool IsPaused { get; }

        /// <summary>Takes a pause handle; the game stays paused until every handle is disposed.</summary>
        /// <param name="reason">Short label of who paused the game, reported in diagnostics (for example <c>"PauseMenu"</c>).</param>
        /// <returns>Handle that releases this pause on <see cref="IDisposable.Dispose"/>; disposing it twice is safe.</returns>
        public IDisposable Pause(string reason);
    }
}
