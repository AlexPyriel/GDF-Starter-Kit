using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// Lets a tutorial step wait for domain messages that game code publishes through MessagePipe
    /// (<c>IPublisher&lt;TMessage&gt;</c>). Game code does not know about the tutorial: it publishes its own
    /// message types, and a step subscribes to them only for the duration of the wait.
    /// </summary>
    public interface ITutorialMessageWaiter
    {
        /// <summary>
        /// Waits until <paramref name="count"/> matching messages of type <typeparamref name="TMessage"/>
        /// are published. Only messages published after the call are counted.
        /// </summary>
        /// <typeparam name="TMessage">Domain message type published by game code.</typeparam>
        /// <param name="count">Required number of matching messages; values below 1 complete immediately.</param>
        /// <param name="predicate">Optional filter; <see langword="null"/> counts every message.</param>
        /// <param name="token">Cancellation token; cancelling unsubscribes and throws <see cref="OperationCanceledException"/>.</param>
        /// <returns>Task that completes when the required number of matching messages has been received.</returns>
        public UniTask WaitFor<TMessage>(int count, Func<TMessage, bool> predicate, CancellationToken token);
    }
}
