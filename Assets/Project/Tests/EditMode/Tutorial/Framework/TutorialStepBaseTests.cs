using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Assets.Project.Features.Tutorial.Framework.Enums;
using Assets.Project.Features.Tutorial.Framework.Steps.Base;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;

namespace Assets.Project.Tests.EditMode.Tutorial.Framework
{
    /// <summary>
    /// Exercises <see cref="TutorialStepAsset"/>: context injection, auto-Dispose (Track + spawned panels),
    /// key-driven highlight, panel spawn, and the waits (action / advance click / button / event) with
    /// cancellation and unsubscribe. Uses raw-cast enum values because the starter-kit enums ship empty
    /// (per-game clones add real members).
    /// </summary>
    public sealed class TutorialStepBaseTests
    {
        private const ETutorialHighlightKey TEST_HIGHLIGHT_KEY = (ETutorialHighlightKey)1;
        private const ETutorialActionType TEST_ACTION_TYPE = (ETutorialActionType)1;

        private sealed class ProbeStep : TutorialStepAsset
        {
            public int ResetCount { get; private set; }

            protected override UniTask OnExecute(CancellationToken token) => UniTask.CompletedTask;
            protected override void OnDispose() => ResetCount++;

            public TutorialContext ContextPublic => Context;
            public IDisposable TrackPublic(IDisposable disposable) => Track(disposable);
            public void HighlightPublic(ETutorialHighlightKey key) => Highlight(key);
            public T SpawnPanelPublic<T>(T prefab) where T : Component => SpawnPanel(prefab);
            public UniTask WaitForActionPublic(ETutorialActionType a, CancellationToken t, int c = 1) => WaitForAction(a, t, c);
            public UniTask WaitForAdvancePublic(CancellationToken t) => WaitForAdvanceClick(t);
            public UniTask WaitForButtonPublic(Button b, CancellationToken t) => WaitForButtonClick(b, t);
            public UniTask WaitForEventPublic(Action<Action> s, Action<Action> u, CancellationToken t) => WaitForEvent(s, u, t);
            public UniTask<int> WaitForEventArgPublic(Action<Action<int>> s, Action<Action<int>> u, CancellationToken t) => WaitForEvent<int>(s, u, t);
        }

        private sealed class TrackedDisposable : IDisposable
        {
            public bool Disposed { get; private set; }
            public void Dispose() => Disposed = true;
        }

        private List<string> _log;
        private FakeOverlay _overlay;
        private FakeActionBus _actionBus;
        private FakeHighlightRegistry _registry;
        private IObjectResolver _resolver;
        private List<ProbeStep> _probes;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _overlay = new FakeOverlay(_log);
            _actionBus = new FakeActionBus();
            _registry = new FakeHighlightRegistry();
            _probes = new List<ProbeStep>();
            TutorialContext context = new TutorialContext(_overlay, new FakeSceneWaiter(_log), new FakeProgress(_log), _registry, _actionBus);

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(context);
            _resolver = builder.Build();
        }

        [TearDown]
        public void TearDown()
        {
            // Probes not routed through Dispose (which self-destructs the clone) are cleaned up here to avoid SO leaks.
            foreach (ProbeStep probe in _probes)
            {
                if (probe != null)
                {
                    UnityEngine.Object.DestroyImmediate(probe);
                }
            }

            _resolver.Dispose();
            _overlay.Cleanup();
        }

        private ProbeStep InjectedProbe()
        {
            ProbeStep probe = ScriptableObject.CreateInstance<ProbeStep>();
            _probes.Add(probe);
            _resolver.Inject(probe);
            return probe;
        }

        [Test]
        public void Inject_SetsContext()
        {
            ProbeStep probe = InjectedProbe();

            Assert.IsNotNull(probe.ContextPublic);
            Assert.AreSame(_overlay, probe.ContextPublic.Overlay);
        }

        [Test]
        public void Track_DisposedOnDispose_AndOnDisposeCalled()
        {
            ProbeStep probe = InjectedProbe();
            TrackedDisposable tracked = new TrackedDisposable();
            probe.TrackPublic(tracked);

            probe.Dispose();

            Assert.IsTrue(tracked.Disposed, "Tracked resource is released on Dispose.");
            Assert.AreEqual(1, probe.ResetCount, "OnDispose is called.");
        }

