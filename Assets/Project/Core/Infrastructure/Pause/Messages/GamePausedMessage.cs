namespace Assets.Project.Core.Infrastructure.Pause.Messages
{
    /// <summary>
    /// Published by <see cref="Interfaces.IGamePauseService"/> when the first pause handle is taken, after
    /// <c>Time.timeScale</c> and <c>AudioListener.pause</c> are already applied. Subscribe to freeze systems
    /// that do not follow the time scale (network-driven objects, custom timers, video).
    /// </summary>
    public readonly struct GamePausedMessage
    {
    }
}
