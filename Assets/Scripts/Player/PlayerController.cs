using System;
using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.UI;
using UnityEngine;

namespace Puzzle.Player
{
    /// <summary>
    /// The player (like Unreal's PlayerController + camera pawn). Put it on the Main Camera.
    /// It is the only script that reads input (from the GameControls input actions asset):
    /// - Move/Zoom drive the <see cref="CameraController"/>.
    /// - Mouse input goes to the current <see cref="IInteractionMode"/> (selecting or placing).
    /// It also owns what the player has selected (one or many buildings) and starts the HUD.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Pan and zoom tuning.")]
        [SerializeField] private CameraSettings cameraSettings = new CameraSettings();
        [Tooltip("The HUD to start. Found automatically if left empty.")]
        [SerializeField] private GameHUD hud;

        /// <summary>Raised when the selection changes. The list is empty when nothing is selected.</summary>
        public event Action<IReadOnlyList<Building>> SelectionChanged;

        /// <summary>Raised when the interaction mode changes (e.g. placing starts or stops).</summary>
        public event Action<IInteractionMode> ModeChanged;

        public Camera Camera { get; private set; }

        /// <summary>The selected buildings, in the order they were selected. Empty when nothing is selected.</summary>
        public IReadOnlyList<Building> Selection => _selection;

        public IInteractionMode CurrentMode { get; private set; }

        /// <summary>The building being placed, or null when not placing.</summary>
        public BuildingData PlacingBuilding =>
            CurrentMode == _placeMode ? _placeMode.Building :
            CurrentMode == _roadMode ? _roadMode.Road : null;

        private Game _game;
        private GameControls _controls;
        private CameraController _cameraController;
        private SelectMode _selectMode;
        private PlaceBuildingMode _placeMode;
        private PlaceRoadMode _roadMode;
        private readonly List<Building> _selection = new List<Building>();

        // ---------- Lifecycle ----------

        private void Awake()
        {
            Camera = GetComponent<Camera>();
            _controls = new GameControls();
            _cameraController = new CameraController(Camera, cameraSettings);
        }

        private void OnEnable() => _controls.Gameplay.Enable();

        private void OnDisable() => _controls.Gameplay.Disable();

        private void Start()
        {
            _game = Game.Instance;
            if (_game == null)
            {
                Debug.LogError("PlayerController needs a Game object in the scene.", this);
                enabled = false;
                return;
            }

            _selectMode = new SelectMode(this, _game);
            _placeMode = new PlaceBuildingMode(this, _game);
            _roadMode = new PlaceRoadMode(this, _game);
            SetMode(_selectMode);

            _game.Buildings.Removed += OnBuildingRemoved;

            if (hud == null) hud = FindAnyObjectByType<GameHUD>();
            if (hud != null) hud.Initialize(this, _game);
        }

        private void OnDestroy()
        {
            if (_game != null) _game.Buildings.Removed -= OnBuildingRemoved;
            _controls?.Dispose();
        }

        private void Update()
        {
            var input = _controls.Gameplay;

            _cameraController.Update(input.Move.ReadValue<Vector2>(), input.Zoom.ReadValue<float>(), Time.unscaledDeltaTime);

            PointerInfo pointer = ReadPointer(input.Point.ReadValue<Vector2>());
            CurrentMode.Update(pointer);

            if (input.Select.WasPressedThisFrame() && !pointer.IsOverUI) CurrentMode.OnSelectPressed(pointer);
            if (input.Select.WasReleasedThisFrame()) CurrentMode.OnSelectReleased(pointer);
            if (input.Cancel.WasPressedThisFrame()) CurrentMode.OnCancel();
        }

        // ---------- Modes ----------

        /// <summary>Starts placing a building, or painting roads for a road (e.g. from a build menu button).</summary>
        public void BeginPlacing(BuildingData building)
        {
            if (building == null) return;

            ClearSelection();
            // Roads are painted by dragging; everything else is placed one at a time with a ghost.
            if (building.IsRoad)
            {
                _roadMode.SetRoad(building);
                SetMode(_roadMode, forceReenter: true);
                return;
            }

            _placeMode.SetBuilding(building);
            SetMode(_placeMode, forceReenter: true);
        }

        /// <summary>Returns to the default select mode.</summary>
        public void EnterSelectMode() => SetMode(_selectMode);

        private void SetMode(IInteractionMode mode, bool forceReenter = false)
        {
            if (mode == CurrentMode && !forceReenter) return;

            CurrentMode?.Exit();
            CurrentMode = mode;
            CurrentMode.Enter();
            ModeChanged?.Invoke(CurrentMode);
        }

        // ---------- Selection ----------

        /// <summary>Selects just this building (null clears the selection).</summary>
        public void Select(Building building) => SelectMany(building != null ? new[] { building } : null);

        /// <summary>Replaces the selection with these buildings (null or empty clears it).</summary>
        public void SelectMany(IEnumerable<Building> buildings)
        {
            var next = new List<Building>();
            if (buildings != null)
            {
                foreach (Building building in buildings)
                {
                    if (building != null && !next.Contains(building)) next.Add(building);
                }
            }

            if (SameAsSelection(next)) return;

            _selection.Clear();
            _selection.AddRange(next);
            SelectionChanged?.Invoke(_selection);
        }

        /// <summary>Removes one building from the selection (e.g. its info box was closed).</summary>
        public void Deselect(Building building)
        {
            if (_selection.Remove(building)) SelectionChanged?.Invoke(_selection);
        }

        /// <summary>Selects nothing.</summary>
        public void ClearSelection() => SelectMany(null);

        public bool IsSelected(Building building) => _selection.Contains(building);

        private bool SameAsSelection(List<Building> buildings)
        {
            if (buildings.Count != _selection.Count) return false;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i] != _selection[i]) return false;
            }
            return true;
        }

        private void OnBuildingRemoved(Building building) => Deselect(building);

        // ---------- Pointer ----------

        private PointerInfo ReadPointer(Vector2 screenPosition)
        {
            bool overUI = hud != null && hud.IsPointerOverUI(screenPosition);

            Vector3? worldPoint = null;
            Vector2Int? cell = null;
            if (_game.Grid.TryRaycast(Camera.ScreenPointToRay(screenPosition), out Vector3 hit))
            {
                worldPoint = hit;
                if (_game.Grid.TryWorldToCell(hit, out Vector2Int c)) cell = c;
            }

            return new PointerInfo(screenPosition, overUI, worldPoint, cell);
        }
    }
}
