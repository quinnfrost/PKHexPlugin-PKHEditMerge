using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using PKHeX.Core;

namespace PKHEdit;

/// <summary>
/// File-backed HOME storage: every .pkh sits flat under {run dir}/Home with a PKHeX-standard file
/// name, and a manifest (Home/home.json) records which box and slot each file occupies.
/// The manifest is the only source of truth for positions — file names carry no slot information,
/// and neither do box display names: renaming a box only stores a label, every file stays bound to
/// its box/slot index (so names may hold any text, including Chinese characters).
/// How many boxes exist is part of the manifest too: <c>MaxBoxes</c> is the capacity (64 by default,
/// set by hand in home.json) and <c>DisplayedBoxes</c> the number the viewer shows, which grows in
/// 8-box units as the tail fills up and only ever shrinks when home.json is edited by hand.
/// Boxes are an index, so inserting or deleting one shifts the indexes of the later boxes (a manifest
/// rewrite); inserting drops the empty tail box, exactly like the games do.
/// Loading reconciles the manifest with the folder: files without an entry get the first free slot
/// (the box being viewed first), entries for vanished files are dropped, and repairs are written back —
/// and the first write of a new schema keeps the previous home.json as home.json.bak.
/// Legacy layout (Box N subfolders, "007 - ..." prefixes) is not migrated — such files, if present
/// in the Home folder, are simply treated as untracked files and given a position.
/// </summary>
internal static class HomeStorage
{
    public const int SlotCount = 30;

    /// <summary>Boxes per display unit: the displayed count is always a multiple of this.</summary>
    public const int BoxUnit = 8;

    /// <summary>How many boxes are shown when home.json says nothing (and none of them hold anything).</summary>
    public const int DefaultDisplayedBoxes = BoxUnit;

    /// <summary>Box capacity used when home.json has no (valid) MaxBoxes.</summary>
    public const int DefaultMaxBoxes = 64;

    /// <summary>Manifest schema version written by this build; the .bak file preserves the pre-upgrade one.</summary>
    private const int CurrentVersion = 2;

    /// <summary>Longest box display name accepted by <see cref="SetBoxName"/>.</summary>
    public const int MaxBoxNameLength = 40;

    // PKHeX is published as a single-file bundle with IncludeAllContentForSelfExtract: at runtime
    // AppContext.BaseDirectory points at the %LOCALAPPDATA%\Temp\.net\PKHeX\<hash> extraction folder,
    // not at PKHeX.exe. Environment.ProcessPath always resolves to the real exe — the same convention
    // as PKHeX's own Program.WorkingDirectory (Program.cs) — so Home lives next to PKHeX.exe.
    private static readonly string AppDir = Path.GetDirectoryName(Environment.ProcessPath) is { Length: not 0 } dir
        ? dir
        : AppContext.BaseDirectory;

    public static string Root => Path.Combine(AppDir, "Home");
    private static string ManifestPath => Path.Combine(Root, "home.json");
    private static string BackupPath => ManifestPath + ".bak";

    // Names are user text (often non-ASCII), so the relaxed encoder keeps home.json readable instead of
    // writing \uXXXX escapes. It is a local metadata file, never embedded in HTML/JS, so leaving
    // &, <, > and quotes literal is safe.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Default display name of a box. Only ever shown to the user — the box a file lives in is
    /// recorded as an index in the manifest, so names can be changed without touching any file.</summary>
    public static string GetDefaultBoxName(int box) => $"Box {box + 1}";

    /// <summary>All displayed box names (the manifest's custom names with defaults filled in), plus the
    /// layout they belong to, from a single manifest read.</summary>
    public static string[] GetBoxNames(out BoxLayout layout)
    {
        var manifest = LoadManifest(out _);
        layout = Layout(manifest);
        var names = new string[layout.Displayed];
        for (int i = 0; i < names.Length; i++)
            names[i] = ResolveBoxName(manifest.BoxNames, i);
        return names;
    }

