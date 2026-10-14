using System;
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
    /// It also owns what the player has selected and starts the HUD.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Pan and zoom tuning.")]
        [SerializeField] private CameraSettings cameraSettings = new CameraSettings();
        [Tooltip("The HUD to start. Found automatically if left empty.")]
        [SerializeField] private GameHUD hud;

        /// <summary>Raised when the selected building changes (null = nothing selected).</summary>
        public event Action<Building> SelectionChanged;

        /// <summary>Raised when the interaction mode changes (e.g. placing starts or stops).</summary>
        public event Action<IInteractionMode> ModeChanged;

        public Camera Camera { get; private set; }

        /// <summary>The selected building, or null.</summary>
        public Building SelectedBuilding { get; private set; }

        public IInteractionMode CurrentMode { get; private set; }

        /// <summary>The building being placed, or null when not placing.</summary>
        public BuildingData PlacingBuilding => CurrentMode == _placeMode ? _placeMode.Building : null;

        private Game _game;
        private GameControls _controls;
        private CameraController _cameraController;
        private SelectMode _selectMode;
        private PlaceBuildingMode _placeMode;

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

        /// <summary>Starts placing a building (e.g. from a build menu button).</summary>
        public void BeginPlacing(BuildingData building)
        {
            if (building == null || building.Prefab == null)
            {
                Debug.LogWarning("Can't place a building with no data or prefab.", this);
                return;
            }

            Select(null);
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

        /// <summary>Selects a building (null to deselect).</summary>
        public void Select(Building building)
        {
            if (building == SelectedBuilding) return;
            SelectedBuilding = building;
            SelectionChanged?.Invoke(building);
        }

        private void OnBuildingRemoved(Building building)
        {
            if (building == SelectedBuilding) Select(null);
        }

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
