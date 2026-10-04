using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using PKHeX.Core;

namespace PKHEdit;

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

    /// <summary>
    /// Versions a HOME slot tries, in order, when rendering its sprite: the version the PKH's data came
    /// from (the Pokémon's own game, so it keeps its own art style and sprite even when the active save's
    /// generation is stored as well), then <see cref="GetPreferredFormat"/> as the fallback. A missing
    /// block is reconstructed by PKHeX on export, so the source version normally renders regardless.
    /// </summary>
    public static HomeGameDataFormat[] GetSpriteFormats(PKH pkh, Type? savPkmType)
    {
        var source = GetOriginFormat(pkh.Version);
        var preferred = GetPreferredFormat(pkh, savPkmType);
        return source == preferred ? [source] : [source, preferred];
    }

    /// <summary>Format of the version block a PKH's data originally came from (its origin game,
    /// <see cref="PKM.Version"/>) — the "source version". Mirrors PKH.OriginalGameData's mapping:
    /// GO/GP/GE→PB7, BD/SP→PB8, PLA→PA8, SL/VL→PK9, ZA→PA9, SW/SH and Gen7-→PK8.</summary>
    public static HomeGameDataFormat GetOriginFormat(GameVersion version) => version switch
    {
        GameVersion.GO or GameVersion.GP or GameVersion.GE => HomeGameDataFormat.PB7,
        GameVersion.BD or GameVersion.SP => HomeGameDataFormat.PB8,
        GameVersion.PLA => HomeGameDataFormat.PA8,
        GameVersion.SL or GameVersion.VL => HomeGameDataFormat.PK9,
        GameVersion.ZA => HomeGameDataFormat.PA9,
        _ => HomeGameDataFormat.PK8,
    };

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

    /// <summary>The selected version's raw data block on the PKH, or null when that format isn't stored.</summary>
    public static object? GetVersionBlock(PKH pkh, HomeGameDataFormat format) => format switch
    {
        HomeGameDataFormat.PB7 => pkh.DataPB7,
        HomeGameDataFormat.PK8 => pkh.DataPK8,
        HomeGameDataFormat.PA8 => pkh.DataPA8,
        HomeGameDataFormat.PB8 => pkh.DataPB8,
        HomeGameDataFormat.PK9 => pkh.DataPK9,
        HomeGameDataFormat.PA9 => pkh.DataPA9,
        _ => null,
    };

    // All side-block types, discovered from PKH itself (typed Data* properties) — a new HOME format
    // added by PKHeX is picked up without touching this file.
    private static readonly Lazy<Type[]> SideBlockTypes = new(() =>
        typeof(PKH).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(z => z.PropertyType.Name.StartsWith("GameData", StringComparison.Ordinal)
                        && z.PropertyType != typeof(GameDataCore))
            .Select(z => z.PropertyType)
            .ToArray());

    /// <summary>Per block type: property names declared on no other block and not on the shared core
    /// (e.g. PA8's GV_*, PB7's AV_* …) — computed structurally, never from a written name list.</summary>
    private static readonly Lazy<IReadOnlyDictionary<Type, HashSet<string>>> FormatOnlyProperties = new(() =>
    {
        var core = PropNames(typeof(GameDataCore));
        var blocks = SideBlockTypes.Value;
        var result = new Dictionary<Type, HashSet<string>>();
        foreach (var t in blocks)
        {
            var shared = new HashSet<string>(core, StringComparer.Ordinal);
            foreach (var o in blocks)
            {
                if (o != t)
                    shared.UnionWith(PropNames(o));
            }
            var own = PropNames(t);
            own.ExceptWith(shared);
            result[t] = own;
        }
        return result;
    });

    private static HashSet<string> PropNames(Type t) =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(z => z.Name).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Readable place name via PKHeX's own tables. Both values come from an entity — version = origin,
    /// context = its current format — exactly how PKHeX itself resolves locations
    /// (GameInfo.GetLocationList, see EntitySuggestionUtil), so no format/version mapping is
    /// maintained here: a new HOME format keeps working as soon as PKHeX can export it.
    /// </summary>
    public static string? GetLocationDisplayName(GameVersion origin, EntityContext context, bool eggLocation, string rawValue)
    {
        if (context == EntityContext.None || !int.TryParse(rawValue, out var id) || id is <= 0 or > ushort.MaxValue)
            return null;
        try
        {
            foreach (var combo in GameInfo.GetLocationList(origin, context, eggLocation))
            {
                if (combo.Value == id)
                    return combo.Text;
            }
        }
        catch (Exception) { /* unmapped origin/context — the caller keeps the raw number */ }
        return null;
    }

    /// <summary>Move-typed fields whose IDs resolve through the move name table.</summary>
    internal static readonly HashSet<string> MoveProps =
    [
        "Move1", "Move2", "Move3", "Move4",
        "RelearnMove1", "RelearnMove2", "RelearnMove3", "RelearnMove4",
    ];

    internal static bool IsMoveProp(string key) => MoveProps.Contains(key);

    /// <summary>
    /// Readable name for an ID-typed field (Ball/Ability/Move/Met/EggLocation), or null when the key
    /// has no table, the value doesn't parse, or no entity context is available for a location.
    /// </summary>
    /// <param name="entity">Entity the value belongs to: its Version (origin) and Context select the
    /// location table, PKHeX-native. Null = no entity, locations keep the raw number.</param>
    public static string? ReadableValue(string key, string raw, PKM? entity)
    {
        // StatAlignment IS a Nature value (Gen8+ stores it separately; PKHeX itself renders it through
        // Strings.natures — see EntitySummary.Nature), so it shares Nature's table. Both arrive as the
        // English enum name, not an ID — handled before the numeric gate.
        if (key is "Nature" or "StatAlignment")
            return NatureName(raw);
        if (key == "Gender")
            return GenderSymbol(raw); // enum name or numeric id; no symbol for None/Genderless
        if (!int.TryParse(raw, out var id) || id <= 0)
            return null; // 0 = no meaningful name for any of these tables
        var strings = GameInfo.Strings;
        if (key == "Ball")
            return id < strings.balllist.Length ? strings.balllist[id] : null;
        if (key == "Ability")
            return id < strings.Ability.Count ? strings.Ability[id] : null;
        if (key is "MetLocation" or "EggLocation")
            return entity is not null ? GetLocationDisplayName(entity.Version, entity.Context, key == "EggLocation", raw) : null;
        if (IsMoveProp(key))
            return id < strings.Move.Count ? strings.Move[id] : null;
        return null;
    }

    /// <summary>Localized nature name from an enum name or numeric id, or null when it doesn't resolve.</summary>
    private static string? NatureName(string raw)
    {
        // Enum.TryParse accepts both the name ("Hardy") and the numeric value ("13").
        if (!Enum.TryParse(raw, out Nature nature))
            return null;
        var names = GameInfo.Strings.Natures;
        return (uint)(int)nature < (uint)names.Count ? names[(int)nature] : null;
    }

    /// <summary>♂/♀ for a Gender value (enum name or numeric id), or null when there is no symbol.</summary>
    private static string? GenderSymbol(string raw)
    {
        if (!Enum.TryParse(raw, out Gender gender))
            return null;
        return gender switch
        {
            Gender.Male => "♂",
            Gender.Female => "♀",
            _ => null, // None / Genderless: no symbol exists, keep the raw value alone
        };
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

    /// <summary>New PKH from the first PKM. A missing tracker gets a generated FFFF-prefixed one only
    /// when custom trackers are enabled; otherwise it stays 0 and identity relies on the fallback match.</summary>
    public static PKH Create(PKM pk, bool customTracker, out bool generatedTracker)
    {
        var pkh = new PKH(PKH.ConvertFromPKM(pk.Clone()).Rebuild());
        generatedTracker = customTracker && pkh.Tracker == 0;
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
    internal enum Identity
    {
        SameTracker,
        DifferentTracker,
        /// <summary>Fallback match with the incoming tracker = 0: the PKH's tracker (or 0) is kept.</summary>
        MatchedWithoutTracker,
        /// <summary>Fallback match with the PKH tracker = 0: the incoming PKM's tracker is written into the PKH.</summary>
        MatchedIncomingTracker,
        MatchedPidDiffers,
        Mismatch,
    }

    /// <summary>Classifies how the dropped PKM relates to the loaded PKH.</summary>
    /// <param name="customTracker">True = a tracker on both sides is the primary identity signal (original
    /// behavior). False = only the EC/ID32/OT fallback match decides sameness; a conflicting pair of
    /// non-zero trackers is still reported via <see cref="Identity.DifferentTracker"/>.</param>
    public static Identity GetIdentity(PKH pkh, PKM pk, bool customTracker)
    {
        ulong pkTracker = pk is IHomeTrack t ? t.Tracker : 0;

        // Species may differ through evolution, so it is not part of the match.
        bool same = pk.EncryptionConstant == pkh.EncryptionConstant
                    && pk.ID32 == pkh.ID32
                    && pk.OriginalTrainerName == pkh.OriginalTrainerName;

        if (customTracker)
        {
            // Original behavior: a tracker present on both sides is the primary identity signal.
            if (pkTracker != 0 && pkh.Tracker != 0)
                return pkTracker == pkh.Tracker ? Identity.SameTracker : Identity.DifferentTracker;
            if (!same)
                return Identity.Mismatch;
            if (pkh.Tracker == 0 && pkTracker != 0)
                return Identity.MatchedIncomingTracker;
            return pk.PID == pkh.PID ? Identity.MatchedWithoutTracker : Identity.MatchedPidDiffers;
        }

        // Custom tracker off: the fallback match alone decides sameness; the tracker that exists still
        // survives (see PlanImport) and its disposition is still reported. A conflicting pair of
        // non-zero trackers still raises the original DifferentTracker warning.
        if (!same)
            return Identity.Mismatch;
        if (pkh.Tracker == 0 && pkTracker != 0)
            return Identity.MatchedIncomingTracker;
        if (pkTracker != 0 && pkTracker != pkh.Tracker)
            return Identity.DifferentTracker; // both non-zero: the 0-PKH case returned above
        if (pk.PID != pkh.PID)
            return Identity.MatchedPidDiffers;
        return pkTracker == 0 ? Identity.MatchedWithoutTracker : Identity.SameTracker;
    }

    /// <summary>
    /// A warning line to append to the HOME slot overwrite prompt, or an empty string when the PKH about
    /// to be written belongs to the same individual as the stored file, judged by the same rules as an
    /// import: the same HOME tracker, or — when a tracker is 0 or the custom tracker is off — the
    /// EC/ID32/OT fallback match. <paramref name="stored"/> is null when the file couldn't be read.
    /// </summary>
    public static string GetOverwriteWarning(PKH? stored, PKH incoming, bool customTracker)
    {
        if (stored == null)
            return "The stored file couldn't be read, so it couldn't be compared with the editor's PKH.";
        if (IsSameIndividual(stored, incoming, customTracker))
            return "";
        return GetIdentity(stored, incoming, customTracker) == Identity.DifferentTracker
            ? $"HOME tracker differs (stored {stored.Tracker:X16}, editor {(incoming is IHomeTrack t ? t.Tracker : 0):X16}) — this is most likely a different Pokémon."
            : "EC, trainer ID or OT differ from the stored file — this is most likely a different Pokémon.";
    }

    /// <summary>Whether <paramref name="pk"/> belongs to the same individual as the PKH, by the import
    /// identity rules: the same HOME tracker, or the EC/ID32/OT fallback match when a tracker is 0 or
    /// the custom tracker is off (see <see cref="GetIdentity"/>).</summary>
    public static bool IsSameIndividual(PKH pkh, PKM pk, bool customTracker) => GetIdentity(pkh, pk, customTracker)
        is Identity.SameTracker or Identity.MatchedWithoutTracker or Identity.MatchedIncomingTracker or Identity.MatchedPidDiffers;

    /// <summary>Slots of <paramref name="sav"/> holding the same individual as <paramref name="pkh"/>: every box
    /// slot, the party, and the save's other slots (daycare, battle box, …), in that order. Read-only.</summary>
    public static List<ISlotInfo> FindMatchingSlots(PKH pkh, SaveFile sav, bool customTracker)
    {
        var hits = new List<ISlotInfo>();
        if (sav.HasBox)
        {
            for (int box = 0; box < sav.BoxCount; box++)
            {
                for (int slot = 0; slot < sav.BoxSlotCount; slot++)
                {
                    var pk = sav.GetBoxSlotAtIndex(box, slot);
                    if (pk.Species != 0 && IsSameIndividual(pkh, pk, customTracker))
                        hits.Add(new SlotInfoBox(box, slot, sav));
                }
            }
        }

        if (sav.HasParty)
        {
            var party = sav.PartyData; // already limited to the six party slots
            for (int i = 0; i < party.Count; i++)
            {
                var pk = party[i];
                if (pk.Species != 0 && IsSameIndividual(pkh, pk, customTracker))
                    hits.Add(new SlotInfoParty(i));
            }
        }

        foreach (var extra in sav.GetExtraSlots(true))
        {
            var pk = extra.Read(sav);
            if (pk.Species != 0 && IsSameIndividual(pkh, pk, customTracker))
                hits.Add(extra);
        }
        return hits;
    }

    /// <summary>
    /// Dry-runs PKHeX's PKH.CopyFrom on a copy and classifies every resulting change.
    /// Nothing is applied until the caller adopts <see cref="ImportPlan.Result"/>.
    /// </summary>
    public static ImportPlan PlanImport(PKH current, PKM incoming, bool customTracker)
    {
        var items = new List<ImportItem>();
        if (HasPC9(current))
        {
            items.Add(new(ImportSeverity.Block, "Format", "PKH files with Champions (PC9) data are read-only in this version."));
            return new ImportPlan(current, items);
        }

        var target = PKH.GetType(incoming is PK7 ? typeof(PK8) : incoming.GetType());
        AddIdentity(items, current, incoming, customTracker);

        // PKHeX copies the PKM's tracker unconditionally (even a 0), so pin the identity here:
        // a non-zero PKH tracker wins (a differing non-zero pair is flagged by AddIdentity); a 0 PKH
        // tracker adopts the incoming PKM's tracker instead of wiping it.
        var source = incoming.Clone();
        if (source is IHomeTrack track && current.Tracker != 0 && track.Tracker != current.Tracker)
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
        AddTargetVersionChanges(items, before, after, target);
        // The dialog is shown for every import now; an empty report would leave a blank list.
        if (items.Count == 0)
            items.Add(new(ImportSeverity.Info, "Import", "No differences detected — applying will overwrite the PKH with identical data."));
        return new ImportPlan(after, items);
    }

    private static void AddIdentity(List<ImportItem> items, PKH pkh, PKM pk, bool customTracker)
    {
        const string group = "Identity";
        switch (GetIdentity(pkh, pk, customTracker))
        {
            case Identity.SameTracker:
                break;
            case Identity.DifferentTracker:
                items.Add(new(ImportSeverity.Confirm, group, $"HOME tracker differs: PKH {pkh.Tracker:X16}, PKM {((IHomeTrack)pk).Tracker:X16}. This may be a different Pokémon."));
                break;
            case Identity.MatchedWithoutTracker:
                // Both trackers 0 + fallback match is the normal case: import silently, no identity prompt.
                if (pkh.Tracker != 0)
                    items.Add(new(ImportSeverity.Confirm, group, $"PKM has no HOME tracker; EC, trainer ID and OT match. The PKH tracker {pkh.Tracker:X16} will be kept."));
                break;
            case Identity.MatchedIncomingTracker:
                items.Add(new(ImportSeverity.Confirm, group,
                    $"PKH has no HOME tracker; EC, trainer ID and OT match. The PKM tracker {((IHomeTrack)pk).Tracker:X16} will be written into the PKH."
                    + (pk.PID != pkh.PID ? $" PID differs (PKH {pkh.PID:X8}, PKM {pk.PID:X8})." : "")));
                break;
            case Identity.MatchedPidDiffers:
                // Reaches here only with the incoming tracker = 0, so pkh.Tracker == 0 means both are 0:
                // same silent rule — any PID overwrite is already reported by the core origin-data line.
                if (pkh.Tracker != 0)
                    items.Add(new(ImportSeverity.Confirm, group, $"EC, trainer ID and OT match but PID differs (PKH {pkh.PID:X8}, PKM {pk.PID:X8})."));
                break;
            default: // fallback UNmatch — still reported, including when both trackers are 0.
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
    internal static bool IsStored(PropertyInfo pi)
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
        => c.Key.StartsWith("Ribbon", StringComparison.Ordinal) // also covers marks: they are RibbonMark*
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

    private static string Describe(Change c, PKM? entity = null) =>
        $"{c.Key}: {Annotate(c.Key, c.Before, entity)} → {Annotate(c.Key, c.After, entity)}";

    /// <summary>Raw value, with a readable table name in parentheses when the key resolves.</summary>
    private static string Annotate(string key, string raw, PKM? entity) =>
        ReadableValue(key, raw, entity) is { } name ? $"{Short(raw)} ({name})" : Short(raw);

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
                    items.Add(new(ImportSeverity.Confirm, group, Describe(c, a) + " (shared size scalar is overwritten by the imported version's Scale, as HOME does)"));
                else if (!coreKeys.Contains(c.Key) && !ExportWhitelist.Contains(c.Key))
                    items.Add(new(ImportSeverity.Confirm, group, Describe(c, a)));
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
                // HeldItem going to 0 is the one whitelisted loss we can actually give back — say so.
                var note = c.Key == nameof(PKM.HeldItem)
                    ? " (not stored by HOME / recalculated; returned to the bag on apply unless discarded)"
                    : " (not stored by HOME / recalculated)";
                items.Add(new(ImportSeverity.Info, group, Describe(c, incoming) + note));
                continue;
            }
            if (native != null && ReadDisplay(native, c.Key) == c.After)
            {
                items.Add(new(ImportSeverity.Info, group, Describe(c, incoming) + " (PKHeX HOME conversion)"));
                continue;
            }
            items.Add(new(ImportSeverity.Block, group, Describe(c, incoming) + " — merging with the existing PKH would change the imported data."));
        }
    }

    /// <summary>
    /// 3.2.4: format-specific fields of the target version block that this import overwrote, shown as
    /// their own Area under the informational section. Emitted only when the PKH already stored that
    /// version AND something in it changed — block creation stays silent to keep the report small.
    /// The format-only field set is computed structurally (see FormatOnlyProperties), so new PKHeX
    /// fields are covered without edits here.
    /// </summary>
    private static void AddTargetVersionChanges(List<ImportItem> items, PKH before, PKH after, HomeGameDataFormat target)
    {
        if (target == HomeGameDataFormat.None)
            return;
        var blockA = GetVersionBlock(before, target);
        var blockB = GetVersionBlock(after, target);
        if (blockA is null || blockB is null) // the PKH didn't store this version → nothing was overwritten
            return;
        var type = blockA.GetType();
        if (!FormatOnlyProperties.Value.TryGetValue(type, out var formatOnly) || formatOnly.Count == 0)
            return;

        var group = $"Version data {target}";
        foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!formatOnly.Contains(pi.Name) || !IsStored(pi) || pi.PropertyType.IsByRefLike)
                continue;
            var x = pi.GetValue(blockA)?.ToString() ?? "null";
            var y = pi.GetValue(blockB)?.ToString() ?? "null";
            if (x != y)
                items.Add(new(ImportSeverity.Info, group, Describe(new Change(pi.Name, x, y))));
        }
    }

    /// <summary>
    /// Gives the incoming PKM's held item to the save's bag. PKHeX's import never copies the item into
    /// the PKH (GameDataCore.CopyFrom has that line commented out), so without this it is lost.
    /// Nothing is written until the pouch edit has succeeded, and a failed write/read-back is restored
    /// byte-for-byte from a snapshot — so a caller that then cancels the import leaves no trace.
    /// </summary>
    /// <returns>False = the item couldn't be stored; <paramref name="detail"/> says why (or names the item on success).</returns>
    public static bool TryReturnItemToBag(SaveFile sav, PKM pk, out string detail)
    {
        detail = "";
        if (pk.HeldItem <= 0)
            return true;

        int destItem = ItemConverter.GetItemForFormat(pk.HeldItem, pk.Context, sav.Context);
        if (destItem <= 0 || destItem > ushort.MaxValue)
        {
            detail = $"item {pk.HeldItem} doesn't exist in {sav.Version} data";
            return false;
        }
        string itemName = ItemName(destItem);

        var bag = sav.Inventory;
        if (bag.Pouches.Count == 0)
        {
            detail = "the save has no bag to store items in";
            return false;
        }
        var pouch = FindItemPouch(bag, destItem);
        if (pouch == null)
        {
            detail = $"{itemName} isn't a pocket item for {sav.Version}";
            return false;
        }

        int before = pouch.Items.FirstOrDefault(z => z.Index == destItem && z.Count > 0)?.Count ?? 0;
        int after = pouch.GiveItem(bag, (ushort)destItem, 1);
        if (after <= before)
        {
            // -1 = no empty slot, equal = clamped at the pouch maximum. The edit stayed local; the save is untouched.
            detail = after < 0 ? "the bag is full" : $"{itemName} is already at its maximum quantity";
            return false;
        }

        var backup = sav.Data.ToArray();
        try
        {
            bag.CopyTo(sav);
            // Inventory is rebuilt from save data on every access: read the item straight back.
            if (!sav.Inventory.GetPouch(pouch.Type).HasItem((ushort)destItem))
                throw new InvalidOperationException("the written bag could not be read back");
        }
        catch (Exception ex)
        {
            backup.AsSpan().CopyTo(sav.Data);
            detail = $"couldn't store {itemName} in the bag: {ex.Message}";
            return false;
        }

        detail = itemName;
        return true;
    }

    /// <summary>First pouch that may legally hold the item, preferring the standard Items pocket.</summary>
    private static InventoryPouch? FindItemPouch(PlayerBag bag, int item)
    {
        foreach (var type in new[] { InventoryType.Items, InventoryType.Berries })
        {
            var pouch = bag.Pouches.FirstOrDefault(z => z.Type == type);
            if (pouch != null && pouch.CanContain((ushort)item) && bag.Info.IsLegal(type, item, 1))
                return pouch;
        }
        return bag.Pouches.FirstOrDefault(z => z.CanContain((ushort)item) && bag.Info.IsLegal(z.Type, item, 1));
    }

    private static string ItemName(int id)
    {
        var names = GameInfo.Strings.Item;
        return id > 0 && id < names.Count ? names[id] : $"item {id}";
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
