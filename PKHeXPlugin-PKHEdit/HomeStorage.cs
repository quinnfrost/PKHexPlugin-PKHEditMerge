using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using PKHeX.Core;

namespace PKHEdit;

/// <summary>
/// File-backed HOME storage: every .pkh sits flat under {run dir}/Home with a PKHeX-standard file
/// name, and a manifest (Home/home.json) records which box and slot each file occupies.
/// The manifest is the only source of truth for positions — file names carry no slot information.
/// Loading reconciles the manifest with the folder: files without an entry get the first free slot
/// (the box being viewed first), entries for vanished files are dropped, and repairs are written back.
/// Legacy layout (Box N subfolders, "007 - ..." prefixes) is not migrated — such files, if present
/// in the Home folder, are simply treated as untracked files and given a position.
/// </summary>
internal static class HomeStorage
{
    public const int BoxCount = 32;
    public const int SlotCount = 30;

    // PKHeX is published as a single-file bundle with IncludeAllContentForSelfExtract: at runtime
    // AppContext.BaseDirectory points at the %LOCALAPPDATA%\Temp\.net\PKHeX\<hash> extraction folder,
    // not at PKHeX.exe. Environment.ProcessPath always resolves to the real exe — the same convention
    // as PKHeX's own Program.WorkingDirectory (Program.cs) — so Home lives next to PKHeX.exe.
    private static readonly string AppDir = Path.GetDirectoryName(Environment.ProcessPath) is { Length: not 0 } dir
        ? dir
        : AppContext.BaseDirectory;

