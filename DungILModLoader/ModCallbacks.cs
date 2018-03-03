using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DungILModLoader
{
    public static class ModCallbacks
    {
        public static void Initialize()
        {
            Log.Out("Calling mod Init functions...");
            foreach (var mod in ModLoader.Mods)
            {
                try
                {
                    Log.Out("Init for: " + mod.Manifest.InternalName);
                    mod.Init();
                }
                catch (Exception ex)
                {
                    Log.Out($"[{mod.Manifest.InternalName}] encountered an error in its OnGameLoaded function." + ex);
                }
            }
        }

        private static void DoUpdate(string m)
        {
            if (ModLoader.Mods == null)
                ModLoader.Mods = new ModBase[0];

            if (ModLoader.ModsToRemove == null)
                ModLoader.ModsToRemove = new List<ModBase>();

            if (ModLoader.ModsToRemove.Any())
            {
                List<ModBase> mods = ModLoader.Mods.ToList();
                foreach (var mod in ModLoader.ModsToRemove)
                {
                    if (mods.Remove(mod))
                    {
                        Log.Out($"Removed [{mod.Manifest.InternalName}] from the update pool.");
                    }
                }
                ModLoader.Mods = mods.ToArray();
            }

            foreach (var mod in ModLoader.Mods)
            {
                try
                {
                    switch (m)
                    {
                        case "screen":
                            //mod.OnScreenUpdate();
                            break;
                        case "game":
                            //mod.OnGameUpdate();
                            break;
                        case "level":
                            //mod.OnLevelUpdate();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Out($"[{mod.Manifest.InternalName}] encountered an error in its Update function." + ex);
                    ModLoader.ModsToRemove.Add(mod);
                }
            }
        }
    }
}
