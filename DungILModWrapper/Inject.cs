using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungILModLoader;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Newtonsoft.Json;

namespace DungILModWrapper
{
    public class Inject
    {
        public AssemblyDefinition InjAssembly = null;
        public AssemblyDefinition TargAssembly = null;

        public void ProcessIlHooks()
        {
            Console.WriteLine("Performing IL Weaving!");
            PublicizeTypes();

            var idata = ReadInjectData();
            if (idata != null)
                WeaveInjectData(idata);
        }

        private void PublicizeTypes()
        {
            Console.WriteLine("Publicizing all defined types...");
            foreach (var t in TargAssembly.MainModule.Types)
            {
                if (!t.IsPublic)
                    t.IsPublic = true;
            }
        }

        private InjectData[] ReadInjectData()
        {
            Console.WriteLine("Checking for inject data json...");
            string execLoc = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            string dataLoc = Path.Combine(execLoc, "inject_data.json");
            FileInfo di = new FileInfo(dataLoc);
            if (di.Exists)
            {
                Console.WriteLine("Reading inject data at: " + dataLoc);
                string data = File.ReadAllText(di.FullName);
                if (string.IsNullOrEmpty(data))
                {
                    Console.WriteLine("Data not present within inject data at: " + dataLoc);
                    return null;
                }
                InjectData[] idata = JsonConvert.DeserializeObject<InjectData[]>(data);
                File.WriteAllText(dataLoc, JsonConvert.SerializeObject(idata, Formatting.Indented));
                if (idata.Any())
                    return idata;
            }
            else
            {
                Console.WriteLine("Failed to locate inject data at: " + dataLoc);
                try
                {
                    InjectData[] id = new InjectData[] {new InjectData()};
                    var fs = File.Create(dataLoc);
                    byte[] dat = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(id, Formatting.Indented));
                    fs.Write(dat, 0, dat.Length);
                    fs.Close();
                }
                catch
                {
                    //eh
                }
            }

            return null;
        }

        private void WeaveInjectData(InjectData[] idata)
        {
            Console.WriteLine("Weaving IL from inject data!");
            foreach (var v in idata)
            {
                if (v.Enabled == false)
                {
                    Console.WriteLine($"Skipping: {v.OriginalModuleFullType}.{v.OriginalModuleMethodName} to {v.NewModuleFullType}.{v.NewModuleMethodName} (Ret: {v.MethodCausesReturn} | O: {v.MethodOverridesReturnValue}) (Enabled is false)");
                    continue;
                }
                Console.WriteLine($"Weave: {v.OriginalModuleFullType}.{v.OriginalModuleMethodName} to {v.NewModuleFullType}.{v.NewModuleMethodName} (Ret: {v.MethodCausesReturn} | O: {v.MethodOverridesReturnValue})");
                var newClassType = InjAssembly.MainModule.GetType(v.NewModuleFullType);
                var newMethod = newClassType.Methods.Single(x => x.Name == v.NewModuleMethodName);
                var importedMethod = TargAssembly.MainModule.ImportReference(newMethod);

                var originalClassType = TargAssembly.MainModule.GetType(v.OriginalModuleFullType);
                var originalMethod = originalClassType.Methods.Single(x => x.Name == v.OriginalModuleMethodName);

                var il = originalMethod.Body.GetILProcessor();

                var fi = il.Body.Instructions.First();
                var li = il.Body.Instructions.Last();
                var ai = fi;
                if (v.PlaceBeforeAbsoluteInstruction)
                    ai = il.Body.Instructions.ElementAt(v.AbsoluteIntructionIndex);

                if (v.MethodCausesReturn)
                {
                    if (v.MethodOverridesReturnValue)
                    {
                        if (importedMethod.ReturnType != originalMethod.ReturnType)
                        {
                            if (v.MethodReturnValueIgnoresTypeConstraint)
                            {
                                Console.WriteLine($"WARNING: New Method return type does not match Original Method return type (New: {importedMethod.ReturnType} | Orig: {originalMethod.ReturnType})");
                            }
                            else
                            {
                                Console.WriteLine($"ERROR: New Method return type does not match Original Method return type (New: {importedMethod.ReturnType} | Orig: {originalMethod.ReturnType})");
                                continue;
                            }
                        }

                        if (v.PlaceBeforeFirstInstruction)
                        {
                            il.InsertBefore(fi, il.Create(OpCodes.Nop));
                            il.InsertBefore(fi, il.Create(OpCodes.Call, importedMethod));
                            il.InsertBefore(fi, il.Create(OpCodes.Stloc_0));
                            il.InsertBefore(fi, il.Create(OpCodes.Ldloc_0));
                            il.InsertBefore(fi, il.Create(OpCodes.Ret));
                        }
                        if (v.PlaceBeforeLastInstruction)
                        {
                            il.InsertBefore(li, il.Create(OpCodes.Nop));
                            il.InsertBefore(li, il.Create(OpCodes.Call, importedMethod));
                            il.InsertBefore(li, il.Create(OpCodes.Stloc_0));
                            il.InsertBefore(li, il.Create(OpCodes.Ldloc_0));
                            il.InsertBefore(li, il.Create(OpCodes.Ret));
                        }
                        if (v.PlaceBeforeAbsoluteInstruction)
                        {
                            il.InsertBefore(ai, il.Create(OpCodes.Nop));
                            il.InsertBefore(ai, il.Create(OpCodes.Call, importedMethod));
                            il.InsertBefore(ai, il.Create(OpCodes.Stloc_0));
                            il.InsertBefore(ai, il.Create(OpCodes.Ldloc_0));
                            il.InsertBefore(ai, il.Create(OpCodes.Ret));
                        }
                    }
                    else
                    {
                        if (v.PlaceBeforeFirstInstruction)
                        {
                            il.InsertBefore(fi, il.Create(OpCodes.Call, importedMethod));
                            il.InsertBefore(fi, il.Create(OpCodes.Ret));
                        }
                        if (v.PlaceBeforeLastInstruction)
                        {
                            il.InsertBefore(li, il.Create(OpCodes.Call, importedMethod));
                            il.InsertBefore(li, il.Create(OpCodes.Ret));
                        }
                        if (v.PlaceBeforeAbsoluteInstruction)
                        {
                            il.InsertBefore(ai, il.Create(OpCodes.Call, importedMethod));
                            il.InsertBefore(ai, il.Create(OpCodes.Ret));
                        }
                    }
                }
                else
                {
                    if (v.PlaceBeforeFirstInstruction)
                    {
                        il.InsertBefore(fi, il.Create(OpCodes.Call, importedMethod));
                    }
                    if (v.PlaceBeforeLastInstruction)
                    {
                        il.InsertBefore(li, il.Create(OpCodes.Call, importedMethod));
                    }
                    if (v.PlaceBeforeAbsoluteInstruction)
                    {
                        il.InsertBefore(ai, il.Create(OpCodes.Call, importedMethod));
                    }
                }
            }
        }
    }
}
