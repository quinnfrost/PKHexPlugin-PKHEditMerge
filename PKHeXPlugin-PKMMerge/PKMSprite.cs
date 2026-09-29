using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKMMerge;

// Borrows PKHeX's already-loaded sprite renderer, so no compile-time reference to PKHeX.Drawing.PokeSprite is needed.
internal static class PKMSprite
{
    private const string SpriteAssembly = "PKHeX.Drawing.PokeSprite";
    private const string SpriteUtilType = "PKHeX.Drawing.PokeSprite.SpriteUtil";
    private const string EditorPreviewName = "dragout";

    private static Func<PKM, Image>? renderer;

    /// <summary>Any control hosted on the PKHeX main window; used to locate the editor preview for the fallback.</summary>
    public static Control? Host { get; set; }

    /// <summary>Renders a sprite for <paramref name="pk"/>, or null if the PKHeX renderer is unavailable.</summary>
    public static Image? Render(PKM pk)
    {
        try
        {
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
}
