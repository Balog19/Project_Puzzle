using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// An amount of one resource, e.g. 5 Wood. Used for costs, production and starting stock.
/// </summary>
[Serializable]
public struct ResourceAmount
{
    [SerializeField] private ResourceType type;
    [SerializeField, Min(0)] private int amount;

    public ResourceType Type => type;
    public int Amount => amount;

    public ResourceAmount(ResourceType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }

    /// <summary>"5 Wood, 2 Stone". Empty string for an empty or null list.</summary>
    public static string Format(IReadOnlyList<ResourceAmount> amounts)
    {
        if (amounts == null || amounts.Count == 0) return string.Empty;

        var text = new StringBuilder();
        foreach (ResourceAmount item in amounts)
        {
            if (item.type == null || item.amount <= 0) continue;
            if (text.Length > 0) text.Append(", ");
            text.Append(item.amount).Append(' ').Append(item.type.DisplayName);
        }
        return text.ToString();
    }
}
