using Mono.Cecil;
using System.Linq;
using System.Collections.Generic;
using BepInEx.Logging;
using Mono.Cecil.Cil;

namespace ExtMap
{
    internal class PrePatchers
    {
        // List of assemblies to patch
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Game.dll" };

        public static ManualLogSource logSource;
        public static void Initialize()
        {
            logSource = Logger.CreateLogSource("ExtMapLog");
        }

        // Patches the assemblies
        public static void Patch(AssemblyDefinition assembly)
        {
            // Patcher code here
            var assemblyNameWithDll = $"{assembly.Name}.dll";

            logSource.LogInfo($"Received assembly {assemblyNameWithDll} for patching");

            ModuleDefinition module = assembly.MainModule;
            //foreach (TypeDefinition type in module.Types)
            //{
                //if (!type.IsAbstract)
                //    continue;
                //Console.WriteLine(type.FullName);
                //logSource.LogInfo($"Received assembly module {type.FullName} for patching");
            //}

            ///TerrainSystem;
            TypeDefinition terrainSystem = module.GetType("Game.Simulation","TerrainSystem");
            logSource.LogInfo($"target class {terrainSystem} for patching");

            //var terrainsys_cctor = terrainSystem.Methods.Single(m => m.IsConstructor);

            // Using Mono.Cecil to manipulate an existing static constructor
            
            MethodDefinition terrainsys_cctor = terrainSystem.Methods.FirstOrDefault(m => m.Name == ".cctor");

            if (terrainsys_cctor != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in terrainsys_cctor.Body.Instructions)
                {
                    if(ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 14336f)
                    {
                        ins.Operand = 57344f;
                    }
                }
                // Add new instructions or logic as needed
            }            
            

            ///WaterSystem;
            var waterSystem = module.GetType("Game.Simulation","WaterSystem");
            logSource.LogInfo($"target class {waterSystem} for patching");

            
            MethodDefinition watersys_cctor = waterSystem.Methods.FirstOrDefault(m => m.Name == ".cctor");

            if (watersys_cctor != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in watersys_cctor.Body.Instructions)
                {
                    //water mapsize;
                    if (ins.OpCode.Name == "ldc.i4" && (int)ins.Operand == 14336)
                    {
                        ins.Operand = 57344;
                    }
                    //water cellsize;
                    if (ins.OpCode.Name == "ldc.r4" && (float)ins.Operand == 7f)
                    {
                        ins.Operand = 28f;
                    }
                }
                // Add new instructions or logic as needed
            }

            logSource.LogInfo($"target field {watersys_cctor} for patching");

            /*
            MethodDefinition watersys_simgpu = waterSystem.Methods.FirstOrDefault(n => n.Name == ".Simulate");
            if (watersys_simgpu != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in watersys_simgpu.Body.Instructions)
                {
                    //lower water speed;
                    if (ins.OpCode.Name == "ldc.i4.4" )
                    {
                        ins.OpCode = OpCodes.Ldc_I4_8;
                    }
                    if (ins.OpCode.Name == "div")
                    {
                        ins.OpCode = OpCodes.Mul;
                    }

                }
                // Add new instructions or logic as needed
            }*/

            ///CellMapSystem;
            ///not sure effect;
            ///
            /*
            TypeDefinition cellmapSystem = module.GetType("Game.Simulation", "CellMapSystem`1");
            logSource.LogInfo($"target class {cellmapSystem} for patching");

            MethodDefinition cellmapsys_cctor = cellmapSystem.Methods.FirstOrDefault(m => m.Name == ".cctor");

            if (cellmapsys_cctor != null)
            {
                // Modify the content of the static constructor
                foreach (Instruction ins in cellmapsys_cctor.Body.Instructions)
                {
                    if (ins.OpCode.Name == "ldc.i4" && (float)ins.Operand == 14336)
                    {
                        ins.Operand = 57344;
                    }
                }
                // Add new instructions or logic as needed
            }*/

        }//patch method;

    }//patcher class;

}//namespace;


