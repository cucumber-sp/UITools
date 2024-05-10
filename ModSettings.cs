using System;
using JetBrains.Annotations;
using SFS.IO;
using SFS.Parsers.Json;

namespace UITools
{
    /// <summary>
    ///     Abstract class that let you easily create your mod configuration
    /// </summary>
    /// <typeparam name="T">Data type which will be stored in config file</typeparam>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public abstract class ModSettings<T> where T : new()
    {
        /// <summary>
        ///     Static variable for getting settings
        /// </summary>
        public static T settings;

        /// <summary>
        ///     Getting settings file path
        /// </summary>
        protected abstract FilePath SettingsFile { get; }

        void Load()
        {
            settings = SettingsFile.FileExists() ? JsonWrapper.FromJson<T>(SettingsFile.ReadText()) : new T();
            settings ??= new T();
        }

        void Save()
        {
            SettingsFile.WriteText(JsonWrapper.ToJson(settings, true));
        }


        /// <summary>
        ///     You should call this function after creating an instance of class
        /// </summary>
        public void Initialize()
        {
            Load();
            Save();
            RegisterOnVariableChange(Save);
        }

        /// <summary>
        ///     Allow you to subscribe save event to config variables change
        /// </summary>
        /// <param name="onChange">Action that you should subscribe to data variables onChange</param>
        protected abstract void RegisterOnVariableChange(Action onChange);
    }
}