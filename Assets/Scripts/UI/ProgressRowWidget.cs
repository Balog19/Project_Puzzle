using UnityEngine;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// A label above a progress bar (layout: UI/Layouts/ProgressRow.uxml).
    /// Reusable anywhere a timer or fill level is shown.
    /// </summary>
    public class ProgressRowWidget : Widget
    {
        private readonly Label _label;
        private readonly VisualElement _fill;

        public ProgressRowWidget(VisualElement root) : base(root)
        {
            _label = Q<Label>("label");
            _fill = Q<VisualElement>("fill");
        }

        /// <param name="text">Label text.</param>
        /// <param name="progress">0 to 1.</param>
        public void Set(string text, float progress)
        {
            _label.text = text;
            _fill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
        }
    }
}
