using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHEdit;

/// <summary>
/// Reaches PKHeX's box grid to select a slot. The <see cref="ISaveFileProvider"/> plugins receive is
/// PKHeX's own SAVEditor control (SAVEditor : UserControl, ISaveFileProvider), so its public Box field and
/// the PokeGrid behind it are one reflection hop away — no need to hunt down the main window. Every member
/// used is public; a missing one simply leaves the slot unselected, and the caller still reports where the
/// Pokémon was found.
/// </summary>
internal static class SaveBoxViewer
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

    /// <summary>Selects <paramref name="slot"/> of <paramref name="box"/> in PKHeX's box grid; false when
    /// the grid can't be reached.</summary>
    public static bool Select(ISaveFileProvider provider, int box, int slot)
    {
        try
        {
            if (GetEntries(provider) is not { } entries || (uint)slot >= (uint)entries.Count)
                return false;
            if (boxField!.GetValue(provider) is not { } boxEditor)
                return false;
            // Setting CurrentBox goes through PKHeX's own box combo, which loads the box and syncs SAV.CurrentBox.
            currentBoxProperty!.SetValue(boxEditor, box);
            (entries[slot] as Control)?.Focus();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PKHEdit: box slot selection failed: {ex.Message}");
            return false;
        }
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
