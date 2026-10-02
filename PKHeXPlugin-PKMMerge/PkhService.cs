using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using PKHeX.Core;

namespace PKMMerge;

/// <summary>Severity of one entry in an <see cref="ImportPlan"/>.</summary>
internal enum ImportSeverity
{
    /// <summary>Expected PKHeX conversion; shown for information only.</summary>
    Info,
    /// <summary>Data loss or a change to other versions; the user must confirm.</summary>
    Confirm,
    /// <summary>Not allowed; the import cannot proceed.</summary>
    Block,
}

internal sealed record ImportItem(ImportSeverity Severity, string Group, string Text);

/// <summary>Result of a dry-run import: the updated PKH (not yet applied) and what would change.</summary>
internal sealed class ImportPlan(PKH result, IReadOnlyList<ImportItem> items)
{
    public PKH Result { get; } = result;
    public IReadOnlyList<ImportItem> Items { get; } = items;
    public bool IsBlocked => Items.Any(i => i.Severity == ImportSeverity.Block);
    public bool NeedsConfirm => Items.Any(i => i.Severity == ImportSeverity.Confirm);
}

/// <summary>
/// PKH load / save / import / export built only on PKHeX's own HOME conversion methods.
/// Every conversion runs on a copy, because PKHeX's ConvertToXX adds a reconstructed block to the PKH it is called on.
/// </summary>
internal static class PkhService
{
    /// <summary>Formats a PKH can be exported to (PKH.ConvertToPKM); PC9 has no PKM form.</summary>
    public static readonly HomeGameDataFormat[] ExportFormats =
    [
        HomeGameDataFormat.PB7, HomeGameDataFormat.PK8, HomeGameDataFormat.PA8,
        HomeGameDataFormat.PB8, HomeGameDataFormat.PK9, HomeGameDataFormat.PA9,
    ];

    // Marks a tracker this plugin generated, so it can't be mistaken for one HOME assigned.
    public const ulong GeneratedTrackerPrefix = 0xFFFF_0000_0000_0000;

    public static bool IsGeneratedTracker(ulong tracker) => (tracker & 0xFFFF_0000_0000_0000) == GeneratedTrackerPrefix;

    public static ulong NewTracker() => GeneratedTrackerPrefix | ((ulong)Random.Shared.NextInt64() & 0x0000_FFFF_FFFF_FFFF);

    public static bool HasPC9(PKH pkh) => pkh.DataPC9 != null;

    /// <summary>Formats that have stored data in this PKH, in PKHeX's write order.</summary>
    public static IReadOnlyList<HomeGameDataFormat> GetVersions(PKH pkh)
    {
        var list = new List<HomeGameDataFormat>();
        if (pkh.DataPB7 != null) list.Add(HomeGameDataFormat.PB7);
        if (pkh.DataPK8 != null) list.Add(HomeGameDataFormat.PK8);
        if (pkh.DataPA8 != null) list.Add(HomeGameDataFormat.PA8);
        if (pkh.DataPB8 != null) list.Add(HomeGameDataFormat.PB8);
        if (pkh.DataPK9 != null) list.Add(HomeGameDataFormat.PK9);
        if (pkh.DataPA9 != null) list.Add(HomeGameDataFormat.PA9);
        return list;
    }

    // Follows the main editor's save, not the right-side selection: picking a version to inspect shouldn't
    // change what "this PKH" looks like at a glance. When the active save's generation was never deposited,
    // falls back in PKH.LatestGameData's own preference order (nearest/newest first), not GetVersions' fixed
    // enum order -- that order put an arbitrary version first, not the best one.
    public static readonly HomeGameDataFormat[] FallbackPreference =
    [
        HomeGameDataFormat.PA9, HomeGameDataFormat.PB7, HomeGameDataFormat.PK9,
        HomeGameDataFormat.PB8, HomeGameDataFormat.PA8, HomeGameDataFormat.PK8,
    ];

