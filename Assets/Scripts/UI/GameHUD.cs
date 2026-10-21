using System.Collections.Generic;
using Puzzle.Core;
using Puzzle.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// The HUD (like Unreal's AHUD + root UMG widget). Lives on the object with the UIDocument whose
    /// Source Asset is UI/Layouts/HUD.uxml. PlayerController calls <see cref="Initialize"/>, which creates
    /// one C# widget per section of the layout. Look and layout live in the UXML/USS files
    /// (editable in the UI Builder); behaviour lives in the widget classes.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameHUD : MonoBehaviour
    {
        [Header("Templates (UI/Layouts)")]
        [Tooltip("One per resource in the top bar.")]
        [SerializeField] private VisualTreeAsset resourceCounterTemplate;
        [Tooltip("One per category in the build bar.")]
        [SerializeField] private VisualTreeAsset categoryButtonTemplate;
        [Tooltip("One per building in a build tray.")]
        [SerializeField] private VisualTreeAsset buildButtonTemplate;
        [Tooltip("The floating info box, one per selected building.")]
        [SerializeField] private VisualTreeAsset buildingInfoTemplate;
        [Tooltip("A label + progress bar, used in the building info box.")]
        [SerializeField] private VisualTreeAsset progressRowTemplate;
        [Tooltip("A label + value line, used in the building info box.")]
        [SerializeField] private VisualTreeAsset infoRowTemplate;

        [Header("Building info")]
        [Tooltip("World units between the top of a building and its info box.")]
        [SerializeField] private float infoHeightAboveBuilding = 0.5f;

        private UIDocument _document;
        private PlayerController _player;
        private SelectionInfoWidget _selectionInfo;
        private readonly List<Widget> _widgets = new List<Widget>();

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        /// <summary>The widgets, for code that needs to drive them (e.g. opening a build category).</summary>
        public BuildMenuWidget BuildMenu { get; private set; }
        public SelectionInfoWidget SelectionInfo => _selectionInfo;

        /// <summary>Builds the widgets. Called once by PlayerController when the game starts.</summary>
        public void Initialize(PlayerController player, Game game)
        {
            _player = player;

            VisualElement root = _document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore; // empty HUD space must not block clicks on the world

            Add(new TopBarWidget(root.Q("top-bar"), game, resourceCounterTemplate));
            BuildMenu = Add(new BuildMenuWidget(root.Q("build-menu"), game, player, categoryButtonTemplate, buildButtonTemplate));
            _selectionInfo = Add(new SelectionInfoWidget(root.Q("selection-info"), game, player, buildingInfoTemplate,
                progressRowTemplate, infoRowTemplate, infoHeightAboveBuilding));
        }

        private T Add<T>(T widget) where T : Widget
        {
            _widgets.Add(widget);
            return widget;
        }

        private void LateUpdate()
        {
            if (_selectionInfo != null) _selectionInfo.LateUpdate(_player.Camera);
        }

        private void OnDestroy()
        {
            foreach (Widget widget in _widgets)
            {
                widget.Dispose();
            }
            _widgets.Clear();
        }

        /// <summary>True if the screen position is over a visible HUD element that takes clicks.</summary>
        public bool IsPointerOverUI(Vector2 screenPosition)
        {
            IPanel panel = _document != null ? _document.rootVisualElement?.panel : null;
            if (panel == null) return false;

            // Screen space has y up; panel space has y down.
            Vector2 flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, flipped);
            return panel.Pick(panelPosition) != null;
        }

#if UNITY_EDITOR
        // Fills in any missing template from UI/Layouts, so new templates never need wiring by hand.
        private void Reset() => FillMissingTemplates();
        private void OnValidate() => FillMissingTemplates();

        private void FillMissingTemplates()
        {
            if (resourceCounterTemplate == null) resourceCounterTemplate = LoadLayout("ResourceCounter");
            if (categoryButtonTemplate == null) categoryButtonTemplate = LoadLayout("CategoryButton");
            if (buildButtonTemplate == null) buildButtonTemplate = LoadLayout("BuildButton");
            if (buildingInfoTemplate == null) buildingInfoTemplate = LoadLayout("BuildingInfo");
            if (progressRowTemplate == null) progressRowTemplate = LoadLayout("ProgressRow");
            if (infoRowTemplate == null) infoRowTemplate = LoadLayout("InfoRow");
        }

        private static VisualTreeAsset LoadLayout(string layoutName) =>
            UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Assets/UI/Layouts/{layoutName}.uxml");
#endif
    }
}
