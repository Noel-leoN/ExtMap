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
using System;

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
        [HarmonyPatch(typeof(AirPollutionSystem), nameof(AirPollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            //int num = index % AirPollutionSystem.kTextureSize;
            //int num2 = index / AirPollutionSystem.kTextureSize;
            //int num3 = 57344 / AirPollutionSystem.kTextureSize;
            //__result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
            __result = CellMapSystemRe.GetCellCenter(index, AirPollutionSystem.kTextureSize);
        }

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

        
        //may not necessary if make ecs "postfix"(updateafter) of this sim system;下同；
        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<AirPollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(AirPollutionSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.AirPollutionSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<AirPollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(AirPollutionSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.AirPollutionSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

    }//airpollution system class;

    //AvailabilityInfoToGridSystem
    [HarmonyPatch]
    internal static class AvailabilityInfoToGridSystemPatch
    {
        /*
        [HarmonyPatch(typeof(AvailabilityInfoToGridSystem), nameof(AvailabilityInfoToGridSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % AvailabilityInfoToGridSystem.kTextureSize;
            int num2 = index / AvailabilityInfoToGridSystem.kTextureSize;
            int num3 = 57344 / AvailabilityInfoToGridSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }*/

        [HarmonyPatch(typeof(AvailabilityInfoToGridSystem), nameof(AvailabilityInfoToGridSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, AvailabilityInfoToGridSystem.kTextureSize);
        }


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

        /*
        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), nameof(CellMapSystem<AvailabilityInfoCell>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<AvailabilityInfoCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            CellMapData<AvailabilityInfoCell> result = default(CellMapData<AvailabilityInfoCell>);
            result.m_Buffer = __result.m_Buffer;
            result.m_CellSize = __result.m_CellSize * 4;
            result.m_TextureSize = __result.m_TextureSize;
            __result = result;
        }*/

        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<AvailabilityInfoCell> __instance, ref CellMapData<AvailabilityInfoCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(AvailabilityInfoToGridSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<ExtMap57km.Systems.AvailabilityInfoToGridSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<AvailabilityInfoCell> __instance, ref NativeArray<AvailabilityInfoCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(AvailabilityInfoToGridSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.AvailabilityInfoToGridSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<AvailabilityInfoCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(AvailabilityInfoToGridSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.AvailabilityInfoToGridSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
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
        //may not necessary if make ecs "postfix"(updateafter) of this sim system;
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

        /*
        [HarmonyPatch(typeof(GroundPollutionSystem), nameof(GroundPollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % GroundPollutionSystem.kTextureSize;
            int num2 = index / GroundPollutionSystem.kTextureSize;
            int num3 = 57344 / GroundPollutionSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }*/

        [HarmonyPatch(typeof(GroundPollutionSystem), nameof(GroundPollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            //int num = index % AirPollutionSystem.kTextureSize;
            //int num2 = index / AirPollutionSystem.kTextureSize;
            //int num3 = 57344 / AirPollutionSystem.kTextureSize;
            //__result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
            __result = CellMapSystemRe.GetCellCenter(index, GroundPollutionSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<GroundPollution> __instance, ref NativeArray<GroundPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(GroundPollutionSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.GroundPollutionSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<GroundPollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(GroundPollutionSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.GroundPollutionSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        /*
        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), nameof(CellMapSystem<GroundPollution>.GetData))]
        [HarmonyPostfix]
        public static void GetData(ref CellMapData<GroundPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }*/


        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<GroundPollution> __instance, ref CellMapData<GroundPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(GroundPollutionSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<ExtMap57km.Systems.GroundPollutionSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }


    }//class

    [HarmonyPatch]
    internal static class GroundWaterSystemPatch
    {
        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.GetGroundWater), new Type[] { typeof(float3), typeof(NativeArray<GroundWater>) })]
        [HarmonyPostfix]
        public static void GetGroundWater(ref GroundWater __result, float3 position, NativeArray<GroundWater> groundWaterMap)
        {
            float2 @float = Systems.CellMapSystem<GroundWater>.GetCellCoords(position, Systems.CellMapSystem<GroundWater>.kMapSize, GroundWaterSystem.kTextureSize) - new float2(0.5f, 0.5f);
            int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
            int2 cell2 = new int2(cell.x + 1, cell.y);
            int2 cell3 = new int2(cell.x, cell.y + 1);
            int2 cell4 = new int2(cell.x + 1, cell.y + 1);
            GroundWater groundWater = ExtMap57km.Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell);
            GroundWater groundWater2 = ExtMap57km.Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell2);
            GroundWater groundWater3 = ExtMap57km.Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell3);
            GroundWater groundWater4 = ExtMap57km.Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell4);
            float sx = @float.x - (float)cell.x;
            float sy = @float.y - (float)cell.y;
            GroundWater result = default(GroundWater);
            result.m_Amount = (short)math.round(ExtMap57km.Systems.GroundWaterSystem.Bilinear(groundWater.m_Amount, groundWater2.m_Amount, groundWater3.m_Amount, groundWater4.m_Amount, sx, sy));
            result.m_Polluted = (short)math.round(ExtMap57km.Systems.GroundWaterSystem.Bilinear(groundWater.m_Polluted, groundWater2.m_Polluted, groundWater3.m_Polluted, groundWater4.m_Polluted, sx, sy));
            result.m_Max = (short)math.round(ExtMap57km.Systems.GroundWaterSystem.Bilinear(groundWater.m_Max, groundWater2.m_Max, groundWater3.m_Max, groundWater4.m_Max, sx, sy));
            __result = result;

        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.TryGetCell))]
        [HarmonyPostfix]
        public static void TryGetCell(ref bool __result, float3 position, ref int2 cell)
        {
            cell = Systems.CellMapSystem<GroundWater>.GetCell(position, Systems.CellMapSystem<GroundWater>.kMapSize, Systems.GroundWaterSystem.kTextureSize);
            __result = Systems.GroundWaterSystem.IsValidCell(cell);
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.IsValidCell))]
        [HarmonyPostfix]
        public static void IsValidCell(ref bool __result, int2 cell)
        {
            if (cell.x >= 0 && cell.y >= 0 && cell.x < GroundWaterSystem.kTextureSize)
            {
                __result = cell.y < GroundWaterSystem.kTextureSize;
            }
            __result = false;
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            //int num = index % AirPollutionSystem.kTextureSize;
            //int num2 = index / AirPollutionSystem.kTextureSize;
            //int num3 = 57344 / AirPollutionSystem.kTextureSize;
            //__result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
            __result = CellMapSystemRe.GetCellCenter(index, GroundWaterSystem.kTextureSize);
        }

        /*
        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % GroundWaterSystem.kTextureSize;
            int num2 = index / GroundWaterSystem.kTextureSize;
            int num3 = 57344 / GroundWaterSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }*/

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.ConsumeGroundWater))]
        [HarmonyPostfix]
        public static void ConsumeGroundWater(float3 position, NativeArray<GroundWater> groundWaterMap, int amount)
        {
            
            Unity.Assertions.Assert.IsTrue(amount >= 0);
            float2 @float = Systems.CellMapSystem<GroundWater>.GetCellCoords(position, Systems.CellMapSystem<GroundWater>.kMapSize, GroundWaterSystem.kTextureSize) - new float2(0.5f, 0.5f);
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

        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<GroundWater> __instance, ref NativeArray<GroundWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(GroundWaterSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.GroundWaterSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }            

        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<GroundWater> __instance, ref CellMapData<GroundWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(GroundWaterSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<ExtMap57km.Systems.GroundWaterSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        //may not necessary if make ecs "postfix"(updateafter) of this sim system;下同；
        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<GroundWater> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(GroundWaterSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.GroundWaterSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<GroundWater> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(GroundWaterSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.GroundWaterSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

    }//class

    [HarmonyPatch]
    internal static class NaturalResourceSystemPatch
    {       
        [HarmonyPatch(typeof(NaturalResourceSystem), nameof(NaturalResourceSystem.ResourceAmountToArea))]
        [HarmonyPostfix]
        public static void ResourceAmountToArea(ref float __result, float amount)
        {
            __result *= 16;
        }

        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<NaturalResourceCell> __instance, ref CellMapData<NaturalResourceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(NaturalResourceSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<ExtMap57km.Systems.NaturalResourceSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        //may not necessary if make ecs "postfix"(updateafter) of this sim system;下同；
        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<NaturalResourceCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(NaturalResourceSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.NaturalResourceSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }
        
    }//NatualResourceSystem class;

    [HarmonyPatch]
    internal static class NoisePollutionSystemPatch
    {
        [HarmonyPatch(typeof(NoisePollutionSystem), nameof(NoisePollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, Systems.NoisePollutionSystem.kTextureSize);
        }

        /*
        [HarmonyPatch(typeof(NoisePollutionSystem), nameof(NoisePollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int num = index % NoisePollutionSystem.kTextureSize;
            int num2 = index / NoisePollutionSystem.kTextureSize;
            int num3 = 57344 / NoisePollutionSystem.kTextureSize;
            __result = new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
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

        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<NoisePollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(NoisePollutionSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.NoisePollutionSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

    }//NoisePollutionSystem class;


    [HarmonyPatch]
    internal static class PopulationToGridSystemPatch
    {
        [HarmonyPatch(typeof(PopulationToGridSystem), nameof(PopulationToGridSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, Systems.PopulationToGridSystem.kTextureSize);
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

        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<PopulationCell> __instance, ref CellMapData<PopulationCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(PopulationToGridSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.PopulationToGridSystem>().GetData(readOnly, out dependencies);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<PopulationCell> __instance, ref NativeArray<PopulationCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(PopulationToGridSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.PopulationToGridSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<PopulationCell> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(PopulationToGridSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.PopulationToGridSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

    }//class;

    [HarmonyPatch]
    internal static class SoilWaterSystemPatch
    {
        [HarmonyPatch(typeof(SoilWaterSystem), nameof(SoilWaterSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, Systems.SoilWaterSystem.kTextureSize);
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

        [HarmonyPatch(typeof(CellMapSystem<SoilWater>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<SoilWater> __instance, ref NativeArray<SoilWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(SoilWaterSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.SoilWaterSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<SoilWater>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<SoilWater> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(SoilWaterSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.SoilWaterSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }
    }//class;

    [HarmonyPatch]
    internal static class TelecomCoverageSystemPatch
    { 
        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<TelecomCoverage> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(TelecomCoverageSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.TelecomCoverageSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<TelecomCoverage> __instance, ref CellMapData<TelecomCoverage> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(TelecomCoverageSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TelecomCoverageSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }
    }//class;

    [HarmonyPatch]
    internal static class TerrainAttractivenessSystemPatch
    {
        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, Systems.TerrainAttractivenessSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.EvaluateAttractiveness),new Type[] {typeof(float),typeof(TerrainAttractiveness),typeof(AttractivenessParameterData) })]
        [HarmonyPostfix]
        public static void EvaluateAttractiveness(ref float __result, float terrainHeight, TerrainAttractiveness attractiveness, AttractivenessParameterData parameters)
        {
            float num = parameters.m_ForestEffect * attractiveness.m_ForestBonus;
            float num2 = parameters.m_ShoreEffect * attractiveness.m_ShoreBonus;
            float num3 = math.min(parameters.m_HeightBonus.z, math.max(0f, terrainHeight - parameters.m_HeightBonus.x) * parameters.m_HeightBonus.y);
            __result = num + num2 + num3;
        }

        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.EvaluateAttractiveness), new Type[] {typeof(float3),typeof(CellMapData<TerrainAttractiveness>),typeof(TerrainHeightData),typeof(AttractivenessParameterData),typeof(NativeArray<int>) })]
        [HarmonyPostfix]
        public static void EvaluateAttractiveness(ref float __result, float3 position, CellMapData<TerrainAttractiveness> data, TerrainHeightData heightData, AttractivenessParameterData parameters, NativeArray<int> factors)
        {
            float num = TerrainUtils.SampleHeight(ref heightData, position);
            TerrainAttractiveness attractiveness = TerrainAttractivenessSystem.GetAttractiveness(position, data.m_Buffer);
            float num2 = parameters.m_ForestEffect * attractiveness.m_ForestBonus;
            AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Forest, num2);
            float num3 = parameters.m_ShoreEffect * attractiveness.m_ShoreBonus;
            AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Beach, num3);
            float num4 = math.min(parameters.m_HeightBonus.z, math.max(0f, num - parameters.m_HeightBonus.x) * parameters.m_HeightBonus.y);
            AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Height, num4);
            __result = num2 + num3 + num4;
        }

        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.GetAttractiveness))]
        [HarmonyPostfix]
        public static void GetAttractiveness(ref TerrainAttractiveness __result, float3 position, NativeArray<TerrainAttractiveness> attractivenessMap)
        {
            TerrainAttractiveness result = default(TerrainAttractiveness);
            int2 cell = Systems.CellMapSystem<TerrainAttractiveness>.GetCell(position, CellMapSystem<TerrainAttractiveness>.kMapSize, TerrainAttractivenessSystem.kTextureSize);
            float2 cellCoords = Systems.CellMapSystem<TerrainAttractiveness>.GetCellCoords(position, Systems.CellMapSystem<TerrainAttractiveness>.kMapSize, TerrainAttractivenessSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= TerrainAttractivenessSystem.kTextureSize || cell.y < 0 || cell.y >= TerrainAttractivenessSystem.kTextureSize)
            {
                __result = result;
            }
            TerrainAttractiveness terrainAttractiveness = attractivenessMap[cell.x + TerrainAttractivenessSystem.kTextureSize * cell.y];
            TerrainAttractiveness terrainAttractiveness2 = ((cell.x < TerrainAttractivenessSystem.kTextureSize - 1) ? attractivenessMap[cell.x + 1 + TerrainAttractivenessSystem.kTextureSize * cell.y] : default(TerrainAttractiveness));
            TerrainAttractiveness terrainAttractiveness3 = ((cell.y < TerrainAttractivenessSystem.kTextureSize - 1) ? attractivenessMap[cell.x + TerrainAttractivenessSystem.kTextureSize * (cell.y + 1)] : default(TerrainAttractiveness));
            TerrainAttractiveness terrainAttractiveness4 = ((cell.x < TerrainAttractivenessSystem.kTextureSize - 1 && cell.y < TerrainAttractivenessSystem.kTextureSize - 1) ? attractivenessMap[cell.x + 1 + TerrainAttractivenessSystem.kTextureSize * (cell.y + 1)] : default(TerrainAttractiveness));
            result.m_ForestBonus = (short)Mathf.RoundToInt(math.lerp(math.lerp(terrainAttractiveness.m_ForestBonus, terrainAttractiveness2.m_ForestBonus, cellCoords.x - (float)cell.x), math.lerp(terrainAttractiveness3.m_ForestBonus, terrainAttractiveness4.m_ForestBonus, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            result.m_ShoreBonus = (short)Mathf.RoundToInt(math.lerp(math.lerp(terrainAttractiveness.m_ShoreBonus, terrainAttractiveness2.m_ShoreBonus, cellCoords.x - (float)cell.x), math.lerp(terrainAttractiveness3.m_ShoreBonus, terrainAttractiveness4.m_ShoreBonus, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            __result = result;
        }

        [HarmonyPatch(typeof(CellMapSystem<TerrainAttractiveness>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<TerrainAttractiveness> __instance, ref CellMapData<TerrainAttractiveness> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(TerrainAttractivenessSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TerrainAttractivenessSystem>().GetData(readOnly, out dependencies);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TerrainAttractiveness>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<TerrainAttractiveness> __instance, ref NativeArray<TerrainAttractiveness> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(TerrainAttractivenessSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TerrainAttractivenessSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TerrainAttractiveness>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<TerrainAttractiveness> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(TerrainAttractivenessSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.TerrainAttractivenessSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }
    }//class;

    [HarmonyPatch]
    internal static class TrafficAmbienceSystemPatch
    {
        [HarmonyPatch(typeof(TrafficAmbienceSystem), nameof(TrafficAmbienceSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, Systems.TrafficAmbienceSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(TrafficAmbienceSystem), nameof(TrafficAmbienceSystem.GetTrafficAmbience2))]
        [HarmonyPostfix]
        public static void GetTrafficAmbience2(ref TrafficAmbienceCell __result,float3 position, NativeArray<TrafficAmbienceCell> trafficAmbienceMap, float maxPerCell)
        {
            TrafficAmbienceCell result = default(TrafficAmbienceCell);
            int2 cell = Systems.CellMapSystem<TrafficAmbienceCell>.GetCell(position, Systems.CellMapSystem<TrafficAmbienceCell>.kMapSize, TrafficAmbienceSystem.kTextureSize);
            float num = 0f;
            float num2 = 0f;
            for (int i = cell.x - 2; i <= cell.x + 2; i++)
            {
                for (int j = cell.y - 2; j <= cell.y + 2; j++)
                {
                    if (i >= 0 && i < TrafficAmbienceSystem.kTextureSize && j >= 0 && j < TrafficAmbienceSystem.kTextureSize)
                    {
                        int index = i + TrafficAmbienceSystem.kTextureSize * j;
                        float num3 = math.max(1f, math.distancesq(TrafficAmbienceSystem.GetCellCenter(index), position));
                        num += math.min(maxPerCell, trafficAmbienceMap[index].m_Traffic) / num3;
                        num2 += 1f / num3;
                    }
                }
            }
            result.m_Traffic = num / num2;
            __result = result;
        }

        [HarmonyPatch(typeof(CellMapSystem<TrafficAmbienceCell>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<TrafficAmbienceCell> __instance, ref NativeArray<TrafficAmbienceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(TrafficAmbienceSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TrafficAmbienceSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TrafficAmbienceCell>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<TrafficAmbienceCell> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(TrafficAmbienceSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.TrafficAmbienceSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }
    }//class;

    [HarmonyPatch]
    internal static class WindSimulationSystemPatch
    {
        [HarmonyPatch(typeof(WindSimulationSystem), "SetWind")]
        [HarmonyPrefix]
        public static bool SetWind(WindSimulationSystem __instance, float2 direction, float pressure)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(WindSimulationSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.WindSimulationSystem>().SetWind(direction, pressure);
                return false;
            }
            return true;

        }
    }//class;

    
    [HarmonyPatch]
    internal static class WindSystemPatch
    {
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

        [HarmonyPatch(typeof(CellMapSystem<Wind>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<Wind> __instance, ref CellMapData<Wind> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(WindSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.WindSystem>().GetData(readOnly, out dependencies);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<Wind>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<Wind> __instance, ref NativeArray<Wind> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(WindSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.WindSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<Wind>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<Wind> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(WindSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.WindSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

    }//class;




}//namespace