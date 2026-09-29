using System;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKMMerge;

public class PKMMerge : IPlugin
{
    public string Name => nameof(PKMMerge);
    public int Priority => 1; // Loading order, lowest is first.

    // Initialized on plugin load
    public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
    public IPKMView PKMEditor { get; private set; } = null!;

    public FormDiff? formDiff;

    public void Initialize(params object[] args)
    {
        Console.WriteLine($"Loading {Name}...");
        SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider)!;
        PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView)!;
        
        var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip)!;
        PKMSprite.Host = menu;
        LoadMenuStrip(menu);
    }

    private void LoadMenuStrip(ToolStrip menuStrip)
    {
        var items = menuStrip.Items;
        if (items.Find("Menu_Tools", false)[0] is not ToolStripDropDownItem tools)
            throw new ArgumentException(nameof(menuStrip));
        AddPluginControl(tools);
    }

    private void AddPluginControl(ToolStripDropDownItem tools)
    {
        var ctrl = new ToolStripMenuItem(Name);
        ctrl.Click += (_, _) => ShowDiffForm();
        tools.DropDownItems.Add(ctrl);
        Console.WriteLine($"{Name} added menu items.");
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

    public void NotifySaveLoaded()
    {
        Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
    }

    public bool TryLoadFile(string filePath)
    {
        Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
        return false; // no action taken
    }
}
