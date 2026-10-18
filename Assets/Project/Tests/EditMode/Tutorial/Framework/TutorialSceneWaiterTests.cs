using System;
using System.Collections;
using System.Threading;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Services;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using VContainer;

namespace Assets.Project.Tests.EditMode.Tutorial.Framework
{
    /// <summary>
    /// Exercises <see cref="ActiveSceneScopeHolder"/> and <see cref="TutorialSceneWaiter"/>: active
    /// state, immediate/deferred wait completion, foreign scene ignore, cancellation, and the key
    /// invariant: the resolver is set BEFORE the active-scene event is raised.
    /// </summary>
    public sealed class TutorialSceneWaiterTests
    {
        private ActiveSceneScopeHolder _holder;
        private TutorialSceneWaiter _waiter;

        [SetUp]
        public void SetUp()
        {
            _holder = new ActiveSceneScopeHolder();
            _waiter = new TutorialSceneWaiter(_holder);
        }

        [Test]
        public void Ctor_NullHolder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TutorialSceneWaiter(null));
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
        public void SetActive_ResolverAvailableBeforeEvent()
        {
            IObjectResolver resolver = new ContainerBuilder().Build();
            IObjectResolver resolverAtEvent = null;
            ESceneType? sceneAtEvent = null;
            _holder.ActiveSceneChanged += _ =>
            {
                resolverAtEvent = _holder.ActiveResolver;
                sceneAtEvent = _holder.ActiveScene;
            };

            _holder.SetActive(ESceneType.Game, resolver);

            Assert.AreSame(resolver, resolverAtEvent, "Resolver must be set BEFORE the event.");
            Assert.AreEqual(ESceneType.Game, sceneAtEvent, "Scene must be set BEFORE the event.");
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
