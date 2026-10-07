using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildingButton : MonoBehaviour
{
    [SerializeField] private BuildingPlacer placer;
    [SerializeField] private BuildingData building;
    [Tooltip("Used to grey the button out when the building can't be afforded.")]
    [SerializeField] private ResourceManager resources;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);

        TMP_Text label = GetComponentInChildren<TMP_Text>();
        if (label != null && building != null)
        {
            string cost = ResourceAmount.Format(building.Cost);
            label.text = string.IsNullOrEmpty(cost) ? building.DisplayName : $"{building.DisplayName}\n{cost}";
        }
    }

    private void OnEnable()
    {
        if (resources != null) resources.Changed += OnResourceChanged;
        RefreshInteractable();
    }

    private void OnDisable()
    {
        if (resources != null) resources.Changed -= OnResourceChanged;
    }

    private void OnResourceChanged(ResourceType type, int amount) => RefreshInteractable();

    private void RefreshInteractable()
    {
        if (_button == null || building == null || resources == null) return;
        _button.interactable = resources.CanAfford(building.Cost);
    }

    private void OnClick()
    {
        if (placer != null) placer.Select(building);
    }
}
