using TMPro;
using UnityEngine;

/// <summary>
/// Shows one resource's amount ("Wood: 12") on a TextMeshPro label.
/// For another resource, duplicate the label and change the type.
/// </summary>
public class ResourceCounter : MonoBehaviour
{
    [SerializeField] private ResourceManager resources;
    [SerializeField] private ResourceType type;
    [Tooltip("Defaults to the TMP_Text on this object.")]
    [SerializeField] private TMP_Text label;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (resources == null) return;
        resources.Changed += OnResourceChanged;
        Refresh(resources.Get(type));
    }

    private void OnDisable()
    {
        if (resources != null) resources.Changed -= OnResourceChanged;
    }

    private void OnResourceChanged(ResourceType changed, int amount)
    {
        if (changed == type) Refresh(amount);
    }

    private void Refresh(int amount)
    {
        if (label == null || type == null) return;
        label.text = $"{type.DisplayName}: {amount}";
    }
}
