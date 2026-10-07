using TMPro;
using UnityEngine;

/// <summary>
/// Shows "housed / total" population on a TextMeshPro label.
/// </summary>
public class PopulationCounter : MonoBehaviour
{
    [SerializeField] private PopulationManager population;
    [Tooltip("Defaults to the TMP_Text on this object.")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private string prefix = "Housed: ";

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (population == null) return;
        population.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (population != null) population.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (label == null || population == null) return;
        label.text = $"{prefix}{population.Housed} / {population.Total}";
    }
}
