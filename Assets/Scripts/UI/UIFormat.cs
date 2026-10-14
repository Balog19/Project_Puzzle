using System.Collections.Generic;
using System.Text;
using Puzzle.Economy;
using UnityEngine;

namespace Puzzle.UI
{
    /// <summary>
    /// How numbers are shown in the HUD, in one place.
    /// Stockpiles show as whole numbers; rates and costs show up to 3 decimals so small per-tick values stay readable.
    /// </summary>
    public static class UIFormat
    {
        /// <summary>A stockpile, rounded down: 12.9 -> "12", -0.4 -> "-1".</summary>
        public static string Whole(float value) => Mathf.FloorToInt(value).ToString();

        /// <summary>A rate or cost with up to 3 decimals: 0.005 -> "0.005", 5 -> "5".</summary>
        public static string Number(float value) => value.ToString("0.###");

        /// <summary>A signed per-tick rate: "+0.03", "-0.01".</summary>
        public static string SignedRate(float value) => (value >= 0f ? "+" : "") + Number(value);

        /// <summary>"5 Wood, 2 Stone" (costs). Empty string for an empty list.</summary>
        public static string Amounts(IReadOnlyList<ResourceAmount> amounts) => Join(amounts, "", "");

        /// <summary>"+0.03 Wood / tick" (production) or with sign "-", "-0.005 Money / tick" (upkeep).</summary>
        public static string RatesPerTick(IReadOnlyList<ResourceAmount> amounts, string sign) => Join(amounts, sign, " / tick");

        private static string Join(IReadOnlyList<ResourceAmount> amounts, string sign, string suffix)
        {
            if (amounts == null) return string.Empty;

            var text = new StringBuilder();
            foreach (ResourceAmount item in amounts)
            {
                if (item.Type == null || item.Amount <= 0f) continue;
                if (text.Length > 0) text.Append(", ");
                text.Append(sign).Append(Number(item.Amount)).Append(' ').Append(item.Type.DisplayName).Append(suffix);
            }
            return text.ToString();
        }
    }
}