        [Test]
        public void Highlight_KnownKey_ShowsAndHighlights()
        {
            ProbeStep probe = InjectedProbe();
            GameObject go = new GameObject("t", typeof(RectTransform), typeof(Image));
            _registry.Set(TEST_HIGHLIGHT_KEY, new Graphic[] { go.GetComponent<Image>() });

            probe.HighlightPublic(TEST_HIGHLIGHT_KEY);

            CollectionAssert.Contains(_log, "overlay:show");
            CollectionAssert.Contains(_log, "overlay:highlight:1");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Highlight_UnknownKey_NoOp()
        {
            ProbeStep probe = InjectedProbe();

            probe.HighlightPublic(TEST_HIGHLIGHT_KEY);

            CollectionAssert.DoesNotContain(_log, "overlay:show");
        }

        [Test]
        public void SpawnPanel_InstantiatesUnderContentRoot_AndDestroyedOnDispose()
        {
            ProbeStep probe = InjectedProbe();
            GameObject prefab = new GameObject("panel", typeof(RectTransform));

            RectTransform instance = probe.SpawnPanelPublic(prefab.GetComponent<RectTransform>());

            Assert.AreEqual(_overlay.ContentRoot, instance.parent, "Panel is spawned under ContentRoot.");
            Assert.AreEqual(1, _overlay.ContentRoot.childCount);

            probe.Dispose();

            Assert.AreEqual(0, _overlay.ContentRoot.childCount, "Panel is destroyed when the step disposes.");
            UnityEngine.Object.DestroyImmediate(prefab);
        }

        [UnityTest]
        public IEnumerator WaitForAction_CompletesWhenPublished() => UniTask.ToCoroutine(async () =>
        {
            ProbeStep probe = InjectedProbe();

            UniTask task = probe.WaitForActionPublic(TEST_ACTION_TYPE, CancellationToken.None);
            Assert.AreEqual(UniTaskStatus.Pending, task.Status);

            _actionBus.Publish(TEST_ACTION_TYPE);

            await task;
            Assert.Pass();
        });

        [UnityTest]
        public IEnumerator WaitForAdvanceClick_CompletesOnOverlayAdvance() => UniTask.ToCoroutine(async () =>
        {
            ProbeStep probe = InjectedProbe();

            UniTask task = probe.WaitForAdvancePublic(CancellationToken.None);
            _overlay.RaiseAdvance();

            await task;
            Assert.Pass();
        });

        [UnityTest]
        public IEnumerator WaitForButtonClick_CompletesOnClick() => UniTask.ToCoroutine(async () =>
        {
            ProbeStep probe = InjectedProbe();
            GameObject go = new GameObject("btn", typeof(RectTransform), typeof(Button));
            Button button = go.GetComponent<Button>();

            UniTask task = probe.WaitForButtonPublic(button, CancellationToken.None);
            button.onClick.Invoke();
            await task;

            UnityEngine.Object.DestroyImmediate(go);
            Assert.Pass();
        });

        [UnityTest]
        public IEnumerator WaitForEvent_ResolvesAndUnsubscribes() => UniTask.ToCoroutine(async () =>
        {
            ProbeStep probe = InjectedProbe();
            Action evt = null;

            UniTask task = probe.WaitForEventPublic(h => evt += h, h => evt -= h, CancellationToken.None);
            Assert.IsNotNull(evt);

            evt.Invoke();
            await task;

            Assert.IsNull(evt, "Handler is unsubscribed after completion.");
        });

        [UnityTest]
        public IEnumerator WaitForEvent_CancelledByToken_Unsubscribes() => UniTask.ToCoroutine(async () =>
        {
            ProbeStep probe = InjectedProbe();
            Action evt = null;
            CancellationTokenSource cts = new CancellationTokenSource();

            UniTask task = probe.WaitForEventPublic(h => evt += h, h => evt -= h, cts.Token);
            cts.Cancel();

            try
            {
                await task;
                Assert.Fail("Wait should be cancelled by the token.");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            finally
            {
                cts.Dispose();
            }

            Assert.IsNull(evt, "Handler is unsubscribed after cancellation.");
        });

        [UnityTest]
        public IEnumerator WaitForEventArg_ReturnsFirstArgument() => UniTask.ToCoroutine(async () =>
        {
            ProbeStep probe = InjectedProbe();
            Action<int> evt = null;

            UniTask<int> task = probe.WaitForEventArgPublic(h => evt += h, h => evt -= h, CancellationToken.None);
            evt.Invoke(42);

            int result = await task;

            Assert.AreEqual(42, result);
            Assert.IsNull(evt);
        });
    }
}
