using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// paint levels in the Scene view without pressing play
// open with Window > Puzzle > Level Painter
public class LevelPainterWindow : EditorWindow
{
    private enum PaintTool { Brush, Fill }
    private enum PaintLayer { Tiles, Slots }
    private static readonly Vector2Int[] Neighbours =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    [SerializeField] private GridMap grid;
    [SerializeField] private TerrainGenerator generator;
    [SerializeField] private LevelData level;
    [SerializeField] private TileType selectedTile;
    [SerializeField] private TileTag selectedSlot = TileTag.ProductionSlot;
    [SerializeField] private PaintTool tool = PaintTool.Brush;
    [SerializeField] private PaintLayer layer = PaintLayer.Tiles;
    [SerializeField] private int brushSize = 1;
    [SerializeField] private bool painting = true;
    [SerializeField] private bool showPreview = true;
    [SerializeField] private float previewOpacity = 0.9f;

    private readonly List<TileType> _palette = new List<TileType>();
    private Vector2 _scroll;
    private Vector2Int? _hoverCell;
    private Material _previewMaterial;
    private bool _stroking;
    private int _strokeUndoGroup;

    [MenuItem("Window/Puzzle/Level Painter")]
    private static void Open() => GetWindow<LevelPainterWindow>("Level Painter");

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += OnUndoRedo;
        RefreshPalette();
        AutoFind();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
        if (_previewMaterial != null) DestroyImmediate(_previewMaterial);
        SceneView.RepaintAll();
    }

    private void OnFocus()
    {
        RefreshPalette();
        AutoFind();
    }

    private void OnUndoRedo()
    {
        Repaint();
        SceneView.RepaintAll();
    }

    private void AutoFind()
    {
        if (grid == null) grid = FindFirstObjectByType<GridMap>();
        if (generator == null) generator = FindFirstObjectByType<TerrainGenerator>();
        if (level == null && generator != null) level = generator.Level;
    }

    private void RefreshPalette()
    {
        _palette.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:TileType"))
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileType>(AssetDatabase.GUIDToAssetPath(guid));
            if (tile != null) _palette.Add(tile);
        }
        _palette.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
        if (selectedTile == null && _palette.Count > 0) selectedTile = _palette[0];
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawWindowContents();
        EditorGUILayout.EndScrollView();
    }

    private void DrawWindowContents()
    {
        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("stop Play mode to paint levels", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
        grid = (GridMap)EditorGUILayout.ObjectField("Grid", grid, typeof(GridMap), true);
        generator = (TerrainGenerator)EditorGUILayout.ObjectField("Terrain Generator", generator, typeof(TerrainGenerator), true);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        level = (LevelData)EditorGUILayout.ObjectField("Level Asset", level, typeof(LevelData), false);
        if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
        if (grid == null)
        {
            EditorGUILayout.HelpBox("assign the Grid (the object with GridMap)", MessageType.Warning);
            return;
        }
        if (level == null)
        {
            EditorGUILayout.HelpBox("pick a Level asset, or create a new one", MessageType.Info);
            if (GUILayout.Button("Create New Level", GUILayout.Height(26))) CreateLevel();
            return;
        }
        if (!level.Matches(grid.Width, grid.Height))
        {
            EditorGUILayout.HelpBox(
                $"this level is {level.Width}x{level.Height} but the grid is {grid.Width}x{grid.Height}.",
                MessageType.Warning);
            if (GUILayout.Button("Resize Level To Grid"))
            {
                Undo.RecordObject(level, "Resize Level");
                level.Resize(grid.Width, grid.Height);
                EditorUtility.SetDirty(level);
                SceneView.RepaintAll();
            }
            return;
        }

        if (generator != null && generator.Level != level)
        {
            EditorGUILayout.HelpBox("the Terrain Generator isnt using this level yet", MessageType.Info);
            if (GUILayout.Button("Use This Level In The Scene")) AssignToGenerator();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Painting", EditorStyles.boldLabel);
        painting = EditorGUILayout.Toggle(new GUIContent("Paint In Scene View", "turn off to select objects in the Scene view again"), painting);
        showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);
        previewOpacity = EditorGUILayout.Slider("Preview Opacity", previewOpacity, 0.1f, 1f);
        layer = (PaintLayer)EditorGUILayout.EnumPopup("Paint", layer);
        tool = (PaintTool)EditorGUILayout.EnumPopup("Tool", tool);
        if (tool == PaintTool.Brush) brushSize = EditorGUILayout.IntSlider("Brush Size", brushSize, 1, 10);

        EditorGUILayout.Space();
        if (layer == PaintLayer.Tiles) DrawPalette();
        else DrawSlotPicker();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Whole Level", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(selectedTile == null))
        {
            if (GUILayout.Button("Fill Everything With Selected Tile")) FillAll();
        }
        using (new EditorGUI.DisabledScope(generator == null))
        {
            if (GUILayout.Button("Start From Random Terrain")) Randomize();
        }
        if (GUILayout.Button("Clear All Slots")) ClearSlots();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "left click / drag: paint\n" +
            "shift + click: pick the tile under the mouse\n" +
            "ctrl/cmd + click: remove the slot (slots layer)\n" +
            "[ and ]: brush size\n" +
            "alt + drag still orbits the camera",
            MessageType.None);
    }

    private void DrawPalette()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tiles", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh", GUILayout.Width(70))) RefreshPalette();
        EditorGUILayout.EndHorizontal();

        if (_palette.Count == 0)
        {
            EditorGUILayout.HelpBox("no tile types found, create some with create > puzzle > tile type", MessageType.Warning);
            return;
        }

        foreach (TileType tile in _palette)
        {
            Rect row = EditorGUILayout.GetControlRect(false, 22f);
            bool selected = tile == selectedTile;

            if (selected) EditorGUI.DrawRect(row, new Color(0.24f, 0.48f, 0.9f, 0.45f));
            Color swatch = tile.Color;
            swatch.a = 1f;
            EditorGUI.DrawRect(new Rect(row.x + 4f, row.y + 3f, 16f, 16f), swatch);
            EditorGUI.LabelField(new Rect(row.x + 26f, row.y, row.width - 26f, row.height),
                tile.DisplayName, selected ? EditorStyles.boldLabel : EditorStyles.label);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && row.Contains(e.mousePosition))
            {
                selectedTile = tile;
                e.Use();
                Repaint();
            }
        }
    }

    private void DrawSlotPicker()
    {
        int index = selectedSlot == TileTag.HousingSlot ? 1 : 0;
        index = EditorGUILayout.Popup("slot type", index, new[] { "production slot", "housing slot" });
        selectedSlot = index == 1 ? TileTag.HousingSlot : TileTag.ProductionSlot;
        EditorGUILayout.HelpBox(
            "slots show as small squares. buildings whose required tags include this slot can only go on these cells",
            MessageType.None);
    }

    private void CreateLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Level", "NewLevel", "asset", "Where should the level be saved?");
        if (string.IsNullOrEmpty(path)) return;
        var asset = CreateInstance<LevelData>();
        asset.Resize(grid.Width, grid.Height);
        TileType fill = generator != null && generator.Fallback != null ? generator.Fallback : selectedTile;
        if (fill != null) asset.FillTiles(fill);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        level = asset;
        if (generator != null) AssignToGenerator();
        SceneView.RepaintAll();
    }

    private void AssignToGenerator()
    {
        Undo.RecordObject(generator, "Assign Level");
        generator.Level = level;
        EditorUtility.SetDirty(generator);
    }

    private void FillAll()
    {
        Undo.RecordObject(level, "Fill Level");
        level.FillTiles(selectedTile);
        EditorUtility.SetDirty(level);
        SceneView.RepaintAll();
    }

    private void ClearSlots()
    {
        Undo.RecordObject(level, "Clear Slots");
        level.ClearTags(TileTag.ProductionSlot | TileTag.HousingSlot);
        EditorUtility.SetDirty(level);
        SceneView.RepaintAll();
    }

    private void Randomize()
    {
        if (!EditorUtility.DisplayDialog("start from random terrain?",
                "this replaces every tile in the level with generated terrain. you can undo it.",
                "replace", "cancel"))
            return;

        int count = grid.Width * grid.Height;
        var types = new TileType[count];
        var extraTags = new TileTag[count];
        generator.Generate(grid.Width, grid.Height, types, extraTags, ignoreLevel: true);

        Undo.RecordObject(level, "Random Terrain");
        level.CopyFrom(types, extraTags);
        EditorUtility.SetDirty(level);
        SceneView.RepaintAll();
    }

    private void OnSceneGUI(SceneView view)
    {
        if (EditorApplication.isPlaying) return;
        if (grid == null || level == null || !level.Matches(grid.Width, grid.Height)) return;

        Event e = Event.current;

        if (showPreview && e.type == EventType.Repaint) DrawPreview();
        if (!painting) return;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(controlId);
        _hoverCell = MouseCell(e.mousePosition);
        if (e.type == EventType.MouseMove) view.Repaint();
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.LeftBracket) { ChangeBrush(-1, view); e.Use(); }
            else if (e.keyCode == KeyCode.RightBracket) { ChangeBrush(1, view); e.Use(); }
        }

        if (e.type == EventType.Repaint && _hoverCell.HasValue) DrawBrush(_hoverCell.Value);

        bool leftButton = e.button == 0 && !e.alt;
        bool erase = e.control || e.command;

        if (leftButton && e.type == EventType.MouseDown && _hoverCell.HasValue)
        {
            if (e.shift)
            {
                Pick(_hoverCell.Value);
            }
            else
            {
                Undo.IncrementCurrentGroup();
                _strokeUndoGroup = Undo.GetCurrentGroup();
                _stroking = true;
                GUIUtility.hotControl = controlId;
                Apply(_hoverCell.Value, erase, isFirstClick: true);
            }
            e.Use();
        }
        else if (leftButton && e.type == EventType.MouseDrag && _stroking)
        {
            if (_hoverCell.HasValue) Apply(_hoverCell.Value, erase, isFirstClick: false);
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0 && _stroking)
        {
            _stroking = false;
            GUIUtility.hotControl = 0;
            Undo.CollapseUndoOperations(_strokeUndoGroup);
            e.Use();
        }
    }

    private void ChangeBrush(int delta, SceneView view)
    {
        brushSize = Mathf.Clamp(brushSize + delta, 1, 10);
        Repaint();
        view.Repaint();
    }

    private Vector2Int? MouseCell(Vector2 guiPosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);
        if (!new Plane(Vector3.up, grid.Origin).Raycast(ray, out float distance)) return null;
        Vector2Int cell = grid.WorldToCell(ray.GetPoint(distance));
        return grid.InBounds(cell) ? cell : (Vector2Int?)null;
    }

    private void Apply(Vector2Int center, bool erase, bool isFirstClick)
    {
        Undo.RecordObject(level, "Paint Level");

        if (tool == PaintTool.Fill)
        {
            if (isFirstClick) FloodFill(center, erase);
        }
        else
        {
            int offset = (brushSize - 1) / 2;
            for (int dy = 0; dy < brushSize; dy++)
            for (int dx = 0; dx < brushSize; dx++)
            {
                var cell = new Vector2Int(center.x + dx - offset, center.y + dy - offset);
                if (level.InBounds(cell)) PaintCell(cell, erase);
            }
        }

        EditorUtility.SetDirty(level);
    }

    private void PaintCell(Vector2Int cell, bool erase)
    {
        if (layer == PaintLayer.Tiles)
        {
            if (selectedTile != null) level.SetTile(cell, selectedTile);
        }
        else
        {
            TileTag current = level.GetTags(cell);
            level.SetTags(cell, erase ? current & ~selectedSlot : current | selectedSlot);
        }
    }

    // fills the connected area of the same tile type
    private void FloodFill(Vector2Int start, bool erase)
    {
        TileType target = level.GetTile(start);
        if (layer == PaintLayer.Tiles && target == selectedTile) return;

        var stack = new Stack<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector2Int cell = stack.Pop();
            if (!level.InBounds(cell) || !seen.Add(cell) || level.GetTile(cell) != target) continue;

            PaintCell(cell, erase);
            foreach (Vector2Int n in Neighbours) stack.Push(cell + n);
        }
    }

    private void Pick(Vector2Int cell)
    {
        TileType tile = level.GetTile(cell);
        if (tile == null) return;
        selectedTile = tile;
        layer = PaintLayer.Tiles;
        Repaint();
    }


    private void DrawPreview()
    {
        if (_previewMaterial == null)
        {
            _previewMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            _previewMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _previewMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _previewMaterial.SetInt("_Cull", (int)CullMode.Off);
            _previewMaterial.SetInt("_ZWrite", 0);
        }

        float size = grid.CellSize;
        float baseY = grid.Origin.y + 0.005f;
        float inset = size * 0.3f;

        _previewMaterial.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.QUADS);

        for (int y = 0; y < grid.Height; y++)
        for (int x = 0; x < grid.Width; x++)
        {
            var cell = new Vector2Int(x, y);
            TileType tile = level.GetTile(cell);

            Color color = tile != null ? tile.Color : new Color(1f, 0f, 1f);
            color.a = previewOpacity;
            Vector3 corner = grid.CellToWorldCorner(cell);
            corner.y = baseY;
            Quad(corner, size, size, color);

            TileTag tags = level.GetTags(cell);
            if ((tags & (TileTag.ProductionSlot | TileTag.HousingSlot)) != 0)
            {
                TileTag slot = (tags & TileTag.HousingSlot) != 0 ? TileTag.HousingSlot : TileTag.ProductionSlot;
                Vector3 markerCorner = corner + new Vector3(inset, 0.002f, inset);
                Quad(markerCorner, size - inset * 2f, size - inset * 2f, SlotColor(slot));
            }
        }

        GL.End();
        GL.PopMatrix();
    }

    private static void Quad(Vector3 corner, float width, float depth, Color color)
    {
        GL.Color(color);
        GL.Vertex(corner);
        GL.Vertex(corner + new Vector3(width, 0f, 0f));
        GL.Vertex(corner + new Vector3(width, 0f, depth));
        GL.Vertex(corner + new Vector3(0f, 0f, depth));
    }

    private void DrawBrush(Vector2Int center)
    {
        int size = tool == PaintTool.Fill ? 1 : brushSize;
        int offset = (size - 1) / 2;
        Vector3 corner = grid.CellToWorldCorner(new Vector2Int(center.x - offset, center.y - offset));
        corner.y = grid.Origin.y + 0.03f;
        float w = size * grid.CellSize;

        Vector3[] verts =
        {
            corner,
            corner + new Vector3(w, 0f, 0f),
            corner + new Vector3(w, 0f, w),
            corner + new Vector3(0f, 0f, w)
        };

        Color fill = layer == PaintLayer.Tiles && selectedTile != null ? selectedTile.Color : SlotColor(selectedSlot);
        fill.a = 0.35f;
        Handles.DrawSolidRectangleWithOutline(verts, fill, Color.white);
    }

    private static Color SlotColor(TileTag slot) =>
        slot == TileTag.HousingSlot ? new Color(0.3f, 0.85f, 1f, 1f) : new Color(1f, 0.85f, 0.2f, 1f);
}