using Puzzle.Economy;
using UnityEngine;

namespace Puzzle.Population
{
    /// <summary>
    /// A demand every person has that is paid for every tick (Hunger: eat 0.002 Wheat per tick).
    /// While it's met the person pays <see cref="IncomePerTick"/> in taxes. Data asset: create via
    /// Create > Puzzle > Need and add it to the GameConfig's Needs list. New demands are just new assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle/Need", fileName = "NewNeed")]
    public class NeedType : ScriptableObject
    {
        [Tooltip("Name shown in the UI.")]
        [SerializeField] private string displayName = "Need";
        [Tooltip("Resource each person consumes for this need.")]
        [SerializeField] private ResourceType resource;
        [Tooltip("How much of the resource each person consumes per tick.")]
        [SerializeField, Min(0f)] private float amountPerTick = 0.002f;
        [Tooltip("Money each person generates per tick while this need is met.")]
        [SerializeField, Min(0f)] private float incomePerTick = 0.02f;
        [Tooltip("If this need isn't met, the person generates no money at all (e.g. starving).")]
        [SerializeField] private bool stopsAllIncomeWhenUnmet;
        [Tooltip("Word shown when met / unmet, e.g. Fed / Starving.")]
        [SerializeField] private string metLabel = "Met";
        [SerializeField] private string unmetLabel = "Unmet";

        public string DisplayName => displayName;
        public ResourceType Resource => resource;
        public float AmountPerTick => amountPerTick;
        public float IncomePerTick => incomePerTick;
        public bool StopsAllIncomeWhenUnmet => stopsAllIncomeWhenUnmet;
        public string MetLabel => metLabel;
        public string UnmetLabel => unmetLabel;
    }
}
