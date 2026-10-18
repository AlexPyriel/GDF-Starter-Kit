namespace Assets.Project.Features.Tutorial.Framework.Enums
{
    /// <summary>
    /// Types of game actions published to <c>ITutorialActionBus</c>. Game code calls
    /// <c>Publish</c> at the right moments; a tutorial step waits for them via
    /// <c>WaitForAction</c> — decoupled waiting on any action (button click, card tap,
    /// ability pick, window open, etc.). Each new action = a new value here.
    /// </summary>
    public enum ETutorialActionType
    {
        /// <summary>No action (neutral value; neither published nor awaited).</summary>
        None = 0,

        // TODO (per-game): add game-specific action values here, for example:
        //   LevelStarted = 1,
        //   EnemyDefeated = 2,
    }
}
