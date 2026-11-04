using System;
using System.Threading;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Cysharp.Threading.Tasks;
using MessagePipe;
using VContainer;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <summary>
    /// MessagePipe-backed <see cref="ITutorialMessageWaiter"/>: resolves <see cref="ISubscriber{TMessage}"/>
    /// from the root container (open-generic brokers, no per-type registration) and holds the
    /// subscription only while a step waits.
    /// </summary>
    public sealed class TutorialMessageWaiter : ITutorialMessageWaiter
    {
        private readonly IObjectResolver _resolver;

        /// <summary>Creates the waiter.</summary>
        /// <param name="resolver">Root resolver that provides MessagePipe subscribers.</param>
        /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <see langword="null"/>.</exception>
        public TutorialMessageWaiter(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <inheritdoc />
        public async UniTask WaitFor<TMessage>(int count, Func<TMessage, bool> predicate, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (count <= 0)
            {
                return;
            }

            ISubscriber<TMessage> subscriber = _resolver.Resolve<ISubscriber<TMessage>>();
            UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
            int received = 0;

            void OnMessage(TMessage message)
            {
                if (predicate != null && !predicate(message))
                {
                    return;
                }

                if (Interlocked.Increment(ref received) >= count)
                {
                    completionSource.TrySetResult();
                }
            }

            using (subscriber.Subscribe(OnMessage))
            {
                await completionSource.Task.AttachExternalCancellation(token);
            }
        }
    }
}