    /// <summary>Display name of one box (its default name when it was never renamed).</summary>
    public static string GetBoxName(int box) =>
        box >= 0 ? ResolveBoxName(LoadManifest(out _).BoxNames, box) : GetDefaultBoxName(box);

    private static string ResolveBoxName(Dictionary<int, string> custom, int box) =>
        custom.TryGetValue(box, out var name) && !string.IsNullOrWhiteSpace(name) ? name : GetDefaultBoxName(box);

    /// <summary>Stores a box display name; null/blank (or the default name itself) resets it to
    /// "Box N". Only the manifest is written: the files keep their box/slot indexes, and a name that
    /// collides with another box's is allowed (nothing is ever resolved by name).</summary>
    public static bool SetBoxName(int box, string? name, out string error)
    {
        error = "";
        if (box < 0)
        {
            error = $"There is no box {box + 1}.";
            return false;
        }
        name = name?.Trim();
        bool reset = string.IsNullOrEmpty(name)
                  || string.Equals(name, GetDefaultBoxName(box), StringComparison.OrdinalIgnoreCase);
        if (!reset)
        {
            if (name!.Length > MaxBoxNameLength)
            {
                error = $"A box name can be at most {MaxBoxNameLength} characters.";
                return false;
            }
            if (name.Any(char.IsControl))
            {
                error = "A box name can't contain line breaks or other control characters.";
                return false;
            }
        }
        var manifest = LoadManifest(out _);
        if (reset)
        {
            if (!manifest.BoxNames.Remove(box))
                return true; // already using the default name
        }
        else
        {
            manifest.BoxNames[box] = name!;
        }
        return SaveManifest(manifest, out error);
    }

    /// <summary>
    /// Inserts an empty box at <paramref name="index"/>, shifting that box and every later one up by one.
    /// The box pushed past the end is dropped when it was empty (the tail box is the viewer's spare slot,
    /// so an insert is really "add one, drop the last"); when the shifted content lands in the last
    /// displayed box, the display grows a unit instead. Fails when the last box already holds a PKH,
    /// because that box cannot be dropped — the same rule the game uses.
    /// </summary>
    public static bool InsertBox(int index, out string error)
    {
        error = "";
        var manifest = LoadManifest(out _);
        var layout = Layout(manifest);
        if (index < 0 || index >= layout.Displayed)
        {
            error = $"Box {index + 1} isn't one of the shown boxes.";
            return false;
        }
        int last = layout.Displayed - 1;
        if (IsOccupied(manifest, last))
        {
            error = $"\"{ResolveBoxName(manifest.BoxNames, last)}\" still holds a PKH, so there is no room to insert a box. Move its PKH out first.";
            return false;
        }

        foreach (var entry in manifest.Files.Values)
        {
            if (entry.Box >= index)
                entry.Box++;
        }
        ShiftNames(manifest, index, +1);
        return SaveManifest(manifest, out error);
    }

    /// <summary>Deletes the empty box at <paramref name="index"/>; the boxes after it move down by one.</summary>
    public static bool DeleteBox(int index, out string error)
    {
        error = "";
        var manifest = LoadManifest(out _);
        var layout = Layout(manifest);
        if (index < 0 || index >= layout.Displayed)
        {
            error = $"Box {index + 1} isn't one of the shown boxes.";
            return false;
        }
        if (IsOccupied(manifest, index))
        {
            error = $"\"{ResolveBoxName(manifest.BoxNames, index)}\" still holds a PKH. Move its PKH out before deleting the box.";
            return false;
        }

        foreach (var entry in manifest.Files.Values)
        {
            if (entry.Box > index)
                entry.Box--;
        }
        manifest.BoxNames.Remove(index);
        ShiftNames(manifest, index + 1, -1);
        return SaveManifest(manifest, out error);
    }

    /// <summary>Exchanges the contents of two boxes; the name follows, like a physical box whose label
    /// travels with what is inside it. Nothing moves on disk — a manifest-only operation.</summary>
    public static bool SwapBoxes(int boxA, int boxB, out string error)
    {
        error = "";
        if (boxA == boxB)
            return true;
        var manifest = LoadManifest(out _);
        foreach (var entry in manifest.Files.Values)
        {
            if (entry.Box == boxA)
                entry.Box = boxB;
            else if (entry.Box == boxB)
                entry.Box = boxA;
        }
        SwapNames(manifest, boxA, boxB);
        return SaveManifest(manifest, out error);
    }

