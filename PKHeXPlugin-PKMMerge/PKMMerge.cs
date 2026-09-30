using System;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKMMerge;

public class PKMMerge : IPlugin
{
    public string Name => nameof(PKMMerge);
    public int Priority => 1; // Loading order, lowest is first.

    public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
    public IPKMView PKMEditor { get; private set; } = null!;

    private FormDiff? formDiff;

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

        var item = new ToolStripMenuItem(Name);
        item.Click += (_, _) => ShowDiffForm();
        tools.DropDownItems.Add(item);
    }

    private void ShowDiffForm()
    {
        if (formDiff is null || formDiff.IsDisposed)
        {
            formDiff = new FormDiff(PKMEditor, SaveFileEditor);
            formDiff.Show();
        }
        else if (formDiff.WindowState == FormWindowState.Minimized)
        {
            formDiff.WindowState = FormWindowState.Normal;
        }
        formDiff.Activate();
    }

    public void NotifyDisplayLanguageChanged(string language)
    {
        if (formDiff is { IsDisposed: false } form)
            form.RefreshNames();
    }

    public void NotifySaveLoaded() { }

    public bool TryLoadFile(string filePath) => false;
}
