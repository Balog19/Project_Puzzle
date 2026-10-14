using System;
using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Player;
using Puzzle.Population;
using UnityEngine;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// The info box for the selected building (layout: UI/Layouts/BuildingInfo.uxml).
    /// Floats above the building on screen at a constant size and lists what the building does,
    /// refreshed every frame. To show something new, add a section in <see cref="BuildSections"/>
    /// using <see cref="AddInfoRow"/> or <see cref="AddProgressRow"/>.
    /// </summary>
    public class BuildingInfoWidget : Widget
    {
        private readonly Game _game;
        private readonly PlayerController _player;
        private readonly VisualTreeAsset _progressRowTemplate;
        private readonly VisualTreeAsset _infoRowTemplate;
        private readonly float _heightAboveBuilding;

        private readonly Label _title;
        private readonly VisualElement _content;

        // The rows currently shown, and how to refresh each of them every frame.
        private readonly List<Widget> _rows = new List<Widget>();
        private readonly List<Action> _refreshers = new List<Action>();

        private Building _building;
        private House _house;
        private Vector3 _worldAnchor;

        public BuildingInfoWidget(VisualElement root, Game game, PlayerController player,
            VisualTreeAsset progressRowTemplate, VisualTreeAsset infoRowTemplate, float heightAboveBuilding) : base(root)
        {
            _game = game;
            _player = player;
            _progressRowTemplate = progressRowTemplate;
            _infoRowTemplate = infoRowTemplate;
            _heightAboveBuilding = heightAboveBuilding;

            _title = Q<Label>("title");
            _content = Q<VisualElement>("content");
            Q<Button>("close").clicked += () => _player.Select(null);

            player.SelectionChanged += ShowFor;
            OnDispose(() => player.SelectionChanged -= ShowFor);
            OnDispose(() => SetHouse(null));

            Hide();
        }

        /// <summary>Called every frame by GameHUD (after the camera moved) to refresh rows and follow the building.</summary>
        public void LateUpdate(Camera camera)
        {
            if (_building == null)
            {
                if (IsVisible) ShowFor(null); // building was destroyed
                return;
            }

            foreach (Action refresh in _refreshers)
            {
                refresh();
            }

            if (Root.panel != null && camera != null)
            {
                Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(Root.panel, _worldAnchor, camera);
                Root.style.left = position.x;
                Root.style.top = position.y;
            }
        }

        private void ShowFor(Building building)
        {
            _building = building;
            SetHouse(building != null ? building.House : null);

            if (building == null)
            {
                ClearRows();
                Hide();
                return;
            }

            _title.text = building.DisplayName;
            _worldAnchor = building.GetTopCenter(_heightAboveBuilding);
            BuildSections();
            Show();
        }

        // ---------- Sections ----------

        private void BuildSections()
        {
            ClearRows();
            if (_building == null) return;

            if (_building.Producer != null)
            {
                string produces = UIFormat.RatesPerTick(_building.Producer.OutputPerTick, "+");
                AddInfoRow(() => ("Produces", produces, false));
            }

            if (_building.Upkeep != null)
            {
                string upkeep = UIFormat.RatesPerTick(_building.Upkeep.CostPerTick, "-");
                AddInfoRow(() => ("Upkeep", upkeep, true));
            }

            House house = _building.House;
            if (house != null)
            {
                AddProgressRow(() => $"Residents: {house.ResidentCount} / {house.Capacity}",
                               () => house.Capacity > 0 ? (float)house.ResidentCount / house.Capacity : 0f);

                foreach (Person person in house.Residents)
                {
                    AddInfoRow(() => (person.ToString(), ResidentStatus(person), person.IsIncomeBlocked));
                }
            }
        }

        /// <summary>"Fed, +0.04 Money / tick" or "Starving, +0 Money / tick".</summary>
        private string ResidentStatus(Person person)
        {
            var status = new List<string>();
            foreach (NeedState need in person.Needs)
            {
                status.Add(need.StatusLabel);
            }

            string money = _game.Config.Money != null ? _game.Config.Money.DisplayName : "Money";
            status.Add($"{UIFormat.SignedRate(person.IncomePerTick(_game.Config))} {money} / tick");
            return string.Join(", ", status);
        }

        // Rebuild when people move in or out of the shown house.
        private void SetHouse(House house)
        {
            if (_house != null) _house.ResidentsChanged -= OnResidentsChanged;
            _house = house;
            if (_house != null) _house.ResidentsChanged += OnResidentsChanged;
        }

        private void OnResidentsChanged(House house) => BuildSections();

        // ---------- Rows ----------

        /// <summary>Adds a "Label ..... Value" row; <paramref name="read"/> is called every frame.</summary>
        private void AddInfoRow(Func<(string label, string value, bool negative)> read)
        {
            var row = new InfoRowWidget(Spawn(_infoRowTemplate, _content));
            Action refresh = () =>
            {
                var (label, value, negative) = read();
                row.Set(label, value, negative);
            };

            refresh();
            _rows.Add(row);
            _refreshers.Add(refresh);
        }

        /// <summary>Adds a label + bar row; both functions are called every frame.</summary>
        private void AddProgressRow(Func<string> text, Func<float> progress)
        {
            var row = new ProgressRowWidget(Spawn(_progressRowTemplate, _content));
            Action refresh = () => row.Set(text(), progress());

            refresh();
            _rows.Add(row);
            _refreshers.Add(refresh);
        }

        private void ClearRows()
        {
            foreach (Widget row in _rows)
            {
                row.Root.RemoveFromHierarchy();
                row.Dispose();
            }
            _rows.Clear();
            _refreshers.Clear();
        }
    }
}
