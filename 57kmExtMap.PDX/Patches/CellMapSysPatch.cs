using Game.Simulation;
using HarmonyLib;
using Unity.Collections;
using Colossal.Mathematics;
using Colossal.Collections;
using Unity.Mathematics;
using Game.Prefabs;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Companies;
using Game.Zones;

namespace ExtMap57km.Patches
{

    public static class CellMapSystemRe
    {
        public static readonly int kMapSize = 57344;       
        public static float3 GetCellCenter(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = kMapSize / textureSize;
            return new float3(-0.5f * kMapSize + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * kMapSize + ((float)num2 + 0.5f) * (float)num3);
        }

        public static Bounds3 GetCellBounds(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = kMapSize / textureSize;
            return new Bounds3(new float3(-0.5f * kMapSize + (float)(num * num3), -100000f, -0.5f * kMapSize + (float)(num2 * num3)), new float3(-0.5f * kMapSize + ((float)num + 1f) * (float)num3, 100000f, -0.5f * kMapSize + ((float)num2 + 1f) * (float)num3));
        }

        public static float3 GetCellCenter(int2 cell, int textureSize)
        {
            int num = kMapSize / textureSize;
            return new float3(-0.5f * kMapSize + ((float)cell.x + 0.5f) * (float)num, 0f, -0.5f * kMapSize + ((float)cell.y + 0.5f) * (float)num);
        }

        public static float2 GetCellCoords(float3 position, int mapSize, int textureSize)
        {
            return (0.5f + position.xz / mapSize) * textureSize;
        }

        public static int2 GetCell(float3 position, int mapSize, int textureSize)
        {
            return (int2)math.floor(GetCellCoords(position, mapSize, textureSize));
        }

    }//cellmapsystem_re class;


