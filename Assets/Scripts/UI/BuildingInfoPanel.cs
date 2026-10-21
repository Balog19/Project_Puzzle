using System;
using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Economy;
using Puzzle.Player;
using Puzzle.Population;
using Puzzle.Transport;
using UnityEngine;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// The info box for one selected building (layout: UI/Layouts/BuildingInfo.uxml). Created by
    /// SelectionInfoWidget, one per selected building. Floats above its building at a constant size and
    /// lists what the building does, refreshed every frame. To show something new, add a section in
    /// <see cref="BuildSections"/> using <see cref="AddInfoRow"/> or <see cref="AddProgressRow"/>.
    /// </summary>
    public class BuildingInfoPanel : Widget
    {
        /// <summary>The building this box describes.</summary>
        public Building Building { get; }

        private readonly Game _game;
        private readonly VisualTreeAsset _progressRowTemplate;
        private readonly VisualTreeAsset _infoRowTemplate;
        private readonly VisualElement _content;
        private readonly Vector3 _worldAnchor;

        // The rows currently shown, and how to refresh each of them every frame.
        private readonly List<Widget> _rows = new List<Widget>();
        private readonly List<Action> _refreshers = new List<Action>();

        public BuildingInfoPanel(VisualElement root, Building building, Game game, PlayerController player,
            VisualTreeAsset progressRowTemplate, VisualTreeAsset infoRowTemplate, float heightAboveBuilding) : base(root)
        {
            Building = building;
            _game = game;
            _progressRowTemplate = progressRowTemplate;
            _infoRowTemplate = infoRowTemplate;
            _worldAnchor = building.GetTopCenter(heightAboveBuilding);

            Q<Label>("title").text = building.DisplayName;
            _content = Q<VisualElement>("content");
            Q<Button>("close").clicked += () => player.Deselect(building);

            // Rebuild when people move in or out, or when roads/buildings change who reaches this building.
            House house = building.House;
            if (house != null)
            {
                house.ResidentsChanged += OnResidentsChanged;
                OnDispose(() => house.ResidentsChanged -= OnResidentsChanged);
            }
            if (house != null || building.Storage != null)
            {
                game.Supply.Changed += BuildSections;
                OnDispose(() => game.Supply.Changed -= BuildSections);
            }

            BuildSections();
        }

        /// <summary>Called every frame (after the camera moved) to refresh rows and follow the building.</summary>
        public void LateUpdate(Camera camera)
        {
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

        public override void Dispose()
        {
            ClearRows();
            base.Dispose();
        }

        // ---------- Sections ----------

        private void BuildSections()
        {
            ClearRows();

            if (Building.Producer != null)
            {
                string produces = UIFormat.RatesPerTick(Building.Producer.OutputPerTick, "+");
                AddInfoRow(() => ("Produces", produces, false));
            }

            if (Building.Inventory != null)
            {
                Inventory inventory = Building.Inventory;
                AddInfoRow(() => ("Stock", StockText(inventory), false));
            }

            if (Building.Upkeep != null)
            {
                string upkeep = UIFormat.RatesPerTick(Building.Upkeep.CostPerTick, "-");
                AddInfoRow(() => ("Upkeep", upkeep, true));
            }

            StorageCollector storage = Building.Storage;
            if (storage != null)
            {
                AddInfoRow(() => ("Storage", "shared pool", false));
                AddInfoRow(() => ("Collecting", $"{UIFormat.Number(storage.CollectedLastTick)} / tick", false));
                AddLinkRows(_game.Supply.ProducersFor(Building), "Collecting from");
            }

            House house = Building.House;
            if (house != null)
            {
                AddProgressRow(() => $"Residents: {house.ResidentCount} / {house.Capacity}",
                               () => house.Capacity > 0 ? (float)house.ResidentCount / house.Capacity : 0f);

                AddLinkRows(_game.Supply.SuppliersFor(Building), "Supplied by");

                foreach (Person person in house.Residents)
                {
                    AddInfoRow(() => (person.ToString(), ResidentStatus(person), person.IsIncomeBlocked));
                }
            }
        }

        /// <summary>One row per linked building ("Supplied by Farmland   5 roads"), or a warning if there are none.</summary>
        private void AddLinkRows(IReadOnlyList<Link> links, string label)
        {
            if (links.Count == 0)
            {
                int max = _game.Supply.MaxDistance;
                AddInfoRow(() => (label, $"nothing within {max} roads", true));
                return;
            }

            foreach (Link link in links)
            {
                string name = link.Building.DisplayName;
                string distance = $"{link.RoadDistance} road{(link.RoadDistance == 1 ? "" : "s")}";
                AddInfoRow(() => ($"{label} {name}", distance, false));
            }
        }

        /// <summary>"23 Wheat, 4 Wood", or "empty".</summary>
        private static string StockText(Inventory inventory)
        {
            var parts = new List<string>();
            foreach (var pair in inventory.Contents)
            {
                parts.Add($"{UIFormat.Whole(pair.Value)} {pair.Key.DisplayName}");
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "empty";
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
