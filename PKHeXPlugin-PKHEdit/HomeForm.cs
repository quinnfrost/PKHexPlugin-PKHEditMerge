using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHEdit;

/// <summary>
/// HOME box viewer, laid out like PKHeX's Box Viewer window (SAV_BoxViewer + BoxEditor + PokeGrid):
/// a toolbar row (swap, ◀, box selector, ▶) over a 6×5 grid drawn on the box wallpaper. All .pkh
/// files live flat under {run dir}/Home; which box and slot a file occupies is recorded in
/// Home/home.json, and file names stay in PKHeX's standard form. Only .pkh files can be dragged
/// in or out; right-click / double-click loads a slot into the PKH Editor or saves the editor's PKH there.
/// </summary>
internal sealed class HomeForm : Form
{
    // Grid metrics copied from PKHeX's PokeGrid (68×56 sprites, 1px padding, 1px shared borders).
    private const int SpriteW = 68, SpriteH = 56, PadEdge = 1, Border = 1;
    private const int Cols = 6, Rows = 5;
    private const int CellW = SpriteW + 2 * Border;                       // 70
    private const int CellH = SpriteH + 2 * Border;                       // 58
    private const int GridW = (2 * PadEdge) + Border + (Cols * (SpriteW + Border)); // 417
    private const int GridH = (2 * PadEdge) + Border + (Rows * (SpriteH + Border)); // 288
    private const int ToolbarH = 27;
    private const string BaseTitle = "HOME Box Viewer";

    private readonly PKHEditor editor;
    private readonly Button B_BoxSwap;
    private readonly Button B_BoxLeft;
    private readonly Button B_BoxRight;
    private readonly ComboBox CB_BoxSelect;
    private readonly Panel BoxPokeGrid;
    private readonly PictureBox[] Slots;
    private readonly ToolTip tip = new();
    private readonly ContextMenuStrip menu;
    private readonly ToolStripMenuItem miLoad;
    private readonly ToolStripMenuItem miSave;
    private readonly ToolStripMenuItem miDelete;

    private readonly string?[] paths = new string?[HomeStorage.SlotCount];
    private int box;
    private bool loading;
    private Rectangle dragBox = Rectangle.Empty;

    public HomeForm(PKHEditor editor)
    {
        this.editor = editor;
        Slots = new PictureBox[HomeStorage.SlotCount];

        menu = new ContextMenuStrip();
        miLoad = new ToolStripMenuItem("Load into PKH Editor", null, (_, _) => WithSlot(OpenInEditor));
        miSave = new ToolStripMenuItem("Save editor PKH here", null, (_, _) => WithSlot(SaveEditorPkhTo));
        miDelete = new ToolStripMenuItem("Delete file", null, (_, _) => WithSlot(DeleteSlot));
        menu.Items.Add(miLoad);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miSave);
        menu.Items.Add(miDelete);
        menu.Opening += Menu_Opening;

        // Toolbar positions are BoxEditor's own values after RecenterControls (combo centered on 417px).
        B_BoxSwap = new Button { Name = "B_BoxSwap", Location = new Point(0, 0), Size = new Size(24, 24), TabStop = false, Text = "⇄" };
        B_BoxLeft = new Button { Name = "B_BoxLeft", Location = new Point(110, 0), Size = new Size(32, 24), Text = "◀" };
        CB_BoxSelect = new ComboBox
        {
            Name = "CB_BoxSelect",
            Location = new Point(144, 0),
            Size = new Size(128, 25),
            MinimumSize = new Size(128, 0),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
        };
        B_BoxRight = new Button { Name = "B_BoxRight", Location = new Point(274, 0), Size = new Size(32, 24), Text = "▶" };
        for (int i = 0; i < HomeStorage.BoxCount; i++)
            CB_BoxSelect.Items.Add(HomeStorage.GetBoxName(i));

