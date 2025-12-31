using System.Numerics;
using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

/// <summary>
///     Table column definition for TableValue rendering.
/// </summary>
public sealed class TableColumn<T>
{
    public required string Header { get; init; }
    public required Action<T, int> Render { get; init; }
    public float Width { get; init; }
}

/// <summary>
///     Table-based list editor with inline editing in a tabular format.
/// </summary>
public sealed class TableValue<T> : SettingValue<List<T>>
{
    private readonly Func<T> _createItem;
    private readonly List<TableColumn<T>> _columns;

    public TableValue(
        Func<T> createItem,
        List<TableColumn<T>> columns)
        : base(new List<T>())
    {
        _createItem = createItem;
        _columns = columns;
    }

    public void Add(T item)
    {
        Value.Add(item);
        OnListChanged();
    }

    public bool Remove(T item)
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
        // Force change event by reassigning
        var list = Value;
        Value = list;
    }

    public override bool RenderImGui(string label)
    {
        var changed = false;

        // Header with count
        ImGui.Text($"{label} ({Value.Count})");

        // Calculate column count: # + user columns + Actions
        var columnCount = _columns.Count + 2;

        var tableFlags = ImGuiTableFlags.Borders |
                         ImGuiTableFlags.RowBg |
                         ImGuiTableFlags.Resizable |
                         ImGuiTableFlags.ScrollY;

        if (ImGui.BeginTable($"##{label}_table", columnCount, tableFlags, new Vector2(0, 200)))
        {
            // Setup columns
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30);
            foreach (var col in _columns)
            {
                if (col.Width > 0)
                    ImGui.TableSetupColumn(col.Header, ImGuiTableColumnFlags.WidthFixed, col.Width);
                else
                    ImGui.TableSetupColumn(col.Header, ImGuiTableColumnFlags.WidthStretch);
            }
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            // Render rows
            int? removeIndex = null;
            for (var i = 0; i < Value.Count; i++)
            {
                ImGui.TableNextRow();
                ImGui.PushID($"row_{i}");
                try
                {
                    // Row number
                    ImGui.TableNextColumn();
                    ImGui.Text($"{i + 1}");

                    // User columns
                    foreach (var col in _columns)
                    {
                        ImGui.TableNextColumn();
                        col.Render(Value[i], i);
                    }

                    // Actions column
                    ImGui.TableNextColumn();
                    if (ImGui.SmallButton("X##delete"))
                    {
                        removeIndex = i;
                    }
                }
                finally
                {
                    ImGui.PopID();
                }
            }

            ImGui.EndTable();

            if (removeIndex.HasValue)
            {
                RemoveAt(removeIndex.Value);
                changed = true;
            }
        }

        // Add button
        if (ImGui.Button($"+ Add Row##{label}_add"))
        {
            Add(_createItem());
            changed = true;
        }

        return changed;
    }
}
