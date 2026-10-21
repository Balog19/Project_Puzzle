using System.Collections.Generic;
using Puzzle.Core;
using Puzzle.Economy;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// The bar at the top of the screen (layout: UI/Layouts/TopBar.uxml).
    /// Shows population (housed / total, starving), then one counter per resource in the GameConfig,
    /// created automatically from the ResourceCounter template. Each counter shows the stockpile (what building
    /// costs can use), plus goods still sitting in buildings; Money also shows its net change per tick.
    /// </summary>
    public class TopBarWidget : Widget
    {
        private const string NegativeClass = "label--negative";

        private readonly Game _game;
        private readonly Label _population;
        private readonly Dictionary<ResourceType, Label> _counters = new Dictionary<ResourceType, Label>();

        public TopBarWidget(VisualElement root, Game game, VisualTreeAsset counterTemplate) : base(root)
        {
            _game = game;
            _population = Q<Label>("population");

            VisualElement resourceList = Q<VisualElement>("resources");
            foreach (ResourceAmount entry in game.Config.Resources)
            {
                if (entry.Type == null || _counters.ContainsKey(entry.Type)) continue;

                _counters[entry.Type] = Spawn(counterTemplate, resourceList).Q<Label>("value");
            }

            // Goods in buildings change every tick, so the counters refresh once per tick.
            game.Ticks.Ticked += RefreshCounters;
            game.Population.Changed += RefreshPopulation;
            OnDispose(() => game.Ticks.Ticked -= RefreshCounters);
            OnDispose(() => game.Population.Changed -= RefreshPopulation);

            RefreshCounters();
            RefreshPopulation();
        }

        private void RefreshCounters()
        {
            foreach (var pair in _counters)
            {
                ResourceType type = pair.Key;
                float stockpile = _game.Resources.Get(type);
                float inBuildings = _game.Supply.TotalStored(type);

                string text = $"{type.DisplayName}: {UIFormat.Whole(stockpile)}";
                if (inBuildings >= 1f) text += $" (+{UIFormat.Whole(inBuildings)} in buildings)";
                if (type == _game.Config.Money) text += $" ({UIFormat.SignedRate(NetMoneyPerTick())}/tick)";

                pair.Value.text = text;
                pair.Value.EnableInClassList(NegativeClass, stockpile < 0f);
            }
        }

        /// <summary>Taxes minus building upkeep, per tick.</summary>
        private float NetMoneyPerTick() =>
            _game.Population.IncomePerTick - _game.Buildings.UpkeepPerTick(_game.Config.Money);

        private void RefreshPopulation()
        {
            var population = _game.Population;
            string text = $"Housed: {population.Housed} / {population.Total}";
            if (population.Starving > 0) text += $"   Starving: {population.Starving}";

            _population.text = text;
            _population.EnableInClassList(NegativeClass, population.Starving > 0);
        }
    }
}
