using System;
using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Grid;
using UnityEngine;

namespace Puzzle.Transport
{
    /// <summary>
    /// Keeps track of every road tile and makes roads join up: whenever a road is placed or removed,
    /// that tile and its four neighbours switch to the right piece (straight, corner, T, cross) and rotation.
    /// Also answers "how far along the roads is X from Y". Plain C# object created by Game;
    /// roads themselves are placed through BuildingSystem like any building.
    /// </summary>
    public class RoadSystem : IDisposable
    {
        /// <summary>Raised after roads were added or removed (the network changed).</summary>
        public event Action Changed;

        /// <summary>Number of road tiles.</summary>
        public int Count => _roads.Count;

        private const string PieceName = "Road Piece";

        private readonly GridMap _grid;
        private readonly BuildingSystem _buildings;
        private readonly Dictionary<Vector2Int, Building> _roads = new Dictionary<Vector2Int, Building>();

        public RoadSystem(GridMap grid, BuildingSystem buildings)
        {
            _grid = grid;
            _buildings = buildings;
            _buildings.Placed += OnPlaced;
            _buildings.Removed += OnRemoved;
        }

        public void Dispose()
        {
            _buildings.Placed -= OnPlaced;
            _buildings.Removed -= OnRemoved;
        }

        // ---------- Queries ----------

        public bool IsRoad(Vector2Int cell) => _roads.ContainsKey(cell);

        /// <summary>Which neighbours of a cell are roads.</summary>
        public RoadMask MaskAt(Vector2Int cell)
        {
            RoadMask mask = RoadMask.None;
            foreach (RoadMask direction in RoadPieceRules.Directions)
            {
                if (IsRoad(cell + RoadPieceRules.Offset(direction))) mask |= direction;
            }
            return mask;
        }

        /// <summary>
        /// Road distance from a building to every road tile within reach. The road tiles touching the
        /// building (on any side) are distance 1; each further tile along connected roads adds 1.
        /// </summary>
        public Dictionary<Vector2Int, int> DistancesFrom(Building building, int maxTiles)
        {
            var distances = new Dictionary<Vector2Int, int>();
            var frontier = new Queue<Vector2Int>();

            foreach (Vector2Int cell in RoadsTouching(building.Area))
            {
                distances[cell] = 1;
                frontier.Enqueue(cell);
            }

            while (frontier.Count > 0)
            {
                Vector2Int cell = frontier.Dequeue();
                int next = distances[cell] + 1;
                if (next > maxTiles) continue;

                foreach (RoadMask direction in RoadPieceRules.Directions)
                {
                    Vector2Int neighbour = cell + RoadPieceRules.Offset(direction);
                    if (!IsRoad(neighbour) || distances.ContainsKey(neighbour)) continue;

                    distances[neighbour] = next;
                    frontier.Enqueue(neighbour);
                }
            }

            return distances;
        }

        /// <summary>Road tiles directly next to an area (sharing an edge, not diagonal).</summary>
        public IEnumerable<Vector2Int> RoadsTouching(RectInt area)
        {
            for (int x = area.xMin; x < area.xMax; x++)
            {
                if (IsRoad(new Vector2Int(x, area.yMin - 1))) yield return new Vector2Int(x, area.yMin - 1);
                if (IsRoad(new Vector2Int(x, area.yMax))) yield return new Vector2Int(x, area.yMax);
            }
            for (int y = area.yMin; y < area.yMax; y++)
            {
                if (IsRoad(new Vector2Int(area.xMin - 1, y))) yield return new Vector2Int(area.xMin - 1, y);
                if (IsRoad(new Vector2Int(area.xMax, y))) yield return new Vector2Int(area.xMax, y);
            }
        }

        // ---------- Placing & removing ----------

        private void OnPlaced(Building building)
        {
            if (!building.IsRoad) return;

            Vector2Int cell = building.Area.position;
            _roads[cell] = building;
            RefreshAround(cell);
            Changed?.Invoke();
        }

        private void OnRemoved(Building building)
        {
            if (!building.IsRoad) return;

            Vector2Int cell = building.Area.position;
            if (!_roads.Remove(cell)) return;

            RefreshAround(cell);
            Changed?.Invoke();
        }

        // ---------- Pieces ----------

        private void RefreshAround(Vector2Int cell)
        {
            Refresh(cell);
            foreach (RoadMask direction in RoadPieceRules.Directions)
            {
                Refresh(cell + RoadPieceRules.Offset(direction));
            }
        }

        /// <summary>Replaces a road tile's piece with the one matching its current neighbours.</summary>
        private void Refresh(Vector2Int cell)
        {
            if (!_roads.TryGetValue(cell, out Building road) || road == null) return;

            Transform root = road.transform;
            // Detach before destroying: Destroy waits until the end of the frame, and a tile can be
            // refreshed several times in one frame while painting.
            Transform old = root.Find(PieceName);
            if (old != null)
            {
                old.SetParent(null, false);
                UnityEngine.Object.Destroy(old.gameObject);
            }

            RoadMask mask = MaskAt(cell);
            (RoadPiece piece, float yRotation) = RoadPieceRules.Resolve(mask);
            GameObject model = ModelFor(road.Data.RoadPieces, piece);

            GameObject instance;
            if (model != null)
            {
                instance = UnityEngine.Object.Instantiate(model, root);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f) * model.transform.localRotation;
            }
            else
            {
                instance = RoadPlaceholder.Build(mask, _grid.CellSize, root);
            }

            instance.name = PieceName;
        }

        private static GameObject ModelFor(RoadPieces pieces, RoadPiece piece)
        {
            if (pieces == null) return null;
            switch (piece)
            {
                case RoadPiece.Corner: return pieces.corner;
                case RoadPiece.TJunction: return pieces.tJunction;
                case RoadPiece.Cross: return pieces.cross;
                default: return pieces.straight;
            }
        }
    }
}