    /// <summary>Moves the names at <paramref name="from"/> and later by <paramref name="delta"/>; shifting
    /// up walks the keys in descending order so an entry is never overwritten.</summary>
    private static void ShiftNames(Manifest manifest, int from, int delta)
    {
        var keys = manifest.BoxNames.Keys.Where(k => k >= from).OrderBy(k => delta > 0 ? -k : k).ToList();
        foreach (var key in keys)
        {
            var name = manifest.BoxNames[key];
            manifest.BoxNames.Remove(key);
            manifest.BoxNames[key + delta] = name;
        }
    }

    private static void SwapNames(Manifest manifest, int boxA, int boxB)
    {
        bool hasA = manifest.BoxNames.Remove(boxA, out var nameA);
        bool hasB = manifest.BoxNames.Remove(boxB, out var nameB);
        if (hasB)
            manifest.BoxNames[boxA] = nameB!;
        if (hasA)
            manifest.BoxNames[boxB] = nameA!;
    }

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
        public int Version { get; set; } = CurrentVersion;

        /// <summary>The PKH Editor's "Use Custom Tracker" checkbox; false when absent (old manifests / default).</summary>
        public bool UseCustomTracker { get; set; }

        /// <summary>How many boxes the viewer shows; a hint only, never a hard limit. Grows by
        /// <see cref="BoxUnit"/> whenever the last displayed box holds something, and only ever shrinks
        /// when home.json is edited by hand. Absent in an old manifest (0) → derived from what is stored.</summary>
        public int DisplayedBoxes { get; set; }

        /// <summary>How many boxes may exist at most (default <see cref="DefaultMaxBoxes"/>, set by hand in
        /// home.json). Boxes beyond the displayed count can hold files, they are just not shown.</summary>
        public int MaxBoxes { get; set; }

        /// <summary>Display names of renamed boxes, keyed by box index; a missing box uses "Box N".
        /// Purely a label — box identity is always the index, never the name.</summary>
        public Dictionary<int, string> BoxNames { get; set; } = new();

        public Dictionary<string, Entry> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class Entry
    {
        public int Box { get; set; }
        public int Slot { get; set; }
    }

    /// <summary>How many boxes exist to place files in (<see cref="Max"/>) and how many the viewer shows
    /// (<see cref="Displayed"/>); <c>box &lt; DisplayedBoxes &lt;= MaxBoxes</c>.</summary>
    public readonly record struct BoxLayout(int Displayed, int Max);

    /// <summary>Manifest-level layout. The displayed count grows to the next whole unit <em>beyond</em> the
    /// last stored box — i.e. it covers every stored PKH and keeps the final box free as the viewer's spare
    /// slot — and never falls below what home.json already recorded, so it only increases (rule 1.2/1.3/1.4).
    /// Only when the capacity is reached can the last shown box hold something.</summary>
    private static BoxLayout Layout(Manifest manifest)
    {
        int max = manifest.MaxBoxes >= BoxUnit ? manifest.MaxBoxes : DefaultMaxBoxes;
        // MaxOccupiedBox is -1 for an empty store, so this is the smallest 8-box unit beyond the last
        // stored box: a unit whose boxes are all occupied therefore grows the display by one unit.
        int needed = UnitEnd(MaxOccupiedBox(manifest) + 2);
        int displayed = Math.Max(Math.Max(UnitEnd(manifest.DisplayedBoxes), DefaultDisplayedBoxes), needed);
        return new BoxLayout(Math.Min(displayed, max), max);
    }

    /// <summary>Rounds a box count up to whole 8-box units (0 stays 0).</summary>
    private static int UnitEnd(int boxes) => boxes <= 0 ? 0 : ((boxes + BoxUnit - 1) / BoxUnit) * BoxUnit;

