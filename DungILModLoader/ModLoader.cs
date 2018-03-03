using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace DungILModLoader
{
    public static class ModLoader
    {
        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        public static readonly string ExeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string ModDir = Path.Combine(ExeDir, "mods");
        public static int ModsLoaded { get; private set; }
        public static ModBase[] Mods { get; private set; }
        public static List<ModBase> ModsToRemove { get; private set; }

        //public static DXGame DxGame { get; private set; }

        public static void TestReeeeeeeeeeee()
        {
            string p = @"E:\test\" + Path.GetTempFileName().Split('\\').Last();
            Directory.CreateDirectory(p);
        }

        public static void Initialize()
        {
            string lp = Path.Combine(ExeDir, "DungIL_Log.latest.txt");
            Log.Initialize(lp);

            Log.Out("DungILModLoader Initialized");
            Log.Out("Executing in: " + ExeDir);

            //ZXGame.IsSteam = false;

            //ZXGame.Current.Entities

            
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            //throw new Exception();

            Log.Out("Checking for mods in: " + ModDir);

            var modIdx = IndexMods();

            Log.Out($"Found {modIdx.Length} mods.");

            Mods = LoadMods(modIdx);

            Log.Out($"Loaded {ModsLoaded} mods.");

            if (ModsLoaded == 0)
                return;

            Log.Out("Calling mod Init functions...");
            foreach (var mod in Mods)
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

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Log.Out("Process is exiting.");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Out("An unhandled exception has occured. Details are listed below.");
            Log.Out($"Sender of type [{sender.GetType()}]: {sender.ToString()}");
            Log.Out($"ExceptionObject of type [{e.ExceptionObject.GetType()}]: {e.ExceptionObject}");
            Log.Out($"Error is terminating: {e.IsTerminating}");
        }

        public static void OnGameLoaded()
        {
            Log.Out("Calling mod OnGameLoaded functions...");
            foreach (var mod in Mods)
            {
                try
                {
                    Log.Out("OnGameLoaded for: " + mod.Manifest.InternalName);
                    //mod.OnGameLoaded();
                }
                catch (Exception ex)
                {
                    Log.Out($"[{mod.Manifest.InternalName}] encountered an error in its OnGameLoaded function." + ex);
                }
            }
        }

        public static void OnScreenUpdate()
        {
            DoUpdate("screen");
        }

        public static void OnGameUpdate()
        {
            DoUpdate("game");
        }

        public static void OnLevelUpdate()
        {
            DoUpdate("level");
        }

        private static void DoUpdate(string m)
        {
            if (Mods == null)
                Mods = new ModBase[0];

            if (ModsToRemove == null)
                ModsToRemove = new List<ModBase>();

            if (ModsToRemove.Any())
            {
                List<ModBase> mods = Mods.ToList();
                foreach (var mod in ModsToRemove)
                {
                    if (mods.Remove(mod))
                    {
                        Log.Out($"Removed [{mod.Manifest.InternalName}] from the update pool.");
                    }
                }
                Mods = mods.ToArray();
            }

            foreach (var mod in Mods)
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
                catch (Exception ex )
                {
                    Log.Out($"[{mod.Manifest.InternalName}] encountered an error in its Update function." + ex);
                    ModsToRemove.Add(mod);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static ModManifest[] IndexMods()
        {
            if (!Directory.Exists(ModDir))
                Directory.CreateDirectory(ModDir);

            List<ModManifest> ret = new List<ModManifest>();

            string[] modDirs = Directory.GetDirectories(ModDir);
            foreach (var dir in modDirs)
            {
                string modManifestPath = Path.Combine(dir, "manifest.json");

                Log.Out("Checking for manifest: " + modManifestPath);

                FileInfo manifestInfo = new FileInfo(modManifestPath);
                if (!File.Exists(manifestInfo.FullName))
                    continue;

                Log.Out("Found manifest; parsing...");

                File.ReadAllText(manifestInfo.FullName);
                Log.Out("Read data, converting...");

                ModManifest manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(manifestInfo.FullName));
                if (manifest == null)
                    continue;

                Log.Out($"Parsed {manifest.Name} V{manifest.Version} by {manifest.Authour}");

                manifest.AssemblyPath = Path.Combine(Path.GetDirectoryName(manifestInfo.FullName), manifest.Assembly);

                if (!File.Exists(manifest.AssemblyPath))
                    continue;

                ret.Add(manifest);
            }

            return ret.OrderBy(x=>x.Assembly).ToArray();
        }

        public static ModBase[] LoadMods(ModManifest[] manifests)
        {
            List<ModBase> loadedMods = new List<ModBase>();
            foreach (var manifest in manifests)
            {
                var ass = Assembly.LoadFrom(manifest.AssemblyPath);
                
                if (ass.GetTypes().Any(x=>x.BaseType == typeof(ModBase)))
                {
                    Log.Out($"Loading mod: {manifest.Name} {manifest.Version} [{manifest.InternalName}]");
                    var tar = ass.GetTypes().First(x => x.BaseType == typeof(ModBase));
                    var m = (ModBase)ass.CreateInstance(tar.ToString());

                    if (m == null)
                    {
                        Log.Out("Failed to create instance of: " + tar.ToString());
                        continue;
                    }

                    m.Manifest = manifest;

                    loadedMods.Add(m);
                    ModsLoaded += 1;
                    Log.Out("Mod loaded successfully.");
                }
            }

            return loadedMods.ToArray();
        }
    }
}
