using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Grid;
using UnityEngine;

namespace Puzzle.Player
{
    /// <summary>
    /// The default mode.
    /// - Shows the cursor cell.
    /// - Click a building to select it; click empty ground to deselect and start a drag selection.
    /// - A drag selection stays highlighted after release until Cancel or a new click.
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
            _game.GridView.SetArea(CurrentDragArea());
        }

        public void OnSelectPressed(in PointerInfo pointer)
        {
            ClearDrag();

            if (!pointer.Cell.HasValue)
            {
                _player.Select(null);
                return;
            }

            if (_game.Buildings.TryGetAt(pointer.Cell.Value, out Building building))
            {
                _player.Select(building);
                return;
            }

            _player.Select(null);
            _dragStart = pointer.Cell;
            _dragging = true;
        }

        public void OnSelectReleased(in PointerInfo pointer)
        {
            if (!_dragging) return;

            _dragging = false;
            _dragEnd = pointer.Cell ?? _lastCell;
        }

        public void OnCancel()
        {
            // First cancel clears the drag selection, the next one deselects the building.
            if (_dragStart.HasValue) ClearDrag();
            else _player.Select(null);
        }

        // ---------- Drag ----------

        /// <summary>The highlighted rectangle: start to mouse while dragging, start to end after release.</summary>
        private RectInt? CurrentDragArea()
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
