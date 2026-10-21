using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Economy;
using Puzzle.Player;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// The build menu at the bottom of the screen (layout: UI/Layouts/BuildMenu.uxml).
    /// - A bar of category buttons, one per GameConfig category (CategoryButton template).
    /// - Clicking a category opens a tray above the bar with one button per building in that category
    ///   (BuildButton template). Clicking the open category again closes it; picking a building starts
    ///   placing it and leaves the tray open.
    /// </summary>
    public class BuildMenuWidget : Widget
    {
        private const string OpenClass = "category-button--open";

        private readonly Game _game;
        private readonly PlayerController _player;
        private readonly VisualTreeAsset _buildButtonTemplate;

        private readonly VisualElement _tray;
        private readonly VisualElement _trayButtons;
        private readonly Label _trayEmpty;

        private readonly Dictionary<BuildingCategory, Button> _categoryButtons = new Dictionary<BuildingCategory, Button>();
        private readonly List<BuildButtonWidget> _buildButtons = new List<BuildButtonWidget>();

        /// <summary>The category whose tray is open, or null.</summary>
        public BuildingCategory OpenCategory { get; private set; }

        /// <summary>The building buttons in the open tray.</summary>
        public IReadOnlyList<BuildButtonWidget> TrayButtons => _buildButtons;

        public BuildMenuWidget(VisualElement root, Game game, PlayerController player,
            VisualTreeAsset categoryButtonTemplate, VisualTreeAsset buildButtonTemplate) : base(root)
        {
            _game = game;
            _player = player;
            _buildButtonTemplate = buildButtonTemplate;

            _tray = Q<VisualElement>("tray");
            _trayButtons = Q<VisualElement>("tray-buttons");
            _trayEmpty = Q<Label>("tray-empty");

            VisualElement bar = Q<VisualElement>("categories");
            foreach (BuildingCategory category in game.Config.Categories)
            {
                if (category == null || _categoryButtons.ContainsKey(category)) continue;

                VisualElement item = Spawn(categoryButtonTemplate, bar);
                item.Q<Label>("name").text = category.DisplayName;
                UIIcons.Apply(item.Q<VisualElement>("icon"), null, category.Icon, category.DisplayName);
                item.Q<VisualElement>("icon").style.display = category.Icon != null ? DisplayStyle.Flex : DisplayStyle.None;

                Button button = item.Q<Button>("button");
                button.clicked += () => ToggleCategory(category);
                _categoryButtons[category] = button;
            }

            game.Resources.Changed += OnResourcesChanged;
            player.ModeChanged += OnModeChanged;
            OnDispose(() => game.Resources.Changed -= OnResourcesChanged);
            OnDispose(() => player.ModeChanged -= OnModeChanged);
            OnDispose(ClearTray);

            SetTrayVisible(false);
        }

        /// <summary>Opens a category's tray, switches to it if another is open, or closes it if it's already open.</summary>
        public void ToggleCategory(BuildingCategory category)
        {
            OpenCategory = OpenCategory == category ? null : category;

            foreach (var pair in _categoryButtons)
            {
                pair.Value.EnableInClassList(OpenClass, pair.Key == OpenCategory);
            }

            ClearTray();
            if (OpenCategory == null)
            {
                SetTrayVisible(false);
                return;
            }

            foreach (BuildingData building in _game.Config.BuildingsIn(OpenCategory))
            {
                _buildButtons.Add(new BuildButtonWidget(Spawn(_buildButtonTemplate, _trayButtons), building, _game, _player));
            }

            _trayEmpty.style.display = _buildButtons.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            SetTrayVisible(true);
        }

        // ---------- Tray ----------

        private void ClearTray()
        {
            foreach (BuildButtonWidget button in _buildButtons)
            {
                button.Root.RemoveFromHierarchy();
                button.Dispose();
            }
            _buildButtons.Clear();
        }

        private void SetTrayVisible(bool visible) =>
            _tray.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private void RefreshButtons()
        {
            foreach (BuildButtonWidget button in _buildButtons)
            {
                button.Refresh();
            }
        }

        private void OnResourcesChanged(ResourceType type, float amount) => RefreshButtons();

        private void OnModeChanged(IInteractionMode mode) => RefreshButtons();
    }
}