    private static int MaxOccupiedBox(Manifest manifest)
    {
        int max = -1;
        foreach (var entry in manifest.Files.Values)
        {
            if (entry.Box > max)
                max = entry.Box;
        }
        return max;
    }

    private static bool IsOccupied(Manifest manifest, int box) => manifest.Files.Values.Any(e => e.Box == box);

    /// <summary>Standard PKHeX-style file name for a PKH, without folder or clash-free suffix.</summary>
    public static string GetCanonicalName(PKH pkh) => PathUtil.CleanFileName(PkhService.GetDefaultFileName(pkh)) + ".pkh";

    /// <summary>Standard PKHeX-style path for a PKH saved into a slot. The position itself is
    /// recorded in the manifest afterwards, so <paramref name="box"/>/<paramref name="slot"/> only
    /// matter for choosing a clash-free name. When <paramref name="replacingPath"/> is given (the file
    /// the new one takes over) and already carries the standard name, it is returned as-is rather than
    /// treated as a clash and suffixed.</summary>
    public static string GetCanonicalPath(int box, int slot, PKH pkh, string? replacingPath = null)
    {
        _ = box;
        _ = slot;
        var name = GetCanonicalName(pkh);
        if (replacingPath != null && string.Equals(Path.GetFileName(replacingPath), name, StringComparison.OrdinalIgnoreCase))
            return replacingPath;
        return MakeUnique(Root, name);
    }

    /// <summary>Renames a file inside the Home folder. The manifest is not touched — call
    /// <see cref="SetPosition"/> afterwards to record the new name for the slot.</summary>
    public static bool Rename(string oldPath, string newPath, out string error)
    {
        error = "";
        try
        {
            File.Move(oldPath, newPath);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
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

        // Drop entries that point nowhere: a missing file, or an impossible slot. Entries in boxes beyond
        // the displayed range are kept — they are invisible, not invalid, so lowering MaxBoxes by hand can
        // never silently delete someone's positions.
        foreach (var key in manifest.Files
                     .Where(kv => kv.Value.Box < 0
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
                warning = $"No free slot left for \"{name}\" — all {Layout(manifest).Max} boxes are full.";
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
                warning = $"No free slot left for \"{kv.Key}\" — all {Layout(manifest).Max} boxes are full.";
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

    /// <summary>Exchanges the recorded positions of two stored files, whatever boxes they live in
    /// (a drag between two HOME windows can thus swap slots across boxes); nothing moves on disk.</summary>
    public static bool SwapPositions(string pathA, string pathB, out string error)
    {
        error = "";
        var manifest = LoadManifest(out _);
        if (!manifest.Files.TryGetValue(Path.GetFileName(pathA), out var entryA)
            || !manifest.Files.TryGetValue(Path.GetFileName(pathB), out var entryB))
        {
            error = "One of the files is no longer tracked in home.json.";
            return false;
        }
        (entryA.Box, entryB.Box) = (entryB.Box, entryA.Box);
        (entryA.Slot, entryB.Slot) = (entryB.Slot, entryA.Slot);
        return SaveManifest(manifest, out error);
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
        int max = Layout(manifest).Max;
        for (int pass = 0; pass < 2; pass++)
        {
            int start = pass == 0 ? preferredBox : 0;
            int end = pass == 0 ? preferredBox + 1 : max;
            for (int b = start; b < end; b++)
            {
                if ((uint)b >= (uint)max)
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
            // Normalize in one place: the display never falls behind what is stored, and the counts have
            // usable values even when an older manifest (or a hand-edited one) lacks them.
            var layout = Layout(manifest);
            manifest.MaxBoxes = layout.Max;
            manifest.DisplayedBoxes = layout.Displayed;
            manifest.Version = CurrentVersion;
            // Keep the file as it was before this build started rewriting it, so the pre-upgrade manifest
            // (old plugin / old schema) survives any later change. Written once, never overwritten.
            if (File.Exists(ManifestPath) && !File.Exists(BackupPath))
                File.Copy(ManifestPath, BackupPath);
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
