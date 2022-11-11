using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using ModLoader;
using System.Security.Cryptography;
using SFS.Input;
using SFS.IO;
using SFS.UI;
using SFS.UI.ModGUI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UITools
{
    static class ModsUpdater
    {
        static HttpClient http = new ();
        static readonly MD5 MD5 = MD5.Create();
        static LoadingScreen loadingScreen;
        static int loadedFiles = 0;
        public static async UniTask UpdateAll()
        {
            try
            {
                IUpdatable[] mods = Loader.main.GetAllMods().OfType<IUpdatable>().ToArray();
                if (mods.Length == 0)
                    return;

                loadingScreen = LoadingMenu();
                foreach (IUpdatable mod in mods)
                    await Update(mod);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                loadingScreen.Close();
                if (loadedFiles > 0)
                    MenuGenerator.OpenConfirmation(CloseMode.Current, () => "Some files were updated. Do you want to restart the game?", () => "Restart",ApplicationUtility.Relaunch);
            }
        }

        static async UniTask Update(IUpdatable mod)
        {
            foreach (KeyValuePair<string, FilePath> file in mod.UpdatableFiles)
            {
                loadingScreen.SetModUpdating((mod as Mod).DisplayName);
                loadingScreen.SetFileLoading(file.Value.FileName);

                HttpRequestMessage request = new (HttpMethod.Head, file.Key);
                HttpResponseMessage response = await http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    continue;
                
                byte[] md5HashLocal = file.Value.FileExists() ? MD5.ComputeHash(file.Value.ReadBytes()) : Array.Empty<byte>();
                byte[] md5HashRemote = response.Content.Headers.ContentMD5;

                if (md5HashLocal.SequenceEqual(md5HashRemote))
                    continue;

                request = new(HttpMethod.Get, file.Key);
                response = await http.SendAsync(request);

                byte[] data = await response.Content.ReadAsByteArrayAsync();
                if (data != null)
                {
                    file.Value.WriteBytes(data);
                    loadedFiles += 1;
                }
            }
        }

        static LoadingScreen LoadingMenu()
        { 
            GameObject holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "LoadingScreen");

            Box background = Builder.CreateBox(holder.transform, 10000, 10000, opacity: 1);
            background.Color = new Color(41 / 255f, 41 / 255f, 41 / 255f, 1);
            background.gameObject.AddComponent<ButtonPC>();
            
            Window window = Builder.CreateWindow(holder.transform, 0, 1200, 800, 0, 400, false, false, 1, "Mods Updater");

            Label loadingMod = Builder.CreateLabel(holder.transform, 400, 50,0, 50,  text: "Updating Example Mod");
            
            Label loadingFile = Builder.CreateLabel(holder.transform, 400, 35, 0, -35, text: "Loading example.txt    85.4%");

            window.Active = false;

            return new LoadingScreen(holder, loadingMod, loadingFile);;
        }

        class LoadingScreen
        {
            GameObject holder;
            Label fileLoading;
            Label modLoading;

            public LoadingScreen(GameObject holder, Label modLoading, Label fileLoading)
            {
                this.holder = holder;
                this.fileLoading = fileLoading;
                this.modLoading = modLoading;
            }

            public void Close()
            {
                Object.Destroy(holder);
            }
            public void SetFileLoading(string file)
            {
                fileLoading.Text = $"Loading {file}...";
            }
            public void SetModUpdating(string file)
            {
                modLoading.Text = $"Updating {file}";
            }
        }
    }

    /// <summary>
    /// Implement this interface on main mod class if you want it to be updated at game start
    /// </summary>
    public interface IUpdatable
    {
        /// <summary>
        /// Returns dictionary of files that should be updated
        /// string is web link, FilePath is path where file will downloaded
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, FilePath> UpdatableFiles { get; }
    }
}