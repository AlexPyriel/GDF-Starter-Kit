using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Assets.Project.Core.Infrastructure.UI.Interfaces;
using UnityEngine;
using VContainer.Unity;

namespace Assets.Project.Core.Infrastructure.UI.Presenters
{
    /// <summary>
    /// Base asynchronous presenter that coordinates a specific view type through the VContainer lifecycle.
    /// </summary>
    /// <typeparam name="TView">The view type managed by the presenter.</typeparam>
    public abstract class Presenter<TView>
        : IAsyncStartable, IDisposable
        where TView : class, IView
    {
        /// <summary>
        /// Gets or sets the view managed by the presenter.
        /// </summary>
        protected TView View { get; set; }

        /// <summary>
        /// Starts the presenter through the asynchronous VContainer lifecycle.
        /// </summary>
        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            try
            {
                await StartPresenterAsync(cancellation);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Runs presenter-specific asynchronous startup logic.
        /// </summary>
        protected abstract UniTask StartPresenterAsync(CancellationToken cancellation);

        /// <summary>
        /// Releases presenter-owned resources.
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
