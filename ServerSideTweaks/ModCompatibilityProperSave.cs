using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ServerSideTweaks
{
    // ProperSave restarts a run when it loads a save, so state that lasts the whole run goes into its save files
    public static class ModCompatibilityProperSave
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");
                }
                return (bool)_enabled;
            }
        }

        // ProperSave saves at the start of each stage; load gets the saved value once a save has loaded
        public static void Register(string key, Func<object> save, Action<object> load)
        {
            if (!enabled)
            {
                return;
            }
            try
            {
                RegisterWithProperSave(key, save, load);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"Could not register {key} with ProperSave - {exception.Message}");
            }
        }

        // Kept apart, so a changed ProperSave fails inside the try block above. Only method bodies name ProperSave's types,
        // never a signature: the game looks through the members of the mod's types for attributes at its start, and with
        // ProperSave missing, a member whose signature named its types would have the runtime search for it on disk each time.
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void RegisterWithProperSave(string key, Func<object> save, Action<object> load)
        {
            var handler = new SaveDataHandler { key = key, save = save, load = load };
            ProperSave.SaveFile.OnGatherSaveData += handler.GatherSaveData;
            // the handler takes the save file as an object, which a delegate for the save file binds to
            ProperSave.Loading.OnLoadingEnded += (Action<ProperSave.SaveFile>)Delegate.CreateDelegate(typeof(Action<ProperSave.SaveFile>), handler,
                typeof(SaveDataHandler).GetMethod(nameof(SaveDataHandler.OnLoadingEnded)));
        }

        private class SaveDataHandler
        {
            public string key;
            public Func<object> save;
            public Action<object> load;

            public void GatherSaveData(Dictionary<string, object> data)
            {
                data[key] = save();
            }

            public void OnLoadingEnded(object saveFile)
            {
                if (saveFile == null)
                {
                    return;
                }
                object value;
                try
                {
                    value = GetModdedData(saveFile);
                }
                catch (KeyNotFoundException)
                {
                    // saved without this data, e.g. by an older version of this mod
                    return;
                }
                load(value);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private object GetModdedData(object saveFile)
            {
                return ((ProperSave.SaveFile)saveFile).GetModdedData<object>(key);
            }
        }
    }
}
