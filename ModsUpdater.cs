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
        // Shared HttpClient instance
        private static readonly HttpClient Http = new();

        // Tracks how many files were successfully updated
        private static int loadedFiles;

        // Entry point for running the update process detached from scene context
        public static void StartUpdate()
        {
            UpdateAll().Forget();
        }

        // Main update flow
        private static async UniTask UpdateAll()
        {
            try
            {
                // Check which files need updating (based on hash mismatch or failures)
                var updatesByMod = await GetFilesNeedingUpdate();
                if (updatesByMod.Count == 0) return;

                // Ask user to confirm update
                if (!await ConfirmUpdatePrompt(updatesByMod))
                    return;

                // Tracks failed mods and which files failed in each
                var failedMods = new Dictionary<Mod, List<string>>();

                // Attempt update per mod
                foreach (var entry in updatesByMod)
                {
                    Mod mod = entry.Key;
                    var files = entry.Value;

                    if (await TryUpdateMod(mod, files, failedMods)) continue;

                    // Ensure mod is listed in failedMods if any file fails
                    if (!failedMods.ContainsKey(mod))
                        failedMods[mod] = new List<string>();
                }

                // Retry failed mods if user agrees
                if (failedMods.Count > 0 && await ConfirmRetryPrompt(failedMods.Keys))
                {
                    foreach (Mod mod in failedMods.Keys.ToList())
                    {
                        var files = updatesByMod[mod];
                        if (await TryUpdateMod(mod, files, failedMods))
                            failedMods.Remove(mod);
                    }

                    // Show final list of failed files (if any)
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
                // If any files were updated, offer to restart
                if (loadedFiles > 0)
                    MenuGenerator.OpenConfirmation(CloseMode.Current,
                        () => "Updates successful. Restart so changes take effect?",
                        () => "Restart",
                        ApplicationUtility.Relaunch);
            }
        }

        // Returns a dictionary of mods -> files that need updating
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
                        // Check remote MD5 hash
                        var hashUrl =
                            $"https://files.cucumber-space.online/api/hashes/md5?file={Uri.EscapeDataString(url)}";
                        HttpResponseMessage resp = await Http.GetAsync(hashUrl);
                        if (!resp.IsSuccessStatusCode) throw new Exception("Hash request failed");
                
                        var local = path.FileExists() ? md5.ComputeHash(path.ReadBytes()) : Array.Empty<byte>();
                        var remoteHashBase64 = await resp.Content.ReadAsStringAsync();
                        var remote = Convert.FromBase64String(remoteHashBase64);
                        
                        // If hash mismatch, mark for update
                        if (!local.SequenceEqual(remote))
                        {
                            if (!result.ContainsKey(mod))
                                result[mod] = new List<(string, FilePath)>();
                            result[mod].Add((url, path));
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Debug.LogWarning($"[ModUpdater] Network error while checking hash for {url}: {ex.Message}");
                    }
                    catch (FormatException ex)
                    {
                        Debug.LogWarning($"[ModUpdater] Invalid base64 hash from server for {url}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[ModUpdater] Unexpected error while checking hash for {url}: {ex.Message}");
                    }
                }
            }

            return result;
        }

        // Attempts to update all files for a single mod atomically
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

            // If all downloads succeed, commit updates
            if (failedFiles.Count == 0)
            {
                foreach ((FilePath original, string tempPath) download in downloads)
                    File.Copy(download.tempPath, download.original, true);

                loadedFiles += downloads.Count;

                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);

                return true;
            }

            // Record failed files
            failedMods[mod] = failedFiles;

            // Clean up downloaded files if not committed
            foreach ((FilePath original, string tempPath) download in downloads.Where(download =>
                         File.Exists(download.tempPath)))
                File.Delete(download.tempPath);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);

            return false;
        }

        // Download a file and write to disk
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

        // Confirmation prompt before updates begin
        private static async UniTask<bool> ConfirmUpdatePrompt(
            Dictionary<Mod, List<(string url, FilePath path)>> updates)
        {
            var list = string.Join("\n", updates
                .OrderBy(kvp => kvp.Key.DisplayName)
                .Select(kvp => kvp.Key.DisplayName));

            return await MenuGenerator.OpenConfirmationAsync(CloseMode.Current,
                () => $"Updates are available for the following mods:\n\n{list}\n\nInstall now?",
                () => "Update All");
        }

        // Retry prompt for mods that failed to update
        private static async UniTask<bool> ConfirmRetryPrompt(IEnumerable<Mod> failed)
        {
            var list = string.Join("\n", failed.Select(m => m.DisplayName));
            return await MenuGenerator.OpenConfirmationAsync(CloseMode.Current,
                () => $"The following mods failed to update:\n\n{list}\n\nRetry failed mods?",
                () => "Retry");
        }

        // Show which files failed after retry
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