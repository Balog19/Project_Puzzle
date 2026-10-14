namespace Puzzle.Core
{
    /// <summary>
    /// Anything stepped by the <see cref="TickSystem"/>'s fixed game clock instead of its own Update.
    /// Register with <c>Game.Instance.Ticks.Register(this, phase)</c>.
    /// </summary>
    public interface ITickable
    {
        /// <summary>Called once per game tick (20 times per second by default).</summary>
        void Tick();
    }
}
