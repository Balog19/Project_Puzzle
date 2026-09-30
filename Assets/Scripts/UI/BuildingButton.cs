using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildingButton : MonoBehaviour
{
    [SerializeField] private BuildingPlacer placer;
    [SerializeField] private BuildingData building;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);

        TMP_Text label = GetComponentInChildren<TMP_Text>();
        if (label != null && building != null) label.text = building.DisplayName;
    }

    private void OnClick()
    {
        if (placer != null) placer.Select(building);
    }
}