    [HarmonyPatch]
    internal static class AirPollutionSystemPatch
    {
        [HarmonyPatch(typeof(AirPollutionSystem), nameof(AirPollutionSystem.GetPollution))]
        [HarmonyPostfix]
        public static void GetPollution(ref AirPollution __result,float3 position, NativeArray<AirPollution> pollutionMap)
        {
            AirPollution result = default(AirPollution);
            float num = 57344 / (float)AirPollutionSystem.kTextureSize;
            int2 cell = CellMapSystemRe.GetCell(position - new float3(num / 2f, 0f, num / 2f), 57344, AirPollutionSystem.kTextureSize);
            float2 @float = CellMapSystemRe.GetCellCoords(position, 57344, AirPollutionSystem.kTextureSize) - new float2(0.5f, 0.5f);
            cell = math.clamp(cell, 0, AirPollutionSystem.kTextureSize - 2);
            short pollution = pollutionMap[cell.x + AirPollutionSystem.kTextureSize * cell.y].m_Pollution;
            short pollution2 = pollutionMap[cell.x + 1 + AirPollutionSystem.kTextureSize * cell.y].m_Pollution;
            short pollution3 = pollutionMap[cell.x + AirPollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
            short pollution4 = pollutionMap[cell.x + 1 + AirPollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
            result.m_Pollution = (short)math.round(math.lerp(math.lerp(pollution, pollution2, @float.x - (float)cell.x), math.lerp(pollution3, pollution4, @float.x - (float)cell.x), @float.y - (float)cell.y));
            __result = result;
        }


        [HarmonyPatch(typeof(AirPollutionSystem), nameof(AirPollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result,int index)
        {
            //int num = index % AirPollutionSystem.kTextureSize;
            //int num2 = index / AirPollutionSystem.kTextureSize;
            //int num3 = 57344 / AirPollutionSystem.kTextureSize;
            //__result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
            __result = CellMapSystemRe.GetCellCenter(index, AirPollutionSystem.kTextureSize);
        }

        /*
        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), nameof(CellMapSystem<AirPollution>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<AirPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            CellMapData<AirPollution> result = default(CellMapData<AirPollution>);
            result.m_Buffer = __result.m_Buffer;
            result.m_CellSize = __result.m_CellSize * 4;
            result.m_TextureSize = __result.m_TextureSize;
            __result = result;
        }*/

        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<AirPollution> __instance, ref CellMapData<AirPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(AirPollutionSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<ExtMap57km.Systems.AirPollutionSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<AirPollution> __instance, ref NativeArray<AirPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(AirPollutionSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.AirPollutionSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<AirPollution> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(AirPollutionSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.AirPollutionSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

    }//airpollution system class;

    [HarmonyPatch]
    internal static class AvailabilityInfoToGridSystemPatch
    {
        [HarmonyPatch(typeof(AvailabilityInfoToGridSystem), nameof(AvailabilityInfoToGridSystem.GetAvailabilityInfo))]
        [HarmonyPostfix]
        public static void GetAvailabilityInfo(ref AvailabilityInfoCell __result,float3 position, NativeArray<AvailabilityInfoCell> AvailabilityInfoMap)
        {
            AvailabilityInfoCell result = default(AvailabilityInfoCell);
            int2 cell = CellMapSystemRe.GetCell(position, 57344, AvailabilityInfoToGridSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, 57344, AvailabilityInfoToGridSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= AvailabilityInfoToGridSystem.kTextureSize || cell.y < 0 || cell.y >= AvailabilityInfoToGridSystem.kTextureSize)
            {
                __result = default(AvailabilityInfoCell);
            }
            float4 availabilityInfo = AvailabilityInfoMap[cell.x + AvailabilityInfoToGridSystem.kTextureSize * cell.y].m_AvailabilityInfo;
            float4 y = ((cell.x < AvailabilityInfoToGridSystem.kTextureSize - 1) ? AvailabilityInfoMap[cell.x + 1 + AvailabilityInfoToGridSystem.kTextureSize * cell.y].m_AvailabilityInfo : ((float4)0));
            float4 x = ((cell.y < AvailabilityInfoToGridSystem.kTextureSize - 1) ? AvailabilityInfoMap[cell.x + AvailabilityInfoToGridSystem.kTextureSize * (cell.y + 1)].m_AvailabilityInfo : ((float4)0));
            float4 y2 = ((cell.x < AvailabilityInfoToGridSystem.kTextureSize - 1 && cell.y < AvailabilityInfoToGridSystem.kTextureSize - 1) ? AvailabilityInfoMap[cell.x + 1 + AvailabilityInfoToGridSystem.kTextureSize * (cell.y + 1)].m_AvailabilityInfo : ((float4)0));
            result.m_AvailabilityInfo = math.lerp(math.lerp(availabilityInfo, y, cellCoords.x - (float)cell.x), math.lerp(x, y2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
            __result = result;
        }


        [HarmonyPatch(typeof(AvailabilityInfoToGridSystem), nameof(AvailabilityInfoToGridSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % AvailabilityInfoToGridSystem.kTextureSize;
            int num2 = index / AvailabilityInfoToGridSystem.kTextureSize;
            int num3 = 57344 / AvailabilityInfoToGridSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }

        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), nameof(CellMapSystem<AvailabilityInfoCell>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<AvailabilityInfoCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            CellMapData<AvailabilityInfoCell> result = default(CellMapData<AvailabilityInfoCell>);
            result.m_Buffer = __result.m_Buffer;
            result.m_CellSize = __result.m_CellSize * 4;
            result.m_TextureSize = __result.m_TextureSize;
            __result = result;
        }

        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), nameof(CellMapSystem<AvailabilityInfoCell>.AddReader))]
        [HarmonyPostfix]
        public static void AddReader(ref JobHandle jobHandle)
        {
            
        }

    }//AvailabilityInfoToGridSystem class

    /// <summary>
    /// this is for gamedebug only;
    /// </summary>
    /*
    internal static class BuildingPollutionAddSystemPatch
    {
        [HarmonyPatch(typeof(BuildableAreaDebugSystem), "RefreshGizmosDebug")]
        [HarmonyPostfix]
        public static void RefreshGizmosDebug(BuildableAreaDebugSystem __instance)
        { 
           
        }
    }//class
    */

    [HarmonyPatch]
    internal static class BuildingPollutionAddSystemPatch
    {
        [HarmonyPatch(typeof(BuildingPollutionAddSystem), nameof(BuildingPollutionAddSystem.GetBuildingPollution))]
        [HarmonyPostfix]
        public static void GetBuildingPollution(ref PollutionData __result,Entity prefab, bool destroyed, bool abandoned, bool isPark, float efficiency, DynamicBuffer<Renter> renters, DynamicBuffer<InstalledUpgrade> installedUpgrades, PollutionParameterData pollutionParameters, DynamicBuffer<CityModifier> cityModifiers, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<SpawnableBuildingData> spawnableDatas, ref ComponentLookup<PollutionData> pollutionDatas, ref ComponentLookup<PollutionModifierData> pollutionModifierDatas, ref ComponentLookup<ZoneData> zoneDatas, ref BufferLookup<Employee> employees, ref BufferLookup<HouseholdCitizen> householdCitizens, ref ComponentLookup<Citizen> citizens)
        {
            PollutionData componentData;
            if (!(destroyed || abandoned))
            {
                if (efficiency > 0f && pollutionDatas.TryGetComponent(prefab, out componentData))
                {
                    if (installedUpgrades.IsCreated)
                    {
                        UpgradeUtils.CombineStats(ref componentData, installedUpgrades, ref prefabRefs, ref pollutionDatas);
                    }
                    SpawnableBuildingData componentData2;
                    if (componentData.m_ScaleWithRenters && !isPark && renters.IsCreated)
                    {
                        ExtMap57km.Systems.BuildingPollutionAddSystem.CountRenters(out var count, out var education, renters, ref employees, ref householdCitizens, ref citizens, ignoreEmployees: false);
                        float num = (spawnableDatas.TryGetComponent(prefab, out componentData2) ? ((float)(int)componentData2.m_Level) : 5f);
                        float num2 = ((count > 0) ? (5f * (float)count / (num + 0.5f * (float)(education / count))) : 0f);
                        componentData.m_GroundPollution *= num2;
                        componentData.m_AirPollution *= num2;
                        componentData.m_NoisePollution *= num2;
                    }
                    if (cityModifiers.IsCreated && spawnableDatas.TryGetComponent(prefab, out componentData2))
                    {
                        ZoneData zoneData = zoneDatas[componentData2.m_ZonePrefab];
                        if (zoneData.m_AreaType == AreaType.Industrial && (zoneData.m_ZoneFlags & ZoneFlags.Office) == 0)
                        {
                            CityUtils.ApplyModifier(ref componentData.m_GroundPollution, cityModifiers, CityModifierType.IndustrialGroundPollution);
                            CityUtils.ApplyModifier(ref componentData.m_AirPollution, cityModifiers, CityModifierType.IndustrialAirPollution);
                        }
                    }
                    if (installedUpgrades.IsCreated)
                    {
                        PollutionModifierData result = default(PollutionModifierData);
                        UpgradeUtils.CombineStats(ref result, installedUpgrades, ref prefabRefs, ref pollutionModifierDatas);
                        componentData.m_GroundPollution *= math.max(0f, 1f + result.m_GroundPollutionMultiplier);
                        componentData.m_AirPollution *= math.max(0f, 1f + result.m_AirPollutionMultiplier);
                        componentData.m_NoisePollution *= math.max(0f, 1f + result.m_NoisePollutionMultiplier);
                    }
                }
                else
                {
                    componentData = default(PollutionData);
                }
            }
            else
            {
                BuildingData buildingData = buildingDatas[prefab];
                PollutionData pollutionData = default(PollutionData);
                pollutionData.m_GroundPollution = 0f;
                pollutionData.m_AirPollution = 0f;
                pollutionData.m_NoisePollution = 5f * (float)(buildingData.m_LotSize.x * buildingData.m_LotSize.y) * pollutionParameters.m_AbandonedNoisePollutionMultiplier;
                componentData = pollutionData;
            }
            if ((abandoned || isPark) && renters.IsCreated)
            {
                Systems.BuildingPollutionAddSystem.CountRenters(out var count2, out var _, renters, ref employees, ref householdCitizens, ref citizens, ignoreEmployees: true);
                componentData.m_NoisePollution += count2 * pollutionParameters.m_HomelessNoisePollution;
            }
            __result = componentData;
        }
    }//BuildingPollutionAddSystem class


    [HarmonyPatch]
    internal static class GroundPollutionSystemPatch
    {
        [HarmonyPatch(typeof(GroundPollutionSystem), nameof(GroundPollutionSystem.GetPollution))]
        [HarmonyPostfix]
        public static void GetPollution(ref GroundPollution __result, float3 position, NativeArray<GroundPollution> pollutionMap)
        {
            GroundPollution result = default(GroundPollution);
            int2 cell = CellMapSystem<GroundPollution>.GetCell(position, CellMapSystem<GroundPollution>.kMapSize, GroundPollutionSystem.kTextureSize);
            float2 cellCoords = CellMapSystem<GroundPollution>.GetCellCoords(position, CellMapSystem<GroundPollution>.kMapSize, GroundPollutionSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= GroundPollutionSystem.kTextureSize || cell.y < 0 || cell.y >= GroundPollutionSystem.kTextureSize)
            {
                __result = result;
            }
            GroundPollution groundPollution = pollutionMap[cell.x + GroundPollutionSystem.kTextureSize * cell.y];
            GroundPollution groundPollution2 = ((cell.x < GroundPollutionSystem.kTextureSize - 1) ? pollutionMap[cell.x + 1 + GroundPollutionSystem.kTextureSize * cell.y] : default(GroundPollution));
            GroundPollution groundPollution3 = ((cell.y < GroundPollutionSystem.kTextureSize - 1) ? pollutionMap[cell.x + GroundPollutionSystem.kTextureSize * (cell.y + 1)] : default(GroundPollution));
            GroundPollution groundPollution4 = ((cell.x < GroundPollutionSystem.kTextureSize - 1 && cell.y < GroundPollutionSystem.kTextureSize - 1) ? pollutionMap[cell.x + 1 + GroundPollutionSystem.kTextureSize * (cell.y + 1)] : default(GroundPollution));
            result.m_Pollution = (short)Mathf.RoundToInt(math.lerp(math.lerp(groundPollution.m_Pollution, groundPollution2.m_Pollution, cellCoords.x - (float)cell.x), math.lerp(groundPollution3.m_Pollution, groundPollution4.m_Pollution, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            result.m_Previous = (short)Mathf.RoundToInt(math.lerp(math.lerp(groundPollution.m_Previous, groundPollution2.m_Previous, cellCoords.x - (float)cell.x), math.lerp(groundPollution3.m_Previous, groundPollution4.m_Previous, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            __result = result;
        }


        [HarmonyPatch(typeof(GroundPollutionSystem), nameof(GroundPollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % GroundPollutionSystem.kTextureSize;
            int num2 = index / GroundPollutionSystem.kTextureSize;
            int num3 = 57344 / GroundPollutionSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), nameof(CellMapSystem<GroundPollution>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<GroundPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }
    }//class

    [HarmonyPatch]
    internal static class GroundWaterSystemPatch
    {
        //[HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.GetGroundWater))]
        //[HarmonyPostfix]
        //public static void GetGroundWater(ref GroundWater __result, float3 position, NativeArray<GroundWater> groundWaterMap)
        //{
       ///未完成，参考NoisePollution;
            /*

            float2 @float = CellMapSystem<GroundWater>.GetCellCoords(position, Systems.CellMapSystem<GroundWater>.kMapSize, Systems.GroundWaterSystem.kTextureSize) - new float2(0.5f, 0.5f);
            int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
            int2 cell2 = new int2(cell.x + 1, cell.y);
            int2 cell3 = new int2(cell.x, cell.y + 1);
            int2 cell4 = new int2(cell.x + 1, cell.y + 1);

            GroundWater groundWater = GroundWaterSystem.GetGroundWater(groundWaterMap, cell);
            GroundWater groundWater2 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell2);
            GroundWater groundWater3 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell3);
            GroundWater groundWater4 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell4);
            float sx = @float.x - (float)cell.x;
            float sy = @float.y - (float)cell.y;
            GroundWater result = default(GroundWater);
            result.m_Amount = (short)math.round(Systems.GroundWaterSystem.Bilinear(groundWater.m_Amount, groundWater2.m_Amount, groundWater3.m_Amount, groundWater4.m_Amount, sx, sy));
            result.m_Polluted = (short)math.round(Systems.GroundWaterSystem.Bilinear(groundWater.m_Polluted, groundWater2.m_Polluted, groundWater3.m_Polluted, groundWater4.m_Polluted, sx, sy));
            result.m_Max = (short)math.round(Systems.GroundWaterSystem.Bilinear(groundWater.m_Max, groundWater2.m_Max, groundWater3.m_Max, groundWater4.m_Max, sx, sy));
            __result = result;*/
        //}

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.TryGetCell))]
        [HarmonyPostfix]
        public static void TryGetCell(ref bool __result, float3 position, out int2 cell)
        {
            cell = Systems.CellMapSystem<GroundWater>.GetCell(position, Systems.CellMapSystem<GroundWater>.kMapSize, Systems.GroundWaterSystem.kTextureSize);
            __result = Systems.GroundWaterSystem.IsValidCell(cell);
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % GroundWaterSystem.kTextureSize;
            int num2 = index / GroundWaterSystem.kTextureSize;
            int num3 = 57344 / GroundWaterSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }


        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), nameof(CellMapSystem<GroundWater>.GetCellCoords))]
        [HarmonyPrefix]
        public static void GetCellCoords(ref float2 __result, float3 position, int mapSize, int textureSize)
        {
            mapSize *= 4;
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), nameof(CellMapSystem<GroundWater>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<GroundWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.ConsumeGroundWater))]
        [HarmonyPostfix]
        public static void ConsumeGroundWater(float3 position, NativeArray<GroundWater> groundWaterMap, int amount)
        {
            /*
            Unity.Assertions.Assert.IsTrue(amount >= 0);
            float2 @float = CellMapSystem<GroundWater>.GetCellCoords(position, CellMapSystem<GroundWater>.kMapSize, GroundWaterSystem.kTextureSize) - new float2(0.5f, 0.5f);
            int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
            int2 cell2 = new int2(cell.x + 1, cell.y);
            int2 cell3 = new int2(cell.x, cell.y + 1);
            int2 cell4 = new int2(cell.x + 1, cell.y + 1);
            GroundWater gw2 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell);
            GroundWater gw3 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell2);
            GroundWater gw4 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell3);
            GroundWater gw5 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell4);
            float sx = @float.x - (float)cell.x;
            float sy = @float.y - (float)cell.y;
            float num = math.ceil(Systems.GroundWaterSystem.Bilinear(gw2.m_Amount, 0, 0, 0, sx, sy));
            float num2 = math.ceil(Systems.GroundWaterSystem.Bilinear(0, gw3.m_Amount, 0, 0, sx, sy));
            float num3 = math.ceil(Systems.GroundWaterSystem.Bilinear(0, 0, gw4.m_Amount, 0, sx, sy));
            float num4 = math.ceil(Systems.GroundWaterSystem.Bilinear(0, 0, 0, gw5.m_Amount, sx, sy));
            float totalAvailable = num + num2 + num3 + num4;
            float totalConsumed = math.min(amount, totalAvailable);
            if (totalAvailable < (float)amount)
            {
                UnityEngine.Debug.LogWarning($"Trying to consume more groundwater than available! amount: {amount}, available: {totalAvailable}");
            }
            ConsumeFraction(ref gw2, num);
            ConsumeFraction(ref gw3, num2);
            ConsumeFraction(ref gw4, num3);
            ConsumeFraction(ref gw5, num4);
            Unity.Assertions.Assert.IsTrue(Mathf.Approximately(totalAvailable, 0f));
            Unity.Assertions.Assert.IsTrue(Mathf.Approximately(totalConsumed, 0f));
            Systems.GroundWaterSystem.SetGroundWater(groundWaterMap, cell, gw2);
            Systems.GroundWaterSystem.SetGroundWater(groundWaterMap, cell2, gw3);
            Systems.GroundWaterSystem.SetGroundWater(groundWaterMap, cell3, gw4);
            Systems.GroundWaterSystem.SetGroundWater(groundWaterMap, cell4, gw5);
            void ConsumeFraction(ref GroundWater gw, float cellAvailable)
            {
                if (!(totalAvailable < 0.5f))
                {
                    float num5 = cellAvailable / totalAvailable;
                    totalAvailable -= cellAvailable;
                    float num6 = math.max(y: math.max(0f, totalConsumed - totalAvailable), x: math.round(num5 * totalConsumed));
                    Unity.Assertions.Assert.IsTrue(num6 <= (float)gw.m_Amount);
                    gw.Consume((int)num6);
                    totalConsumed -= num6;
                }
        }
        }

    [HarmonyPatch]
    internal static class NaturalResourceSystemPatch
    {
        /*[HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % NaturalResourceSystem.kTextureSize;
            int num2 = index / NaturalResourceSystem.kTextureSize;
            int num3 = 57344 / NaturalResourceSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }*/


        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.GetCellCoords))]
        [HarmonyPrefix]
        public static void GetCellCoords(ref float2 __result, float3 position, int mapSize, int textureSize)
        {
            mapSize *= 4;
        }

        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<NaturalResourceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(NaturalResourceSystem), nameof(NaturalResourceSystem.ResourceAmountToArea))]
        [HarmonyPostfix]
        public static void ResourceAmountToArea(ref float __result,float amount)
        {
            __result *= 16;
        }
    }

    [HarmonyPatch]
    internal static class NoisePollutionSystemPatch
    {
        [HarmonyPatch(typeof(NoisePollutionSystem), nameof(NoisePollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % NoisePollutionSystem.kTextureSize;
            int num2 = index / NoisePollutionSystem.kTextureSize;
            int num3 = 57344 / NoisePollutionSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }

        /*
        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), nameof(CellMapSystem<NoisePollution>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<NoisePollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }*/

        [HarmonyPatch(typeof(NoisePollutionSystem), nameof(NoisePollutionSystem.GetPollution))]
        [HarmonyPostfix]
        public static void GetPollution(ref NoisePollution __result, float3 position, NativeArray<NoisePollution> pollutionMap)
        {
            NoisePollution result = default(NoisePollution);
            float num = (float)Systems.CellMapSystem<NoisePollution>.kMapSize / (float)NoisePollutionSystem.kTextureSize;
            int2 cell = Systems.CellMapSystem<NoisePollution>.GetCell(position - new float3(num / 2f, 0f, num / 2f), Systems.CellMapSystem<NoisePollution>.kMapSize, NoisePollutionSystem.kTextureSize);
            float2 @float = Systems.CellMapSystem<NoisePollution>.GetCellCoords(position, Systems.CellMapSystem<NoisePollution>.kMapSize, NoisePollutionSystem.kTextureSize) - new float2(0.5f, 0.5f);
            cell = math.clamp(cell, 0, NoisePollutionSystem.kTextureSize - 2);
            short pollution = pollutionMap[cell.x + NoisePollutionSystem.kTextureSize * cell.y].m_Pollution;
            short pollution2 = pollutionMap[cell.x + 1 + NoisePollutionSystem.kTextureSize * cell.y].m_Pollution;
            short pollution3 = pollutionMap[cell.x + NoisePollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
            short pollution4 = pollutionMap[cell.x + 1 + NoisePollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
            result.m_Pollution = (short)Mathf.RoundToInt(math.lerp(math.lerp(pollution, pollution2, @float.x - (float)cell.x), math.lerp(pollution3, pollution4, @float.x - (float)cell.x), @float.y - (float)cell.y));
            __result = result;
        }

        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<NoisePollution> __instance, ref CellMapData<NoisePollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(NoisePollutionSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.NoisePollutionSystem>().GetData(readOnly, out dependencies);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<NoisePollution> __instance, ref NativeArray<NoisePollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(NoisePollutionSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.NoisePollutionSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<NoisePollution> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(NoisePollutionSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.NoisePollutionSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

    }//class;


    [HarmonyPatch]
    internal static class PopulationToGridSystemPatch
    {
        [HarmonyPatch(typeof(PopulationToGridSystem), nameof(PopulationToGridSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % NoisePollutionSystem.kTextureSize;
            int num2 = index / NoisePollutionSystem.kTextureSize;
            int num3 = 57344 / NoisePollutionSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }


        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), nameof(CellMapSystem<PopulationCell>.GetCellCoords))]
        [HarmonyPrefix]
        public static void GetCellCoords(ref float2 __result, float3 position, int mapSize, int textureSize)
        {
            mapSize *= 4;
        }

        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), nameof(CellMapSystem<PopulationCell>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<PopulationCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(PopulationToGridSystem), nameof(PopulationToGridSystem.GetPopulation))]
        [HarmonyPostfix]
        public static void GetPopulation(ref PopulationCell __result, float3 position, NativeArray<PopulationCell> populationMap)
        {
            PopulationCell result = default(PopulationCell);
            int2 cell = Systems.CellMapSystem<PopulationCell>.GetCell(position, Systems.CellMapSystem<PopulationCell>.kMapSize, Systems.PopulationToGridSystem.kTextureSize);
            float2 cellCoords = Systems.CellMapSystem<PopulationCell>.GetCellCoords(position, Systems.CellMapSystem<PopulationCell>.kMapSize, Systems.PopulationToGridSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= Systems.PopulationToGridSystem.kTextureSize || cell.y < 0 || cell.y >= Systems.PopulationToGridSystem.kTextureSize)
            {
                __result = result;
            }
            float population = populationMap[cell.x + Systems.PopulationToGridSystem.kTextureSize * cell.y].m_Population;
            float y = ((cell.x < Systems.PopulationToGridSystem.kTextureSize - 1) ? populationMap[cell.x + 1 + Systems.PopulationToGridSystem.kTextureSize * cell.y].m_Population : 0f);
            float x = ((cell.y < Systems.PopulationToGridSystem.kTextureSize - 1) ? populationMap[cell.x + Systems.PopulationToGridSystem.kTextureSize * (cell.y + 1)].m_Population : 0f);
            float y2 = ((cell.x < Systems.PopulationToGridSystem.kTextureSize - 1 && cell.y < Systems.PopulationToGridSystem.kTextureSize - 1) ? populationMap[cell.x + 1 + Systems.PopulationToGridSystem.kTextureSize * (cell.y + 1)].m_Population : 0f);
            result.m_Population = math.lerp(math.lerp(population, y, cellCoords.x - (float)cell.x), math.lerp(x, y2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
            __result = result;
        }

    [HarmonyPatch]
    internal static class SoilWaterSystemPatch
    {
        [HarmonyPatch(typeof(SoilWaterSystem), nameof(SoilWaterSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % SoilWaterSystem.kTextureSize;
            int num2 = index / SoilWaterSystem.kTextureSize;
            int num3 = 57344 / SoilWaterSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }


        [HarmonyPatch(typeof(CellMapSystem<SoilWater>), nameof(CellMapSystem<SoilWater>.GetCellCoords))]
        [HarmonyPrefix]
        public static void GetCellCoords(ref float2 __result, float3 position, int mapSize, int textureSize)
        {
            mapSize *= 4;
        }

        [HarmonyPatch(typeof(CellMapSystem<SoilWater>), nameof(CellMapSystem<SoilWater>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<SoilWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(SoilWaterSystem), nameof(SoilWaterSystem.GetSoilWater))]
        [HarmonyPostfix]
        public static void GetSoilWater(ref SoilWater __result, float3 position, NativeArray<SoilWater> soilWaterMap)
        {
            SoilWater result = default(SoilWater);
            int2 cell = Systems.CellMapSystem<SoilWater>.GetCell(position, Systems.CellMapSystem<SoilWater>.kMapSize, SoilWaterSystem.kTextureSize);
            float2 cellCoords = Systems.CellMapSystem<SoilWater>.GetCellCoords(position, Systems.CellMapSystem<SoilWater>.kMapSize, Systems.SoilWaterSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= Systems.SoilWaterSystem.kTextureSize || cell.y < 0 || cell.y >= Systems.SoilWaterSystem.kTextureSize)
            {
                __result = result;
            }
            float x = soilWaterMap[cell.x + Systems.SoilWaterSystem.kTextureSize * cell.y].m_Amount;
            float y = ((cell.x < Systems.SoilWaterSystem.kTextureSize - 1) ? soilWaterMap[cell.x + 1 + Systems.SoilWaterSystem.kTextureSize * cell.y].m_Amount : 0);
            float x2 = ((cell.y < Systems.SoilWaterSystem.kTextureSize - 1) ? soilWaterMap[cell.x + Systems.SoilWaterSystem.kTextureSize * (cell.y + 1)].m_Amount : 0);
            float y2 = ((cell.x < Systems.SoilWaterSystem.kTextureSize - 1 && cell.y < Systems.SoilWaterSystem.kTextureSize - 1) ? soilWaterMap[cell.x + 1 + Systems.SoilWaterSystem.kTextureSize * (cell.y + 1)].m_Amount : 0);
            result.m_Amount = (short)Mathf.RoundToInt(math.lerp(math.lerp(x, y, cellCoords.x - (float)cell.x), math.lerp(x2, y2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            __result = result;
        }
    }//class;

    [HarmonyPatch]
    internal static class TelecomCoverageSystemPatch
    {
        
        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), nameof(CellMapSystem<TelecomCoverage>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<TelecomCoverage> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }

        
    }//class;

    [HarmonyPatch]
    internal static class WindSystemPatch
    {

        [HarmonyPatch(typeof(CellMapSystem<Wind>), nameof(CellMapSystem<Wind>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<Wind> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(WindSystem), nameof(WindSystem.GetWind))]
        [HarmonyPostfix]
        public static void GetWind(ref Wind __result, float3 position, NativeArray<Wind> windMap)
        {
            int2 cell = CellMapSystem<Wind>.GetCell(position, CellMapSystem<Wind>.kMapSize, WindSystem.kTextureSize);
            cell = math.clamp(cell, 0, WindSystem.kTextureSize - 1);
            float2 cellCoords = CellMapSystem<Wind>.GetCellCoords(position, CellMapSystem<Wind>.kMapSize, WindSystem.kTextureSize);
            int num = math.min(WindSystem.kTextureSize - 1, cell.x + 1);
            int num2 = math.min(WindSystem.kTextureSize - 1, cell.y + 1);
            Wind result = default(Wind);
            result.m_Wind = math.lerp(math.lerp(windMap[cell.x + WindSystem.kTextureSize * cell.y].m_Wind, windMap[num + WindSystem.kTextureSize * cell.y].m_Wind, cellCoords.x - (float)cell.x), math.lerp(windMap[cell.x + WindSystem.kTextureSize * num2].m_Wind, windMap[num + WindSystem.kTextureSize * num2].m_Wind, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
            __result = result;
        }

    }//class;


}//namespace