using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHEdit;

/// <summary>
/// Reaches PKHeX's save view to select a slot. The <see cref="ISaveFileProvider"/> plugins receive is
/// PKHeX's own SAVEditor control (SAVEditor : UserControl, ISaveFileProvider), so its public Box field and
/// the PokeGrid behind it are one reflection hop away — no need to hunt down the main window. Every member
/// used is public; a missing one simply leaves the slot unselected, and the caller still reports where the
/// Pokémon was found. Party and other slots need no names at all: the control tree is searched through the
/// public <see cref="ISlotViewer{T}"/> interface for the viewer that knows the slot.
/// </summary>
internal static class SaveSlotViewer
{
    private static FieldInfo? boxField;
    private static PropertyInfo? currentBoxProperty;
    private static FieldInfo? gridField;
    private static FieldInfo? entriesField;
    private static bool resolved;

    /// <summary>Column count of the live box grid, or 0 when it can't be read. PKHeX lays the grid out to
    /// fit its panel, so slot math uses the actual layout rather than an assumed width.</summary>
    public static int GetColumns(ISaveFileProvider provider)
    {
        if (GetEntries(provider) is not { Count: > 0 } entries || entries[0] is not Control first)
            return 0;
        int columns = 0;
        foreach (var entry in entries)
        {
            if (entry is Control c && c.Location.Y == first.Location.Y)
                columns++;
        }
        return columns;
    }

    /// <summary>Selects <paramref name="slot"/> in PKHeX's save view; false when it can't be reached.</summary>
    public static bool Select(ISaveFileProvider provider, ISlotInfo slot)
    {
        if (slot is SlotInfoBox box)
            return SelectBox(provider, box.Box, box.Slot);
        return provider is Control root && FindViewer(root, slot) is { } viewer && FocusSlot(ViewerSlot(viewer, slot));
    }

    private static bool SelectBox(ISaveFileProvider provider, int box, int slot)
    {
        try
        {
            if (GetEntries(provider) is not { } entries || (uint)slot >= (uint)entries.Count)
                return false;
            if (boxField!.GetValue(provider) is not { } boxEditor)
                return false;
            // Setting CurrentBox goes through PKHeX's own box combo, which loads the box and syncs SAV.CurrentBox.
            currentBoxProperty!.SetValue(boxEditor, box);
            return FocusSlot(entries[slot] as Control);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PKHEdit: box slot selection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>The picture box showing <paramref name="slot"/>, or null when the viewer doesn't show it.</summary>
    private static Control? ViewerSlot(ISlotViewer<PictureBox> viewer, ISlotInfo slot)
    {
        try
        {
            int index = viewer.GetViewIndex(slot);
            return (uint)index < (uint)viewer.SlotPictureBoxes.Count ? viewer.SlotPictureBoxes[index] : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PKHEdit: slot view lookup failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>The viewer inside the save view that shows this slot (box grid, party, or the other-slots list).</summary>
    private static ISlotViewer<PictureBox>? FindViewer(Control parent, ISlotInfo slot)
    {
        foreach (Control child in parent.Controls)
        {
            if (FindViewer(child, slot) is { } nested)
                return nested;
        }
        return parent is ISlotViewer<PictureBox> viewer && viewer.GetViewIndex(slot) >= 0 ? viewer : null;
    }

    /// <summary>Shows the tab page holding the slot (the party lives on its own tab), then focuses it.</summary>
    private static bool FocusSlot(Control? slot)
    {
        if (slot is null)
            return false;
        Control? child = slot;
        for (var parent = slot.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is TabControl tabs && child is TabPage page && tabs.SelectedTab != page)
            {
                tabs.SelectedTab = page;
                break;
            }
            child = parent;
        }
        slot.Focus();
        return true;
    }

    private static IList? GetEntries(ISaveFileProvider provider)
    {
        try
        {
            if (!resolved)
                Resolve(provider);
            if (boxField is null || gridField is null || entriesField is null)
                return null;
            if (boxField.GetValue(provider) is not { } boxEditor)
                return null;
            if (gridField.GetValue(boxEditor) is not { } grid)
                return null;
            return entriesField.GetValue(grid) as IList;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PKHEdit: box grid lookup failed: {ex.Message}");
            return null;
        }
    }

    private static void Resolve(ISaveFileProvider provider)
    {
        resolved = true;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        boxField = provider.GetType().GetField("Box", flags);
        currentBoxProperty = boxField?.FieldType.GetProperty("CurrentBox", flags);
        gridField = boxField?.FieldType.GetField("BoxPokeGrid", flags);
        entriesField = gridField?.FieldType.GetField("Entries", flags);
    }
}
