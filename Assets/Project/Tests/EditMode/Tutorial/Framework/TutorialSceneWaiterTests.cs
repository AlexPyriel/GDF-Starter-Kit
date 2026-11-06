using System;
using System.Collections;
using System.Threading;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Core.Infrastructure.SceneLoading.Messages;
using Assets.Project.Core.Infrastructure.SceneLoading.Services;
using Assets.Project.Features.Tutorial.Framework.Services;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using UnityEngine.TestTools;
using VContainer;

namespace Assets.Project.Tests.EditMode.Tutorial.Framework
{
    /// <summary>
    /// Exercises <see cref="ActiveSceneScopeHolder"/> and <see cref="TutorialSceneWaiter"/>: active
    /// state, immediate/deferred wait completion, foreign scene ignore, cancellation, and the key
    /// invariant: the resolver is set BEFORE <see cref="SceneActivatedMessage"/> is published. Runs on a
    /// real MessagePipe broker.
    /// </summary>
    public sealed class TutorialSceneWaiterTests
    {
        private IObjectResolver _container;
        private ISubscriber<SceneActivatedMessage> _sceneActivated;
        private ActiveSceneScopeHolder _holder;
        private TutorialSceneWaiter _waiter;

        [SetUp]
        public void SetUp()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterMessagePipe();
            _container = builder.Build();
            _sceneActivated = _container.Resolve<ISubscriber<SceneActivatedMessage>>();
            _holder = new ActiveSceneScopeHolder(_container.Resolve<IPublisher<SceneActivatedMessage>>());
            _waiter = new TutorialSceneWaiter(_holder, _sceneActivated);
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }

        [Test]
        public void Ctor_NullDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ActiveSceneScopeHolder(null));
            Assert.Throws<ArgumentNullException>(() => new TutorialSceneWaiter(null, _sceneActivated));
            Assert.Throws<ArgumentNullException>(() => new TutorialSceneWaiter(_holder, null));
        }

        [Test]
        public void IsActive_ReflectsHolder()
        {
            Assert.IsFalse(_waiter.IsActive(ESceneType.Game), "No scene is active before registration.");

            _holder.SetActive(ESceneType.Game, null);

            Assert.IsTrue(_waiter.IsActive(ESceneType.Game));
            Assert.IsFalse(_waiter.IsActive(ESceneType.Home));
        }

        [Test]
        public void SetActive_ResolverAvailableBeforeMessage()
        {
            IObjectResolver resolver = new ContainerBuilder().Build();
            IObjectResolver resolverAtMessage = null;
            ESceneType? sceneAtMessage = null;
            ESceneType? publishedScene = null;
            using IDisposable subscription = _sceneActivated.Subscribe(message =>
            {
                resolverAtMessage = _holder.ActiveResolver;
                sceneAtMessage = _holder.ActiveScene;
                publishedScene = message.SceneType;
            });

            _holder.SetActive(ESceneType.Game, resolver);

            Assert.AreSame(resolver, resolverAtMessage, "Resolver must be set BEFORE the message.");
            Assert.AreEqual(ESceneType.Game, sceneAtMessage, "Scene must be set BEFORE the message.");
            Assert.AreEqual(ESceneType.Game, publishedScene);
            resolver.Dispose();
        }

        [UnityTest]
        public IEnumerator WaitForScene_AlreadyActive_CompletesImmediately() => UniTask.ToCoroutine(async () =>
        {
            _holder.SetActive(ESceneType.Home, null);

            await _waiter.WaitForScene(ESceneType.Home, CancellationToken.None);

            Assert.Pass();
        });

        [UnityTest]
        public IEnumerator WaitForScene_BecomesActive_Completes() => UniTask.ToCoroutine(async () =>
        {
            UniTask task = _waiter.WaitForScene(ESceneType.Game, CancellationToken.None);
            Assert.AreEqual(UniTaskStatus.Pending, task.Status, "While the scene is not active the wait is pending.");

            _holder.SetActive(ESceneType.Game, null);

            await task;
            Assert.Pass();
        });

        [UnityTest]
        public IEnumerator WaitForScene_IgnoresOtherScene_ResolvesOnTarget() => UniTask.ToCoroutine(async () =>
        {
            UniTask task = _waiter.WaitForScene(ESceneType.Game, CancellationToken.None);

            _holder.SetActive(ESceneType.Home, null);
            Assert.AreEqual(UniTaskStatus.Pending, task.Status, "A different scene must not complete the wait.");

            _holder.SetActive(ESceneType.Game, null);

            await task;
            Assert.Pass();
        });

        [UnityTest]
        public IEnumerator WaitForScene_CancelledByToken_Throws() => UniTask.ToCoroutine(async () =>
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            UniTask task = _waiter.WaitForScene(ESceneType.Game, cts.Token);

            cts.Cancel();

            try
            {
                await task;
                Assert.Fail("The wait should have been cancelled by the token.");
            }
            catch (OperationCanceledException)
            {
                Assert.Pass();
            }
            finally
            {
                cts.Dispose();
            }
        });

        [UnityTest]
        public IEnumerator WaitForScene_PreCancelledToken_Throws() => UniTask.ToCoroutine(async () =>
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                await _waiter.WaitForScene(ESceneType.Game, cts.Token);
                Assert.Fail("A pre-cancelled token must throw immediately.");
            }
            catch (OperationCanceledException)
            {
                Assert.Pass();
            }
            finally
            {
                cts.Dispose();
            }
        });
    }
}
