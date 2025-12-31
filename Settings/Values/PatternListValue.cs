using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

/// <summary>
///     List of wildcard patterns with add/remove functionality.
///     Supports * (any characters) and ? (single character).
/// </summary>
public sealed class PatternListValue : SettingValue<List<string>>
{
    private string _newItem = "";

    public int MaxLength { get; set; } = 256;
    public string PatternHint { get; set; } = "Supports * (any chars) and ? (single char)";

    public PatternListValue() : base([]) { }
    public PatternListValue(List<string> defaultValue) : base(defaultValue) { }

    /// <summary>
    ///     Returns true if the list contains any patterns.
    /// </summary>
    public bool HasPatterns => Value.Count > 0;

    public void Add(string item)
    {
        if (string.IsNullOrWhiteSpace(item)) return;
        Value.Add(item);
        OnListChanged();
    }

    public bool Remove(string item)
    {
        var result = Value.Remove(item);
        if (result) OnListChanged();
        return result;
    }

    public void RemoveAt(int index)
    {
        Value.RemoveAt(index);
        OnListChanged();
    }

    public void Clear()
    {
        Value.Clear();
        OnListChanged();
    }

    private void OnListChanged()
    {
        var list = Value;
        Value = list;
    }

    /// <summary>
    ///     Checks if the value matches any pattern in the list.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="ignoreCase">Whether to ignore case (default: true).</param>
    public bool Matches(string value, bool ignoreCase = true)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var pattern in Value)
        {
            if (MatchesPattern(value, pattern, ignoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if the value matches the wildcard pattern.
    /// </summary>
    private static bool MatchesPattern(string value, string pattern, bool ignoreCase)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        var regex = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        var options = ignoreCase
            ? RegexOptions.IgnoreCase
            : RegexOptions.None;

        return Regex.IsMatch(value, regex, options);
    }

    public override bool RenderImGui(string label)
    {
        var changed = false;

        ImGui.Text($"{label} ({Value.Count})");

        if (!string.IsNullOrEmpty(PatternHint))
        {
            ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), PatternHint);
        }

        if (ImGui.BeginChild($"##{label}_list", new Vector2(0, 120), ImGuiChildFlags.Borders))
        {
            int? removeIndex = null;
            for (var i = 0; i < Value.Count; i++)
            {
                ImGui.PushID($"item_{i}");
                try
                {
                    if (ImGui.SmallButton("X"))
                    {
                        removeIndex = i;
                    }
                    ImGui.SameLine();
                    ImGui.Text(Value[i]);
                }
                finally
                {
                    ImGui.PopID();
                }
            }

            if (removeIndex.HasValue)
            {
                RemoveAt(removeIndex.Value);
                changed = true;
            }
        }
        ImGui.EndChild();

        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText($"##{label}_input", ref _newItem, (uint)MaxLength, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (!string.IsNullOrWhiteSpace(_newItem))
            {
                Add(_newItem);
                _newItem = "";
                changed = true;
            }
        }
        ImGui.SameLine();
        if (ImGui.Button($"Add##{label}_add"))
        {
            if (!string.IsNullOrWhiteSpace(_newItem))
            {
                Add(_newItem);
                _newItem = "";
                changed = true;
            }
        }

        return changed;
    }
}
