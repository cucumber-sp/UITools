using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using Cysharp.Threading.Tasks;
using ModLoader;
using SFS.Input;
using SFS.IO;
using SFS.UI;
using UnityEngine;

namespace UITools
{
    internal static class ModsUpdater
    {
        private static readonly HttpClient Http = new();
        private static int loadedFiles;

        public static void StartUpdate()
        {
            UpdateAll().Forget();
        }

        private static async UniTask UpdateAll()
        {
            try
            {
                var updatesByMod = await GetFilesNeedingUpdate();
                if (updatesByMod.Count == 0) return;

                if (!await ConfirmUpdatePrompt(updatesByMod))
                    return;

                var failedMods = new Dictionary<Mod, List<string>>();

                foreach (var entry in updatesByMod)
                {
                    Mod mod = entry.Key;
                    var files = entry.Value;

                    if (!await TryUpdateMod(mod, files, failedMods))
                        if (!failedMods.ContainsKey(mod))
                            failedMods[mod] = new List<string>();
                }

                if (failedMods.Count > 0 && await ConfirmRetryPrompt(failedMods.Keys))
                {
                    foreach (Mod mod in failedMods.Keys.ToList())
                    {
                        var files = updatesByMod[mod];
                        if (await TryUpdateMod(mod, files, failedMods))
                            failedMods.Remove(mod);
                    }

                    if (failedMods.Count > 0)
                        await ShowFailedModsPrompt(failedMods);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                if (loadedFiles > 0)
                    MenuGenerator.OpenConfirmation(CloseMode.Current,
                        () => "Updates successful. Restart so changes take effect?",
                        () => "Restart",
                        ApplicationUtility.Relaunch);
            }
        }

        private static async UniTask<Dictionary<Mod, List<(string url, FilePath path)>>> GetFilesNeedingUpdate()
        {
            var result = new Dictionary<Mod, List<(string, FilePath)>>();
            using var md5 = MD5.Create();

            foreach (IUpdatable updatable in Loader.main.GetAllMods().OfType<IUpdatable>())
            {
                if (updatable is not Mod mod) continue;

                foreach (var kvp in updatable.UpdatableFiles)
                {
                    var url = kvp.Key;
                    FilePath path = kvp.Value;

                    try
                    {
                        var hashUrl =
                            $"https://files.cucumber-space.online/api/hashes/md5?file={Uri.EscapeDataString(url)}";
                        HttpResponseMessage resp = await Http.GetAsync(hashUrl);
                        if (!resp.IsSuccessStatusCode) throw new Exception("Hash request failed");

                        var local = path.FileExists() ? md5.ComputeHash(path.ReadBytes()) : Array.Empty<byte>();
                        var remote = Convert.FromBase64String(await resp.Content.ReadAsStringAsync());

                        if (!local.SequenceEqual(remote))
                        {
                            if (!result.ContainsKey(mod))
                                result[mod] = new List<(string, FilePath)>();
                            result[mod].Add((url, path));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[ModUpdater] Failed to check hash for {url}: {ex.Message}");
                        if (!result.ContainsKey(mod))
                            result[mod] = new List<(string, FilePath)>();
                        result[mod].Add((url, path));
                    }
                }
            }

            return result;
        }

        private static async UniTask<bool> TryUpdateMod(Mod mod, List<(string url, FilePath path)> files,
            Dictionary<Mod, List<string>> failedMods)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ModUpdates", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var semaphore = new SemaphoreSlim(3);
            var downloads = new List<(FilePath original, string tempPath)>();
            var failedFiles = new List<string>();

            await UniTask.WhenAll(files.Select(async file =>
            {
                var url = file.url;
                FilePath originalPath = file.path;
                var tempPath = Path.Combine(tempDir, Path.GetFileName(originalPath));

                await semaphore.WaitAsync();
                try
                {
                    var success = await Download(url, tempPath);
                    if (success)
                    {
                        lock (downloads)
                        {
                            downloads.Add((originalPath, tempPath));
                        }
                    }
                    else
                    {
                        var fileName = Path.GetFileName(originalPath);
                        lock (failedFiles)
                        {
                            failedFiles.Add(fileName);
                        }

                        Debug.Log($"[ModUpdater] Failed to download '{fileName}' for mod '{mod.DisplayName}'");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));

            if (failedFiles.Count == 0)
            {
                foreach ((FilePath original, string tempPath) download in downloads)
                    File.Copy(download.tempPath, download.original, true);

                loadedFiles += downloads.Count;

                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);

                return true;
            }

            failedMods[mod] = failedFiles;

            foreach ((FilePath original, string tempPath) download in downloads.Where(download => File.Exists(download.tempPath)))
                File.Delete(download.tempPath);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);

            return false;
        }

        private static async UniTask<bool> Download(string url, string path)
        {
            try
            {
                HttpResponseMessage resp = await Http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return false;

                var data = await resp.Content.ReadAsByteArrayAsync();
                if (data == null || data.Length == 0) return false;

                File.WriteAllBytes(path, data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log($"[ModUpdater] Exception during download: {ex.Message}");
                return false;
            }
        }

        private static async UniTask<bool> ConfirmUpdatePrompt(
            Dictionary<Mod, List<(string url, FilePath path)>> updates)
        {
            var list = string.Join("\n", updates
                .OrderBy(kvp => kvp.Key.DisplayName)
                .Select(kvp => kvp.Key.DisplayName));

            return await MenuGenerator.OpenConfirmationAsync(CloseMode.Current,
                () => $"Updates are available for the following mods:\n\n{list}\n\nDo you want to update all?",
                () => "Update All");
        }

        private static async UniTask<bool> ConfirmRetryPrompt(IEnumerable<Mod> failed)
        {
            var list = string.Join("\n", failed.Select(m => m.DisplayName));
            return await MenuGenerator.OpenConfirmationAsync(CloseMode.Current,
                () => $"The following mods failed to update:\n\n{list}\n\nRetry failed mods?",
                () => "Retry");
        }

        private static async UniTask ShowFailedModsPrompt(Dictionary<Mod, List<string>> failedMods)
        {
            var msg = string.Join("\n\n", failedMods.OrderBy(m => m.Key.DisplayName)
                .Select(kvp =>
                {
                    var files = string.Join("\n  - ", kvp.Value.OrderBy(f => f));
                    return $"{kvp.Key.DisplayName}\n  - {files}";
                }));

            await MenuGenerator.OpenConfirmationAsync(CloseMode.Current,
                () => $"The following mods still failed to update:\n\n{msg}",
                () => "OK");
        }
    }

    /// <summary>
    ///     Implement this interface on main mod class if you want it to be updated at game start
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public interface IUpdatable
    {
        /// <summary>
        ///     Returns dictionary of files that should be updated
        ///     string is web link, FilePath is path where file will be downloaded
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, FilePath> UpdatableFiles { get; }
    }
}