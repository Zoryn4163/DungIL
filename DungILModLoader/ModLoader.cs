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
        public static int ModsLoaded { get; internal set; }
        public static ModBase[] Mods { get; internal set; }
        public static List<ModBase> ModsToRemove { get; internal set; }

        public static void Initialize()
        {
            string lp = Path.Combine(ExeDir, "DungIL_Log.latest.txt");
            Log.Initialize(lp);

            Log.Out("DungILModLoader Initialized");
            Log.Out("Executing in: " + ExeDir);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Log.Out("Checking for mods in: " + ModDir);

            var modIdx = IndexMods();

            Log.Out($"Found {modIdx.Length} mods.");

            Mods = LoadMods(modIdx);

            Log.Out($"Loaded {ModsLoaded} mods.");

            if (ModsLoaded == 0)
                return;
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
