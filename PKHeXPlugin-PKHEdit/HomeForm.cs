using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHEdit;

/// <summary>
/// HOME box viewer, laid out like PKHeX's Box Viewer window (SAV_BoxViewer + BoxEditor + PokeGrid):
/// a toolbar row (◀, box selector, ▶, +) over a 6×5 grid drawn on the box wallpaper. All .pkh
/// files live flat under {run dir}/Home; which box and slot a file occupies is recorded in
/// Home/home.json, and file names stay in PKHeX's standard form. Only .pkh files can be dragged
/// in or out; right-click / double-click loads a slot into the PKH Editor or saves the editor's PKH there
/// (an overwrite adds a warning line when the stored file is a different Pokémon).
/// "+" opens another window on a different box; every window shares the one Home folder and refreshes
/// together after any change, so a slot dragged from one window shows up in the other.
/// Right-clicking the box selector renames boxes (the name is only a label stored in home.json — the
/// box a file lives in is an index, so renaming moves nothing) or opens the Home folder.
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
    private readonly Button B_BoxLeft;
    private readonly Button B_BoxRight;
    private readonly Button B_NewWindow;
    private readonly ComboBox CB_BoxSelect;
    private readonly Panel BoxPokeGrid;
    private readonly PictureBox[] Slots;
    private readonly ToolTip tip = new();
    private readonly ContextMenuStrip menu;
    private readonly ToolStripMenuItem miLoad;
    private readonly ToolStripMenuItem miSave;
    private readonly ToolStripMenuItem miDelete;
    private readonly ContextMenuStrip boxMenu;
    private readonly ToolStripMenuItem miRenameBox;
    private readonly ToolStripMenuItem miResetBoxName;

    // Every open HOME window: storage is shared (one Home folder + home.json), so a change made in one
    // window has to be reflected in the others.
    private static readonly List<HomeForm> LiveWindows = [];

    private readonly string?[] paths = new string?[HomeStorage.SlotCount];
    private int box;
    private bool loading;
    private Rectangle dragBox = Rectangle.Empty;

    public HomeForm(PKHEditor editor, int initialBox = 0)
    {
        this.editor = editor;
        Slots = new PictureBox[HomeStorage.SlotCount];

        menu = new ContextMenuStrip();
        miLoad = new ToolStripMenuItem("Load into PKH Editor", null, (_, _) => WithSlot(OpenInEditor));
        miSave = new ToolStripMenuItem("Save editor PKH here", null, (_, _) => WithSlot(SaveEditorPkhTo));
        miDelete = new ToolStripMenuItem("Delete file (Shift = skip Recycle Bin)", null, (_, _) => WithSlot(DeleteSlot));
        menu.Items.Add(miLoad);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miSave);
        menu.Items.Add(miDelete);
        menu.Opening += Menu_Opening;

        // The flat layout keeps every box in the same folder, so right-click offers that folder directly;
        // renaming only relabels the box in home.json, it never touches file positions.
        boxMenu = new ContextMenuStrip();
        miRenameBox = new ToolStripMenuItem("Rename Box…", null, (_, _) => RenameBox());
        miResetBoxName = new ToolStripMenuItem("Reset Box Name", null, (_, _) => ResetBoxName());
        boxMenu.Items.Add(miRenameBox);
        boxMenu.Items.Add(miResetBoxName);
        boxMenu.Items.Add(new ToolStripSeparator());
        boxMenu.Items.Add(new ToolStripMenuItem("Show in File Explorer", null, (_, _) => OpenHomeFolder()));
        boxMenu.Opening += (_, _) => miResetBoxName.Enabled = HomeStorage.GetBoxName(box) != HomeStorage.GetDefaultBoxName(box);

        // Toolbar positions are BoxEditor's own values after RecenterControls (combo centered on 417px).
        B_BoxLeft = new Button { Name = "B_BoxLeft", Location = new Point(110, 0), Size = new Size(32, 24), Text = "◀" };
        CB_BoxSelect = new ComboBox
        {
            Name = "CB_BoxSelect",
            Location = new Point(144, 0),
            Size = new Size(128, 25),
            MinimumSize = new Size(128, 0),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
            ContextMenuStrip = boxMenu,
        };
        B_BoxRight = new Button { Name = "B_BoxRight", Location = new Point(274, 0), Size = new Size(32, 24), Text = "▶" };
        B_NewWindow = new Button { Name = "B_NewWindow", Location = new Point(GridW - 24, 0), Size = new Size(24, 24), Text = "+" };
        foreach (var name in HomeStorage.GetBoxNames())
            CB_BoxSelect.Items.Add(name);

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
        Controls.AddRange([B_BoxLeft, CB_BoxSelect, B_BoxRight, B_NewWindow, BoxPokeGrid]);
        ResumeLayout(false);

        CB_BoxSelect.SelectedIndexChanged += (_, _) => { if (!loading) SelectBox(CB_BoxSelect.SelectedIndex); };
        B_BoxLeft.Click += (_, _) => SelectBox((box + HomeStorage.BoxCount - 1) % HomeStorage.BoxCount);
        B_BoxRight.Click += (_, _) => SelectBox((box + 1) % HomeStorage.BoxCount);
        B_NewWindow.Click += (_, _) => OpenNewWindow();
        DragEnter += OnDragEnter;
        DragOver += OnDragOver;
        DragDrop += OnDragDrop;
        GiveFeedback += (_, e) => e.UseDefaultCursors = false;
        MouseWheel += (_, e) => SelectBox(e.Delta > 0 ? (box + HomeStorage.BoxCount - 1) % HomeStorage.BoxCount : (box + 1) % HomeStorage.BoxCount);

        tip.SetToolTip(B_BoxLeft, "Previous box (wraps around). Scroll wheel works too.");
        tip.SetToolTip(B_BoxRight, "Next box (wraps around). Scroll wheel works too.");
        tip.SetToolTip(B_NewWindow, "Open another HOME window on the next box (several can be open at once).");
        tip.SetToolTip(CB_BoxSelect, $"Boxes are stored in:\n{HomeStorage.Root}\nRight-click to rename the box or show the folder.");

        LiveWindows.Add(this);
        SelectBox(initialBox);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LiveWindows.Remove(this);
            foreach (var pb in Slots)
                pb.Image?.Dispose();
            BoxPokeGrid.BackgroundImage?.Dispose();
            menu.Dispose();
            boxMenu.Dispose();
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
        PkmUtil.DragOutPkh(Slots[index], pkh, path); // the tag is the source path, so any window can move it
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

    // The version the PKH's data came from decides the art style (that game's own look), so a Pokémon
    // from SV keeps SV's artwork even while an older save is open; the editor sprite stays on the active
    // save's generation. When nothing renders, the PKH itself is asked last.
    private Image? RenderSprite(PKH pkh)
    {
        foreach (var format in PkhService.GetSpriteFormats(pkh, editor.EditorPkmType))
        {
            if (format == HomeGameDataFormat.None)
                continue;
            if (PkhService.Export(pkh, format) is { } exported && PKMSprite.Render(exported) is { } img)
                return img;
        }
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
        UpdateBoxNames();
        var loaded = HomeStorage.LoadBox(box, out _);
        Array.Copy(loaded, paths, HomeStorage.SlotCount);
        SetWallpaper(box);
        Text = $"{BaseTitle} - {HomeStorage.GetBoxName(box)}";
        for (int i = 0; i < Slots.Length; i++)
            RenderSlot(i);
    }

    /// <summary>Re-reads the box names into the selector. A rename in any window changes the shared
    /// home.json, so the other windows pick it up here (called from every <see cref="ReloadBox"/>).</summary>
    private void UpdateBoxNames()
    {
        var names = HomeStorage.GetBoxNames();
        loading = true;
        try
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (!string.Equals(CB_BoxSelect.Items[i] as string, names[i], StringComparison.Ordinal))
                    CB_BoxSelect.Items[i] = names[i];
            }
        }
        finally { loading = false; }
    }

    private void RenameBox()
    {
        var current = HomeStorage.GetBoxName(box);
        // The framework's own input box (no bespoke dialog, localized buttons). Cancelling and clearing the
        // field both come back empty, so an empty answer just leaves the name alone — "Reset Box Name"
        // restores the default instead.
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            $"Name for {HomeStorage.GetDefaultBoxName(box)} (currently \"{current}\"):", "Rename Box", current);
        var name = input.Trim();
        if (name.Length == 0 || string.Equals(name, current, StringComparison.Ordinal))
            return;
        if (!HomeStorage.SetBoxName(box, name, out var error))
        {
            MessageBox.Show(this, error, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        RefreshAllBoxes();
    }

    private void ResetBoxName()
    {
        if (!HomeStorage.SetBoxName(box, null, out var error))
        {
            MessageBox.Show(this, error, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        RefreshAllBoxes();
    }

    /// <summary>Re-renders after PKHeX's display language changed: the localized GameInfo name tables
    /// are already swapped by the time plugins are notified, so re-reading the files refreshes tooltips.</summary>
    internal void RefreshNames()
    {
        for (int i = 0; i < Slots.Length; i++)
            RenderSlot(i);
    }

    /// <summary>Reloads every open HOME window, so all views match the storage after a change.</summary>
    private static void RefreshAllBoxes()
    {
        foreach (var form in LiveWindows.ToArray())
        {
            if (!form.IsDisposed)
                form.ReloadBox();
        }
    }

    /// <summary>Re-applies localized names in every open HOME window.</summary>
    internal static void RefreshAllNames()
    {
        foreach (var form in LiveWindows.ToArray())
        {
            if (!form.IsDisposed)
                form.RefreshNames();
        }
    }

    /// <summary>Opens another HOME window on the next box, offset so both stay visible.</summary>
    private void OpenNewWindow()
    {
        var form = new HomeForm(editor, (box + 1) % HomeStorage.BoxCount)
        {
            Owner = editor,
            StartPosition = FormStartPosition.Manual, // otherwise CenterParent would hide this window
        };
        var area = Screen.FromControl(this).WorkingArea;
        var location = new Point(Left + 24, Top + 24);
        if (!area.Contains(new Rectangle(location, form.Size)))
            location = new Point(area.Left + 24, area.Top + 24);
        form.Location = location;
        form.Show();
        form.Activate();
    }

    /// <summary>Shows the folder holding every box file in the system file viewer.</summary>
    private void OpenHomeFolder()
    {
        try
        {
            Directory.CreateDirectory(HomeStorage.Root);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{HomeStorage.Root}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Couldn't open {HomeStorage.Root}: {ex.Message}", BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
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

    private void HandleDrop(string[] files, string? tag, int target)
    {
        if (tag != null && HandleInternalDrop(tag, target))
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
        RefreshAllBoxes();
        if (error != null)
            MessageBox.Show(this, error, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>True when a drag tag points at a .pkh inside the Home folder, i.e. a slot drag rather
    /// than an import from the PKH Editor or Explorer.</summary>
    private static bool IsStoredFile(string path)
    {
        if (!path.EndsWith(".pkh", StringComparison.OrdinalIgnoreCase))
            return false;
        var root = Path.GetFullPath(HomeStorage.Root) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Moves/swaps a slot dragged out of any HOME window onto a slot of this one; returns
    /// false when the tag is not a stored slot, so the drop falls back to file handling.</summary>
    private bool HandleInternalDrop(string sourcePath, int target)
    {
        if (!IsStoredFile(sourcePath) || !File.Exists(sourcePath))
            return false;
        if (target < 0)
            target = FirstEmpty();
        if (target < 0)
        {
            MessageBox.Show(this, $"{HomeStorage.GetBoxName(box)} is full.", BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
        if (paths[target] is { } own && string.Equals(own, sourcePath, StringComparison.OrdinalIgnoreCase))
            return true; // dropped back where it came from

        string? error;
        if (paths[target] is { } dstPath)
            error = HomeStorage.SwapPositions(sourcePath, dstPath, out var swapError) ? null : swapError;
        else
            error = HomeStorage.SetPosition(sourcePath, box, target, out var placeError) ? null : placeError;
        RefreshAllBoxes();
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
        var oldPath = paths[index];
        // A stored file always carries the standard name for the PKH it holds, so overwriting a slot
        // renames it instead of keeping the superseded name (an empty slot just gets a fresh name).
        var path = HomeStorage.GetCanonicalPath(box, index, current, oldPath);
        if (oldPath != null)
        {
            // Same identity rules as an import; a different Pokémon only adds a warning line here.
            PkhService.TryLoad(oldPath, out var stored, out _);
            var identity = PkhService.GetOverwriteWarning(stored, current, editor.UseCustomTracker);
            var details = identity.Length == 0 ? "" : $"\n{identity}";
            var rename = string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase) ? "" : $"\nIt will be saved as \"{Path.GetFileName(path)}\".";
            if (MessageBox.Show(this, $"Replace \"{Path.GetFileName(oldPath)}\" with the PKH currently in the editor?{details}{rename}", BaseTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
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
        // Overwrite the slot in place first: the file then holds the new data even if the rename is
        // refused, and a failed rename only leaves the old (still accurate) name behind.
        var savedPath = oldPath ?? path;
        if (!PkhService.TrySave(current, savedPath, out var error))
        {
            MessageBox.Show(this, $"Couldn't save to {HomeStorage.GetBoxName(box)}: {error}", BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string? warning = null;
        if (oldPath != null && !string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
        {
            if (HomeStorage.Rename(oldPath, path, out var renameError))
                editor.RetargetFile(oldPath, path);
            else
            {
                path = oldPath;
                warning = $"Saved, but the file couldn't be renamed: {renameError}";
            }
        }
        // Pin the file to the slot it was saved into; otherwise the next reconcile would only see an
        // untracked file and hand it the first free slot (which may be a different one).
        HomeStorage.SetPosition(path, box, index, out _);
        RefreshAllBoxes();
        if (warning != null)
            MessageBox.Show(this, warning, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void DeleteSlot(int index)
    {
        if (paths[index] is not { } path)
            return;
        // Read the Shift state now, while the menu click is still the foreground action — the
        // confirmation dialog below moves focus and the modifier could be released by then.
        bool recycle = (Control.ModifierKeys & Keys.Shift) == 0;
        if (MessageBox.Show(this, $"Delete {Path.GetFileName(path)} from the Home folder?", BaseTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        if (!HomeStorage.Delete(path, recycle, out var error))
        {
            MessageBox.Show(this, $"Delete failed: {error}", BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        RefreshAllBoxes();
    }

    #endregion
}
