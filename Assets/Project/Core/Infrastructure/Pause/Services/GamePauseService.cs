using System;
using System.Collections.Generic;
using Assets.Project.Core.Infrastructure.Pause.Interfaces;
using Assets.Project.Core.Infrastructure.Pause.Messages;
using Assets.Project.Core.Infrastructure.SceneLoading.Messages;
using MessagePipe;
using UnityEngine;

namespace Assets.Project.Core.Infrastructure.Pause.Services
{
    /// <inheritdoc cref="IGamePauseService" />
    /// <remarks>
    /// Root singleton. A scene that activates while handles are still held logs a warning with the held
    /// reasons instead of resetting the pause: a leaked handle is a bug in its owner, and a silent reset
    /// would hide it.
    /// </remarks>
    public sealed class GamePauseService : IGamePauseService, IDisposable
    {
        private readonly IPublisher<GamePausedMessage> _pausedPublisher;
        private readonly IPublisher<GameResumedMessage> _resumedPublisher;
        private readonly IDisposable _sceneActivatedSubscription;
        private readonly List<PauseHandle> _handles = new List<PauseHandle>();
        private float _timeScaleBeforePause = 1f;

        /// <summary>Creates the pause service.</summary>
        /// <param name="pausedPublisher">Channel for <see cref="GamePausedMessage"/>.</param>
        /// <param name="resumedPublisher">Channel for <see cref="GameResumedMessage"/>.</param>
        /// <param name="sceneActivatedSubscriber">Scene activation channel, used to report pauses that outlive their scene.</param>
        /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
        public GamePauseService(
            IPublisher<GamePausedMessage> pausedPublisher,
            IPublisher<GameResumedMessage> resumedPublisher,
            ISubscriber<SceneActivatedMessage> sceneActivatedSubscriber)
        {
            _pausedPublisher = pausedPublisher ?? throw new ArgumentNullException(nameof(pausedPublisher));
            _resumedPublisher = resumedPublisher ?? throw new ArgumentNullException(nameof(resumedPublisher));
            if (sceneActivatedSubscriber == null)
                throw new ArgumentNullException(nameof(sceneActivatedSubscriber));

            _sceneActivatedSubscription = sceneActivatedSubscriber.Subscribe(OnSceneActivated);
        }

        /// <inheritdoc />
        public bool IsPaused => _handles.Count > 0;

        /// <inheritdoc />
        public IDisposable Pause(string reason)
        {
            PauseHandle handle = new PauseHandle(this, string.IsNullOrEmpty(reason) ? "(unnamed)" : reason);
            _handles.Add(handle);

            if (_handles.Count == 1)
            {
                _timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
                AudioListener.pause = true;
                _pausedPublisher.Publish(new GamePausedMessage());
            }

            return handle;
        }

        /// <summary>Stops listening for scene activation. Held handles stay valid and still resume the game.</summary>
        public void Dispose()
        {
            _sceneActivatedSubscription.Dispose();
        }

        private void Release(PauseHandle handle)
        {
            if (!_handles.Remove(handle))
                return;

            if (_handles.Count > 0)
                return;

            Time.timeScale = _timeScaleBeforePause;
            AudioListener.pause = false;
            _resumedPublisher.Publish(new GameResumedMessage());
        }

        private void OnSceneActivated(SceneActivatedMessage message)
        {
            if (_handles.Count == 0)
                return;

            List<string> reasons = new List<string>(_handles.Count);
            foreach (PauseHandle handle in _handles)
                reasons.Add(handle.Reason);

            Debug.LogWarning(
                $"[{nameof(GamePauseService)}] Scene '{message.SceneType}' activated while the game is still paused by: " +
                $"{string.Join(", ", reasons)}. Dispose the pause handle when its owner closes.");
        }

        private sealed class PauseHandle : IDisposable
        {
            private GamePauseService _owner;

            public PauseHandle(GamePauseService owner, string reason)
            {
                _owner = owner;
                Reason = reason;
            }

            public string Reason { get; }

            public void Dispose()
            {
                GamePauseService owner = _owner;
                _owner = null;
                owner?.Release(this);
            }
        }
    }
}
