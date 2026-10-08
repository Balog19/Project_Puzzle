/// <summary>
/// Anything driven by the TickManager's fixed game clock instead of its own Update.
/// </summary>
public interface ITickable
{
    void Tick();
}