        BoxPokeGrid = new Panel
        {
            Name = "BoxPokeGrid",
            Location = new Point(0, ToolbarH),
            Size = new Size(GridW, GridH),
            BackgroundImageLayout = ImageLayout.Stretch,
        };
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                int index = row * Cols + col;
                string slotName = $"Pokémon Grid Row {row:00} Column {col:00}";
                var pb = new PictureBox
                {
                    Name = slotName,
                    AccessibleName = slotName,
                    AccessibleRole = AccessibleRole.Alert, // same quirk as PokeGrid.GetControl
                    AutoSize = false,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent, // wallpaper shows through, like PKHeX slots
                    Padding = Padding.Empty,
                    Margin = Padding.Empty,
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(PadEdge + (col * (SpriteW + Border)), PadEdge + (row * (SpriteH + Border))),
                    Size = new Size(CellW, CellH),
                    ContextMenuStrip = menu,
                };
                WireSlot(pb, index);
                Slots[index] = pb;
                BoxPokeGrid.Controls.Add(pb);
            }
        }

        SuspendLayout();
        Text = BaseTitle;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        AutoScaleMode = AutoScaleMode.Inherit; // as in SAV_BoxViewer
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AllowDrop = true;
        ClientSize = new Size(GridW, ToolbarH + GridH); // 417 × 315
        Controls.AddRange([B_BoxSwap, B_BoxLeft, CB_BoxSelect, B_BoxRight, BoxPokeGrid]);
        ResumeLayout(false);

        CB_BoxSelect.SelectedIndexChanged += (_, _) => { if (!loading) SelectBox(CB_BoxSelect.SelectedIndex); };
        B_BoxLeft.Click += (_, _) => SelectBox((box + HomeStorage.BoxCount - 1) % HomeStorage.BoxCount);
        B_BoxRight.Click += (_, _) => SelectBox((box + 1) % HomeStorage.BoxCount);
        B_BoxSwap.Click += B_BoxSwap_Click;
        DragEnter += OnDragEnter;
        DragOver += OnDragOver;
        DragDrop += OnDragDrop;
        GiveFeedback += (_, e) => e.UseDefaultCursors = false;
        MouseWheel += (_, e) => SelectBox(e.Delta > 0 ? (box + HomeStorage.BoxCount - 1) % HomeStorage.BoxCount : (box + 1) % HomeStorage.BoxCount);

        tip.SetToolTip(B_BoxSwap, "Swap this box's contents with the next box.");
        tip.SetToolTip(B_BoxLeft, "Previous box (wraps around). Scroll wheel works too.");
        tip.SetToolTip(B_BoxRight, "Next box (wraps around). Scroll wheel works too.");
        tip.SetToolTip(CB_BoxSelect, $"Boxes are stored in:\n{HomeStorage.Root}");

        SelectBox(0);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var pb in Slots)
                pb.Image?.Dispose();
            BoxPokeGrid.BackgroundImage?.Dispose();
            menu.Dispose();
            tip.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Slots

    private void WireSlot(PictureBox pb, int index)
    {
        pb.MouseDown += (_, e) =>
        {
            var size = SystemInformation.DragSize;
            dragBox = e.Button == MouseButtons.Left
                ? new Rectangle(e.X - (size.Width / 2), e.Y - (size.Height / 2), size.Width, size.Height)
                : Rectangle.Empty;
        };
        pb.MouseUp += (_, _) => dragBox = Rectangle.Empty;
        pb.MouseMove += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || dragBox == Rectangle.Empty || dragBox.Contains(e.Location))
                return;
            dragBox = Rectangle.Empty;
            DragOutSlot(index);
        };
        pb.MouseDoubleClick += (_, _) => OpenInEditor(index);
        pb.MouseEnter += (_, _) => pb.Cursor = paths[index] != null ? Cursors.Hand : Cursors.Default;
        pb.MouseLeave += (_, _) => pb.Cursor = Cursors.Default;
    }

    private void DragOutSlot(int index)
    {
        if (paths[index] is not { } path)
            return;
        if (!PkhService.TryLoad(path, out var pkh, out _))
        {
            MessageBox.Show(this, $"Can't read \"{Path.GetFileName(path)}\".", BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        PkmUtil.DragOutPkh(Slots[index], pkh, $"{box}:{index}");
    }

    private void RenderSlot(int index)
    {
        var pb = Slots[index];
        var old = pb.Image;
        pb.Image = null;
        old?.Dispose();

        var path = paths[index];
        if (path == null)
        {
            tip.SetToolTip(pb, $"Empty slot {index + 1}. Drop a .pkh file here.");
            return;
        }
        tip.SetToolTip(pb, $"{Path.GetFileName(path)}\nDrag out to export; right-click for actions.");
        if (PkhService.TryLoad(path, out var pkh, out _))
        {
            if (RenderSprite(pkh) is { } img)
                pb.Image = img;
            tip.SetToolTip(pb, $"{Path.GetFileName(path)}\n{PkmUtil.GetDisplayName(pkh)}\nDrag out to export; double-click to load into the PKH Editor.");
        }
    }

    // Prefer the version matching the active save (like the editor's sprite), then the shared fallback
    // order; when the PKH carries no version data at all, ask the sprite renderer about the PKH itself.
    private Image? RenderSprite(PKH pkh)
    {
        var format = PkhService.GetPreferredFormat(pkh, editor.EditorPkmType);
        if (format != HomeGameDataFormat.None && PkhService.Export(pkh, format) is { } exported && PKMSprite.Render(exported) is { } img)
            return img;
        return PKMSprite.Render(pkh);
    }

    private int FirstEmpty() => Array.IndexOf(paths, null);

    #endregion

    #region Boxes

    private void SelectBox(int index)
    {
        box = Math.Clamp(index, 0, HomeStorage.BoxCount - 1);
        loading = true;
        try { CB_BoxSelect.SelectedIndex = box; }
        finally { loading = false; }
        ReloadBox();
    }

    private void ReloadBox()
    {
        var loaded = HomeStorage.LoadBox(box, out _);
        Array.Copy(loaded, paths, HomeStorage.SlotCount);
        SetWallpaper(box);
        for (int i = 0; i < Slots.Length; i++)
            RenderSlot(i);
    }

    /// <summary>Re-renders after PKHeX's display language changed: the localized GameInfo name tables
    /// are already swapped by the time plugins are notified, so re-reading the files refreshes tooltips.</summary>
    internal void RefreshNames()
    {
        for (int i = 0; i < Slots.Length; i++)
            RenderSlot(i);
    }

    private void B_BoxSwap_Click(object? sender, EventArgs e)
    {
        int other = (box + 1) % HomeStorage.BoxCount;
        if (!HomeStorage.SwapBoxes(box, other, out var error))
        {
            MessageBox.Show(this, error, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        ReloadBox();
    }

    // Wallpaper comes from PKHeX's own WallpaperUtil (PKHeX.Drawing.Misc, not on NuGet), reached by
    // reflection exactly like PKMSprite reaches the sprite renderer. The result is copied, because
    // PKHeX owns the original bitmap.
    private static Func<SaveFile, int, Bitmap>? wallpaperFn;

    private void SetWallpaper(int boxIndex)
    {
        Image? img = null;
        try
        {
            var fn = wallpaperFn ??= FindWallpaperFn();
            if (fn != null)
            {
                var sav = editor.ActiveSave;
                int index = sav.HasBox ? boxIndex % Math.Max(1, sav.BoxCount) : 0;
                img = new Bitmap(fn(sav, index));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PKHEdit: wallpaper load failed: {ex.Message}");
        }
        var old = BoxPokeGrid.BackgroundImage;
        BoxPokeGrid.BackgroundImage = img;
        old?.Dispose();
    }

    private static Func<SaveFile, int, Bitmap>? FindWallpaperFn()
    {
        try
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "PKHeX.Drawing.Misc")
                      ?? Assembly.Load(new AssemblyName("PKHeX.Drawing.Misc"));
            var method = asm.GetType("PKHeX.Drawing.Misc.WallpaperUtil")
                ?.GetMethod("WallpaperImage", BindingFlags.Public | BindingFlags.Static, [typeof(SaveFile), typeof(int)]);
            if (method is null || !typeof(Bitmap).IsAssignableFrom(method.ReturnType))
                return null;
            return method.CreateDelegate<Func<SaveFile, int, Bitmap>>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region Drag & drop

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] { Length: not 0 } files)
        {
            e.Effect = DragDropEffects.None;
            return;
        }
        foreach (var file in files)
        {
            if (!file.EndsWith(".pkh", StringComparison.OrdinalIgnoreCase)) // only PKH files are accepted
            {
                e.Effect = DragDropEffects.None;
                return;
            }
        }
        e.Effect = e.AllowedEffect.HasFlag(DragDropEffects.Copy) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDragOver(object? sender, DragEventArgs e) =>
        e.Effect = e.AllowedEffect.HasFlag(DragDropEffects.Copy) ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] { Length: not 0 } files)
            return;
        int target = SlotAtCursor(e);
        string? tag = e.Data.GetDataPresent(PkmUtil.SlotDataFormat) ? e.Data.GetData(PkmUtil.SlotDataFormat) as string : null;
        // Defer so the drag source (a slot or the editor's sprite) is released before anything opens.
        BeginInvoke(() => HandleDrop(files, tag, target));
    }

    private int SlotAtCursor(DragEventArgs e)
    {
        var formPoint = PointToClient(new Point(e.X, e.Y));
        var gridPoint = BoxPokeGrid.PointToClient(PointToScreen(formPoint));
        if (gridPoint.X < 0 || gridPoint.Y < 0)
            return -1;
        int col = gridPoint.X / CellW, row = gridPoint.Y / CellH;
        return col < Cols && row < Rows ? row * Cols + col : -1;
    }

    private static bool ParseTag(string tag, out int srcBox, out int srcSlot)
    {
        var parts = tag.Split(':');
        srcBox = -1;
        srcSlot = -1;
        return parts.Length == 2 && int.TryParse(parts[0], out srcBox) && int.TryParse(parts[1], out srcSlot);
    }

    private void HandleDrop(string[] files, string? tag, int target)
    {
        if (tag != null && ParseTag(tag, out int srcBox, out int srcSlot) && HandleInternalDrop(srcBox, srcSlot, target))
            return;

        string? error = null;
        foreach (var file in files)
        {
            if (!PkhService.TryLoad(file, out _, out _))
            {
                error = $"\"{Path.GetFileName(file)}\" is not a PKH file — only .pkh files can be dropped here.";
                continue;
            }
            int slot = target >= 0 ? target : FirstEmpty();
            if (slot < 0)
            {
                error = $"{HomeStorage.GetBoxName(box)} is full; \"{Path.GetFileName(file)}\" was not stored.";
                break;
            }
            if (!HomeStorage.Place(box, slot, file, out var placeError))
            {
                error = $"Couldn't store \"{Path.GetFileName(file)}\": {placeError}";
                break;
            }
            target = -1; // remaining files go to the next free slot
        }
        ReloadBox();
        if (error != null)
            MessageBox.Show(this, error, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>Moves/swaps a slot of this window onto another slot; returns false to fall back to file handling.</summary>
    private bool HandleInternalDrop(int srcBox, int srcSlot, int target)
    {
        if (srcBox != box || (uint)srcSlot >= (uint)paths.Length || paths[srcSlot] is not { } srcPath)
            return false; // can't normally happen mid-drag
        if (target == srcSlot)
            return true;
        if (target < 0)
            target = FirstEmpty();

        string? error;
        if (target < 0)
        {
            error = $"{HomeStorage.GetBoxName(box)} is full.";
        }
        else if (paths[target] is { } dstPath)
        {
            error = HomeStorage.Swap(box, srcPath, srcSlot, dstPath, target, out var swapError) ? null : swapError;
        }
        else
        {
            error = HomeStorage.SetPosition(srcPath, box, target, out var placeError) ? null : placeError;
        }
        ReloadBox();
        if (error != null)
            MessageBox.Show(this, error, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }

    #endregion

    #region Editor actions (context menu)

    private int SlotFromMenu() => menu.SourceControl is PictureBox pb ? Array.IndexOf(Slots, pb) : -1;

    private void WithSlot(Action<int> action)
    {
        int i = SlotFromMenu();
        if (i >= 0)
            action(i);
    }

    private void Menu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        int i = SlotFromMenu();
        if (i < 0)
        {
            e.Cancel = true;
            return;
        }
        miLoad.Enabled = miDelete.Enabled = paths[i] != null;
        miSave.Enabled = editor.CurrentPkh != null;
    }

    private void OpenInEditor(int index)
    {
        if (paths[index] is not { } path)
            return;
        if (editor.OpenFile(path))
            editor.Activate();
    }

    private void SaveEditorPkhTo(int index)
    {
        if (editor.CurrentPkh is not { } current)
            return;
        var path = paths[index] ?? HomeStorage.GetCanonicalPath(box, index, current);
        if (paths[index] != null
            && MessageBox.Show(this, $"Replace {Path.GetFileName(path)} with the PKH currently in the editor?", BaseTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Couldn't create the box folder: {ex.Message}", BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!PkhService.TrySave(current, path, out var error))
            MessageBox.Show(this, $"Couldn't save to {HomeStorage.GetBoxName(box)}: {error}", BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            // Pin the new file to the slot it was saved into; otherwise the next reconcile would only
            // see an untracked file and hand it the first free slot (which may be a different one).
            HomeStorage.SetPosition(path, box, index, out _);
            ReloadBox();
        }
    }

    private void DeleteSlot(int index)
    {
        if (paths[index] is not { } path)
            return;
        if (MessageBox.Show(this, $"Delete {Path.GetFileName(path)} from the Home folder?", BaseTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        if (!HomeStorage.Delete(path, out var error))
        {
            MessageBox.Show(this, $"Delete failed: {error}", BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        ReloadBox();
    }

    #endregion
}
