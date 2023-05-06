using HarmonyLib;

namespace UITools;

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

    /// <summary>Early Load</summary>
    public override void Early_Load()
    {
        PatchAll();
        ConfigurationMenu.Initialize();
        PositionSaver.Initialize();
    }

    /// <summary>
    ///     Load
    /// </summary>
    public override async void Load()
    {
        await ModsUpdater.UpdateAll();
    }

    void PatchAll()
    {
        (patcher ??= new Harmony("UITools")).PatchAll();
    }
}