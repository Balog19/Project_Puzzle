using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Economy;
using Puzzle.Player;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// The build menu (layout: UI/Layouts/BuildMenu.uxml). One button per building in the GameConfig,
    /// created automatically from the BuildButton template, showing its name and cost.
    /// Buttons grey out when unaffordable, and the building being placed is highlighted.
    /// </summary>
    public class BuildMenuWidget : Widget
    {
        private const string ActiveClass = "build-button--active";

        private readonly Game _game;
        private readonly PlayerController _player;
        private readonly Dictionary<BuildingData, Button> _buttons = new Dictionary<BuildingData, Button>();

        public BuildMenuWidget(VisualElement root, Game game, PlayerController player, VisualTreeAsset buttonTemplate)
            : base(root)
        {
            _game = game;
            _player = player;

            VisualElement list = Q<VisualElement>("buttons");
            foreach (BuildingData building in game.Config.Buildings)
            {
                if (building == null || _buttons.ContainsKey(building)) continue;

                VisualElement item = Spawn(buttonTemplate, list);
                item.Q<Label>("name").text = building.DisplayName;
                item.Q<Label>("cost").text = UIFormat.Amounts(building.Cost);

                Button button = item.Q<Button>("button");
                button.clicked += () => _player.BeginPlacing(building);
                _buttons[building] = button;
            }

            game.Resources.Changed += OnResourcesChanged;
            player.ModeChanged += OnModeChanged;
            OnDispose(() => game.Resources.Changed -= OnResourcesChanged);
            OnDispose(() => player.ModeChanged -= OnModeChanged);

            RefreshAffordable();
            RefreshActive();
        }

        private void OnResourcesChanged(ResourceType type, float amount) => RefreshAffordable();

        private void OnModeChanged(IInteractionMode mode) => RefreshActive();

        private void RefreshAffordable()
        {
            foreach (var pair in _buttons)
            {
                pair.Value.SetEnabled(_game.Buildings.CanAfford(pair.Key));
            }
        }

        private void RefreshActive()
        {
            foreach (var pair in _buttons)
            {
                pair.Value.EnableInClassList(ActiveClass, pair.Key == _player.PlacingBuilding);
            }
        }
    }
}
