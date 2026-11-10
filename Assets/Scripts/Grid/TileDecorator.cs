using System.Collections.Generic;
using UnityEngine;

// spawns models like trees on a tile from tiletype's decorationsettings
// seeded per cell so same level always looks the same
public class TileDecorator : MonoBehaviour
{
    [SerializeField] private int seed = 777;

    private static readonly Vector2Int[] Neighbours =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private readonly List<Vector2> _placed = new List<Vector2>();

    public void Decorate(TileMap map, Vector2Int cell, List<GameObject> into, Transform parent)
    {
        TileType type = map.GetTile(cell);
        if (type == null) return;

        DecorationSettings d = type.Decorations;
        if (d == null || d.prefabs == null || d.prefabs.Length == 0 || d.maxCount <= 0) return;

        var rng = new System.Random(Hash(seed, cell));
        if (rng.NextDouble() > d.spawnChance) return;

        int count = rng.Next(d.minCount, Mathf.Max(d.minCount, d.maxCount) + 1);
        if (count <= 0) return;

        if (d.thinAtEdges)
        {
            int same = 0;
            foreach (Vector2Int n in Neighbours)
                if (map.GetTile(cell + n) == type) same++;
            count = Mathf.Max(1, Mathf.RoundToInt(count * Mathf.Lerp(0.4f, 1f, same / 4f)));
        }

        GridMap grid = map.Grid;
        float range = grid.CellSize * (0.5f - d.edgePadding);
        float minDistSq = Mathf.Pow(d.minSpacing * grid.CellSize, 2f);
        Vector3 center = grid.CellToWorldCenter(cell);
        _placed.Clear();

        for (int attempt = 0; _placed.Count < count && attempt < count * 8; attempt++)
        {
            var p = new Vector2(Range(rng, -range, range), Range(rng, -range, range));

            bool tooClose = false;
            foreach (Vector2 q in _placed)
            {
                if ((p - q).sqrMagnitude < minDistSq) { tooClose = true; break; }
            }
            if (tooClose) continue;
            _placed.Add(p);

            GameObject prefab = d.prefabs[rng.Next(d.prefabs.Length)];
            if (prefab == null) continue;

            Quaternion rotation = d.randomYRotation
                ? Quaternion.Euler(0f, Range(rng, 0f, 360f), 0f)
                : Quaternion.identity;

            GameObject model = Instantiate(prefab, center + new Vector3(p.x, 0f, p.y), rotation, parent);
            model.transform.localScale *= Range(rng, d.scaleRange.x, d.scaleRange.y);
            into.Add(model);
        }
    }

    private static float Range(System.Random rng, float min, float max) =>
        min + (float)rng.NextDouble() * (max - min);

    private static int Hash(int seed, Vector2Int cell)
    {
        unchecked
        {
            int h = seed;
            h = h * 31 + cell.x * 73856093;
            h = h * 31 + cell.y * 19349663;
            return h;
        }
    }
}