    /// <summary>The stored version to display for <paramref name="pkh"/>: the active save's own generation when deposited, else the fallback order.</summary>
    public static HomeGameDataFormat GetPreferredFormat(PKH pkh, Type? savPkmType)
    {
        var versions = GetVersions(pkh);
        var current = savPkmType != null ? PKH.GetType(savPkmType) : HomeGameDataFormat.None;
        return versions.Contains(current)
            ? current
            : FallbackPreference.FirstOrDefault(versions.Contains, HomeGameDataFormat.None);
    }

    // PKH.FileNameWithoutExtension throws: PKHeX's namer slices Data up to SIZE_STORED, which exceeds a PKH's buffer.
    public static string GetDefaultFileName(PKH p)
    {
        foreach (var format in GetVersions(p))
        {
            if (Export(p, format) is { } pk)
                return pk.FileNameWithoutExtension;
        }
        return $"{p.Species:0000} - {p.Nickname} - {p.EncryptionConstant:X8}";
    }

    public static bool TryLoad(string path, [NotNullWhen(true)] out PKH? pkh, out string error)
    {
        pkh = null;
        try
        {
            pkh = FileUtil.GetSupportedFile(path) as PKH;
            error = pkh == null ? "not a PKH (HOME) file." : "";
            return pkh != null;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Independent copy. A PKH created in memory has an empty header until it is serialized,
    /// and PKHeX's Clone() re-parses that header, so serialize first when possible.
    /// </summary>
    public static PKH Copy(PKH pkh)
    {
        if (HasPC9(pkh))
            return pkh.Clone(); // Rebuild() can't serialize PC9 (WriteLength omits it); Clone keeps PC9 and needs a parsed header.
        return new PKH(pkh.Rebuild());
    }

    /// <summary>Exports the stored version as a PKM, without touching <paramref name="pkh"/>.</summary>
    public static PKM? Export(PKH pkh, HomeGameDataFormat format)
    {
        try { return Copy(pkh).ConvertToPKM(format); }
        catch (Exception) { return null; }
    }

    /// <summary>New PKH from the first PKM. A missing tracker gets a generated FFFF-prefixed one.</summary>
    public static PKH Create(PKM pk, out bool generatedTracker)
    {
        var pkh = new PKH(PKH.ConvertFromPKM(pk.Clone()).Rebuild());
        generatedTracker = pkh.Tracker == 0;
        if (generatedTracker)
            pkh.Tracker = NewTracker();
        return pkh;
    }

    /// <summary>Serialized bytes, and a check that PKHeX itself reads them back as an equivalent PKH.</summary>
    public static bool TrySerialize(PKH pkh, [NotNullWhen(true)] out byte[]? data, out string error)
    {
        data = null;
        if (HasPC9(pkh))
        {
            error = "PKH files with Champions (PC9) data can't be saved yet.";
            return false;
        }
        try
        {
            var bytes = pkh.Rebuild();
            if (FileUtil.GetSupportedFile(bytes, ".pkh") is not PKH back)
            {
                error = "PKHeX did not recognize the saved data as a PKH.";
                return false;
            }
            var mismatch = DiffCore(pkh, back).FirstOrDefault();
            if (mismatch != null || !GetVersions(pkh).SequenceEqual(GetVersions(back)))
            {
                error = $"Saved data did not read back identically ({mismatch?.Key ?? "versions"}).";
                return false;
            }
            data = bytes;
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TrySave(PKH pkh, string path, out string error)
    {
        if (!TrySerialize(pkh, out var data, out error))
            return false;
        try
        {
            File.WriteAllBytes(path, data);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    #region Import planning

    /// <summary>How the dropped PKM relates to the loaded PKH.</summary>
    internal enum Identity { SameTracker, DifferentTracker, MatchedWithoutTracker, MatchedPidDiffers, Mismatch }

    public static Identity GetIdentity(PKH pkh, PKM pk)
    {
        ulong pkTracker = pk is IHomeTrack t ? t.Tracker : 0;
        if (pkTracker != 0 && pkh.Tracker != 0)
            return pkTracker == pkh.Tracker ? Identity.SameTracker : Identity.DifferentTracker;

        // Species may differ through evolution, so it is not part of the match.
        bool same = pk.EncryptionConstant == pkh.EncryptionConstant
                    && pk.ID32 == pkh.ID32
                    && pk.OriginalTrainerName == pkh.OriginalTrainerName;
        if (!same)
            return Identity.Mismatch;
        return pk.PID == pkh.PID ? Identity.MatchedWithoutTracker : Identity.MatchedPidDiffers;
    }

    /// <summary>
    /// Dry-runs PKHeX's PKH.CopyFrom on a copy and classifies every resulting change.
    /// Nothing is applied until the caller adopts <see cref="ImportPlan.Result"/>.
    /// </summary>
    public static ImportPlan PlanImport(PKH current, PKM incoming)
    {
        var items = new List<ImportItem>();
        if (HasPC9(current))
        {
            items.Add(new(ImportSeverity.Block, "Format", "PKH files with Champions (PC9) data are read-only in this version."));
            return new ImportPlan(current, items);
        }

        var target = PKH.GetType(incoming is PK7 ? typeof(PK8) : incoming.GetType());
        AddIdentity(items, current, incoming);

        // Keep the HOME tracker: PKHeX copies the PKM's tracker unconditionally, even when it is 0.
        var source = incoming.Clone();
        if (source is IHomeTrack track && track.Tracker != current.Tracker)
            track.Tracker = current.Tracker;

        var before = Copy(current);
        var after = Copy(current);
        try
        {
            after.CopyFrom(source);
            after = new PKH(after.Rebuild());
        }
        catch (Exception ex)
        {
            items.Add(new(ImportSeverity.Block, "Import", $"PKHeX failed to import this PKM: {ex.Message}"));
            return new ImportPlan(current, items);
        }

        if (target == HomeGameDataFormat.None)
            items.Add(new(ImportSeverity.Info, "Format", $"{incoming.GetType().Name} has no HOME version data; only the shared (core) data is updated."));
        else if (incoming is PK7)
            items.Add(new(ImportSeverity.Info, "Format", "PK7 is stored in the PK8 version data, as HOME does."));

        var coreKeys = AddCoreChanges(items, before, after);
        AddOtherVersionChanges(items, before, after, target, coreKeys);
        AddOwnVersionCheck(items, after, incoming, target);
        return new ImportPlan(after, items);
    }

    private static void AddIdentity(List<ImportItem> items, PKH pkh, PKM pk)
    {
        const string group = "Identity";
        switch (GetIdentity(pkh, pk))
        {
            case Identity.SameTracker:
                break;
            case Identity.DifferentTracker:
                items.Add(new(ImportSeverity.Confirm, group, $"HOME tracker differs: PKH {pkh.Tracker:X16}, PKM {((IHomeTrack)pk).Tracker:X16}. This may be a different Pokémon."));
                break;
            case Identity.MatchedWithoutTracker:
                items.Add(new(ImportSeverity.Confirm, group, $"PKM has no HOME tracker; EC, trainer ID and OT match. The PKH tracker {pkh.Tracker:X16} will be kept."));
                break;
            case Identity.MatchedPidDiffers:
                items.Add(new(ImportSeverity.Confirm, group, $"EC, trainer ID and OT match but PID differs (PKH {pkh.PID:X8}, PKM {pk.PID:X8})."));
                break;
            default:
                items.Add(new(ImportSeverity.Confirm, group, "EC, trainer ID or OT differ from the PKH. This is most likely a different Pokémon."));
                break;
        }
        if (pkh.Species != pk.Species)
            items.Add(new(ImportSeverity.Info, group, $"Species changes {pkh.Species} → {pk.Species} (evolution)."));
    }

    // Declared per format (ISanityChecksum / PKH), not on PKM.
    private const string ChecksumKey = "Checksum";

    // Properties that are fakes on PKH, derived elsewhere, or always differ after a rebuild.
    private static readonly HashSet<string> IgnoredCore =
    [
        nameof(PKM.Data), ChecksumKey, nameof(PKM.FileName), nameof(PKM.FileNameWithoutExtension),
        nameof(PKH.EncryptionSeed), nameof(PKH.EncodedDataSize), nameof(PKH.GameDataSize), nameof(PKH.DataVersion),
        "SIZE_PARTY", "SIZE_STORED", nameof(PKM.ExtraBytes),
        // Read through the "latest" version data rather than core; covered by the per-version comparison.
        nameof(PKM.Move1), nameof(PKM.Move2), nameof(PKM.Move3), nameof(PKM.Move4),
        nameof(PKM.Move1_PP), nameof(PKM.Move2_PP), nameof(PKM.Move3_PP), nameof(PKM.Move4_PP),
        nameof(PKM.Move1_PPUps), nameof(PKM.Move2_PPUps), nameof(PKM.Move3_PPUps), nameof(PKM.Move4_PPUps),
        nameof(PKM.Ball), nameof(PKM.MetLocation), nameof(PKM.EggLocation), nameof(PKM.PersonalInfo),
    ];

    internal sealed record Change(string Key, string Before, string After);

    // Stored data only: get-only properties (SV, Gen9, Generation, ...) are derived from these.
    private static bool IsStored(PropertyInfo pi)
        => pi.GetIndexParameters().Length == 0 && (pi.SetMethod is { IsPublic: true } || pi.PropertyType.IsByRefLike);

    /// <summary>Changed shared (core) data: the PKH's own properties plus ribbons and marks that live only on <see cref="PKH.Core"/>.</summary>
    internal static IEnumerable<Change> DiffCore(PKH a, PKH b)
    {
        var seen = new HashSet<string>(IgnoredCore);
        foreach (var pi in typeof(PKH).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!IsStored(pi) || !seen.Add(pi.Name) || pi.PropertyType.Name.StartsWith("GameData", StringComparison.Ordinal))
                continue;
            var x = PropertyValue.Read(pi, a);
            var y = PropertyValue.Read(pi, b);
            if (x.IsComparable && y.IsComparable && x.Display != y.Display)
                yield return new Change(pi.Name, x.Display, y.Display);
        }

        foreach (var pi in typeof(GameDataCore).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!IsStored(pi) || pi.PropertyType.IsByRefLike || !seen.Add(pi.Name))
                continue;
            var x = pi.GetValue(a.Core)?.ToString() ?? "null";
            var y = pi.GetValue(b.Core)?.ToString() ?? "null";
            if (x != y)
                yield return new Change(pi.Name, x, y);
        }
    }

    // Shared fields that define where the Pokémon came from; overwriting them needs confirmation.
    private static readonly HashSet<string> OriginKeys =
    [
        nameof(PKM.Version), nameof(PKM.Language), nameof(PKM.EncryptionConstant), nameof(PKM.PID),
        nameof(PKM.ID32), nameof(PKM.TID16), nameof(PKM.SID16), nameof(PKM.OriginalTrainerName), nameof(PKM.OriginalTrainerGender),
        nameof(PKM.MetLevel), nameof(PKM.MetYear), nameof(PKM.MetMonth), nameof(PKM.MetDay),
        nameof(PKM.EggYear), nameof(PKM.EggMonth), nameof(PKM.EggDay), nameof(PKM.FatefulEncounter),
    ];

    private static HashSet<string> AddCoreChanges(List<ImportItem> items, PKH before, PKH after)
    {
        const string group = "Shared data";
        var keys = new HashSet<string>();
        var changes = DiffCore(before, after).ToList();
        foreach (var c in changes)
        {
            keys.Add(c.Key);
            if (IsLoss(c))
                items.Add(new(ImportSeverity.Confirm, group, Describe(c) + " (existing data removed)"));
            else if (OriginKeys.Contains(c.Key))
                items.Add(new(ImportSeverity.Confirm, group, Describe(c) + " (origin data overwritten)"));
            else
                items.Add(new(ImportSeverity.Info, group, Describe(c)));
        }
        // Net totals (RibbonCount etc.) can hide a loss when the import also adds ribbons; call it out explicitly.
        if (GetRibbonLossSummary(changes) is { } summary)
            items.Add(new(ImportSeverity.Confirm, group, summary));
        return keys;
    }

    /// <summary>True for a single ribbon/mark flag going from set to unset (not a net total; see <see cref="GetRibbonLossSummary"/>).</summary>
    private static bool IsRibbonFlagLoss(Change c)
        => (c.Key.StartsWith("Ribbon", StringComparison.Ordinal) || c.Key.StartsWith("HasMark", StringComparison.Ordinal))
           && c.Before == "True" && c.After == "False";

    /// <summary>True for changes that remove existing information: ribbons, marks, markings, memories cleared, or EXP decreased.</summary>
    private static bool IsLoss(Change c)
    {
        var key = c.Key;
        if (IsRibbonFlagLoss(c))
            return true;
        if (key is nameof(PKH.MarkCount) or nameof(PKH.RibbonCount) or nameof(PKH.RibbonMarkCount))
            return IsCountDecrease(c);
        if (key.StartsWith("Marking", StringComparison.Ordinal))
            return c.Before != "None" && c.After == "None" || c.Before != "0" && c.After == "0";
        if (key.Contains("Memory", StringComparison.Ordinal))
            return c.Before != "0" && c.After == "0";
        if (key == nameof(PKM.EXP))
            return IsCountDecrease(c);
        return false;
    }

    /// <summary>
    /// A one-line summary when any ribbon/mark flag is lost, since <see cref="RibbonCount"/> alone can stay flat
    /// or even rise if the import also adds ribbons the PKH did not have (gains and losses netting out).
    /// </summary>
    private static string? GetRibbonLossSummary(IEnumerable<Change> coreChanges)
    {
        var lost = coreChanges.Where(IsRibbonFlagLoss).Select(c => c.Key).ToList();
        if (lost.Count == 0)
            return null;
        return $"{lost.Count} ribbon/mark flag(s) cleared: {string.Join(", ", lost)}";
    }

    private static bool IsCountDecrease(Change c)
        => long.TryParse(c.Before, out var b) && long.TryParse(c.After, out var a) && a < b;

    private static string Describe(Change c) => $"{c.Key}: {Short(c.Before)} → {Short(c.After)}";

    private static string Short(string s) => s.Length > 24 ? s[..24] + "…" : s;

    // PKHeX-native export differences that are expected and only reported (see the Phase 0 round-trip probe).
    private static readonly HashSet<string> ExportWhitelist =
    [
        nameof(PKM.HeldItem), nameof(PKM.SpriteItem), nameof(PKM.Stat_HPCurrent), nameof(PKM.Stats),
        nameof(PKM.Stat_HPMax), nameof(PKM.Stat_ATK), nameof(PKM.Stat_DEF), nameof(PKM.Stat_SPA), nameof(PKM.Stat_SPD), nameof(PKM.Stat_SPE),
        nameof(PKM.Status_Condition), "HandlingTrainerID", "Stat_CP",
        "HeightAbsolute", "WeightAbsolute", "CalcHeightAbsolute", "CalcWeightAbsolute",
        nameof(PKM.Data), ChecksumKey, nameof(PKM.FileName), nameof(PKM.FileNameWithoutExtension),
    ];

    // Size values that PKHeX/HOME overwrite from the per-version Scale on import or export.
    private static readonly HashSet<string> SizeKeys = ["HeightScalar", "WeightScalar", "Scale", "HeightAbsolute", "WeightAbsolute", "CalcHeightAbsolute", "CalcWeightAbsolute"];

    // Bookkeeping that always differs after a conversion and carries no information of its own.
    private static readonly HashSet<string> NoiseKeys =
    [
        nameof(PKM.Data), ChecksumKey, "Sanity", nameof(PKM.FileName), nameof(PKM.FileNameWithoutExtension),
        nameof(PKM.SpriteItem), nameof(PKM.Stats),
    ];

    private static List<Change> DiffPkm(PKM a, PKM b)
    {
        var list = new List<Change>();
        var batch = EntityBatchEditor.Instance;
        foreach (var key in batch.Properties[0])
        {
            if (NoiseKeys.Contains(key))
                continue;
            if (!batch.TryGetHasProperty(a, key, out var pi) || !batch.TryGetHasProperty(b, key, out var pi2))
                continue;
            if (!IsStored(pi))
                continue;
            var x = PropertyValue.Read(pi, a);
            var y = PropertyValue.Read(pi2, b);
            if (x.IsComparable && y.IsComparable && x.Display != y.Display)
                list.Add(new Change(key, x.Display, y.Display));
        }
        return list;
    }

    /// <summary>
    /// 3.2.2: changes to the exports of the other stored versions need confirmation. Changes that simply follow a
    /// shared-data change are already listed there, and recalculated values (stats) are skipped; size scalars are
    /// always flagged because HOME overwrites them from the imported version's Scale.
    /// </summary>
    private static void AddOtherVersionChanges(List<ImportItem> items, PKH before, PKH after, HomeGameDataFormat target, HashSet<string> coreKeys)
    {
        foreach (var format in GetVersions(before))
        {
            if (format == target)
                continue;
            var a = Export(before, format);
            var b = Export(after, format);
            if (a == null || b == null)
                continue;

            var group = $"Export {format}";
            foreach (var c in DiffPkm(a, b))
            {
                if (SizeKeys.Contains(c.Key))
                    items.Add(new(ImportSeverity.Confirm, group, Describe(c) + " (shared size scalar is overwritten by the imported version's Scale, as HOME does)"));
                else if (!coreKeys.Contains(c.Key) && !ExportWhitelist.Contains(c.Key))
                    items.Add(new(ImportSeverity.Confirm, group, Describe(c)));
            }
        }
    }

    /// <summary>3.2.3: the imported version must export back unchanged, apart from PKHeX's documented conversions.</summary>
    private static void AddOwnVersionCheck(List<ImportItem> items, PKH after, PKM incoming, HomeGameDataFormat target)
    {
        if (target == HomeGameDataFormat.None || incoming is PK7)
            return;

        var back = Export(after, target);
        if (back == null)
        {
            items.Add(new(ImportSeverity.Block, $"Export {target}", "The imported version could not be exported back."));
            return;
        }

        // PKHeX's own round trip of this PKM alone defines what counts as a native conversion.
        var native = GetNativeRoundTrip(incoming, target, after.Tracker);
        var group = $"Export {target}";
        foreach (var c in DiffPkm(incoming, back))
        {
            if (c.Key == "Tracker")
                continue; // Kept from the PKH on purpose.
            if (ExportWhitelist.Contains(c.Key))
            {
                items.Add(new(ImportSeverity.Info, group, Describe(c) + " (not stored by HOME / recalculated)"));
                continue;
            }
            if (native != null && ReadDisplay(native, c.Key) == c.After)
            {
                items.Add(new(ImportSeverity.Info, group, Describe(c) + " (PKHeX HOME conversion)"));
                continue;
            }
            items.Add(new(ImportSeverity.Block, group, Describe(c) + " — merging with the existing PKH would change the imported data."));
        }
    }

    private static PKM? GetNativeRoundTrip(PKM incoming, HomeGameDataFormat target, ulong tracker)
    {
        try
        {
            var source = incoming.Clone();
            if (source is IHomeTrack t)
                t.Tracker = tracker;
            var alone = new PKH(PKH.ConvertFromPKM(source).Rebuild());
            return alone.ConvertToPKM(target);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ReadDisplay(PKM pk, string key)
        => EntityBatchEditor.Instance.TryGetHasProperty(pk, key, out var pi) ? PropertyValue.Read(pi, pk).Display : null;

    #endregion
}
