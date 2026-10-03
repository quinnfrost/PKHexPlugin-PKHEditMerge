using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHEdit;

/// <summary>Helpers shared by the plugin's windows.</summary>
internal static class PkmUtil
{
    // PKHeX deletes its own drag-out temp files after a similar delay, giving the drop target time to read them.
    private static readonly TimeSpan TempFileLifetime = TimeSpan.FromSeconds(20);

    /// <summary>Species name in the current PKHeX display language, followed by the nickname if it has one.</summary>
    public static string GetDisplayName(PKM pk)
    {
        var names = GameInfo.Strings.Species;
        var species = pk.Species < names.Count ? names[pk.Species] : $"#{pk.Species}";
        return pk.IsNicknamed && !string.IsNullOrEmpty(pk.Nickname) ? $"{species} \"{pk.Nickname}\"" : species;
    }

    /// <summary>Same checksum PKHeX's default file namer puts in the exported file name.</summary>
    public static ushort GetFileNameChecksum(PKM pk) => pk switch
    {
        PK1 gb1 => gb1.GetSingleListChecksum(),
        PK2 gb2 => gb2.GetSingleListChecksum(),
        GBPKM gb => Checksums.CRC16_CCITT(gb.Data),
        ISanityChecksum s => s.Checksum,
        _ => Checksums.Add16(pk.Data[8..pk.SIZE_STORED]),
    };

    /// <summary>Loads a dropped file as a single PKM; gifts and encounters are converted for <paramref name="sav"/>.</summary>
    public static bool TryLoadPkm(string path, SaveFile sav, [NotNullWhen(true)] out PKM? pk, out string error)
    {
        pk = null;
        try
        {
            pk = FileUtil.GetSupportedFile(path, sav) switch
            {
                PKH => null,
                PKM p => p,
                MysteryGift g => g.ConvertToPKM(sav),
                IEncounterConvertible enc => enc.ConvertToPKM(sav),
                _ => null,
            };
            error = pk == null ? "not a supported PKM file." : "";
            return pk != null;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Drags <paramref name="pk"/> out of <paramref name="source"/> as a temporary PKM file, like PKHeX's own slots.</summary>
    public static void DragOut(Control source, PKM pk)
    {
        string? file = null;
        try
        {
            var copy = pk.Clone();
            copy.ForcePartyData();
            var data = new byte[copy.SIZE_PARTY];
            copy.WriteDecryptedDataParty(data);

            file = FileUtil.GetPKMTempFileName(copy, encrypt: false);
            File.WriteAllBytes(file, data);
            // Copy only: PKHeX's box slots treat Link as "moved to another slot".
            source.DoDragDrop(new DataObject(DataFormats.FileDrop, new[] { file }), DragDropEffects.Copy);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Drag && Drop failed: {ex.Message}", source.FindForm()?.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (file != null)
                _ = DeleteLaterAsync(file);
        }
    }

    /// <summary>Data format tagging a drag with its origin slot ("box:slot") inside the HOME window.</summary>
    public const string SlotDataFormat = "PKHEdit.HomeSlot";

    /// <summary>Drags <paramref name="pkh"/> out of <paramref name="source"/> as a temporary .pkh file, like the editor's sprite drag.</summary>
    public static void DragOutPkh(Control source, PKH pkh, string? slotTag = null)
    {
        if (!PkhService.TrySerialize(pkh, out var data, out _))
            return; // e.g. PC9 data can't be serialized; same silent behavior the editor's sprite had.
        string? file = null;
        try
        {
            file = Path.Combine(Path.GetTempPath(), PathUtil.CleanFileName($"{PkhService.GetDefaultFileName(pkh)}.pkh"));
            File.WriteAllBytes(file, data);
            var obj = new DataObject();
            obj.SetData(DataFormats.FileDrop, new[] { file });
            if (slotTag != null)
                obj.SetData(SlotDataFormat, slotTag);
            // Copy only: PKHeX's box slots treat Link as "moved to another slot".
            source.DoDragDrop(obj, DragDropEffects.Copy);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Drag && Drop failed: {ex.Message}", source.FindForm()?.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (file != null)
                _ = DeleteLaterAsync(file);
        }
    }

    private static async Task DeleteLaterAsync(string path)
    {
        await Task.Delay(TempFileLifetime).ConfigureAwait(false);
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
