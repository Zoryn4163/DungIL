using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using DungILModLoader;
using Mono.Cecil;
using DungILModWrapper;

namespace DungIL
{
    public static class Program
    {
        public static string AssemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string AssemblyName = @"Assembly-CSharp.dll";
        public static readonly string BackupAssemblyName = @"Assembly-CSharp.bak.dll";
        public static readonly string CacheAssemblyName = @"Assembly-CSharp.cache.dll";

        public static string AssemblyPath => Path.Combine(AssemblyFolder, AssemblyName);
        public static string BackupAssemblyPath => Path.Combine(AssemblyFolder, BackupAssemblyName);
        public static string CacheAssemblyPath => Path.Combine(AssemblyFolder, CacheAssemblyName);

        public static readonly string ExecuteWhenFinished = AssemblyName;

        public static void Main(string[] args)
        {
            Console.WriteLine("Operating in: " + AssemblyFolder);
            
            var assemblyInfo = new FileInfo(AssemblyPath);
            var backupInfo = new FileInfo(BackupAssemblyPath);
            var cacheInfo = new FileInfo(CacheAssemblyPath);

            if (cacheInfo.Exists)
            {
                cacheInfo.Delete();
            }

            AssemblyInfo binfo = new AssemblyInfo();
            AssemblyInfo oinfo = new AssemblyInfo();

            if (backupInfo.Exists)
                binfo = GetAssemblyInfo(backupInfo.FullName);

            if (assemblyInfo.Exists)
                oinfo = GetAssemblyInfo(assemblyInfo.FullName);

            if (backupInfo.Exists && !assemblyInfo.Exists)
            {
                if (binfo.DisplayName == "Assembly-CSharp")
                {
                    Console.WriteLine("Only backup exists. Copying to original...");
                    backupInfo.CopyTo(assemblyInfo.FullName);
                    Console.WriteLine("Copying backup assembly to cache...");
                    backupInfo.CopyTo(cacheInfo.FullName, false);
                }
                else
                {
                    ShowNotOriginalMsg(binfo);
                    return;
                }
            }
            else if (assemblyInfo.Exists && !backupInfo.Exists)
            {
                if (oinfo.DisplayName == "Assembly-CSharp")
                {
                    Console.WriteLine("Only original exists. Copying to backup...");
                    assemblyInfo.CopyTo(backupInfo.FullName);
                    Console.WriteLine("Copying original assembly to cache...");
                    assemblyInfo.CopyTo(cacheInfo.FullName, false);
                }
                else
                {
                    ShowNotOriginalMsg(oinfo);
                    return;
                }
            }
            else if (backupInfo.Exists && assemblyInfo.Exists)
            {
                Console.WriteLine("Backup and original both exist. Comparing...");

                bool biso = binfo.DisplayName == "Assembly-CSharp";
                bool oiso = oinfo.DisplayName == "Assembly-CSharp";

                if (!biso && !oiso)
                {
                    ShowNotOriginalMsg(oinfo);
                    return;
                }

                if (!biso)
                {
                    Console.WriteLine("Backup assembly is not original. Removing...");
                    backupInfo.Delete();
                }

                if (!oiso)
                {
                    Console.WriteLine("Original assembly is not original. Replacing with backup...");
                    assemblyInfo.Delete();
                    backupInfo.CopyTo(assemblyInfo.FullName);
                    Console.WriteLine("Copying backup assembly to cache...");
                    backupInfo.CopyTo(cacheInfo.FullName, false);
                }

                if (biso && oiso)
                {
                    Console.WriteLine("Both files are original. Comparing compile times...");
                    if (oinfo.CompileTime > binfo.CompileTime)
                    {
                        Console.WriteLine("Original game assembly more updated than backup, will replace backup with: " + assemblyInfo.FullName);
                        Console.Write("Press Y to confirm > ");
                        var k = Console.ReadKey();
                        if (k.Key != ConsoleKey.Y)
                        {
                            Console.WriteLine("\n'Y' was not pressed. The program will now exit.");
                            Environment.Exit(0);
                        }

                        Console.WriteLine("\nRemoving backup assembly...");
                        backupInfo.Delete();
                        Console.WriteLine("Copying original assembly to cache...");
                        assemblyInfo.CopyTo(cacheInfo.FullName, false);

                        Thread.Sleep(100);
                    }
                    else
                    {
                        Console.WriteLine("Backup game assembly more updated than original. Use backup or original?");

                        while (true)
                        {
                            Console.Write("Press [b / o] to select > ");
                            var k = Console.ReadKey();
                            if (k.Key != ConsoleKey.B && k.Key != ConsoleKey.O)
                            {
                                Console.WriteLine("\n'b' or 'o' must be pressed.");
                                continue;
                            }

                            if (k.Key == ConsoleKey.B)
                            {
                                Console.WriteLine("\nRemoving original assembly...");
                                assemblyInfo.Delete();
                                Console.WriteLine("Copying backup assembly to original...");
                                backupInfo.CopyTo(assemblyInfo.FullName, false);
                                Console.WriteLine("Copying backup assembly to cache...");
                                backupInfo.CopyTo(cacheInfo.FullName, false);
                                break;
                            }
                            else if (k.Key == ConsoleKey.O)
                            {
                                Console.WriteLine("\nRemoving backup assembly...");
                                backupInfo.Delete();
                                Console.WriteLine("Copying original assembly to backup...");
                                assemblyInfo.CopyTo(backupInfo.FullName, false);
                                Console.WriteLine("Copying original assembly to cache...");
                                assemblyInfo.CopyTo(cacheInfo.FullName, false);
                                break;
                            }
                        }
                    }
                }
                
            }

            if (!File.Exists(cacheInfo.FullName))
            {
                Console.WriteLine("Failed to find cache file: " + cacheInfo.FullName);
                return;
            }

            Console.WriteLine("Testing cached assembly for original assembly definition...");
            var cai = GetAssemblyInfo(cacheInfo.FullName);
            if (!ExeIsOriginal(cai))
            {
                ShowNotOriginalMsg(cai);
            }

            Console.WriteLine("Reading cached assembly for injection...");
            var inj = new Inject();
            inj.TargAssembly = AssemblyDefinition.ReadAssembly(cacheInfo.FullName);
            inj.InjAssembly = AssemblyDefinition.ReadAssembly(typeof(ModCallbacks).Assembly.Location);

            Console.WriteLine("Processing injections...");
            inj.ProcessIlHooks();

            Console.WriteLine("Appending references...");
            inj.TargAssembly.MainModule.AssemblyReferences.Add(AssemblyNameReference.Parse(inj.InjAssembly.FullName));

            Console.WriteLine("Rewriting assembly definition to modified definition...");
            inj.TargAssembly.Name.Name = inj.TargAssembly.Name.Name + "_DungIL";

            Console.WriteLine("Writing out modified assembly to original assembly...");
            inj.TargAssembly.MainModule.Write(assemblyInfo.FullName);

            if (true)
                return;

            Console.WriteLine("Finished, executing: " + ExecuteWhenFinished);
            ProcessStartInfo psi = new ProcessStartInfo("cmd");
            psi.WorkingDirectory = AssemblyFolder;
            psi.Arguments = "/c \"" + ExecuteWhenFinished + "\"";
            Process.Start(psi);
        }

