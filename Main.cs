using HarmonyLib;
using ModLoader;
using SFS.Variables;
using SFS.IO;

namespace UITools;

/// <summary>
///     Class that does Update Checking on off
/// </summary>

[Serializable]
public class Data
{
    // Bool_Local and Float_Local are classes in SFS.Variables that let you detect onChange events.
    public Bool_Local disableModUpdates = new Bool_Local();
}



public class Config : ModSettings<Data>
{
    static Config main;

    Action saveAction;

    protected override FilePath SettingsFile { get; } = Main.modFolder.ExtendToFile("Config.txt");

    public static void Load()
    {
        main = new Config();
        main.Initialize();
    }

    protected override void RegisterOnVariableChange(Action onChange)
    {
        /*
        This tells it to save when the vars are changed. You need to do this for every 
        var you want to save, so every time a value changes it is immediately saved.

        Technically you can just tie this to some other event like the game closing, but 
        that is bad practice.
        */
        settings.disableModUpdates.OnChange += onChange;
    }
}

/// <summary>
///     Main class of the mod
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Main : Mod, IUpdatable
{
    internal static Main main;

    Harmony patcher;

    /// <summary>Default constructor</summary>
    public Main()
    {
        main = this;
    }
    // Implementation

    /// <summary>NameID</summary>
    public override string ModNameID => "UITools";

    /// <summary>DisplayName</summary>
    public override string DisplayName => "UI Tools";

    /// <summary>Author</summary>
    public override string Author => "StarMods";

    /// <summary>MinimumGameVersionNecessary</summary>
    public override string MinimumGameVersionNecessary => "1.5.8";

    /// <summary>ModVersion</summary>
    public override string ModVersion => "1.0";

    /// <summary>Description</summary>
    public override string Description =>
        "Mod that provides advanced UI functionality and used by other mods as dependency";

    /// <summary>Icon</summary>
    public override string IconLink => "https://i.imgur.com/r7rCmJT.jpg";

    /// <inheritdoc />
    public Dictionary<string, FilePath> UpdatableFiles => new()
    {
        {
            "https://github.com/cucumber-sp/UITools/releases/latest/download/UITools.dll",
            new FolderPath(ModFolder).ExtendToFile("UITools.dll")
        }
    };

    public static FolderPath modFolder;

    /// <summary>Early Load</summary>
    public override void Early_Load()
    {
        PatchAll();
        ConfigurationMenu.Initialize();
        PositionSaver.Initialize();

        /*
            ModFolder is an existing variable in the base class. It cannot be accessed by other classes by
            default, so we copy it to the var.
        */
        modFolder = new FolderPath(ModFolder);

        Config.Load();
    }

    /// <summary>
    ///     Load
    /// </summary>
    public override async void Load()
    {
        // Check for the presence of the DisableModUpdates flag in any assembly


        if (!Config.settings.disableModUpdates)
        {
            await ModsUpdater.UpdateAll();
        }
    }

    void PatchAll()
    {
        (patcher ??= new Harmony("UITools")).PatchAll();
    }
}