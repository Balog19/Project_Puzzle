using Puzzle.Buildings;
using UnityEngine;

namespace Puzzle.Transport
{
    /// <summary>
    /// Builds a stand-in road piece from flat boxes (a centre square plus one arm per connection),
    /// used when the road's model for that piece hasn't been assigned yet. Already shaped for its
    /// connections, so it needs no rotation.
    /// </summary>
    public static class RoadPlaceholder
    {
        private const float Thickness = 0.04f;
        private const float Width = 0.5f; // fraction of the cell
        private static readonly Color RoadColor = new Color(0.32f, 0.32f, 0.34f);

        public static GameObject Build(RoadMask mask, float cellSize, Transform parent)
        {
            var root = new GameObject("Road Placeholder");
            root.transform.SetParent(parent, false);

            float width = cellSize * Width;
            Vector3 lift = Vector3.up * (Thickness * 0.5f);
            Placeholders.Box(root.transform, lift, new Vector3(width, Thickness, width), RoadColor);

            float armLength = (cellSize - width) * 0.5f;
            float armOffset = (width + armLength) * 0.5f;
            foreach (RoadMask direction in RoadPieceRules.Directions)
            {
                if ((mask & direction) == 0) continue;

                Vector2Int d = RoadPieceRules.Offset(direction);
                Vector3 center = new Vector3(d.x, 0f, d.y) * armOffset + lift;
                Vector3 size = d.x != 0 ? new Vector3(armLength, Thickness, width) : new Vector3(width, Thickness, armLength);
                Placeholders.Box(root.transform, center, size, RoadColor);
            }

            return root;
        }
    }
}
