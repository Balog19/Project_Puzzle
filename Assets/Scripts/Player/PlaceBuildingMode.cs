using Puzzle.Buildings;
using Puzzle.Core;
using UnityEngine;

namespace Puzzle.Player
{
    /// <summary>
    /// Placing a building.
    /// - A ghost of the building and its footprint follow the mouse, snapped to the grid.
    /// - The footprint turns red where it can't be placed (occupied, off-grid, or unaffordable).
    /// - Select places it and stays in this mode so more can be placed; Cancel goes back to SelectMode.
    /// </summary>
    public class PlaceBuildingMode : IInteractionMode
    {
        /// <summary>The building being placed.</summary>
        public BuildingData Building { get; private set; }

        private readonly PlayerController _player;
        private readonly Game _game;
        private GameObject _ghost;

        public PlaceBuildingMode(PlayerController player, Game game)
        {
            _player = player;
            _game = game;
        }

        /// <summary>Sets which building to place. Call before the mode is entered.</summary>
        public void SetBuilding(BuildingData building) => Building = building;

        public void Enter()
        {
            _ghost = SpawnGhost(Building, _game.Grid.CellSize);
            _game.GridView.SetCursor(null);
        }

        public void Exit()
        {
            if (_ghost != null) Object.Destroy(_ghost);
            _ghost = null;
            _game.GridView.Clear();
        }

        public void Update(in PointerInfo pointer)
        {
            if (!TryGetFootprint(pointer, out RectInt area))
            {
                _ghost.SetActive(false);
                _game.GridView.SetArea(null);
                return;
            }

            _ghost.SetActive(true);
            _ghost.transform.position = _game.Grid.AreaToWorldCenter(area);
            _game.GridView.SetArea(area, _game.Buildings.CanPlace(Building, area));
        }

        public void OnSelectPressed(in PointerInfo pointer)
        {
            if (TryGetFootprint(pointer, out RectInt area))
            {
                _game.Buildings.Place(Building, area, _ghost.transform.rotation);
            }
        }

        public void OnSelectReleased(in PointerInfo pointer) { }

        public void OnCancel() => _player.EnterSelectMode();

        // ---------- Helpers ----------

        private bool TryGetFootprint(in PointerInfo pointer, out RectInt area)
        {
            area = pointer.Cell.HasValue ? BuildingSystem.FootprintAt(Building, pointer.Cell.Value) : default;
            return pointer.Cell.HasValue;
        }

        /// <summary>A preview copy of the building (or its placeholder box) that can't block raycasts or physics.</summary>
        private static GameObject SpawnGhost(BuildingData building, float cellSize)
        {
            GameObject ghost;
            if (building.Prefab != null)
            {
                ghost = Object.Instantiate(building.Prefab);
            }
            else
            {
                ghost = new GameObject();
                Placeholders.Building(building.Size, cellSize, ghost.transform);
            }
            ghost.name = building.DisplayName + " (Ghost)";

            foreach (Collider col in ghost.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            ghost.SetActive(false);
            return ghost;
        }
    }
}
