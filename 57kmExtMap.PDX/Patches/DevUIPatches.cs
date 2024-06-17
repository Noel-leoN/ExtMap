using Unity.Entities;
using Colossal.Entities;
using Game.Buildings;
using Game.UI.InGame;
using HarmonyLib;
using Game.Net;
using Game.Prefabs;
using Game.Economy;
using Game.Zones;
using Game.Areas;
using Game.City;
using Game.Simulation;
using UnityEngine;
using System.Security.AccessControl;
using Game.Citizens;
using Game.Objects;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.ComponentModel;

namespace ExtMap57km.Patches
{
    [HarmonyPatch]
    class DevUIPatches
    {
        /*
        [HarmonyPatch(typeof(Game.UI.InGame.DeveloperInfoUISystem), "UpdateLandValueInfo")]
        [HarmonyPostfix]
        
        public static void DeveloperInfoUISystem_UpdateLandValueInfo_Postfix(DeveloperInfoUISystem __instance, Entity entity, Entity prefab, GenericInfo info)
        {
            //Patcher.Instance.Log.Info($"DevInfo LV addon");
            DynamicBuffer<Renter> buffer = __instance.EntityManager.GetBuffer<Renter>(entity, isReadOnly: true);
            Building componentData = __instance.EntityManager.GetComponentData<Building>(entity);
            //int num = 0;
            //int num2 = 0;
            //int3 @int = default(int3);
            int num3 = 0;
            //int num4 = 0;
            //int num5 = 0;
            //int num6 = 0;
            //int num7 = 0;
            //float num8 = 0f;
            //short num9 = 0;
            float num20 = 0f;
            float num21 = 0f;
            float num22 = 0f;
            float num23 = 0f;
            float num24 = 0f;
            //int num31 = 0;//upkeep/户数;
            float num32 = 0;//lv max;maxrent-upkeep=3lv;
            float num33 = 0f;//lvr max;
            float lvfactor;

            if (__instance.EntityManager.TryGetComponent<BuildingPropertyData>(prefab, out var component))
            {
                int num10 = component.CountProperties();
                num21 = component.m_ResidentialProperties;
                num22 = component.m_SpaceMultiplier;

                if (__instance.EntityManager.TryGetComponent<LandValue>(componentData.m_RoadEdge, out var component2))
                {
                    num20 = component2.m_Weight;
                }
                if (__instance.EntityManager.TryGetComponent<ConsumptionData>(prefab, out var component3))
                {
                    num3 = component3.m_Upkeep;
                }
                if (__instance.EntityManager.TryGetComponent<BuildingData>(prefab, out var component4))
                {
                    //num5 = component4.m_LotSize.x * component4.m_LotSize.y;
                    num23 = component4.m_LotSize.x;
                    num24 = component4.m_LotSize.y;
                }

                lvfactor = 0.1f;

                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity renter = buffer[i].m_Renter;
                    if (!__instance.EntityManager.TryGetComponent<PropertyRenter>(renter, out var component9))
                    {
                        continue;
                    }
                    //num += component9.m_Rent;
                    //num2 += component9.m_MaxRent;
                    num32 += 0.1f * (component9.m_MaxRent - num3/math.max(1, num10)) / 3;
                    num33 += 0.1f *(component9.m_MaxRent * lvfactor- num3 / math.max(1, num10)) / 3;
                }
            }

            info.value += $" LVWeight {num20} ResProps {num21} SpaceMul {num22} LotX{num23} LotY{num24} LVmax {num32} LVRmax {num33}";
        }*/

