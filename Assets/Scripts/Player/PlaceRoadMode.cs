using Puzzle.Buildings;
using Puzzle.Core;
using UnityEngine;

namespace Puzzle.Player
{
    /// <summary>
    /// Painting roads.
    /// - The cell under the mouse is highlighted (red if a road can't go there).
    /// - Hold Select and drag: every empty cell the mouse passes over becomes road, paying its cost per tile.
    ///   Cells skipped between frames are filled in, so fast drags still give a connected road.
    /// - Release stops painting; Cancel goes back to SelectMode.
    /// Roads join up with their neighbours automatically (RoadSystem).
    /// </summary>
    public class PlaceRoadMode : IInteractionMode
    {
        /// <summary>The road type being painted.</summary>
        public BuildingData Road { get; private set; }

        /// <summary>True while Select is held and roads are being laid.</summary>
        public bool IsPainting { get; private set; }

        private readonly PlayerController _player;
        private readonly Game _game;
        private Vector2Int? _lastPainted;

        public PlaceRoadMode(PlayerController player, Game game)
        {
            _player = player;
            _game = game;
        }

        /// <summary>Sets which road to paint. Call before the mode is entered.</summary>
        public void SetRoad(BuildingData road) => Road = road;

        public void Enter()
        {
            IsPainting = false;
            _lastPainted = null;
            _game.GridView.SetCursor(null);
        }

        public void Exit()
        {
            IsPainting = false;
            _game.GridView.Clear();
        }

        public void Update(in PointerInfo pointer)
        {
            if (!pointer.Cell.HasValue)
            {
                _game.GridView.SetArea(null);
                return;
            }

            Vector2Int cell = pointer.Cell.Value;
            if (IsPainting) PaintTo(cell);

            var area = new RectInt(cell, Vector2Int.one);
            bool valid = _game.Buildings.CanPlace(Road, area) || _game.Roads.IsRoad(cell);
            _game.GridView.SetArea(area, valid);
        }

        public void OnSelectPressed(in PointerInfo pointer)
        {
            IsPainting = true;
            _lastPainted = null;
            if (pointer.Cell.HasValue) PaintTo(pointer.Cell.Value);
        }

        public void OnSelectReleased(in PointerInfo pointer)
        {
            IsPainting = false;
            _lastPainted = null;
        }

        public void OnCancel() => _player.EnterSelectMode();

        // ---------- Painting ----------

        /// <summary>Lays road on every cell from the last painted cell to this one, one step at a time.</summary>
        private void PaintTo(Vector2Int target)
        {
            Vector2Int current = _lastPainted ?? target;
            TryLay(current);

            while (current != target)
            {
                // Step along whichever axis is further away, so the path stays 4-connected.
                Vector2Int delta = target - current;
                if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) current.x += System.Math.Sign(delta.x);
                else current.y += System.Math.Sign(delta.y);
                TryLay(current);
            }

            _lastPainted = target;
        }

        private void TryLay(Vector2Int cell)
        {
            var area = new RectInt(cell, Vector2Int.one);
            if (_game.Buildings.CanPlace(Road, area)) _game.Buildings.Place(Road, area, Quaternion.identity);
        }
    }
}
