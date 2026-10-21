using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Player;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// One building's button in a build tray (layout: UI/Layouts/BuildButton.uxml): icon, name and cost.
    /// Clicking starts placing the building. Created and refreshed by BuildMenuWidget.
    /// </summary>
    public class BuildButtonWidget : Widget
    {
        private const string ActiveClass = "build-button--active";

        public BuildingData Building { get; }

        private readonly Game _game;
        private readonly PlayerController _player;
        private readonly Button _button;

        public BuildButtonWidget(VisualElement root, BuildingData building, Game game, PlayerController player) : base(root)
        {
            Building = building;
            _game = game;
            _player = player;

            _button = Q<Button>("button");
            _button.clicked += () => _player.BeginPlacing(Building);

            Q<Label>("name").text = building.DisplayName;
            Q<Label>("cost").text = UIFormat.Amounts(building.Cost);
            UIIcons.Apply(Q<VisualElement>("icon"), Q<Label>("icon-letter"), building.Icon, building.DisplayName);

            Refresh();
        }

        /// <summary>Greys the button out when unaffordable and highlights it while this building is being placed.</summary>
        public void Refresh()
        {
            _button.SetEnabled(_game.Buildings.CanAfford(Building));
            _button.EnableInClassList(ActiveClass, _player.PlacingBuilding == Building);
        }
    }
}
