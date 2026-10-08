using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A label above a progress bar. The fill Image must use Image Type = Filled.
/// </summary>
public class ProgressRow : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [Tooltip("Image with Image Type = Filled (Horizontal).")]
    [SerializeField] private Image fill;

    public void Set(string text, float progress)
    {
        if (label != null) label.text = text;
        if (fill != null) fill.fillAmount = Mathf.Clamp01(progress);
    }
}