        public static AssemblyInfo GetAssemblyInfo(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                Console.WriteLine("Error: File not found: " + assemblyPath);
                Console.WriteLine("The program cannot continue. Press any key to throw the exception.");
                Console.ReadKey();
                throw new FileNotFoundException(assemblyPath);
            }

            AppDomain d = AppDomain.CreateDomain("GetVersion", AppDomain.CurrentDomain.Evidence, AppDomain.CurrentDomain.SetupInformation);

            AssemblyInfo ret = new AssemblyInfo();

            try
            {
                Type t = typeof(Proxy);
                var v = (Proxy) d.CreateInstanceAndUnwrap(t.Assembly.FullName, t.FullName);
                ret = v.GetAssemblyInfo(assemblyPath);

                if (ret.Equals(AssemblyInfo.Default))
                {
                    Console.WriteLine("Failed to retrieve assembly info for: " + assemblyPath);
                    Environment.Exit(0);
                    return ret;
                }

                Console.WriteLine($"Retreived assembly info for [{assemblyPath}]: " + ret.FullName);
            }
            finally
            {
                AppDomain.Unload(d);
            }

            return ret;
        }

        public static bool ExeIsOriginal(AssemblyInfo ai)
        {
            return ai.DisplayName == "Assembly-CSharp";
        }

        public static void ShowNotOriginalMsg(AssemblyInfo ai)
        {
            Console.WriteLine("Original game assembly could not be found. Please verify the integrity of your game files and try again.");

            if (!ai.Equals(new AssemblyInfo()))
                Console.WriteLine($"Expected: 'Assembly-CSharp' | Got: '{ai.DisplayName}'");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }

    public class Proxy : MarshalByRefObject
    {
        public AssemblyInfo GetAssemblyInfo(string assemblyPath)
        {
            try
            {
                Console.WriteLine("Loading assembly: " + assemblyPath);

                FileInfo fi = new FileInfo(assemblyPath);
                if (!fi.Exists)
                {
                    Console.WriteLine("Could not find assembly: " + fi.FullName);
                    return new AssemblyInfo();
                }

                var a = Assembly.LoadFrom(assemblyPath);
                //Console.WriteLine("Assembly is: " + a.FullName);

                var ret = new AssemblyInfo();
                ret.FullName = a.FullName;
                ret.Version = a.GetName().Version;
                ret.DisplayName = a.GetName().Name;
                ret.Location = a.Location;
                ret.CompileTime = fi.LastWriteTime;
                return ret;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new AssemblyInfo();
            }
        }
    }

    [Serializable]
    public struct AssemblyInfo
    {
        public string DisplayName { get; set; }
        public string FullName { get; set; }
        public string Location { get; set; }
        public Version Version { get; set; }

        public DateTime CompileTime { get; set; }

        public override string ToString()
        {
            return $"{FullName} [{Location}]";
        }

        public static AssemblyInfo Default = new AssemblyInfo();
    }
}