using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using ModLoader;
using System.Security.Cryptography;
using SFS.Input;
using SFS.IO;
using SFS.UI;
using UnityEngine;

namespace UITools
{
    static class ModsUpdater
    {
        static readonly HttpClient Http = new ();
        static readonly MD5 MD5 = MD5.Create();
        static int loadedFiles;
        public static async UniTask UpdateAll()
        {
            try
            {
                IUpdatable[] mods = Loader.main.GetAllMods().OfType<IUpdatable>().ToArray();
                if (mods.Length == 0)
                    return;

                foreach (IUpdatable mod in mods)
                    await Update(mod);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                if (loadedFiles > 0)
                    MenuGenerator.OpenConfirmation(CloseMode.Current, () => "Some files were updated. Do you want to restart the game?", () => "Restart",ApplicationUtility.Relaunch);
            }
        }

        static async UniTask Update(IUpdatable mod)
        {
            foreach (KeyValuePair<string, FilePath> file in mod.UpdatableFiles)
            {
                HttpRequestMessage request = new (HttpMethod.Head, file.Key);
                HttpResponseMessage response = await Http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    continue;
                
                byte[] md5HashLocal = file.Value.FileExists() ? MD5.ComputeHash(file.Value.ReadBytes()) : Array.Empty<byte>();
                byte[] md5HashRemote = response.Content.Headers.ContentMD5;

                if (md5HashLocal.SequenceEqual(md5HashRemote))
                    continue;
                if (!await AsyncDialogs.OpenConfirmation(CloseMode.Current, () => $"Update for {(mod as Mod)?.DisplayName} is available. Update?", () => "Update"))
                    continue;

                request = new(HttpMethod.Get, file.Key);
                response = await Http.SendAsync(request);

                byte[] data = await response.Content.ReadAsByteArrayAsync();
                if (data != null)
                {
                    file.Value.WriteBytes(data);
                    loadedFiles += 1;
                }
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