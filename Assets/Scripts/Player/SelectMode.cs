using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Grid;
using UnityEngine;

namespace Puzzle.Player
{
    /// <summary>
    /// The default mode.
    /// - Shows the cursor cell.
    /// - Click a building to select it (its cells stay highlighted).
    /// - Press on empty ground and drag to select every building (not roads) in the rectangle; the rectangle stays
    ///   highlighted while that selection is active.
    /// - Cancel clears the selection and the highlight.
    /// </summary>
    public class SelectMode : IInteractionMode
    {
        private readonly PlayerController _player;
        private readonly Game _game;

        private Vector2Int? _dragStart;
        private Vector2Int? _dragEnd;
        private Vector2Int? _lastCell;
        private bool _dragging;

        public SelectMode(PlayerController player, Game game)
        {
            _player = player;
            _game = game;
        }

        public void Enter() { }

        public void Exit()
        {
            ClearDrag();
            _game.GridView.Clear();
        }

        public void Update(in PointerInfo pointer)
        {
            if (pointer.Cell.HasValue) _lastCell = pointer.Cell;

            _game.GridView.SetCursor(pointer.Cell);
            _game.GridView.SetArea(HighlightedArea());
        }

        public void OnSelectPressed(in PointerInfo pointer)
        {
            ClearDrag();

            // Roads start a drag like empty ground (towns are full of them); a plain click still selects one.
            if (pointer.Cell.HasValue && _game.Buildings.TryGetAt(pointer.Cell.Value, out Building building) && !building.IsRoad)
            {
                _player.Select(building);
                return;
            }

            _player.ClearSelection();
            if (!pointer.Cell.HasValue) return;

            _dragStart = pointer.Cell;
            _dragging = true;
        }

        public void OnSelectReleased(in PointerInfo pointer)
        {
            if (!_dragging) return;

            _dragging = false;
            _dragEnd = pointer.Cell ?? _lastCell;

            RectInt? area = DragArea();
            List<Building> found = area.HasValue ? _game.Buildings.GetInArea(area.Value) : new List<Building>();
            found.RemoveAll(b => b.IsRoad); // a drag over a town shouldn't open a box per road tile

            // A click (no drag) on a road selects that road.
            if (found.Count == 0 && area.HasValue && area.Value.width == 1 && area.Value.height == 1
                && _game.Buildings.TryGetAt(area.Value.position, out Building road))
            {
                ClearDrag();
                _player.Select(road);
                return;
            }

            _player.SelectMany(found);

            // A drag that caught nothing leaves nothing highlighted.
            if (found.Count == 0) ClearDrag();
        }

        public void OnCancel()
        {
            ClearDrag();
            _player.ClearSelection();
        }

        // ---------- Highlight ----------

        /// <summary>The drag rectangle while dragging or while its selection is active; else a lone selected building's cells.</summary>
        private RectInt? HighlightedArea()
        {
            if (_dragStart.HasValue)
            {
                // The drag's selection was cleared elsewhere (e.g. info boxes closed): drop the rectangle too.
                if (!_dragging && _player.Selection.Count == 0) ClearDrag();
                else return DragArea();
            }

            return _player.Selection.Count == 1 ? _player.Selection[0].Area : (RectInt?)null;
        }

        /// <summary>Start to mouse while dragging, start to end after release.</summary>
        private RectInt? DragArea()
        {
            if (!_dragStart.HasValue) return null;

            Vector2Int? end = _dragging ? _lastCell : _dragEnd;
            return end.HasValue ? GridMap.AreaBetween(_dragStart.Value, end.Value) : (RectInt?)null;
        }

        private void ClearDrag()
        {
            _dragStart = null;
            _dragEnd = null;
            _dragging = false;
        }
    }
}
