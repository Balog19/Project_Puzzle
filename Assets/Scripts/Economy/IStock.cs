namespace Puzzle.Economy
{
    /// <summary>
    /// Anywhere goods can be taken from: a producer's own <see cref="Inventory"/>, or the shared storage pool
    /// (<see cref="ResourceSystem"/>). Lets people and storages pull from either without caring which it is.
    /// </summary>
    public interface IStock
    {
        /// <summary>How much of a resource is available here.</summary>
        float Get(ResourceType type);

        /// <summary>Removes up to the amount and returns how much was actually taken.</summary>
        float Take(ResourceType type, float amount);
    }
}
