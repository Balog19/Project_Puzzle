using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// A "Label ........ Value" line (layout: UI/Layouts/InfoRow.uxml).
    /// Reusable anywhere a named stat is shown, e.g. "Produces   +0.03 Wood / tick".
    /// </summary>
    public class InfoRowWidget : Widget
    {
        private const string NegativeClass = "label--negative";

        private readonly Label _label;
        private readonly Label _value;

        public InfoRowWidget(VisualElement root) : base(root)
        {
            _label = Q<Label>("label");
            _value = Q<Label>("value");
        }

        /// <param name="negative">Shows the value in the warning colour (e.g. starving, upkeep).</param>
        public void Set(string label, string value, bool negative = false)
        {
            _label.text = label;
            _value.text = value;
            _value.EnableInClassList(NegativeClass, negative);
        }
    }
}
