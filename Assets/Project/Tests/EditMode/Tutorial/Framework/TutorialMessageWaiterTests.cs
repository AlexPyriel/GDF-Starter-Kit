using System;
using System.Collections;
using System.Threading;
using Assets.Project.Features.Tutorial.Framework.Services;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using UnityEngine.TestTools;
using VContainer;

namespace Assets.Project.Tests.EditMode.Tutorial.Framework
{
    /// <summary>
    /// Exercises <see cref="TutorialMessageWaiter"/> on a real MessagePipe broker (VContainer open
    /// generics, no per-type registration): count, predicate, step-scoped counting, cancellation and
    /// unsubscribe.
    /// </summary>
    public sealed class TutorialMessageWaiterTests
    {
        private readonly struct TestMessage
        {
            public TestMessage(int id)
            {
                Id = id;
            }

            public int Id { get; }
        }

        private IObjectResolver _resolver;
        private IPublisher<TestMessage> _publisher;
        private TutorialMessageWaiter _waiter;

        [SetUp]
        public void SetUp()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterMessagePipe();
            _resolver = builder.Build();
            _publisher = _resolver.Resolve<IPublisher<TestMessage>>();
            _waiter = new TutorialMessageWaiter(_resolver);
        }

        [TearDown]
        public void TearDown()
        {
            _resolver.Dispose();
        }

        [Test]
        public void Ctor_NullResolver_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TutorialMessageWaiter(null));
        }

        [Test]
        public void WaitFor_CountReached_Completes()
        {
            UniTask task = _waiter.WaitFor<TestMessage>(2, null, CancellationToken.None);

            _publisher.Publish(new TestMessage(1));
            Assert.AreEqual(UniTaskStatus.Pending, task.Status, "One of two messages is not enough.");

            _publisher.Publish(new TestMessage(2));
            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
        }

        [Test]
        public void WaitFor_Predicate_IgnoresNonMatchingMessages()
        {
            UniTask task = _waiter.WaitFor<TestMessage>(1, message => message.Id == 5, CancellationToken.None);

            _publisher.Publish(new TestMessage(4));
            Assert.AreEqual(UniTaskStatus.Pending, task.Status);

            _publisher.Publish(new TestMessage(5));
            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
        }

        [Test]
        public void WaitFor_MessagesBeforeCall_AreNotCounted()
        {
            _publisher.Publish(new TestMessage(1));

            UniTask task = _waiter.WaitFor<TestMessage>(1, null, CancellationToken.None);

            Assert.AreEqual(UniTaskStatus.Pending, task.Status, "Counting starts at the call, not at the run start.");
        }

        [Test]
        public void WaitFor_NonPositiveCount_CompletesImmediately()
        {
            UniTask task = _waiter.WaitFor<TestMessage>(0, null, CancellationToken.None);

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
        }

        [UnityTest]
        public IEnumerator WaitFor_Cancelled_ThrowsAndUnsubscribes() => UniTask.ToCoroutine(async () =>
        {
            int predicateCalls = 0;
            using CancellationTokenSource cts = new CancellationTokenSource();
            UniTask task = _waiter.WaitFor<TestMessage>(1, _ =>
            {
                predicateCalls++;
                return false;
            }, cts.Token);

            cts.Cancel();

            bool cancelled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            _publisher.Publish(new TestMessage(1));

            Assert.IsTrue(cancelled, "Cancellation surfaces as OperationCanceledException.");
            Assert.AreEqual(0, predicateCalls, "Subscription is released on cancel.");
        });

        [Test]
        public void WaitFor_PreCancelledToken_IsCanceled()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            UniTask task = _waiter.WaitFor<TestMessage>(1, null, cts.Token);

            Assert.AreEqual(UniTaskStatus.Canceled, task.Status);
        }
    }
}