        /* Example how to add extra info to the Developer UI Info
        [HarmonyPatch(typeof(Game.UI.InGame.DeveloperInfoUISystem), "UpdateExtractorCompanyInfo")]
        [HarmonyPostfix]
        public static void UpdateExtractorCompanyInfo_Postfix(Entity entity, Entity prefab, InfoList info, EntityQuery _____query_746694603_5)
        {
            // private EntityQuery __query_746694603_5;
            //Plugin.Log("UpdateExtractorCompanyInfo");
            ExtractorParameterData singleton = _____query_746694603_5.GetSingleton<ExtractorParameterData>();
            info.Add(new InfoList.Item($"ExtPar: {singleton.m_FertilityConsumption} {singleton.m_ForestConsumption} {singleton.m_OreConsumption} {singleton.m_OilConsumption}"));
        }
        

        [HarmonyPatch(typeof(DeveloperInfoUISystem), "UpdateZoneInfo")]
        [HarmonyPostfix]
        public static void DeveloperInfoUISystem_UpdateZoneInfo_Postfix(DeveloperInfoUISystem __instance, Entity entity, Entity prefab, GenericInfo info)
        {
            //Plugin.Log("UpdateExtractorCompanyInfo");
            if (!__instance.EntityManager.HasComponent<Building>(entity))
            {
                entity = __instance.EntityManager.GetComponentData<PropertyRenter>(entity).m_Property;
                prefab = __instance.EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            }
            BuildingPropertyData comp = __instance.EntityManager.GetComponentData<BuildingPropertyData>(prefab);
            info.value += $" space {comp.m_SpaceMultiplier} res {comp.m_ResidentialProperties}";
        }

        [HarmonyPatch(typeof(DeveloperInfoUISystem), "UpdateStorageInfo")]
        [HarmonyPrefix]

        private static bool DeveloperInfoUISystem_UpdateStorageInfo_Prefix(DeveloperInfoUISystem __instance, Entity entity, Entity prefab, InfoList info)
        {
            info.label = "Storage";
            if (__instance.EntityManager.TryGetComponent<StorageCompanyData>(prefab, out var component2))
                info.label = "Storage Company";
            if (__instance.EntityManager.TryGetComponent<StorageLimitData>(prefab, out var component) && __instance.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Economy.Resources> buffer))
            {
                //AddUpgradeData(entity, ref component);
                info.Add(new InfoList.Item($"Storage Limit: {component.m_Limit}"));
                //int num = math.max(1, EconomyUtils.CountResources(component2.m_StoredResources));
                //long num2 = component.m_Limit; // / num;
                for (int i = 0; i < buffer.Length; i++)
                {
                    info.Add(new InfoList.Item($"{EconomyUtils.GetName(buffer[i].m_Resource)} ({buffer[i].m_Amount})"));
                }
            }
            return false; // don't execute the original
        }



        [HarmonyPatch(typeof(Game.Debug.EconomyDebugSystem), "PrintCompanyDebug")]
        [HarmonyPrefix]
        public static bool EconomyDebugSystem_PrintCompanyDebug_Prefix(ComponentLookup<ResourceData> resourceDatas)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<ServiceCompanyData>(), ComponentType.ReadOnly<WorkplaceData>());
            EntityQuery entityQuery2 = entityManager.CreateEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<IndustrialCompanyData>(), ComponentType.ReadOnly<WorkplaceData>(), ComponentType.Exclude<StorageCompanyData>());
            ResourcePrefabs prefabs = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs();
            NativeArray<ServiceCompanyData> nativeArray = entityQuery.ToComponentDataArray<ServiceCompanyData>(Allocator.TempJob);
            NativeArray<IndustrialProcessData> nativeArray2 = entityQuery.ToComponentDataArray<IndustrialProcessData>(Allocator.TempJob);
            NativeArray<WorkplaceData> nativeArray3 = entityQuery.ToComponentDataArray<WorkplaceData>(Allocator.TempJob);
            NativeArray<IndustrialProcessData> nativeArray4 = entityQuery2.ToComponentDataArray<IndustrialProcessData>(Allocator.TempJob);
            NativeArray<WorkplaceData> nativeArray5 = entityQuery2.ToComponentDataArray<WorkplaceData>(Allocator.TempJob);
            NativeArray<Entity> nativeArray6 = entityQuery2.ToEntityArray(Allocator.TempJob);
            NativeArray<EconomyParameterData> nativeArray7 = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EconomyParameterData>()).ToComponentDataArray<EconomyParameterData>(Allocator.TempJob);
            EconomyParameterData economyParameters = nativeArray7[0];
            Debug.Log("Company data per cell");
            for (int i = 0; i < nativeArray.Length; i++)
            {
                ServiceCompanyData serviceData = nativeArray[i];
                IndustrialProcessData industrialProcessData = nativeArray2[i];
                BuildingData buildingData = default;
                buildingData.m_LotSize = new int2(100, 10);
                BuildingData buildingData2 = buildingData;
                ServiceAvailable serviceAvailable = default;
                serviceAvailable.m_MeanPriority = 0.5f;
                ServiceAvailable service = serviceAvailable;
                WorkplaceData workplaceData = nativeArray3[i];
                SpawnableBuildingData spawnableBuildingData = default;
                spawnableBuildingData.m_Level = 1;
                SpawnableBuildingData spawnableData = spawnableBuildingData;
                spawnableBuildingData = default;
                spawnableBuildingData.m_Level = 5;
                SpawnableBuildingData spawnableData2 = spawnableBuildingData;
                float3 tradeCosts = EconomyUtils.BuildPseudoTradeCost(5000f, industrialProcessData, resourceDatas, prefabs);
                string text = "C " + EconomyUtils.GetName(industrialProcessData.m_Output.m_Resource) + ": ";
                int workers = Mathf.RoundToInt(serviceData.m_MaxWorkersPerCell * 1000f);
                int num = ServiceCompanySystem.EstimateDailyProfit(workers, service, serviceData, buildingData2, industrialProcessData, ref economyParameters, workplaceData, spawnableData, prefabs, resourceDatas, tradeCosts);
                int num2 = ServiceCompanySystem.EstimateDailyProfit(workers, service, serviceData, buildingData2, industrialProcessData, ref economyParameters, workplaceData, spawnableData2, prefabs, resourceDatas, tradeCosts);
                int num3 = ServiceCompanySystem.EstimateDailyProduction(1f, workers, spawnableData.m_Level, serviceData, workplaceData, ref economyParameters, industrialProcessData.m_Output.m_Resource, 1000);
                int num4 = ServiceCompanySystem.EstimateDailyProduction(1f, workers, spawnableData2.m_Level, serviceData, workplaceData, ref economyParameters, industrialProcessData.m_Output.m_Resource, 1000);
                int num9 = WorkProviderSystem.CalculateTotalWage(workers, workplaceData.m_Complexity, 1, economyParameters);
                int num10 = WorkProviderSystem.CalculateTotalWage(workers, workplaceData.m_Complexity, 5, economyParameters);
                text = text + "Production " + num3 / 1000f + "|" + num4 / 1000f + ", wage " + num9 + "|" + num10 + ", profit " + num / 1000f + "|" + num2 / 1000f + "), wpu = " + serviceData.m_WorkPerUnit;
                Debug.Log(text);
            }
            for (int j = 0; j < nativeArray4.Length; j++)
            {
                IndustrialProcessData industrialProcessData2 = nativeArray4[j];
                BuildingData buildingData = default;
                buildingData.m_LotSize = new int2(100, 10);
                BuildingData buildingData3 = buildingData;
                float3 tradeCosts2 = EconomyUtils.BuildPseudoTradeCost(5000f, industrialProcessData2, resourceDatas, prefabs);
                WorkplaceData workplaceData2 = nativeArray5[j];
                SpawnableBuildingData spawnableBuildingData = default;
                spawnableBuildingData.m_Level = 1;
                SpawnableBuildingData building = spawnableBuildingData;
                spawnableBuildingData = default;
                spawnableBuildingData.m_Level = 5;
                SpawnableBuildingData building2 = spawnableBuildingData;
                string text2 = "I " + EconomyUtils.GetName(industrialProcessData2.m_Input1.m_Resource) + " => " + EconomyUtils.GetName(industrialProcessData2.m_Output.m_Resource) + ": ";
                int num5 = 0;
                float num6 = 0f;
                int num7;
                float num8;
                int num9;
                int num10;
                if (World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<ExtractorCompanyData>(nativeArray6[j]))
                {
                    int fittingWorkers = ExtractorAISystem.GetFittingWorkers(10000f, 1f, industrialProcessData2);
                    num7 = ExtractorCompanySystem.EstimateDailyProduction(1f, fittingWorkers, 1, workplaceData2, industrialProcessData2, ref economyParameters);
                    num8 = ExtractorCompanySystem.EstimateDailyProfit(num7, fittingWorkers, industrialProcessData2, ref economyParameters, workplaceData2, building, prefabs, resourceDatas);
                    num7 = Mathf.RoundToInt(num7 * 6.4f);
                    num8 *= 6.4f;
                    num9 = WorkProviderSystem.CalculateTotalWage(fittingWorkers, workplaceData2.m_Complexity, 1, economyParameters);
                    num10 = WorkProviderSystem.CalculateTotalWage(fittingWorkers, workplaceData2.m_Complexity, 5, economyParameters);
                }
                else
                {
                    int num11 = Mathf.RoundToInt(industrialProcessData2.m_MaxWorkersPerCell * 1000f);
                    num8 = ProcessingCompanySystem.EstimateDailyProfit(num11, industrialProcessData2, buildingData3, ref economyParameters, tradeCosts2, workplaceData2, building, prefabs, resourceDatas);
                    num6 = ProcessingCompanySystem.EstimateDailyProfit(num11, industrialProcessData2, buildingData3, ref economyParameters, tradeCosts2, workplaceData2, building2, prefabs, resourceDatas);
                    num7 = ProcessingCompanySystem.EstimateDailyProduction(1f, num11, building.m_Level, workplaceData2, industrialProcessData2, ref economyParameters);
                    num5 = ProcessingCompanySystem.EstimateDailyProduction(1f, num11, building2.m_Level, workplaceData2, industrialProcessData2, ref economyParameters);
                    num9 = WorkProviderSystem.CalculateTotalWage(num11, workplaceData2.m_Complexity, 1, economyParameters);
                    num10 = WorkProviderSystem.CalculateTotalWage(num11, workplaceData2.m_Complexity, 5, economyParameters);
                }
                text2 = text2 + "Production " + num7 / 1000f + " | " + num5 / 1000f + ", wage " + num9 + "|" + num10 + ", profit " + num8 / 1000f + "|" + num6 / 1000f + "), wpu = " + industrialProcessData2.m_WorkPerUnit;
                Debug.Log(text2);
            }
            nativeArray.Dispose();
            nativeArray2.Dispose();
            nativeArray3.Dispose();
            nativeArray6.Dispose();
            nativeArray4.Dispose();
            nativeArray5.Dispose();
            nativeArray7.Dispose();

            // end of original code
            return false;
        }*/


    }
}
