using System;
using System.Collections.Generic;
using System.IO;
using PKHeX.Core;

namespace PKMMerge;

/// <summary>
/// File-backed HOME storage next to PKHeX.exe: Home/Box 1 .. Home/Box 32, at most
/// <see cref="SlotCount"/> .pkh files per box (6×5, the same box shape as PKHeX's box viewer).
/// A slot's position is persisted as a 3-digit file-name prefix: "007 - Pikachu.pkh" sits in slot 7.
/// Files without a usable prefix are renamed into the first free slot when the box is loaded,
/// so dropping a fresh .pkh into the folder from Explorer still gets a stable position.
/// </summary>
internal static class HomeStorage
{
    public const int BoxCount = 32;
    public const int SlotCount = 30;

    public static string Root => Path.Combine(AppContext.BaseDirectory, "Home");
    public static string GetBoxName(int box) => $"Box {box + 1}";
    public static string GetBoxDir(int box) => Path.Combine(Root, GetBoxName(box));

    /// <summary>Canonical path a PKH saved into <paramref name="slot"/> by the editor should get.</summary>
    public static string GetCanonicalPath(int box, int slot, PKH pkh) =>
        Path.Combine(GetBoxDir(box), BuildFileName(slot, PathUtil.CleanFileName(PkhService.GetDefaultFileName(pkh))));

    /// <summary>Reads a box into 30 slot paths (null = empty), renaming unplaced files into free slots.</summary>
    public static string?[] LoadBox(int box, out string? warning)
    {
        warning = null;
        var slots = new string?[SlotCount];
        var dir = GetBoxDir(box);
        string[] files;
        try
        {
            files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.pkh") : [];
        }
        catch (Exception ex)
        {
            warning = $"Can't read {dir}: {ex.Message}";
            return slots;
        }

        var used = new bool[SlotCount];
        var odd = new List<string>();
        foreach (var file in files)
        {
            int index = GetIndex(Path.GetFileNameWithoutExtension(file));
            if ((uint)index < (uint)SlotCount && !used[index])
            {
                slots[index] = file;
                used[index] = true;
            }
            else
            {
                odd.Add(file);
            }
        }

        foreach (var file in odd)
        {
            int free = Array.FindIndex(used, z => !z);
            if (free < 0)
            {
                warning = $"{GetBoxName(box)} holds more than {SlotCount} .pkh files; the extras are ignored.";
                break;
            }
            var target = Path.Combine(dir, BuildFileName(free, StripIndex(Path.GetFileNameWithoutExtension(file))));
            try
            {
                if (File.Exists(target))
                    throw new IOException($"name clash with \"{Path.GetFileName(target)}\"");
                File.Move(file, target);
                slots[free] = target;
                used[free] = true;
            }
            catch (Exception ex)
            {
                warning = $"Couldn't place \"{Path.GetFileName(file)}\": {ex.Message}";
            }
        }
        return slots;
    }

    /// <summary>Copies (or moves) <paramref name="sourcePath"/> into a box slot under its canonical name.</summary>
    public static bool Place(int box, int slot, string sourcePath, bool move, out string error)
    {
        error = "";
        try
        {
            var dir = GetBoxDir(box);
            Directory.CreateDirectory(dir);
            var target = Path.Combine(dir, BuildFileName(slot, StripIndex(Path.GetFileNameWithoutExtension(sourcePath))));
            if (string.Equals(Path.GetFullPath(target), Path.GetFullPath(sourcePath), StringComparison.OrdinalIgnoreCase))
                return true; // already there
            if (move)
                File.Move(sourcePath, target);
            else
                File.Copy(sourcePath, target);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Exchanges two slots of the same box by renaming. On failure the caller just re-scans the box.</summary>
    public static bool Swap(int box, string pathA, int slotA, string pathB, int slotB, out string error)
    {
        error = "";
        var dir = GetBoxDir(box);
        // Not *.pkh, so a stray temp file can never be picked up as box content.
        var tmp = Path.Combine(dir, $".swap-{Guid.NewGuid():N}.tmp");
        try
        {
            File.Move(pathA, tmp);
            File.Move(pathB, Path.Combine(dir, BuildFileName(slotA, StripIndex(Path.GetFileNameWithoutExtension(pathB)))));
            File.Move(tmp, Path.Combine(dir, BuildFileName(slotB, StripIndex(Path.GetFileNameWithoutExtension(pathA)))));
            return true;
        }
        catch (Exception ex)
        {
            error = $"Swap failed: {ex.Message}";
            try { if (File.Exists(tmp)) File.Move(tmp, pathA); }
            catch { /* best effort; a re-scan below reflects whatever is actually on disk */ }
            return false;
        }
    }

    /// <summary>Deletes a stored file, refusing anything outside the Home folder.</summary>
    public static bool Delete(string path, out string error)
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
            File.Delete(full);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Exchanges the contents of two boxes (slot prefixes are kept), like PKHeX's box swap.</summary>
    public static bool SwapBoxes(int boxA, int boxB, out string error)
    {
        error = "";
        if (boxA == boxB)
            return true;
        var dirA = GetBoxDir(boxA);
        var dirB = GetBoxDir(boxB);
        try
        {
            var slotsA = LoadBox(boxA, out _); // normalizes any stray file names first
            var slotsB = LoadBox(boxB, out _);
            for (int i = 0; i < SlotCount; i++)
            {
                var fa = slotsA[i];
                var fb = slotsB[i];
                if (fa == null && fb == null)
                    continue;
                Directory.CreateDirectory(dirA);
                Directory.CreateDirectory(dirB);
                if (fa != null && fb != null && string.Equals(Path.GetFileName(fa), Path.GetFileName(fb), StringComparison.OrdinalIgnoreCase))
                {
                    // Identical names (same slot, same Pokémon name): park one in a temp file first.
                    var tmp = Path.Combine(dirA, $".swap-{Guid.NewGuid():N}.tmp");
                    File.Move(fa, tmp);
                    File.Move(fb, Path.Combine(dirA, Path.GetFileName(fa)));
                    File.Move(tmp, Path.Combine(dirB, Path.GetFileName(fb)));
                }
                else
                {
                    if (fa != null)
                        File.Move(fa, Path.Combine(dirB, Path.GetFileName(fa)));
                    if (fb != null)
                        File.Move(fb, Path.Combine(dirA, Path.GetFileName(fb)));
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            error = $"Box swap failed: {ex.Message}";
            return false;
        }
    }

    private static string BuildFileName(int slot, string baseName)
    {
        baseName = string.IsNullOrWhiteSpace(baseName) ? "pokemon" : baseName;
        return $"{slot:000} - {baseName}.pkh";
    }

    /// <summary>Slot index from a "NNN - ..." prefix, or -1 when the name doesn't carry one.</summary>
    private static int GetIndex(string name) =>
        name.Length >= 6 && name[3] == ' ' && name[4] == '-' && name[5] == ' ' && int.TryParse(name.AsSpan(0, 3), out var index)
            ? index
            : -1;

    private static string StripIndex(string name) => GetIndex(name) >= 0 ? name[6..] : name;
}
