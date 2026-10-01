using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKMMerge;

// Borrows PKHeX's already-loaded sprite renderer, so no compile-time reference to PKHeX.Drawing.PokeSprite is needed.
// PKHeX.Drawing.PokeSprite isn't on NuGet; referencing it directly would mean pointing at a local build of the
// ../PKHeX source tree, which breaks on any machine that doesn't have that exact path. Reflection avoids that.
internal static class PKMSprite
{
    private const string SpriteAssembly = "PKHeX.Drawing.PokeSprite";
    private const string SpriteUtilType = "PKHeX.Drawing.PokeSprite.SpriteUtil";
    private const string BuilderUtilType = "PKHeX.Drawing.PokeSprite.SpriteBuilderUtil";
    private const string EditorPreviewName = "dragout";

    // SpriteBuilder subclasses are art styles (classic/mugshot/artwork), not per-generation renderers;
    // PKHeX itself only ever has one active globally. Switching by PKM type is this plugin's own choice.
    public const bool RenderPerGenerationStyle = true;

    private static Func<PKM, Image>? renderer;
    private static Func<PKM, Image>? styledRenderer;

    /// <summary>Any control hosted on the PKHeX main window; used to locate the editor preview for the fallback.</summary>
    public static Control? Host { get; set; }

    /// <summary>Renders a sprite for <paramref name="pk"/>, or null if the PKHeX renderer is unavailable.</summary>
    public static Image? Render(PKM pk)
    {
        try
        {
            if (RenderPerGenerationStyle)
            {
                var styled = styledRenderer ??= FindStyledRenderer();
                if (styled is not null)
                    return new Bitmap(styled(pk));
            }

            var render = renderer ??= FindRenderer();
            if (render is null)
                return null;
            // Copy so this form owns (and may dispose) its image without touching PKHeX's instances.
            return new Bitmap(render(pk));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PKMMerge: sprite render failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>Copies the main window's preview sprite, which reflects the PKM currently in the editor.</summary>
    public static Image? CopyEditorPreview()
    {
        try
        {
            var form = Host?.FindForm();
            var preview = form?.Controls.Find(EditorPreviewName, true).OfType<PictureBox>().FirstOrDefault();
            return preview?.Image is { } img ? new Bitmap(img) : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PKMMerge: editor preview copy failed: {ex.Message}");
            return null;
        }
    }

    private static Func<PKM, Image>? FindRenderer()
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == SpriteAssembly);
        var method = asm?.GetType(SpriteUtilType)?.GetMethod("Sprite", BindingFlags.Public | BindingFlags.Static, [typeof(PKM)]);
        if (method is null || !typeof(Image).IsAssignableFrom(method.ReturnType))
            return null;
        return method.CreateDelegate<Func<PKM, Image>>();
    }

    /// <summary>Picks a builder instance per <see cref="PKM"/> type, matching what each generation's own game uses in HOME/PKHeX.</summary>
    private static Func<PKM, Image>? FindStyledRenderer()
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == SpriteAssembly);
        var spriteUtil = asm?.GetType(SpriteUtilType);
        var builderUtil = asm?.GetType(BuilderUtilType);
        if (spriteUtil is null || builderUtil is null)
            return null;

        var getSuggested = builderUtil.GetMethod("GetSuggestedMode", BindingFlags.Public | BindingFlags.Static);
        var sb8s = spriteUtil.GetField("SB8s", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        var sb8c = spriteUtil.GetField("SB8c", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        var sb8a = spriteUtil.GetField("SB8a", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (getSuggested is null || sb8s is null || sb8c is null || sb8a is null)
            return null;

        // "Mode" per PKM type: a blank save of each generation decides it the same way PKHeX itself would
        // (SpriteBuilderUtil.GetSuggestedMode only switches on the SaveFile's type, no save data is read).
        // PKHeX.Core is already a compile-time (NuGet) reference, so the SaveFile types themselves don't need reflection.
        var defaultBuilder = sb8s;
        var byType = new (Type Pk, SaveFile Sav)[]
        {
            (typeof(PA8), new SAV8LA()), (typeof(PK9), new SAV9SV()), (typeof(PA9), new SAV9ZA()),
        }.Select(x => (x.Pk, Builder: GetBuilderFor(x.Sav, getSuggested, sb8s, sb8c, sb8a)))
         .Where(x => x.Builder != null)
         .ToDictionary(x => x.Pk, x => x.Builder!);

        var sampleMethod = sb8s.GetType().GetMethod("GetSprite", BindingFlags.Public | BindingFlags.Instance,
            [typeof(ushort), typeof(byte), typeof(byte), typeof(uint), typeof(int), typeof(bool), typeof(Shiny), typeof(EntityContext)]);
        if (sampleMethod is null)
            return null;

        return pk =>
        {
            var builder = byType.GetValueOrDefault(pk.GetType(), defaultBuilder);
            var shiny = ShinyExtensions.GetType(pk);
            var formArg = pk is IFormArgument f ? f.FormArgument : 0u;
            var result = sampleMethod.Invoke(builder, [pk.Species, pk.Form, pk.Gender, formArg, pk.SpriteItem, pk.IsEgg, shiny, pk.Context]);
            return (Image)result!;
        };
    }

    // Only GetSuggestedMode (PKHeX.Drawing.PokeSprite) is reflected; the blank SaveFile is a normal PKHeX.Core type.
    private static object? GetBuilderFor(SaveFile blankSav, MethodInfo getSuggested, object sb8s, object sb8c, object sb8a)
    {
        try
        {
            var mode = getSuggested.Invoke(null, [blankSav])?.ToString();
            return mode switch
            {
                "CircleMugshot5668" => sb8c,
                "SpritesArtwork5668" => sb8a,
                _ => sb8s,
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PKMMerge: blank save probe for {blankSav.GetType().Name} failed: {ex.Message}");
            return null;
        }
    }
}
