using System.Collections.Generic;
using UnityEngine;

namespace Puzzle.Buildings
{
    /// <summary>
    /// Simple stand-in visuals made of boxes, used for anything that has no model yet
    /// (buildings without a prefab, road pieces without a model). Materials are shared per colour.
    /// </summary>
    public static class Placeholders
    {
        /// <summary>Colour of placeholder buildings.</summary>
        public static readonly Color BuildingColor = new Color(0.78f, 0.7f, 0.58f);

        private const float BuildingHeight = 0.8f;
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        /// <summary>A box filling a building's footprint (in cells), standing on its pivot.</summary>
        public static GameObject Building(Vector2Int size, float cellSize, Transform parent)
        {
            var root = new GameObject("Placeholder");
            root.transform.SetParent(parent, false);

            // Slightly smaller than the footprint so neighbouring buildings don't merge visually.
            Vector3 boxSize = new Vector3(size.x * cellSize * 0.9f, BuildingHeight, size.y * cellSize * 0.9f);
            Box(root.transform, new Vector3(0f, BuildingHeight * 0.5f, 0f), boxSize, BuildingColor);
            return root;
        }

        /// <summary>A box without a collider, centred at <paramref name="localCenter"/>.</summary>
        public static GameObject Box(Transform parent, Vector3 localCenter, Vector3 size, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(box.GetComponent<Collider>()); // picking uses grid math, not physics
            box.name = "Box";
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localCenter;
            box.transform.localScale = size;

            var renderer = box.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Material(color, renderer.sharedMaterial);
            return box;
        }

        // One tinted copy of the default material per colour.
        private static Material Material(Color color, Material source)
        {
            if (Materials.TryGetValue(color, out Material material) && material != null) return material;

            material = new Material(source) { name = "Placeholder " + ColorUtility.ToHtmlStringRGB(color) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;

            Materials[color] = material;
            return material;
        }
    }
}
