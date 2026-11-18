using System;
using System.Text.RegularExpressions;
using Assets.Project.Core.Infrastructure.Pause.Messages;
using Assets.Project.Core.Infrastructure.Pause.Services;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Core.Infrastructure.SceneLoading.Messages;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace Assets.Project.Tests.EditMode.Pause
{
    /// <summary>
    /// Exercises <see cref="GamePauseService"/> on a real MessagePipe broker: nested handles, time scale
    /// restore, listener pause, one message per transition, idempotent dispose and the leaked-pause warning.
    /// </summary>
    public sealed class GamePauseServiceTests
    {
        private const float CUSTOM_TIME_SCALE = 0.5f;

        private IObjectResolver _resolver;
        private GamePauseService _service;
        private IPublisher<SceneActivatedMessage> _sceneActivatedPublisher;
        private IDisposable _subscriptions;
        private int _pausedCount;
        private int _resumedCount;
        private float _timeScaleAtPausedMessage;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterMessagePipe();
            _resolver = builder.Build();

            _sceneActivatedPublisher = _resolver.Resolve<IPublisher<SceneActivatedMessage>>();
            _service = new GamePauseService(
                _resolver.Resolve<IPublisher<GamePausedMessage>>(),
                _resolver.Resolve<IPublisher<GameResumedMessage>>(),
                _resolver.Resolve<ISubscriber<SceneActivatedMessage>>());

            _pausedCount = 0;
            _resumedCount = 0;
            _timeScaleAtPausedMessage = -1f;
            _subscriptions = DisposableBag.Create(
                _resolver.Resolve<ISubscriber<GamePausedMessage>>().Subscribe(_ =>
                {
                    _pausedCount++;
                    _timeScaleAtPausedMessage = Time.timeScale;
                }),
                _resolver.Resolve<ISubscriber<GameResumedMessage>>().Subscribe(_ => _resumedCount++));
        }

        [TearDown]
        public void TearDown()
        {
            _subscriptions.Dispose();
            _service.Dispose();
            _resolver.Dispose();
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [Test]
        public void Ctor_NullArgument_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GamePauseService(
                null,
                _resolver.Resolve<IPublisher<GameResumedMessage>>(),
                _resolver.Resolve<ISubscriber<SceneActivatedMessage>>()));
        }

        [Test]
        public void Pause_FreezesTimeAndAudio_ThenPublishes()
        {
            _service.Pause("Test");

            Assert.That(_service.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(AudioListener.pause, Is.True);
            Assert.That(_pausedCount, Is.EqualTo(1));
            Assert.That(_timeScaleAtPausedMessage, Is.EqualTo(0f), "State must be applied before the message.");
        }

        [Test]
        public void Release_RestoresPreviousTimeScale()
        {
            Time.timeScale = CUSTOM_TIME_SCALE;
            IDisposable handle = _service.Pause("Test");

            handle.Dispose();

            Assert.That(_service.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(CUSTOM_TIME_SCALE));
            Assert.That(AudioListener.pause, Is.False);
            Assert.That(_resumedCount, Is.EqualTo(1));
        }

        [Test]
        public void NestedHandles_ResumeOnlyAfterLastRelease()
        {
            IDisposable menu = _service.Pause("Menu");
            IDisposable ad = _service.Pause("Ad");

            menu.Dispose();

            Assert.That(_service.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(_pausedCount, Is.EqualTo(1));
            Assert.That(_resumedCount, Is.EqualTo(0));

            ad.Dispose();

            Assert.That(_service.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_resumedCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleDisposedTwice_ReleasesOnce()
        {
            IDisposable first = _service.Pause("First");
            _service.Pause("Second");

            first.Dispose();
            first.Dispose();

            Assert.That(_service.IsPaused, Is.True);
            Assert.That(_resumedCount, Is.EqualTo(0));
        }

        [Test]
        public void SceneActivated_WhilePaused_WarnsAndKeepsPause()
        {
            _service.Pause("LeakedMenu");

            LogAssert.Expect(LogType.Warning, new Regex("LeakedMenu"));
            _sceneActivatedPublisher.Publish(new SceneActivatedMessage(ESceneType.Home));

            Assert.That(_service.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        [Test]
        public void SceneActivated_WhileRunning_DoesNotWarn()
        {
            _sceneActivatedPublisher.Publish(new SceneActivatedMessage(ESceneType.Home));

            LogAssert.NoUnexpectedReceived();
        }
    }
}
