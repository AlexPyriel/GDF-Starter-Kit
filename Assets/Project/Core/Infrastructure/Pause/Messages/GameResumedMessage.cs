namespace Assets.Project.Core.Infrastructure.Pause.Messages
{
    /// <summary>
    /// Published by <see cref="Interfaces.IGamePauseService"/> when the last pause handle is released, after
    /// the previous <c>Time.timeScale</c> is restored and <c>AudioListener.pause</c> is cleared.
    /// </summary>
    public readonly struct GameResumedMessage
    {
    }
}
