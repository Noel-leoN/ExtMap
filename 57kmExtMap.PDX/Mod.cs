using System;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Modding;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using HarmonyLib;
using Unity.Entities;
//using ExtMap57km.Patches;
using ExtMap57km;
using Game.Serialization;
using Game.Audio;
using Game.Rendering;


namespace ExtMap57km
{
    public sealed class Mod : IMod
    {
        public const string ModName = "ExtMap57km";
        public const string ModNameCN = "16倍扩展地图57km";

        public static Mod Instance { get; private set; }

        public static ExecutableAsset ModAsset { get; private set; }

        public static readonly string harmonyID = ModName;

        // log init;
        public static ILog log = LogManager.GetLogger($"{ModName}").SetShowsErrorsInUI(false);

        public static void Log(string text) => log.Info(text);

        //public static void LogIf(string text)
        //{
         //   if (Setting.Logging) log.Info(text);
        //}

        //public static Setting Setting { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;

            // Log;
            log.Info(nameof(OnLoad));
#if DEBUG
            log.Info("setting logging level to Debug");
            log.effectivenessLevel = Level.Debug;
#endif
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"{asset.name} v{asset.version} mod asset at {asset.path}");
            ModAsset = asset;

            // harmony patches.
            var harmony = new Harmony(harmonyID);
            harmony.PatchAll(typeof(Mod).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods.Length);
            foreach (var patchedMethod in patchedMethods)
            {
                log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.DeclaringType.Name}.{patchedMethod.Name}");
            }

            // UI settings; 
            //Setting = new Setting(this);
            //Setting.RegisterInOptionsUI();
            // UI locale;
            //GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Setting));
            //GameManager.instance.localizationManager.AddSource("zh-HANS", new LocaleCN(Setting));

            // Load UI saved setting;
            //AssetDatabase.global.LoadSettings(ModName, Setting, new Setting(this));
            //Setting.Contra = false;

            ///系统替换&重用；
            //Disable vanilla systmes & enable custom systems；

            ///地图系统；
            //MapTileSystem;
            ///Postfix mode;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Areas.MapTileSystem>().Enabled = false;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.MapTileSystem>();
            updateSystem?.UpdateAfter<PostDeserialize<Systems.MapTileSystem>, PostDeserialize<Game.Areas.MapTileSystem>>(SystemUpdatePhase.Deserialize);

            //AreaToolSystem;
            ///Postfix mode;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Tools.AreaToolSystem>().Enabled = false;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.AreaToolSystem>();
            updateSystem?.UpdateAfter<Systems.AreaToolSystem, Game.Tools.AreaToolSystem>(SystemUpdatePhase.ToolUpdate);

            //TerrainSystem;
            ///disabled if using preloader patcher;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().Enabled = false;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.TerrainSystem>();
            //updateSystem?.UpdateAfter<Systems.TerrainSystem,Game.Simulation.TerrainSystem>(SystemUpdatePhase.ModificationEnd);

            //Mod system & reuse;
            //ExtMap57km.Patches.MapSysPatch.MapSysInit(updateSystem);


            ///水系统；
            ///disabled if using preloader patcher;
            //WaterSystem;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.WaterSystem>().Enabled = false;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.WaterSystem>();
            //updateSystem?.UpdateAfter<Systems.WaterSystem, WaterSystem>(SystemUpdatePhase.PostSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Systems.FloodCheckSystem>().Enabled = false;

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Systems.WaterDangerSystem>().Enabled = false;

            //orld.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Systems.WaterLevelChangeSystem>().Enabled = false;

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Systems.WeatherAudioSystem>().Enabled = false;

            //Mod system & reuse;
            //ExtMap57km.Patches.WaterSysPatch.WaterSysInit(updateSystem);


            //Water System;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.WaterSystem>().Enabled = false;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.WaterSystem>();
            //updateSystem.UpdateAfter<Systems.WaterSystem, WaterSystem>(SystemUpdatePhase.PostSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.FloodCheckSystem>();
            //updateSystem.UpdateAfter<Systems.FloodCheckSystem, FloodCheckSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.WaterDangerSystem>();
            //updateSystem.UpdateAfter<Systems.WaterDangerSystem, WaterDangerSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.WaterLevelChangeSystem>();
            //updateSystem.UpdateAfter<Systems.WaterLevelChangeSystem, WaterLevelChangeSystem>(SystemUpdatePhase.GameSimulation);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.WeatherAudioSystem>();
            //updateSystem.UpdateAfter<Systems.WeatherAudioSystem, WeatherAudioSystem>(SystemUpdatePhase.Modification2);

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.WaterRenderSystem>();
            //updateSystem.UpdateAfter<Systems.WaterRenderSystem,WaterRenderSystem>(SystemUpdatePhase.PreCulling);


            //CellMapSystem;
            ///"Postfix" mode;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.AirPollutionSystem>();
            updateSystem.UpdateAfter<Systems.AirPollutionSystem,AirPollutionSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.BuildingPollutionAddSystem>();
            updateSystem.UpdateAfter<Systems.BuildingPollutionAddSystem,BuildingPollutionAddSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.GroundPollutionSystem>();
            updateSystem.UpdateAfter<Systems.GroundPollutionSystem, GroundPollutionSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.GroundWaterSystem>();
            updateSystem.UpdateAfter<Systems.GroundWaterSystem, GroundWaterSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.NaturalResourceSystem>();
            updateSystem.UpdateAfter<Systems.NaturalResourceSystem, NaturalResourceSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.NoisePollutionSystem>();
            updateSystem.UpdateAfter<Systems.NoisePollutionSystem, NoisePollutionSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.PopulationToGridSystem>();
            updateSystem.UpdateAfter<Systems.PopulationToGridSystem, PopulationToGridSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.SoilWaterSystem>();
            updateSystem.UpdateAfter<PostDeserialize<SoilWaterSystem>>(SystemUpdatePhase.Deserialize);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.TelecomCoverageSystem>();
            updateSystem.UpdateAfter<Systems.TelecomCoverageSystem, TelecomCoverageSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.TerrainAttractivenessSystem>();
            updateSystem.UpdateAfter<Systems.TerrainAttractivenessSystem, TerrainAttractivenessSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.TrafficAmbienceSystem>();
            updateSystem.UpdateAfter<Systems.TrafficAmbienceSystem, TrafficAmbienceSystem>(SystemUpdatePhase.GameSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.WindSimulationSystem>();
            updateSystem.UpdateAfter<Systems.WindSimulationSystem, WindSimulationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<Systems.WindSimulationSystem, WindSimulationSystem>(SystemUpdatePhase.EditorSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.WindSystem>();
            updateSystem.UpdateAfter<Systems.WindSystem, WindSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<Systems.WindSystem, WindSystem>(SystemUpdatePhase.EditorSimulation);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.ZoneAmbienceSystem>();
            updateSystem.UpdateAfter<Systems.ZoneAmbienceSystem, ZoneAmbienceSystem>(SystemUpdatePhase.GameSimulation);
            

            //Cell ref Sys;
            ///Prefix mode;//单一系统引用，采取替换模式；
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Tools.ApplyBrushesSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.ApplyBrushesSystem>();
            updateSystem.UpdateAt<Systems.ApplyBrushesSystem>(SystemUpdatePhase.ApplyTool);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.AudioGroupingSystem>();
            updateSystem.UpdateAt<Systems.AudioGroupingSystem>(SystemUpdatePhase.Modification2);

            //not sure which mode;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.CarNavigationSystem>();
            updateSystem.UpdateAt<Systems.CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<Systems.CarNavigationSystem.Actions, Systems.CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Systems.CarNavigationSystem>(SystemUpdatePhase.LoadSimulation);
            updateSystem.UpdateAfter<Systems.CarNavigationSystem.Actions, Systems.CarNavigationSystem>(SystemUpdatePhase.LoadSimulation);

            //not sure which mode;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.NetPollutionSystem>();
            updateSystem.UpdateAfter<Systems.NetPollutionSystem, NetPollutionSystem>(SystemUpdatePhase.GameSimulation);

            //not sure which mode;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Systems.SpawnableAmbienceSystem>();
            updateSystem.UpdateAfter<Systems.SpawnableAmbienceSystem,SpawnableAmbienceSystem>(SystemUpdatePhase.GameSimulation);
            

        }


        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            Instance = null;

            // un-Harmony;
            var harmony = new Harmony(harmonyID);
            harmony.UnpatchAll(harmonyID);

            // un-Setting;
            //if (Setting != null)
            //{
            //    Setting.UnregisterInOptionsUI();
            //    Setting = null;
            //}
        }
    }
}
