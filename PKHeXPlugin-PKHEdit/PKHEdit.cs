using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHEdit;

public class PKHEdit : IPlugin
{
    public string Name => nameof(PKHEdit);
    public int Priority => 1; // Loading order, lowest is first.

    public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
    public IPKMView PKMEditor { get; private set; } = null!;

    // One plain (owner-less) Compare window per menu click — several can be open at once.
    private readonly List<FormDiff> compareForms = [];
    private PKHEditor? pkhEditor;

    public void Initialize(params object[] args)
    {
        SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider)!;
        PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView)!;

        var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip)!;
        PKMSprite.Host = menu;
        AddMenuItem(menu);
    }

    private void AddMenuItem(ToolStrip menuStrip)
    {
        if (menuStrip.Items.Find("Menu_Tools", false) is not [ToolStripDropDownItem tools, ..])
            throw new ArgumentException("PKHeX menu strip has no Tools menu.", nameof(menuStrip));

        var root = new ToolStripMenuItem(Name);
        var compare = new ToolStripMenuItem("PKM Compare");
        compare.Click += (_, _) => NewCompareWindow();
        var homeEditor = new ToolStripMenuItem("PKH Editor");
        homeEditor.Click += (_, _) => pkhEditor = Show(pkhEditor, () => new PKHEditor(PKMEditor, SaveFileEditor));
        root.DropDownItems.Add(compare);
        root.DropDownItems.Add(homeEditor);
        tools.DropDownItems.Add(root);
    }

    // One instance per window: re-activate it if it is still open.
    private static T Show<T>(T? existing, Func<T> create) where T : Form
    {
        if (existing is null || existing.IsDisposed)
        {
            existing = create();
            existing.Show();
        }
        else if (existing.WindowState == FormWindowState.Minimized)
        {
            existing.WindowState = FormWindowState.Normal;
        }
        existing.Activate();
        return existing;
    }

    // Unlike the PKH Editor (single instance), every menu click opens a NEW plain Compare window.
    private void NewCompareWindow()
    {
        var form = new FormDiff(PKMEditor, SaveFileEditor);
        form.FormClosed += (_, _) => compareForms.Remove(form);
        compareForms.Add(form);
        form.Show();
        form.Activate();
    }

    public void NotifyDisplayLanguageChanged(string language)
    {
        foreach (var form in compareForms)
            if (!form.IsDisposed)
                form.RefreshNames();
        if (pkhEditor is { IsDisposed: false } editor)
            editor.RefreshNames();
    }

    public void NotifySaveLoaded() { }

    public bool TryLoadFile(string filePath) => false;
}
