using Game.Simulation;
using HarmonyLib;
using UnityEngine;
using Unity.Mathematics;
using Game.Prefabs;
using System.Reflection;
using Unity.Jobs;
using Game.Routes;
using UnityEngine.Experimental.Rendering;
using System;
using Unity.Collections;
using UnityEngine.Rendering;
using Game;
using Game.Serialization;
using Unity.Entities;

namespace ExtMap57km.Patches
{
    [HarmonyPatch]
    internal static class TerrainSystemMethodPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TerrainSystem), "GetTerrainBounds")]
        static void TerrainSystemGetTerrainBounds_Mod(TerrainSystem __instance, ref Bounds __result)
        {
            //Debug.Log($"Method, __instance:{__instance.GetType().FullName}, GetTerrainBounds:{__result}");
            //Mod.log.Info($"TerrainSystemGetTerrainBounds:{__result}");
            __result = new Bounds(center:new float3(0f,0f,0f), size:new float3(57344f, 4096, 57344f));
            
        }


        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TerrainSystem), "GetHeightData")]
        static void TerrainSystem_Mod(TerrainSystem __instance, ref TerrainHeightData __result)
        {
            //Debug.Log($"Method, __instance:{__instance.GetType().FullName}, TerrainHeightData:{__result}");
            //Mod.log.Info($"TerrainSystemGetHeightData:{__result}");
            //___kDefaultMapSize = new float2(57344f, 57344f);
            //Traverse.Create(__instance).Field("kDefaultMapSize").SetValue(new float2(57344f,57344f)); 

            //float3 scale4x = new float3(__result.scale.x * 4, __result.scale.y, __result.scale.z * 4);
            //float3 offset4x = new float3(__result.offset.x / 4, __result.offset.y, __result.offset.z / 4);
            //float3 scale4x = new float3(__result.scale.x / 4, __result.scale.y, __result.scale.z / 4);
            //float3 offset4x = new float3(__result.offset.x - 0.5f / scale4x.x, __result.offset.y, __result.offset.z - 0.5f / scale4x.z);
            //__result = new TerrainHeightData(__result.heights, __result.resolution, scale4x, offset4x);

            //float3 @float4x = new float3(57344f, math.max(1f, __instance.heightScaleOffset.x), 57344f);
            //float3 scale4x = new float3(__result.resolution.x, __result.resolution.y - 1, __result.resolution.z) / @float4x;
            //float3 offset4x = -__instance.positionOffset;
            //offset4x.xz -= 0.5f / scale4x.xz;

            float3 scale4x = new float3(__result.scale.x / 4, __result.scale.y, __result.scale.z / 4);
            //float3 offset4x = new float3(__result.offset.x + 0.625f / __result.scale.x, __result.offset.y, __result.offset.z + 0.625f / __result.scale.z);
            float3 offset4x = new float3(__result.offset.x + 0.625f / __result.scale.x, __result.offset.y, __result.offset.z + 0.625f / __result.scale.z);
            __result =  new TerrainHeightData(__result.heights, __result.resolution, scale4x, offset4x);

        }//method;
        */

        //新方法；
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TerrainSystem), "GetHeightData")]
        static void TerrainSystem_Mod(TerrainSystem __instance, ref TerrainHeightData __result)
        {
            //float3 scale4x = new float3(__result.scale.x * 4, __result.scale.y, __result.scale.z * 4);
            //float3 offset4x = new float3(__result.offset.x / 4, __result.offset.y, __result.offset.z / 4);
            //float3 scale4x = new float3(__result.scale.x / 4, __result.scale.y, __result.scale.z / 4);
            //float3 offset4x = new float3(__result.offset.x - 0.5f / scale4x.x, __result.offset.y, __result.offset.z - 0.5f / scale4x.z);
            //__result = new TerrainHeightData(__result.heights, __result.resolution, scale4x, offset4x);

            //float3 @float4x = new float3(57344f, math.max(1f, __instance.heightScaleOffset.x), 57344f);
            //float3 scale4x = new float3(__result.resolution.x, __result.resolution.y - 1, __result.resolution.z) / @float4x;
            //float3 offset4x = -__instance.positionOffset;
            //offset4x.xz -= 0.5f / scale4x.xz;
            NativeArray<ushort> cpuheights = __result.heights;
            int3 resolution = __result.resolution;
            float3 @float = new float3(57344f, math.max(1f, __instance.heightScaleOffset.x), 57344f);
            float3 scale = new float3(resolution.x, resolution.y - 1, resolution.z) / @float;
            float3 offset = -__instance.positionOffset;
            offset.xz -= 0.5f / scale.xz;
            __result = new TerrainHeightData(cpuheights, resolution, scale, offset);
        }//method;


        ///bepinex preloader模式下禁用；
        /*
        [HarmonyPatch(typeof(Game.Simulation.TerrainSystem), "FinalizeTerrainData")]
        [HarmonyPrefix]
        public static void TerrainSystem_FinalizeTerrainData(TerrainSystem __instance,Texture2D map, Texture2D worldMap, float2 heightScaleOffset, ref float2 inMapCorner, ref float2 inMapSize, ref float2 inWorldCorner, ref float2 inWorldSize, float2 inWorldHeightMinMax)
        {
            //Patcher.Instance.Log.Info($"TerrainFinalized Modded!");
            
            inMapSize *= 4;
            inMapCorner *= 4;
            inWorldSize *= 4;
            inWorldCorner *= 4;

        }*/
                
    }//class;

}//namespace;

