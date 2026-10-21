using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// Shows one <see cref="BuildingInfoPanel"/> above every selected building (one for a click,
    /// many for a drag selection). Owns the click-through #selection-info layer in HUD.uxml and
    /// keeps its panels in step with <see cref="PlayerController.Selection"/>.
    /// </summary>
    public class SelectionInfoWidget : Widget
    {
        private const string PanelClass = "info-panel";

        private readonly Game _game;
        private readonly PlayerController _player;
        private readonly VisualTreeAsset _panelTemplate;
        private readonly VisualTreeAsset _progressRowTemplate;
        private readonly VisualTreeAsset _infoRowTemplate;
        private readonly float _heightAboveBuilding;

        private readonly Dictionary<Building, BuildingInfoPanel> _panels = new Dictionary<Building, BuildingInfoPanel>();
        private readonly List<Building> _stale = new List<Building>();

        /// <summary>The panels currently shown, one per selected building.</summary>
        public IReadOnlyCollection<BuildingInfoPanel> Panels => _panels.Values;

        public SelectionInfoWidget(VisualElement root, Game game, PlayerController player, VisualTreeAsset panelTemplate,
            VisualTreeAsset progressRowTemplate, VisualTreeAsset infoRowTemplate, float heightAboveBuilding) : base(root)
        {
            _game = game;
            _player = player;
            _panelTemplate = panelTemplate;
            _progressRowTemplate = progressRowTemplate;
            _infoRowTemplate = infoRowTemplate;
            _heightAboveBuilding = heightAboveBuilding;

            Root.pickingMode = PickingMode.Ignore; // only the panels themselves take clicks

            player.SelectionChanged += Sync;
            OnDispose(() => player.SelectionChanged -= Sync);
            OnDispose(() => Sync(null));
        }

        /// <summary>Called every frame by GameHUD (after the camera moved).</summary>
        public void LateUpdate(Camera camera)
        {
            foreach (BuildingInfoPanel panel in _panels.Values)
            {
                panel.LateUpdate(camera);
            }
        }

        // Adds panels for newly selected buildings and removes panels for deselected ones.
        private void Sync(IReadOnlyList<Building> selection)
        {
            _stale.Clear();
            foreach (Building building in _panels.Keys)
            {
                if (selection == null || !Contains(selection, building)) _stale.Add(building);
            }

            foreach (Building building in _stale)
            {
                BuildingInfoPanel panel = _panels[building];
                panel.Root.RemoveFromHierarchy();
                panel.Dispose();
                _panels.Remove(building);
            }

            if (selection == null) return;

            foreach (Building building in selection)
            {
                if (building == null || _panels.ContainsKey(building)) continue;

                VisualElement panelRoot = Spawn(_panelTemplate, Root);
                panelRoot.AddToClassList(PanelClass);
                _panels[building] = new BuildingInfoPanel(panelRoot, building, _game, _player,
                    _progressRowTemplate, _infoRowTemplate, _heightAboveBuilding);
            }
        }

        private static bool Contains(IReadOnlyList<Building> list, Building building)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == building) return true;
            }
            return false;
        }
    }
}