    public static string Root => Path.Combine(AppDir, "Home");
    private static string ManifestPath => Path.Combine(Root, "home.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string GetBoxName(int box) => $"Box {box + 1}";

    /// <summary>Saved state of the editor's "Use Custom Tracker" checkbox (false when never stored).</summary>
    public static bool GetUseCustomTracker() => LoadManifest(out _).UseCustomTracker;

    /// <summary>Persists the checkbox state to home.json; a failed write is tolerated (preference only).</summary>
    public static void SetUseCustomTracker(bool value)
    {
        var manifest = LoadManifest(out _);
        if (manifest.UseCustomTracker == value)
            return; // nothing changed — avoid a redundant disk write
        manifest.UseCustomTracker = value;
        SaveManifest(manifest, out _);
    }

    private sealed class Manifest
    {
        public int Version { get; set; } = 1;

        /// <summary>The PKH Editor's "Use Custom Tracker" checkbox; false when absent (old manifests / default).</summary>
        public bool UseCustomTracker { get; set; }

        public Dictionary<string, Entry> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class Entry
    {
        public int Box { get; set; }
        public int Slot { get; set; }
    }

    /// <summary>Standard PKHeX-style file name for a PKH saved into a slot. The position itself is
    /// recorded in the manifest afterwards, so <paramref name="box"/>/<paramref name="slot"/> only
    /// matter for choosing a clash-free name.</summary>
    public static string GetCanonicalPath(int box, int slot, PKH pkh)
    {
        _ = box;
        _ = slot;
        var name = PathUtil.CleanFileName(PkhService.GetDefaultFileName(pkh)) + ".pkh";
        return MakeUnique(Root, name);
    }

    /// <summary>Reads a box from the manifest, reconciling it with the Home folder first.</summary>
    public static string?[] LoadBox(int box, out string? warning)
    {
        warning = null;
        var slots = new string?[SlotCount];
        try
        {
            Directory.CreateDirectory(Root);
        }
        catch (Exception ex)
        {
            warning = $"Can't create {Root}: {ex.Message}";
            return slots;
        }

        var manifest = LoadManifest(out var manifestWarning);
        warning = manifestWarning;
        bool changed = false;

        // Drop entries that point nowhere: missing file, or an out-of-range box/slot.
        foreach (var key in manifest.Files
                     .Where(kv => (uint)kv.Value.Box >= (uint)BoxCount
                              || (uint)kv.Value.Slot >= (uint)SlotCount
                              || !File.Exists(Path.Combine(Root, kv.Key)))
                     .Select(kv => kv.Key)
                     .ToList())
        {
            manifest.Files.Remove(key);
            changed = true;
        }

        // Files the manifest doesn't know yet get a position: the viewed box first, then anywhere.
        foreach (var file in Directory.GetFiles(Root, "*.pkh"))
        {
            var name = Path.GetFileName(file);
            if (manifest.Files.ContainsKey(name))
                continue;
            if (!TryFindFreeSlot(manifest, box, out var freeBox, out var freeSlot))
            {
                warning = $"No free slot left for \"{name}\" — all {BoxCount} boxes are full.";
                break;
            }
            manifest.Files[name] = new Entry { Box = freeBox, Slot = freeSlot };
            changed = true;
        }

        // Two files sharing a slot (hand-edited manifest): keep the first, relocate the rest.
        var seen = new HashSet<(int Box, int Slot)>();
        foreach (var kv in manifest.Files.OrderBy(z => z.Key, StringComparer.OrdinalIgnoreCase).ToList())
        {
            if (seen.Add((kv.Value.Box, kv.Value.Slot)))
                continue;
            manifest.Files.Remove(kv.Key);
            changed = true;
            if (TryFindFreeSlot(manifest, kv.Value.Box, out var newBox, out var newSlot))
            {
                manifest.Files[kv.Key] = new Entry { Box = newBox, Slot = newSlot };
                seen.Add((newBox, newSlot));
            }
            else
            {
                warning = $"No free slot left for \"{kv.Key}\" — all {BoxCount} boxes are full.";
            }
        }

        foreach (var kv in manifest.Files)
        {
            if (kv.Value.Box == box)
                slots[kv.Value.Slot] = Path.Combine(Root, kv.Key);
        }

        if (changed && warning == null && !SaveManifest(manifest, out var saveError))
            warning = $"Couldn't write home.json: {saveError}";
        return slots;
    }

    /// <summary>Imports an external .pkh into a slot: copies it into Home under a unique standard name.</summary>
    public static bool Place(int box, int slot, string sourcePath, out string error)
    {
        error = "";
        try
        {
            Directory.CreateDirectory(Root);
            var target = Path.Combine(Root, Path.GetFileName(sourcePath));
            if (!string.Equals(Path.GetFullPath(target), Path.GetFullPath(sourcePath), StringComparison.OrdinalIgnoreCase))
            {
                target = MakeUnique(Root, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, target);
            }
            // Already a Home file (re-dropped): keep the name, just record the new position.
            return Record(Path.GetFileName(target), box, slot, out error);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Points an existing Home file at a slot; only the manifest changes, the file doesn't move.</summary>
    public static bool SetPosition(string path, int box, int slot, out string error)
    {
        if (!File.Exists(path))
        {
            error = $"\"{Path.GetFileName(path)}\" no longer exists.";
            return false;
        }
        return Record(Path.GetFileName(path), box, slot, out error);
    }

    /// <summary>Exchanges the recorded positions of two slots of the same box; nothing moves on disk.</summary>
    public static bool Swap(int box, string pathA, int slotA, string pathB, int slotB, out string error)
    {
        var manifest = LoadManifest(out _);
        manifest.Files[Path.GetFileName(pathA)] = new Entry { Box = box, Slot = slotB };
        manifest.Files[Path.GetFileName(pathB)] = new Entry { Box = box, Slot = slotA };
        return SaveManifest(manifest, out error);
    }

    /// <summary>Exchanges the contents of two boxes (slot numbers kept) — a manifest-only operation.</summary>
    public static bool SwapBoxes(int boxA, int boxB, out string error)
    {
        error = "";
        if (boxA == boxB)
            return true;
        var manifest = LoadManifest(out _);
        bool changed = false;
        foreach (var entry in manifest.Files.Values)
        {
            if (entry.Box == boxA)
            {
                entry.Box = boxB;
                changed = true;
            }
            else if (entry.Box == boxB)
            {
                entry.Box = boxA;
                changed = true;
            }
        }
        return !changed || SaveManifest(manifest, out error);
    }

    /// <summary>Deletes a stored file, refusing anything outside the Home folder. With
    /// <paramref name="recycle"/> the file is first sent to the Recycle Bin; if no bin is available
    /// (disabled/unsupported/too large) it is deleted directly instead.</summary>
    public static bool Delete(string path, bool recycle, out string error)
    {
        error = "";
        try
        {
            var full = Path.GetFullPath(path);
            var root = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                error = "Refusing to delete a file outside the Home folder.";
                return false;
            }
            if (recycle)
                TryMoveToRecycleBin(full);
            if (File.Exists(full)) // Shift-delete, or the Recycle Bin refused/unavailable
                File.Delete(full);
            var manifest = LoadManifest(out _);
            if (manifest.Files.Remove(Path.GetFileName(full)))
                SaveManifest(manifest, out _); // a stale entry would be pruned on the next load anyway
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    // --- Recycle Bin (FOF_ALLOWUNDO), fully silent: any refusal leaves the file for the direct delete ---

    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040; // use the Recycle Bin instead of erasing
    private const ushort FOF_NOERRORUI = 0x0400;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCTW
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCTW lpFileOp);

    /// <summary>Best-effort move to the Recycle Bin; leaves the file untouched when the shell refuses
    /// (bin disabled, too large, unsupported volume) so the caller can delete it directly.</summary>
    private static void TryMoveToRecycleBin(string path)
    {
        try
        {
            var op = new SHFILEOPSTRUCTW
            {
                wFunc = FO_DELETE,
                pFrom = path + "\0", // the marshaller adds the terminator → path\0\0, a single-item list
                pTo = string.Empty,
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI,
            };
            SHFileOperation(ref op);
        }
        catch (Exception)
        {
            // shell unavailable — the caller falls back to a direct delete
        }
    }

    /// <summary>Records where a file (already inside Home) lives; a different file in that slot becomes untracked.</summary>
    private static bool Record(string fileName, int box, int slot, out string error)
    {
        var manifest = LoadManifest(out _);
        foreach (var clash in manifest.Files
                     .Where(kv => kv.Value.Box == box && kv.Value.Slot == slot
                              && !string.Equals(kv.Key, fileName, StringComparison.OrdinalIgnoreCase))
                     .Select(kv => kv.Key)
                     .ToList())
        {
            manifest.Files.Remove(clash); // now untracked; the next load assigns it a free slot
        }
        manifest.Files[fileName] = new Entry { Box = box, Slot = slot };
        return SaveManifest(manifest, out error);
    }

    private static bool TryFindFreeSlot(Manifest manifest, int preferredBox, out int foundBox, out int foundSlot)
    {
        var taken = new HashSet<(int, int)>(manifest.Files.Values.Select(e => (e.Box, e.Slot)));
        for (int pass = 0; pass < 2; pass++)
        {
            int start = pass == 0 ? preferredBox : 0;
            int end = pass == 0 ? preferredBox + 1 : BoxCount;
            for (int b = start; b < end; b++)
            {
                if ((uint)b >= (uint)BoxCount)
                    continue;
                for (int s = 0; s < SlotCount; s++)
                {
                    if (taken.Contains((b, s)))
                        continue;
                    foundBox = b;
                    foundSlot = s;
                    return true;
                }
            }
        }
        foundBox = -1;
        foundSlot = -1;
        return false;
    }

    private static string MakeUnique(string dir, string fileName)
    {
        var target = Path.Combine(dir, fileName);
        if (!File.Exists(target))
            return target;
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (int i = 2; ; i++)
        {
            var candidate = Path.Combine(dir, $"{baseName} ({i}){ext}");
            if (!File.Exists(candidate))
                return candidate;
        }
    }

    private static Manifest LoadManifest(out string? warning)
    {
        warning = null;
        try
        {
            if (!File.Exists(ManifestPath))
                return new Manifest();
            var loaded = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(ManifestPath));
            if (loaded?.Files == null)
            {
                warning = "home.json is invalid and was ignored.";
                return new Manifest();
            }
            // Rebuild the dictionary with a case-insensitive comparer IN PLACE. Constructing a new
            // Manifest here used to drop every field other than Version/Files (UseCustomTracker),
            // so the next manifest write from any box operation silently erased the checkbox state.
            loaded.Files = new Dictionary<string, Entry>(loaded.Files, StringComparer.OrdinalIgnoreCase);
            return loaded;
        }
        catch (Exception ex)
        {
            warning = $"home.json couldn't be read ({ex.Message}) and was ignored.";
            return new Manifest();
        }
    }

    private static bool SaveManifest(Manifest manifest, out string error)
    {
        error = "";
        try
        {
            Directory.CreateDirectory(Root);
            // Write-then-replace so a crash mid-write can never truncate the previous manifest.
            var tmp = ManifestPath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(manifest, JsonOptions));
            File.Move(tmp, ManifestPath, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
