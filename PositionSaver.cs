using System.Collections.Generic;
using SFS.IO;
using SFS.Parsers.Json;
using SFS.UI.ModGUI;
using UnityEngine;

namespace UITools
{
    /// <summary>
    /// Utility class that adds extra saving functionality
    /// </summary>
    public static class PositionSaver
    {
        static Dictionary<string, Vector2> windows;
        static FilePath saveFile;
        
        internal static void Initialize()
        {
            saveFile = new FolderPath(Main.main.ModFolder).ExtendToFile("positions.txt");
            Load();
            Save();
        }

        /// <summary>
        /// Allow you to register you window for saving that will save position even through game relaunch.
        /// You should call it every time you rebuild the window.
        /// Default saving function should be disabled!
        /// </summary>
        /// <param name="window">Window that will be saved</param>
        /// <param name="uniqueName">Unique name id which uses to find your window position</param>
        /// <example>
        /// The following code register window for permanent position saving
        /// <code>
        /// Window window = Builder.CreateWindow(..., savePosition: false);
        /// window.RegisterPermanentSaving("UITools.myAwesomeWindow");
        /// </code>
        /// </example>
        public static void RegisterPermanentSaving(this Window window, string uniqueName)
        {
            if (windows.ContainsKey(uniqueName))
                window.Position = windows[uniqueName];
            else
                windows.Add(uniqueName, window.Position);
            window.RegisterOnDropListener(() => OnPositionChange(uniqueName, window.Position));
        }

        static void OnPositionChange(string name, Vector2 position)
        {
            windows[name] = position;
            Save();
        }
        
        static void Load()
        {
            windows = !saveFile.FileExists() ? new Dictionary<string, Vector2>() : JsonWrapper.FromJson<Dictionary<string, Vector2>>(saveFile.ReadText());
            windows ??= new Dictionary<string, Vector2>();
        }
        static void Save() => saveFile.WriteText(JsonWrapper.ToJson(windows, true));
    }
}