using UnityEngine;

namespace Puzzle.Transport
{
    /// <summary>Which neighbouring cells a road connects to. Combine with |.</summary>
    [System.Flags]
    public enum RoadMask
    {
        None = 0,
        North = 1, // +Z
        East = 2,  // +X
        South = 4, // -Z
        West = 8,  // -X
        All = North | East | South | West
    }

    /// <summary>The four road models (see BuildingData.RoadPieces for how each must be oriented).</summary>
    public enum RoadPiece
    {
        Straight,
        Corner,
        TJunction,
        Cross
    }

    /// <summary>
    /// Picks the road piece and Y rotation for a set of connections. Pure logic, no Unity objects.
    /// Rotations are clockwise seen from above (+90 turns north into east), applied to each piece's
    /// rotation-0 orientation: Straight N-S, Corner N+E, T N+E+S.
    /// </summary>
    public static class RoadPieceRules
    {
        public static (RoadPiece piece, float yRotation) Resolve(RoadMask mask)
        {
            bool n = (mask & RoadMask.North) != 0;
            bool e = (mask & RoadMask.East) != 0;
            bool s = (mask & RoadMask.South) != 0;
            bool w = (mask & RoadMask.West) != 0;
            int count = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0);

            switch (count)
            {
                case 4:
                    return (RoadPiece.Cross, 0f);

                case 3:
                    // T rotation is set by the open (missing) side: W 0, N 90, E 180, S 270.
                    if (!w) return (RoadPiece.TJunction, 0f);
                    if (!n) return (RoadPiece.TJunction, 90f);
                    if (!e) return (RoadPiece.TJunction, 180f);
                    return (RoadPiece.TJunction, 270f);

                case 2:
                    if (n && s) return (RoadPiece.Straight, 0f);
                    if (e && w) return (RoadPiece.Straight, 90f);
                    if (n && e) return (RoadPiece.Corner, 0f);
                    if (e && s) return (RoadPiece.Corner, 90f);
                    if (s && w) return (RoadPiece.Corner, 180f);
                    return (RoadPiece.Corner, 270f); // w && n

                default:
                    // Lone tile or dead end: a straight along the one connection (north-south if none).
                    return (RoadPiece.Straight, e || w ? 90f : 0f);
            }
        }

        /// <summary>The grid offset for one direction.</summary>
        public static Vector2Int Offset(RoadMask direction)
        {
            switch (direction)
            {
                case RoadMask.North: return Vector2Int.up;
                case RoadMask.East: return Vector2Int.right;
                case RoadMask.South: return Vector2Int.down;
                case RoadMask.West: return Vector2Int.left;
                default: return Vector2Int.zero;
            }
        }

        /// <summary>The four directions, in N, E, S, W order.</summary>
        public static readonly RoadMask[] Directions = { RoadMask.North, RoadMask.East, RoadMask.South, RoadMask.West };
    }
}
