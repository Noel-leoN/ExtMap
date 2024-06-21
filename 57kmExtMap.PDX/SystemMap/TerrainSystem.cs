using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.AssetPipeline.Native;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Mathematics;
using Colossal.Rendering;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Assets;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Rendering.Utilities;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;


namespace ExtMap57km.Systems
{
	[FormerlySerializedAs("Colossal.Terrain.TerrainSystem, Game")]
	//[CompilerGenerated]
	public partial class TerrainSystem : GameSystemBase, IDefaultSerializable, ISerializable
	{
		public static class ShaderID
		{
			public static readonly int _BlurTempHorz = Shader.PropertyToID("_BlurTempHorz");

			public static readonly int _AvgTerrainHeightsTemp = Shader.PropertyToID("_AvgTerrainHeightsTemp");

			public static readonly int _DebugSmooth = Shader.PropertyToID("_DebugSmooth");

			public static readonly int _Heightmap = Shader.PropertyToID("_Heightmap");

			public static readonly int _BrushTexture = Shader.PropertyToID("_BrushTexture");

			public static readonly int _WorldTexture = Shader.PropertyToID("_WorldTexture");

			public static readonly int _WaterTexture = Shader.PropertyToID("_WaterTexture");

			public static readonly int _Range = Shader.PropertyToID("_Range");

			public static readonly int _CenterSizeRotation = Shader.PropertyToID("_CenterSizeRotation");

			public static readonly int _Dims = Shader.PropertyToID("_Dims");

			public static readonly int _BrushData = Shader.PropertyToID("_BrushData");

			public static readonly int _BrushData2 = Shader.PropertyToID("_BrushData2");

			public static readonly int _ClampArea = Shader.PropertyToID("_ClampArea");

			public static readonly int _WorldOffsetScale = Shader.PropertyToID("_WorldOffsetScale");

			public static readonly int _EdgeMaxDifference = Shader.PropertyToID("_EdgeMaxDifference");

			public static readonly int _BuildingLotID = Shader.PropertyToID("_BuildingLots");

			public static readonly int _LanesID = Shader.PropertyToID("_Lanes");

			public static readonly int _TrianglesID = Shader.PropertyToID("_Triangles");

			public static readonly int _EdgesID = Shader.PropertyToID("_Edges");

			public static readonly int _HeightmapID = Shader.PropertyToID("_BaseHeightMap");

			public static readonly int _TerrainScaleOffsetID = Shader.PropertyToID("_TerrainScaleOffset");

			public static readonly int _MapOffsetScaleID = Shader.PropertyToID("_MapOffsetScale");

			public static readonly int _BrushID = Shader.PropertyToID("_Brush");

			public static readonly int _CascadeRangesID = Shader.PropertyToID("colossal_TerrainCascadeRanges");

			public static readonly int _CascadeOffsetScale = Shader.PropertyToID("_CascadeOffsetScale");

			public static readonly int _HeightScaleOffset = Shader.PropertyToID("_HeightScaleOffset");

			public static readonly int _RoadData = Shader.PropertyToID("_RoadData");

			public static readonly int _ClipOffset = Shader.PropertyToID("_ClipOffset");
		}

		public struct BuildingLotDraw
		{
			public float2x4 m_HeightsX;

			public float2x4 m_HeightsZ;

			public float3 m_FlatX0;

			public float3 m_FlatZ0;

			public float3 m_FlatX1;

			public float3 m_FlatZ1;

			public float3 m_Position;

			public float3 m_AxisX;

			public float3 m_AxisZ;

			public float2 m_Size;

			public float4 m_MinLimit;

			public float4 m_MaxLimit;

			public float m_Circular;

			public float m_SmoothingWidth;
		}

		public struct LaneSection
		{
			public Bounds2 m_Bounds;

			public float4x3 m_Left;

			public float4x3 m_Right;

			public float3 m_MinOffset;

			public float3 m_MaxOffset;

			public float2 m_ClipOffset;

			public float m_WidthOffset;

			public LaneFlags m_Flags;
		}

		public struct LaneDraw
		{
			public float4x3 m_Left;

			public float4x3 m_Right;

			public float4 m_MinOffset;

			public float4 m_MaxOffset;

			public float2 m_WidthOffset;
		}

		public struct AreaTriangle
		{
			public float2 m_PositionA;

			public float2 m_PositionB;

			public float2 m_PositionC;

			public float2 m_NoiseSize;

			public float m_HeightDelta;
		}

		public struct AreaEdge
		{
			public float2 m_PositionA;

			public float2 m_PositionB;

			public float2 m_Angles;

			public float m_SideOffset;
		}

		[Flags]
		public enum LaneFlags
		{
			ShiftTerrain = 1,
			ClipTerrain = 2,
			MiddleLeft = 4,
			MiddleRight = 8,
			InverseClipOffset = 0x10
		}

		private class CascadeCullInfo
		{
			public JobHandle m_BuildingHandle;

			public NativeList<BuildingLotDraw> m_BuildingRenderList;

			public Material m_LotMaterial;

			public JobHandle m_LaneHandle;

			public NativeList<LaneDraw> m_LaneRenderList;

			public Material m_LaneMaterial;

			public JobHandle m_AreaHandle;

			public NativeList<AreaTriangle> m_TriangleRenderList;

			public NativeList<AreaEdge> m_EdgeRenderList;

			public Material m_AreaMaterial;

			public CascadeCullInfo(Material building, Material lane, Material area)
			{
				this.m_LotMaterial = new Material(building);
				this.m_LaneMaterial = new Material(lane);
				this.m_AreaMaterial = new Material(area);
				this.m_BuildingRenderList = default(NativeList<BuildingLotDraw>);
				this.m_BuildingHandle = default(JobHandle);
				this.m_LaneHandle = default(JobHandle);
				this.m_LaneRenderList = default(NativeList<LaneDraw>);
				this.m_TriangleRenderList = default(NativeList<AreaTriangle>);
				this.m_EdgeRenderList = default(NativeList<AreaEdge>);
				this.m_AreaHandle = default(JobHandle);
			}
		}

		private struct ClipMapDraw
		{
			public float4x3 m_Left;

			public float4x3 m_Right;

			public float m_Height;

			public float m_OffsetFactor;
		}

		private class TerrainMinMaxMap
		{
			private RenderTexture[] m_IntermediateTex;

			private RenderTexture m_DownsampledDetail;

			private RenderTexture m_ResultTex;

			public NativeArray<half4> MinMaxMap;

			private NativeArray<half4> m_UpdateBuffer;

			private AsyncGPUReadbackRequest m_Current;

			private ComputeShader m_Shader;

			private int2 m_IntermediateSize;

			private int2 m_ResultSize;

			private int4 m_UpdatedArea;

			private int4 m_DebugArea;

			private bool m_Pending;

			private bool m_Updated;

			private bool m_Valid;

			private bool m_Partial;

			private int m_Steps;

			private int m_DetailSteps;

			private int m_BlockSize;

			private int m_DetailBlockSize;

			private int m_ID_WorldTexture;

			private int m_ID_DetailTexture;

			private int m_ID_UpdateArea;

			private int m_ID_WorldOffsetScale;

			private int m_ID_DetailOffsetScale;

			private int m_ID_WorldTextureSizeInvSize;

			private int m_ID_Result;

			private int m_KernalCSTerainMinMax;

			private int m_KernalCSWorldTerainMinMax;

			private int m_KernalCSDownsampleMinMax;

			private int2 m_InitValues = int2.zero;

			private Texture m_AsyncNeeded;

			private List<int4> m_UpdatesRequested = new List<int4>();

			private TerrainSystem m_TerrainSystem;

			private JobHandle m_UpdateJob;

			public bool isValid => this.m_Valid;

			public bool isUpdated => this.m_Updated;

			public int size => this.m_ResultSize.x;

			public int4 UpdateArea => this.m_UpdatedArea;

			private RenderTexture CreateRenderTexture(string name, int2 size, bool compact)
			{
				RenderTexture renderTexture = new RenderTexture(size.x, size.y, 0, compact ? GraphicsFormat.R16G16_SFloat : GraphicsFormat.R16G16B16A16_SFloat);
				renderTexture.name = name;
				renderTexture.hideFlags = HideFlags.DontSave;
				renderTexture.enableRandomWrite = true;
				renderTexture.wrapMode = TextureWrapMode.Clamp;
				renderTexture.filterMode = FilterMode.Bilinear;
				renderTexture.Create();
				return renderTexture;
			}

			public void Init(int size, int original)
			{
				if (this.m_IntermediateTex != null && size == this.m_InitValues.x && original == this.m_InitValues.y)
				{
					this.m_UpdateJob.Complete();
					this.m_UpdateJob = default(JobHandle);
					return;
				}
				this.Dispose();
				this.m_InitValues = new int2(size, original);
				this.m_IntermediateSize = original / 2;
				this.m_ResultSize = size;
				if (this.m_ResultSize.x > this.m_IntermediateSize.x || this.m_ResultSize.y > this.m_IntermediateSize.y)
				{
					this.m_ResultSize = this.m_IntermediateSize;
					this.m_Steps = 1;
				}
				else
				{
					this.m_Steps = math.floorlog2(original) + 1 - (math.floorlog2(size) + 1);
				}
				int num = math.max(math.floorlog2(original) - 2, 1);
				int num2 = (int)math.pow(2f, num - 1);
				this.m_DetailSteps = math.floorlog2(original) + 1 - num;
				this.m_BlockSize = (int)math.pow(2f, this.m_Steps);
				this.m_DetailBlockSize = (int)math.pow(2f, this.m_DetailSteps);
				this.m_IntermediateTex = new RenderTexture[2];
				this.m_IntermediateTex[0] = this.CreateRenderTexture("HeightMinMax_Setup0", this.m_IntermediateSize, compact: true);
				this.m_IntermediateTex[1] = this.CreateRenderTexture("HeightMinMax_Setup1", this.m_IntermediateSize / 2, compact: true);
				this.m_DownsampledDetail = this.CreateRenderTexture("HeightMinMax_Detail", num2, compact: true);
				this.m_ResultTex = this.CreateRenderTexture("HeightMinMax_Result", this.m_ResultSize.x, compact: false);
				this.m_Valid = false;
				this.m_Partial = false;
				this.m_Updated = false;
				this.m_Pending = false;
				this.MinMaxMap = new NativeArray<half4>(size * size, Allocator.Persistent);
				this.m_UpdateBuffer = new NativeArray<half4>(size * size, Allocator.Persistent);
				this.m_Shader = Resources.Load<ComputeShader>("TerrainMinMax");
				this.m_KernalCSTerainMinMax = this.m_Shader.FindKernel("CSTerainGenerateMinMax");
				this.m_KernalCSWorldTerainMinMax = this.m_Shader.FindKernel("CSTerainWorldGenerateMinMax");
				this.m_KernalCSDownsampleMinMax = this.m_Shader.FindKernel("CSDownsampleMinMax");
				this.m_ID_WorldTexture = Shader.PropertyToID("_WorldTexture");
				this.m_ID_DetailTexture = Shader.PropertyToID("_DetailHeightTexture");
				this.m_ID_UpdateArea = Shader.PropertyToID("_UpdateArea");
				this.m_ID_WorldOffsetScale = Shader.PropertyToID("_WorldOffsetScale");
				this.m_ID_DetailOffsetScale = Shader.PropertyToID("_DetailOffsetScale");
				this.m_ID_WorldTextureSizeInvSize = Shader.PropertyToID("_WorldTextureSizeInvSize");
				this.m_ID_Result = Shader.PropertyToID("ResultMinMax");
			}

			public void Debug(TerrainSystem terrain, Texture map, Texture worldMap)
			{
				using CommandBuffer commandBuffer = new CommandBuffer
				{
					name = "DebugMinMax"
				};
				commandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
				this.RequestUpdate(terrain, map, worldMap, this.m_DebugArea, commandBuffer, debug: true);
				Graphics.ExecuteCommandBuffer(commandBuffer);
				commandBuffer.Dispose();
			}

			public void UpdateMap(TerrainSystem terrain, Texture map, Texture worldMap)
			{
				this.m_Valid = false;
				this.m_Updated = false;
				this.m_Partial = false;
				this.m_UpdateJob.Complete();
				this.m_UpdateJob = default(JobHandle);
				if (this.m_Pending && !this.m_Current.done)
				{
					this.m_Current.WaitForCompletion();
				}
				using (CommandBuffer commandBuffer = new CommandBuffer
				{
					name = "TerrainMinMaxInit"
				})
				{
					this.m_AsyncNeeded = this.RequestUpdate(terrain, map, worldMap, new int4(0, 0, map.width, map.height), commandBuffer);
					Graphics.ExecuteCommandBuffer(commandBuffer);
				}
				this.m_Pending = true;
			}

			private int4 RemapArea(int4 area, int blockSize, int textureWidth, int textureHeight)
			{
				int2 @int = area.xy / new int2(blockSize, blockSize) * new int2(blockSize, blockSize);
				area.zw += area.xy - @int;
				area.xy = @int;
				area.zw = (area.zw + new int2(blockSize - 1, blockSize - 1)) / new int2(blockSize, blockSize) * new int2(blockSize, blockSize);
				if (area.z > textureWidth)
				{
					area.z = textureWidth;
				}
				if (area.x + area.z > textureWidth)
				{
					area.x = textureWidth - area.z;
				}
				if (area.w > textureHeight)
				{
					area.w = textureHeight;
				}
				if (area.y + area.w > textureHeight)
				{
					area.y = textureHeight - area.w;
				}
				return area;
			}

			public bool RequestUpdate(TerrainSystem terrain, Texture map, Texture worldMap, int4 area)
			{
				if (this.m_Pending || this.m_Updated)
				{
					this.m_UpdatesRequested.Add(area);
					this.m_TerrainSystem = terrain;
					return false;
				}
				int2 @int = area.xy / new int2(this.m_BlockSize, this.m_BlockSize) * new int2(this.m_BlockSize, this.m_BlockSize);
				area.zw += area.xy - @int;
				area.xy = @int;
				area.zw = (area.zw + new int2(this.m_BlockSize - 1, this.m_BlockSize - 1)) / new int2(this.m_BlockSize, this.m_BlockSize) * new int2(this.m_BlockSize, this.m_BlockSize);
				if (area.z > map.width)
				{
					area.z = map.width;
				}
				if (area.x + area.z > map.width)
				{
					area.x = map.width - area.z;
				}
				if (area.w > map.height)
				{
					area.w = map.height;
				}
				if (area.y + area.w > map.height)
				{
					area.y = map.height - area.w;
				}
				area = this.RemapArea(area, this.m_BlockSize, (worldMap != null) ? worldMap.width : map.width, (worldMap != null) ? worldMap.height : map.height);
				using (CommandBuffer commandBuffer = new CommandBuffer
				{
					name = "TerainMinMaxUpdate"
				})
				{
					commandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
					this.m_AsyncNeeded = this.RequestUpdate(terrain, map, worldMap, area, commandBuffer);
					this.m_Pending = true;
					this.m_Partial = true;
					Graphics.ExecuteCommandBuffer(commandBuffer);
				}
				return true;
			}

			private bool Downsample(CommandBuffer commandBuffer, Texture target, int steps, int4 area, ref int4 updated)
			{
				if (steps == 1)
				{
					return false;
				}
				float4 @float = new float4(area.x, area.y, area.x / 2, area.y / 2);
				int num = 1;
				int2 @int = area.zw / 2;
				int4 int2 = area / 2;
				int2.zw = math.max(int2.zw, new int2(1, 1));
				@int.xy = math.max(@int.xy, new int2(1, 1));
				updated = area / 2;
				updated.zw = math.max(updated.zw, new int2(1, 1));
				Texture texture = this.m_IntermediateTex[1];
				Texture texture2 = this.m_IntermediateTex[0];
				do
				{
					Texture texture3 = texture2;
					texture2 = texture;
					texture = texture3;
					if (num == steps - 1)
					{
						texture2 = target;
					}
					@float.xy = int2.xy;
					int2 /= 2;
					int2.zw = math.max(int2.zw, new int2(1, 1));
					@float.zw = int2.xy;
					@int /= 2;
					@int.xy = math.max(@int.xy, new int2(1, 1));
					updated /= 2;
					updated.zw = math.max(updated.zw, new int2(1, 1));
					commandBuffer.SetComputeVectorParam(this.m_Shader, this.m_ID_UpdateArea, @float);
					commandBuffer.SetComputeTextureParam(this.m_Shader, this.m_KernalCSDownsampleMinMax, this.m_ID_WorldTexture, texture);
					commandBuffer.SetComputeTextureParam(this.m_Shader, this.m_KernalCSDownsampleMinMax, this.m_ID_Result, texture2);
					commandBuffer.DispatchCompute(this.m_Shader, this.m_KernalCSDownsampleMinMax, (@int.x + 7) / 8, (@int.y + 7) / 8, 1);
				}
				while (++num < steps);
				return true;
			}

			private Texture RequestUpdate(TerrainSystem terrain, Texture map, Texture worldMap, int4 area, CommandBuffer commandBuffer, bool debug = false)
			{
				if (!debug)
				{
					this.m_DebugArea = area;
				}
				bool num = worldMap != null;
				float4 @float = new float4(area.x, area.y, area.x / 2, area.y / 2);
				commandBuffer.SetComputeVectorParam(val: new float4(terrain.heightScaleOffset.y, terrain.heightScaleOffset.x, 0f, 0f), computeShader: this.m_Shader, nameID: this.m_ID_WorldOffsetScale);
				int4 updated = int4.zero;
				if (num)
				{
					float4 float2 = new float4((terrain.worldOffset - terrain.playableOffset) / terrain.playableArea, 1f / (float)worldMap.width * (terrain.worldSize / terrain.playableArea));
					float4 x = new float4(area.xy * float2.zw + float2.xy, (area.xy + area.zw) * float2.zw + float2.xy);
					if (!(x.x > 1f) && !(x.z < 0f) && !(x.y > 1f) && !(x.w < 0f))
					{
						x = math.clamp(x, float4.zero, new float4(1f, 1f, 1f, 1f));
						x.zw -= x.xy;
						x.xy = math.floor(x.xy * new float2(map.width, map.height));
						x.zw = math.max(math.ceil(x.zw * new float2(map.width, map.height)), new float2(1f, 1f));
						int4 area2 = this.RemapArea(new int4((int)x.x, (int)x.y, (int)x.z, (int)x.w), this.m_DetailBlockSize, map.width, map.height);
						commandBuffer.SetComputeVectorParam(val: new float4(x.x, x.y, x.x / 2f, x.y / 2f), computeShader: this.m_Shader, nameID: this.m_ID_UpdateArea);
						commandBuffer.SetComputeTextureParam(this.m_Shader, this.m_KernalCSTerainMinMax, this.m_ID_WorldTexture, map);
						commandBuffer.SetComputeTextureParam(this.m_Shader, this.m_KernalCSTerainMinMax, this.m_ID_Result, this.m_IntermediateTex[0]);
						commandBuffer.DispatchCompute(this.m_Shader, this.m_KernalCSTerainMinMax, (area2.z + 7) / 8, (area2.w + 7) / 8, 1);
						this.Downsample(commandBuffer, this.m_DownsampledDetail, this.m_DetailSteps, area2, ref updated);
					}
					commandBuffer.SetComputeVectorParam(val: new float4(this.m_DownsampledDetail.width, this.m_DownsampledDetail.height, 1f / (float)this.m_DownsampledDetail.width, 1f / (float)this.m_DownsampledDetail.height), computeShader: this.m_Shader, nameID: this.m_ID_WorldTextureSizeInvSize);
					commandBuffer.SetComputeVectorParam(this.m_Shader, this.m_ID_DetailOffsetScale, float2);
					commandBuffer.SetComputeVectorParam(this.m_Shader, this.m_ID_UpdateArea, @float);
					commandBuffer.SetComputeTextureParam(this.m_Shader, this.m_KernalCSWorldTerainMinMax, this.m_ID_WorldTexture, worldMap);
					commandBuffer.SetComputeTextureParam(this.m_Shader, this.m_KernalCSWorldTerainMinMax, this.m_ID_DetailTexture, this.m_DownsampledDetail);
					commandBuffer.SetComputeTextureParam(this.m_Shader, this.m_KernalCSWorldTerainMinMax, this.m_ID_Result, (this.m_Steps == 1) ? this.m_ResultTex : this.m_IntermediateTex[0]);
					commandBuffer.DispatchCompute(this.m_Shader, this.m_KernalCSWorldTerainMinMax, (area.z + 7) / 8, (area.w + 7) / 8, 1);
				}
				else
				{
					commandBuffer.SetComputeVectorParam(this.m_Shader, this.m_ID_UpdateArea, @float);
					commandBuffer.SetComputeTextureParam(this.m_Shader, this.m_KernalCSTerainMinMax, this.m_ID_WorldTexture, map);
					commandBuffer.SetComputeTextureParam(this.m_Shader, this.m_KernalCSTerainMinMax, this.m_ID_Result, (this.m_Steps == 1) ? this.m_ResultTex : this.m_IntermediateTex[0]);
					commandBuffer.DispatchCompute(this.m_Shader, this.m_KernalCSTerainMinMax, (area.z + 7) / 8, (area.w + 7) / 8, 1);
				}
				if (!debug)
				{
					this.m_UpdatedArea = area / 2;
					this.m_UpdatedArea.zw = math.max(this.m_UpdatedArea.zw, new int2(1, 1));
				}
				this.Downsample(commandBuffer, this.m_ResultTex, this.m_Steps, area, ref updated);
				if (!debug)
				{
					this.m_UpdatedArea = updated;
				}
				return this.m_ResultTex;
			}

			public unsafe void Update()
			{
				this.m_UpdateJob.Complete();
				this.m_UpdateJob = default(JobHandle);
				if (this.m_Pending)
				{
					if (this.m_AsyncNeeded != null)
					{
						if (this.m_Partial)
						{
							this.m_Current = AsyncGPUReadback.RequestIntoNativeArray(ref this.m_UpdateBuffer, this.m_AsyncNeeded, 0, this.m_UpdatedArea.x, this.m_UpdatedArea.z, this.m_UpdatedArea.y, this.m_UpdatedArea.w, 0, 1, GraphicsFormat.R16G16B16A16_SFloat, delegate(AsyncGPUReadbackRequest request)
							{
								this.m_Pending = false;
								if (!request.hasError)
								{
									this.m_Valid = true;
									this.m_Updated = true;
									if (this.m_Partial)
									{
										int num2 = this.m_UpdatedArea.y * this.m_ResultSize.x + this.m_UpdatedArea.x;
										for (int k = 0; k < this.m_UpdatedArea.w; k++)
										{
											UnsafeUtility.MemCpy((byte*)this.MinMaxMap.GetUnsafePtr() + (long)num2 * (long)sizeof(float2), (byte*)this.m_UpdateBuffer.GetUnsafePtr() + (long)(k * this.m_UpdatedArea.z) * (long)sizeof(float2), 8 * this.m_UpdatedArea.z);
											num2 += this.m_ResultSize.x;
										}
										this.m_Partial = false;
									}
								}
							});
						}
						else
						{
							this.m_Current = AsyncGPUReadback.RequestIntoNativeArray(ref this.MinMaxMap, this.m_AsyncNeeded, 0, 0, this.m_ResultSize.x, 0, this.m_ResultSize.y, 0, 1, GraphicsFormat.R16G16B16A16_SFloat, delegate(AsyncGPUReadbackRequest request)
							{
								this.m_Pending = false;
								if (!request.hasError)
								{
									this.m_Valid = true;
									this.m_Updated = true;
									if (this.m_Partial)
									{
										int num = this.m_UpdatedArea.y * this.m_ResultSize.x + this.m_UpdatedArea.x;
										for (int j = 0; j < this.m_UpdatedArea.w; j++)
										{
											UnsafeUtility.MemCpy((byte*)this.MinMaxMap.GetUnsafePtr() + (long)num * (long)sizeof(float2), (byte*)this.m_UpdateBuffer.GetUnsafePtr() + (long)(j * this.m_UpdatedArea.z) * (long)sizeof(float2), 8 * this.m_UpdatedArea.z);
											num += this.m_ResultSize.x;
										}
										this.m_Partial = false;
									}
								}
							});
						}
						this.m_AsyncNeeded = null;
					}
					else
					{
						this.m_Current.Update();
					}
				}
				else if (!this.m_Updated && this.m_UpdatesRequested.Count > 0)
				{
					int4 area = this.m_UpdatesRequested[0];
					area.zw += area.xy;
					for (int i = 1; i < this.m_UpdatesRequested.Count; i++)
					{
						area.xy = math.min(area.xy, this.m_UpdatesRequested[i].xy);
						area.zw = math.max(area.zw, this.m_UpdatesRequested[i].xy + this.m_UpdatesRequested[i].zw);
					}
					area.zw -= area.xy;
					area.zw = math.clamp(area.zw, new int2(1, 1), new int2(this.m_ResultSize.x, this.m_ResultSize.y));
					this.RequestUpdate(this.m_TerrainSystem, this.m_TerrainSystem.heightmap, this.m_TerrainSystem.worldHeightmap, area);
					this.m_TerrainSystem = null;
					this.m_UpdatesRequested.Clear();
				}
			}

			private unsafe void UpdateMinMax(AsyncGPUReadbackRequest request)
			{
				this.m_Pending = false;
				if (request.hasError)
				{
					return;
				}
				this.m_Valid = true;
				this.m_Updated = true;
				if (this.m_Partial)
				{
					int num = this.m_UpdatedArea.y * this.m_ResultSize.x + this.m_UpdatedArea.x;
					for (int i = 0; i < this.m_UpdatedArea.w; i++)
					{
						UnsafeUtility.MemCpy((byte*)this.MinMaxMap.GetUnsafePtr() + (long)num * (long)sizeof(float2), (byte*)this.m_UpdateBuffer.GetUnsafePtr() + (long)(i * this.m_UpdatedArea.z) * (long)sizeof(float2), 8 * this.m_UpdatedArea.z);
						num += this.m_ResultSize.x;
					}
					this.m_Partial = false;
				}
			}

			public int4 ComsumeUpdate()
			{
				this.m_Updated = false;
				return this.m_UpdatedArea;
			}

			public float2 GetMinMax(int4 area)
			{
				float2 result = new float2(999999f, 0f);
				for (int i = 0; i < area.z * area.w; i++)
				{
					int index = (area.y + i / area.z) * this.m_ResultSize.x + area.x + i % area.z;
					result.x = math.min(result.x, this.MinMaxMap[index].x);
					result.y = math.max(result.y, this.MinMaxMap[index].y);
				}
				return result;
			}

			public void RegisterJobUpdate(JobHandle handle)
			{
				this.m_UpdateJob = JobHandle.CombineDependencies(handle, this.m_UpdateJob);
			}

			public void Dispose()
			{
				this.m_Current.WaitForCompletion();
				this.m_Pending = false;
				this.m_AsyncNeeded = null;
				this.m_Updated = false;
				if (this.m_IntermediateTex != null)
				{
					RenderTexture[] intermediateTex = this.m_IntermediateTex;
					for (int i = 0; i < intermediateTex.Length; i++)
					{
						CoreUtils.Destroy(intermediateTex[i]);
					}
				}
				CoreUtils.Destroy(this.m_DownsampledDetail);
				CoreUtils.Destroy(this.m_ResultTex);
				if (this.MinMaxMap.IsCreated)
				{
					this.MinMaxMap.Dispose(this.m_UpdateJob);
				}
				if (this.m_UpdateBuffer.IsCreated)
				{
					this.m_UpdateBuffer.Dispose();
				}
				this.m_UpdateJob = default(JobHandle);
			}
		}

		private class TerrainDesc
		{
			public Colossal.Hash128 heightMapGuid { get; set; }

			public Colossal.Hash128 diffuseMapGuid { get; set; }

			public float heightScale { get; set; }

			public float heightOffset { get; set; }

			public Colossal.Hash128 worldHeightMapGuid { get; set; }

			public float2 mapSize { get; set; }

			public float2 worldSize { get; set; }

			public float2 worldHeightMinMax { get; set; }

			private static void SupportValueTypesForAOT()
			{
				JSON.SupportTypeForAOT<float2>();
			}
		}

		[BurstCompile]
		private struct CullBuildingLotsJob : IJobChunk
		{
			[ReadOnly]
			public ComponentTypeHandle<Game.Buildings.Lot> m_LotHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Transform> m_TransformHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Elevation> m_ElevationHandle;

			[ReadOnly]
			public ComponentTypeHandle<Stack> m_StackHandle;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

			[ReadOnly]
			public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeHandle;

			[ReadOnly]
			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			[ReadOnly]
			public ComponentLookup<BuildingData> m_PrefabBuildingData;

			[ReadOnly]
			public ComponentLookup<PrefabRef> m_PrefabRefData;

			[ReadOnly]
			public ComponentLookup<AssetStampData> m_PrefabAssetStampData;

			[ReadOnly]
			public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

			[ReadOnly]
			public ComponentLookup<BuildingTerraformData> m_OverrideTerraform;

			[ReadOnly]
			public BufferLookup<AdditionalBuildingTerraformElement> m_AdditionalLots;

			[ReadOnly]
			public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

			[ReadOnly]
			public float4 m_Area;

			public NativeQueue<BuildingUtils.LotInfo>.ParallelWriter Result;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<Game.Buildings.Lot> nativeArray = chunk.GetNativeArray(ref this.m_LotHandle);
				NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref this.m_TransformHandle);
				NativeArray<Game.Objects.Elevation> nativeArray3 = chunk.GetNativeArray(ref this.m_ElevationHandle);
				NativeArray<Stack> nativeArray4 = chunk.GetNativeArray(ref this.m_StackHandle);
				NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref this.m_PrefabRefHandle);
				BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref this.m_InstalledUpgradeHandle);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					PrefabRef prefabRef = nativeArray5[i];
					Game.Objects.Transform transform = nativeArray2[i];
					Game.Objects.Elevation elevation = default(Game.Objects.Elevation);
					if (nativeArray3.Length != 0)
					{
						elevation = nativeArray3[i];
					}
					Game.Buildings.Lot lot = default(Game.Buildings.Lot);
					if (nativeArray.Length != 0)
					{
						lot = nativeArray[i];
					}
					bool flag = this.m_PrefabBuildingData.HasComponent(prefabRef.m_Prefab);
					bool flag2 = !flag && this.m_PrefabBuildingExtensionData.HasComponent(prefabRef.m_Prefab);
					bool flag3 = !flag && !flag2 && this.m_PrefabAssetStampData.HasComponent(prefabRef.m_Prefab);
					bool flag4 = !flag && !flag2 && !flag3 && this.m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab);
					if (!(flag || flag2 || flag3 || flag4))
					{
						continue;
					}
					ObjectGeometryData objectGeometryData = this.m_ObjectGeometryData[prefabRef.m_Prefab];
					Bounds2 xz = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, objectGeometryData).xz;
					float2 @float;
					if (flag)
					{
						@float = new float2(this.m_PrefabBuildingData[prefabRef.m_Prefab].m_LotSize) * 4f;
					}
					else if (flag2)
					{
						BuildingExtensionData buildingExtensionData = this.m_PrefabBuildingExtensionData[prefabRef.m_Prefab];
						if (!buildingExtensionData.m_External)
						{
							continue;
						}
						@float = new float2(buildingExtensionData.m_LotSize) * 4f;
					}
					else if (flag3)
					{
						@float = new float2(this.m_PrefabAssetStampData[prefabRef.m_Prefab].m_Size) * 4f;
					}
					else
					{
						if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != 0)
						{
							@float = objectGeometryData.m_LegSize.xz * 0.5f;
						}
						else
						{
							transform.m_Position.xz += MathUtils.Center(objectGeometryData.m_Bounds.xz);
							@float = MathUtils.Size(objectGeometryData.m_Bounds.xz) * 0.5f;
						}
						if (nativeArray4.Length != 0)
						{
							Stack stack = nativeArray4[i];
							transform.m_Position.y += stack.m_Range.min - objectGeometryData.m_Bounds.min.y;
						}
					}
					xz = MathUtils.Expand(xz, ObjectUtils.GetTerrainSmoothingWidth(objectGeometryData) - 8f);
					if (xz.max.x < this.m_Area.x || xz.min.x > this.m_Area.z || xz.max.y < this.m_Area.y || xz.min.y > this.m_Area.w)
					{
						continue;
					}
					DynamicBuffer<InstalledUpgrade> upgrades = default(DynamicBuffer<InstalledUpgrade>);
					if (bufferAccessor.Length != 0)
					{
						upgrades = bufferAccessor[i];
					}
					bool hasExtensionLots;
					BuildingUtils.LotInfo lotInfo = BuildingUtils.CalculateLotInfo(@float, transform, elevation, lot, prefabRef, upgrades, this.m_TransformData, this.m_PrefabRefData, this.m_ObjectGeometryData, this.m_OverrideTerraform, this.m_PrefabBuildingExtensionData, flag4, out hasExtensionLots);
					float terrainSmoothingWidth = ObjectUtils.GetTerrainSmoothingWidth(@float * 2f);
					lotInfo.m_Radius += terrainSmoothingWidth;
					this.Result.Enqueue(lotInfo);
					if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != 0)
					{
						BuildingUtils.LotInfo value = lotInfo;
						value.m_Extents = MathUtils.Size(objectGeometryData.m_Bounds.xz) * 0.5f;
						float terrainSmoothingWidth2 = ObjectUtils.GetTerrainSmoothingWidth(value.m_Extents * 2f);
						value.m_Position.xz += MathUtils.Center(objectGeometryData.m_Bounds.xz);
						value.m_Position.y += objectGeometryData.m_LegSize.y;
						value.m_MaxLimit = new float4(terrainSmoothingWidth2, terrainSmoothingWidth2, 0f - terrainSmoothingWidth2, 0f - terrainSmoothingWidth2);
						value.m_MinLimit = new float4(-value.m_Extents.xy, value.m_Extents.xy);
						value.m_FrontHeights = default(float3);
						value.m_RightHeights = default(float3);
						value.m_BackHeights = default(float3);
						value.m_LeftHeights = default(float3);
						value.m_FlatX0 = value.m_MinLimit.x * 0.5f;
						value.m_FlatZ0 = value.m_MinLimit.y * 0.5f;
						value.m_FlatX1 = value.m_MinLimit.z * 0.5f;
						value.m_FlatZ1 = value.m_MinLimit.w * 0.5f;
						value.m_Radius = math.length(value.m_Extents) + terrainSmoothingWidth2;
						value.m_Circular = math.select(0f, 1f, (objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != 0);
						this.Result.Enqueue(value);
					}
					if (this.m_AdditionalLots.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
					{
						for (int j = 0; j < bufferData.Length; j++)
						{
							AdditionalBuildingTerraformElement additionalBuildingTerraformElement = bufferData[j];
							BuildingUtils.LotInfo value2 = lotInfo;
							value2.m_Position.y += additionalBuildingTerraformElement.m_HeightOffset;
							value2.m_MinLimit = new float4(additionalBuildingTerraformElement.m_Area.min, additionalBuildingTerraformElement.m_Area.max);
							value2.m_FlatX0 = math.max(value2.m_FlatX0, value2.m_MinLimit.x);
							value2.m_FlatZ0 = math.max(value2.m_FlatZ0, value2.m_MinLimit.y);
							value2.m_FlatX1 = math.min(value2.m_FlatX1, value2.m_MinLimit.z);
							value2.m_FlatZ1 = math.min(value2.m_FlatZ1, value2.m_MinLimit.w);
							value2.m_Circular = math.select(0f, 1f, additionalBuildingTerraformElement.m_Circular);
							value2.m_MaxLimit = math.select(value2.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), additionalBuildingTerraformElement.m_DontRaise);
							value2.m_MinLimit = math.select(value2.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), additionalBuildingTerraformElement.m_DontLower);
							this.Result.Enqueue(value2);
						}
					}
					if (!hasExtensionLots)
					{
						continue;
					}
					for (int k = 0; k < upgrades.Length; k++)
					{
						Entity upgrade = upgrades[k].m_Upgrade;
						PrefabRef prefabRef2 = this.m_PrefabRefData[upgrade];
						if (!this.m_PrefabBuildingExtensionData.TryGetComponent(prefabRef2.m_Prefab, out var componentData) || componentData.m_External || !this.m_OverrideTerraform.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
						{
							continue;
						}
						float3 float2 = this.m_TransformData[upgrade].m_Position - transform.m_Position;
						float num = 0f;
						if (this.m_ObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData3))
						{
							bool flag5 = (componentData3.m_Flags & Game.Objects.GeometryFlags.Standing) != 0;
							bool c = ((uint)componentData3.m_Flags & (uint)((!flag5) ? 1 : 256)) != 0;
							num = math.select(0f, 1f, c);
						}
						if (!math.all(componentData2.m_Smooth + float2.xzxz == lotInfo.m_MaxLimit) || num != lotInfo.m_Circular)
						{
							BuildingUtils.LotInfo value3 = lotInfo;
							value3.m_Circular = num;
							value3.m_Position.y += componentData2.m_HeightOffset;
							value3.m_MinLimit = componentData2.m_Smooth + float2.xzxz;
							value3.m_MaxLimit = value3.m_MinLimit;
							value3.m_MinLimit.xy = math.min(new float2(value3.m_FlatX0.y, value3.m_FlatZ0.y), value3.m_MinLimit.xy);
							value3.m_MinLimit.zw = math.max(new float2(value3.m_FlatX1.y, value3.m_FlatZ1.y), value3.m_MinLimit.zw);
							value3.m_MinLimit = math.select(value3.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), componentData2.m_DontLower);
							value3.m_MaxLimit = math.select(value3.m_MaxLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), componentData2.m_DontRaise);
							this.Result.Enqueue(value3);
						}
						if (this.m_AdditionalLots.TryGetBuffer(prefabRef2.m_Prefab, out var bufferData2))
						{
							for (int l = 0; l < bufferData2.Length; l++)
							{
								AdditionalBuildingTerraformElement additionalBuildingTerraformElement2 = bufferData2[l];
								BuildingUtils.LotInfo value4 = lotInfo;
								value4.m_Position.y += additionalBuildingTerraformElement2.m_HeightOffset;
								value4.m_MinLimit = new float4(additionalBuildingTerraformElement2.m_Area.min, additionalBuildingTerraformElement2.m_Area.max) + float2.xzxz;
								value4.m_FlatX0 = math.max(value4.m_FlatX0, value4.m_MinLimit.x);
								value4.m_FlatZ0 = math.max(value4.m_FlatZ0, value4.m_MinLimit.y);
								value4.m_FlatX1 = math.min(value4.m_FlatX1, value4.m_MinLimit.z);
								value4.m_FlatZ1 = math.min(value4.m_FlatZ1, value4.m_MinLimit.w);
								value4.m_Circular = math.select(0f, 1f, additionalBuildingTerraformElement2.m_Circular);
								value4.m_MaxLimit = math.select(value4.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), additionalBuildingTerraformElement2.m_DontRaise);
								value4.m_MinLimit = math.select(value4.m_MinLimit, new float4(terrainSmoothingWidth, terrainSmoothingWidth, 0f - terrainSmoothingWidth, 0f - terrainSmoothingWidth), additionalBuildingTerraformElement2.m_DontLower);
								this.Result.Enqueue(value4);
							}
						}
					}
				}
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct DequeBuildingLotsJob : IJob
		{
			[ReadOnly]
			public NativeQueue<BuildingUtils.LotInfo> m_Queue;

			public NativeList<BuildingUtils.LotInfo> m_List;

			public void Execute()
			{
				NativeArray<BuildingUtils.LotInfo> other = this.m_Queue.ToArray(Allocator.Temp);
				this.m_List.CopyFrom(in other);
				other.Dispose();
			}
		}

		[BurstCompile]
		private struct CullRoadsJob : IJobChunk
		{
			[ReadOnly]
			public EntityTypeHandle m_EntityHandle;

			[ReadOnly]
			public ComponentLookup<Composition> m_CompositionData;

			[ReadOnly]
			public ComponentLookup<Orphan> m_OrphanData;

			[ReadOnly]
			public ComponentLookup<Game.Net.Node> m_NodeData;

			[ReadOnly]
			public ComponentLookup<NodeGeometry> m_NodeGeometryData;

			[ReadOnly]
			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			[ReadOnly]
			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			[ReadOnly]
			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			[ReadOnly]
			public ComponentLookup<PrefabRef> m_PrefabRefData;

			[ReadOnly]
			public ComponentLookup<NetData> m_NetData;

			[ReadOnly]
			public ComponentLookup<NetGeometryData> m_NetGeometryData;

			[ReadOnly]
			public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

			[ReadOnly]
			public ComponentLookup<TerrainComposition> m_TerrainCompositionData;

			[ReadOnly]
			public float4 m_Area;

			public NativeList<LaneSection>.ParallelWriter Result;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityHandle);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					if (!this.m_PrefabRefData.HasComponent(entity))
					{
						continue;
					}
					Entity prefab = this.m_PrefabRefData[entity].m_Prefab;
					if (!this.m_NetGeometryData.HasComponent(prefab))
					{
						continue;
					}
					NetData net = this.m_NetData[prefab];
					NetGeometryData netGeometry = this.m_NetGeometryData[prefab];
					if (this.m_CompositionData.HasComponent(entity))
					{
						Composition composition = this.m_CompositionData[entity];
						EdgeGeometry geometry = this.m_EdgeGeometryData[entity];
						StartNodeGeometry startNodeGeometry = this.m_StartNodeGeometryData[entity];
						EndNodeGeometry endNodeGeometry = this.m_EndNodeGeometryData[entity];
						if (math.any(geometry.m_Start.m_Length + geometry.m_End.m_Length > 0.1f))
						{
							NetCompositionData prefabCompositionData = this.m_PrefabCompositionData[composition.m_Edge];
							TerrainComposition terrainComposition = default(TerrainComposition);
							if (this.m_TerrainCompositionData.HasComponent(composition.m_Edge))
							{
								terrainComposition = this.m_TerrainCompositionData[composition.m_Edge];
							}
							this.AddEdge(geometry, this.m_Area, net, netGeometry, prefabCompositionData, terrainComposition);
						}
						if (math.any(startNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(startNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
						{
							NetCompositionData prefabCompositionData2 = this.m_PrefabCompositionData[composition.m_StartNode];
							TerrainComposition terrainComposition2 = default(TerrainComposition);
							if (this.m_TerrainCompositionData.HasComponent(composition.m_StartNode))
							{
								terrainComposition2 = this.m_TerrainCompositionData[composition.m_StartNode];
							}
							this.AddNode(startNodeGeometry.m_Geometry, this.m_Area, net, netGeometry, prefabCompositionData2, terrainComposition2);
						}
						if (math.any(endNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(endNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
						{
							NetCompositionData prefabCompositionData3 = this.m_PrefabCompositionData[composition.m_EndNode];
							TerrainComposition terrainComposition3 = default(TerrainComposition);
							if (this.m_TerrainCompositionData.HasComponent(composition.m_EndNode))
							{
								terrainComposition3 = this.m_TerrainCompositionData[composition.m_EndNode];
							}
							this.AddNode(endNodeGeometry.m_Geometry, this.m_Area, net, netGeometry, prefabCompositionData3, terrainComposition3);
						}
					}
					else if (this.m_OrphanData.HasComponent(entity))
					{
						Orphan orphan = this.m_OrphanData[entity];
						Game.Net.Node node = this.m_NodeData[entity];
						NetCompositionData prefabCompositionData4 = this.m_PrefabCompositionData[orphan.m_Composition];
						TerrainComposition terrainComposition4 = default(TerrainComposition);
						if (this.m_TerrainCompositionData.HasComponent(orphan.m_Composition))
						{
							terrainComposition4 = this.m_TerrainCompositionData[orphan.m_Composition];
						}
						NodeGeometry nodeGeometry = this.m_NodeGeometryData[entity];
						this.AddOrphans(node, nodeGeometry, this.m_Area, net, netGeometry, prefabCompositionData4, terrainComposition4);
					}
				}
			}

			private LaneFlags GetFlags(NetGeometryData netGeometry, NetCompositionData prefabCompositionData)
			{
				LaneFlags laneFlags = (LaneFlags)0;
				if ((netGeometry.m_Flags & Game.Net.GeometryFlags.ClipTerrain) != 0)
				{
					laneFlags |= LaneFlags.ClipTerrain;
				}
				if ((netGeometry.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) != 0)
				{
					laneFlags |= LaneFlags.ShiftTerrain;
				}
				return laneFlags;
			}

			private void AddEdge(EdgeGeometry geometry, float4 area, NetData net, NetGeometryData netGeometry, NetCompositionData prefabCompositionData, TerrainComposition terrainComposition)
			{
				LaneFlags laneFlags = this.GetFlags(netGeometry, prefabCompositionData);
				if ((laneFlags & (LaneFlags.ShiftTerrain | LaneFlags.ClipTerrain)) == 0)
				{
					return;
				}
				Bounds2 xz = geometry.m_Bounds.xz;
				if (!math.any(xz.max < area.xy) && !math.any(xz.min > area.zw))
				{
					if ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
					{
						laneFlags |= LaneFlags.InverseClipOffset;
					}
					this.AddSegment(geometry.m_Start, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags, isStart: true);
					this.AddSegment(geometry.m_End, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags, isStart: false);
				}
			}

			private void MoveTowards(ref float3 position, float3 other, float amount)
			{
				float3 value = other - position;
				value = MathUtils.Normalize(value, value.xz);
				position += value * amount;
			}

			private void AddNode(EdgeNodeGeometry node, float4 area, NetData net, NetGeometryData netGeometry, NetCompositionData prefabCompositionData, TerrainComposition terrainComposition)
			{
				LaneFlags laneFlags = this.GetFlags(netGeometry, prefabCompositionData);
				if ((laneFlags & (LaneFlags.ShiftTerrain | LaneFlags.ClipTerrain)) == 0)
				{
					return;
				}
				Bounds2 xz = node.m_Bounds.xz;
				if (math.any(xz.max < area.xy) || math.any(xz.min > area.zw))
				{
					return;
				}
				if (node.m_MiddleRadius > 0f)
				{
					NetCompositionData compositionData = prefabCompositionData;
					float num = 0f;
					float num2 = 0f;
					if ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Elevated) != 0)
					{
						if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.HighTransition) != 0)
						{
							num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
							compositionData.m_Flags.m_General &= ~CompositionFlags.General.Elevated;
							compositionData.m_Flags.m_Left &= ~CompositionFlags.Side.HighTransition;
						}
						else if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.LowTransition) != 0)
						{
							num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
							compositionData.m_Flags.m_General &= ~CompositionFlags.General.Elevated;
							compositionData.m_Flags.m_Left &= ~CompositionFlags.Side.LowTransition;
							compositionData.m_Flags.m_Left |= CompositionFlags.Side.Raised;
						}
						if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.HighTransition) != 0)
						{
							num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
							compositionData.m_Flags.m_General &= ~CompositionFlags.General.Elevated;
							compositionData.m_Flags.m_Right &= ~CompositionFlags.Side.HighTransition;
						}
						else if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.LowTransition) != 0)
						{
							num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
							compositionData.m_Flags.m_General &= ~CompositionFlags.General.Elevated;
							compositionData.m_Flags.m_Right &= ~CompositionFlags.Side.LowTransition;
							compositionData.m_Flags.m_Right |= CompositionFlags.Side.Raised;
						}
					}
					else if ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
					{
						laneFlags |= LaneFlags.InverseClipOffset;
						if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.HighTransition) != 0)
						{
							num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
							compositionData.m_Flags.m_General &= ~CompositionFlags.General.Tunnel;
							compositionData.m_Flags.m_Left &= ~CompositionFlags.Side.HighTransition;
							laneFlags &= ~LaneFlags.InverseClipOffset;
						}
						else if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.LowTransition) != 0)
						{
							num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
							compositionData.m_Flags.m_General &= ~CompositionFlags.General.Tunnel;
							compositionData.m_Flags.m_Left &= ~CompositionFlags.Side.LowTransition;
							compositionData.m_Flags.m_Left |= CompositionFlags.Side.Lowered;
							laneFlags &= ~LaneFlags.InverseClipOffset;
						}
						if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.HighTransition) != 0)
						{
							num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
							compositionData.m_Flags.m_General &= ~CompositionFlags.General.Tunnel;
							compositionData.m_Flags.m_Right &= ~CompositionFlags.Side.HighTransition;
							laneFlags &= ~LaneFlags.InverseClipOffset;
						}
						else if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.LowTransition) != 0)
						{
							num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
							compositionData.m_Flags.m_General &= ~CompositionFlags.General.Tunnel;
							compositionData.m_Flags.m_Right &= ~CompositionFlags.Side.LowTransition;
							compositionData.m_Flags.m_Right |= CompositionFlags.Side.Lowered;
							laneFlags &= ~LaneFlags.InverseClipOffset;
						}
					}
					else
					{
						if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.LowTransition) != 0)
						{
							if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.Raised) != 0)
							{
								num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
								compositionData.m_Flags.m_Left &= ~(CompositionFlags.Side.Raised | CompositionFlags.Side.LowTransition);
							}
							else if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.Lowered) != 0)
							{
								num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
								compositionData.m_Flags.m_Left &= ~(CompositionFlags.Side.Lowered | CompositionFlags.Side.LowTransition);
							}
							else if ((prefabCompositionData.m_Flags.m_Left & CompositionFlags.Side.SoundBarrier) != 0)
							{
								num = prefabCompositionData.m_SyncVertexOffsetsLeft.x;
								compositionData.m_Flags.m_Left &= ~(CompositionFlags.Side.LowTransition | CompositionFlags.Side.SoundBarrier);
							}
						}
						if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.LowTransition) != 0)
						{
							if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.Raised) != 0)
							{
								num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
								compositionData.m_Flags.m_Right &= ~(CompositionFlags.Side.Raised | CompositionFlags.Side.LowTransition);
							}
							else if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.Lowered) != 0)
							{
								num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
								compositionData.m_Flags.m_Right &= ~(CompositionFlags.Side.Lowered | CompositionFlags.Side.LowTransition);
							}
							else if ((prefabCompositionData.m_Flags.m_Right & CompositionFlags.Side.SoundBarrier) != 0)
							{
								num2 = 1f - prefabCompositionData.m_SyncVertexOffsetsRight.w;
								compositionData.m_Flags.m_Right &= ~(CompositionFlags.Side.LowTransition | CompositionFlags.Side.SoundBarrier);
							}
						}
					}
					if (num != 0f)
					{
						num *= math.distance(node.m_Left.m_Left.a.xz, node.m_Middle.a.xz);
					}
					if (num2 != 0f)
					{
						num2 *= math.distance(node.m_Middle.a.xz, node.m_Left.m_Right.a.xz);
					}
					Segment left = node.m_Left;
					left.m_Right = node.m_Middle;
					this.AddSegment(left, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
					left.m_Left = left.m_Right;
					left.m_Right = node.m_Left.m_Right;
					this.AddSegment(left, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
					left = node.m_Right;
					left.m_Right = new Bezier4x3(node.m_Middle.d, node.m_Middle.d, node.m_Middle.d, node.m_Middle.d);
					if (num != 0f)
					{
						this.MoveTowards(ref left.m_Left.a, node.m_Middle.d, num);
						this.MoveTowards(ref left.m_Left.b, node.m_Middle.d, num);
						this.MoveTowards(ref left.m_Left.c, node.m_Middle.d, num);
						this.MoveTowards(ref left.m_Left.d, node.m_Middle.d, num);
					}
					this.AddSegment(left, net, netGeometry, compositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: false);
					left.m_Left = left.m_Right;
					left.m_Right = node.m_Right.m_Right;
					if (num2 != 0f)
					{
						this.MoveTowards(ref left.m_Right.a, node.m_Middle.d, num2);
						this.MoveTowards(ref left.m_Right.b, node.m_Middle.d, num2);
						this.MoveTowards(ref left.m_Right.c, node.m_Middle.d, num2);
						this.MoveTowards(ref left.m_Right.d, node.m_Middle.d, num2);
					}
					this.AddSegment(left, net, netGeometry, compositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: false);
				}
				else if (math.lengthsq(node.m_Left.m_Right.d - node.m_Right.m_Left.d) > 0.0001f)
				{
					Segment left2 = node.m_Left;
					this.AddSegment(left2, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
					left2.m_Left = left2.m_Right;
					left2.m_Right = node.m_Middle;
					this.AddSegment(left2, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | (LaneFlags.MiddleLeft | LaneFlags.MiddleRight), isStart: true);
					left2 = node.m_Right;
					this.AddSegment(left2, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
					left2.m_Right = left2.m_Left;
					left2.m_Left = node.m_Middle;
					this.AddSegment(left2, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | (LaneFlags.MiddleLeft | LaneFlags.MiddleRight), isStart: true);
				}
				else
				{
					Segment left3 = node.m_Left;
					left3.m_Right = node.m_Middle;
					this.AddSegment(left3, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
					left3.m_Left = node.m_Middle;
					left3.m_Right = node.m_Right.m_Right;
					this.AddSegment(left3, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
				}
			}

			private void AddOrphans(Game.Net.Node node, NodeGeometry nodeGeometry, float4 area, NetData net, NetGeometryData netGeometry, NetCompositionData prefabCompositionData, TerrainComposition terrainComposition)
			{
				LaneFlags laneFlags = this.GetFlags(netGeometry, prefabCompositionData);
				if ((laneFlags & (LaneFlags.ShiftTerrain | LaneFlags.ClipTerrain)) == 0)
				{
					return;
				}
				Segment segment = default(Segment);
				Bounds2 xz = nodeGeometry.m_Bounds.xz;
				if (!math.any(xz.max < area.xy) && !math.any(xz.min > area.zw))
				{
					if ((prefabCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
					{
						laneFlags |= LaneFlags.InverseClipOffset;
					}
					segment.m_Left.a = new float3(node.m_Position.x - prefabCompositionData.m_Width * 0.5f, node.m_Position.y, node.m_Position.z);
					segment.m_Left.b = new float3(node.m_Position.x - prefabCompositionData.m_Width * 0.5f, node.m_Position.y, node.m_Position.z + prefabCompositionData.m_Width * 0.2761424f);
					segment.m_Left.c = new float3(node.m_Position.x - prefabCompositionData.m_Width * 0.2761424f, node.m_Position.y, node.m_Position.z + prefabCompositionData.m_Width * 0.5f);
					segment.m_Left.d = new float3(node.m_Position.x, node.m_Position.y, node.m_Position.z + prefabCompositionData.m_Width * 0.5f);
					segment.m_Right = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position);
					segment.m_Length = new float2(prefabCompositionData.m_Width * (Mathf.PI / 2f), 0f);
					this.AddSegment(segment, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
					CommonUtils.Swap(ref segment.m_Left, ref segment.m_Right);
					segment.m_Right.a.x += prefabCompositionData.m_Width;
					segment.m_Right.b.x += prefabCompositionData.m_Width;
					segment.m_Right.c.x = node.m_Position.x * 2f - segment.m_Right.c.x;
					this.AddSegment(segment, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
					segment.m_Left.a = new float3(node.m_Position.x + prefabCompositionData.m_Width * 0.5f, node.m_Position.y, node.m_Position.z);
					segment.m_Left.b = new float3(node.m_Position.x + prefabCompositionData.m_Width * 0.5f, node.m_Position.y, node.m_Position.z - prefabCompositionData.m_Width * 0.2761424f);
					segment.m_Left.c = new float3(node.m_Position.x + prefabCompositionData.m_Width * 0.2761424f, node.m_Position.y, node.m_Position.z - prefabCompositionData.m_Width * 0.5f);
					segment.m_Left.d = new float3(node.m_Position.x, node.m_Position.y, node.m_Position.z - prefabCompositionData.m_Width * 0.5f);
					segment.m_Right = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position);
					segment.m_Length = new float2(prefabCompositionData.m_Width * (Mathf.PI / 2f), 0f);
					this.AddSegment(segment, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleRight, isStart: true);
					CommonUtils.Swap(ref segment.m_Left, ref segment.m_Right);
					segment.m_Right.a.x -= prefabCompositionData.m_Width;
					segment.m_Right.b.x -= prefabCompositionData.m_Width;
					segment.m_Right.c.x = node.m_Position.x * 2f - segment.m_Right.c.x;
					this.AddSegment(segment, net, netGeometry, prefabCompositionData, terrainComposition, laneFlags | LaneFlags.MiddleLeft, isStart: true);
				}
			}

			private void AddSegment(Segment segment, NetData net, NetGeometryData netGeometry, NetCompositionData compositionData, TerrainComposition terrainComposition, LaneFlags flags, bool isStart)
			{
				if (math.any(terrainComposition.m_WidthOffset != 0f))
				{
					Segment segment2 = segment;
					float4 @float = 1f / math.max(y: new float4(math.distance(segment.m_Left.a.xz, segment.m_Right.a.xz), math.distance(segment.m_Left.b.xz, segment.m_Right.b.xz), math.distance(segment.m_Left.c.xz, segment.m_Right.c.xz), math.distance(segment.m_Left.d.xz, segment.m_Right.d.xz)), x: 0.001f);
					if (terrainComposition.m_WidthOffset.x != 0f && (flags & LaneFlags.MiddleLeft) == 0)
					{
						Bezier4x1 t = default(Bezier4x1);
						t.abcd = terrainComposition.m_WidthOffset.x * @float;
						segment.m_Left = MathUtils.Lerp(segment2.m_Left, segment2.m_Right, t);
					}
					if (terrainComposition.m_WidthOffset.y != 0f && (flags & LaneFlags.MiddleRight) == 0)
					{
						Bezier4x1 t2 = default(Bezier4x1);
						t2.abcd = terrainComposition.m_WidthOffset.y * @float;
						segment.m_Right = MathUtils.Lerp(segment2.m_Right, segment2.m_Left, t2);
					}
				}
				float3 float2 = math.select(new float3(compositionData.m_EdgeHeights.z, compositionData.m_SurfaceHeight.min, compositionData.m_EdgeHeights.w), new float3(compositionData.m_EdgeHeights.x, compositionData.m_SurfaceHeight.min, compositionData.m_EdgeHeights.y), isStart);
				float3 float3 = float2;
				float2 clipOffset = new float2(math.cmin(float2), math.cmax(float3));
				float terrainSmoothingWidth = NetUtils.GetTerrainSmoothingWidth(net);
				clipOffset += terrainComposition.m_ClipHeightOffset;
				float2 += terrainComposition.m_MinHeightOffset;
				float3 += terrainComposition.m_MaxHeightOffset;
				float3 float4 = 1000000f;
				float3 float5 = 1000000f;
				float3 float6 = 1000000f;
				if ((compositionData.m_State & CompositionState.HasSurface) == 0)
				{
					if ((compositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
					{
						float2 = 1000000f;
						float3 = compositionData.m_HeightRange.max + 1f + terrainComposition.m_MaxHeightOffset;
					}
					else
					{
						float2 = compositionData.m_HeightRange.min + terrainComposition.m_MinHeightOffset;
						float3 = -1000000f;
					}
				}
				else if ((compositionData.m_Flags.m_General & CompositionFlags.General.Elevated) != 0 || (netGeometry.m_MergeLayers & Layer.Waterway) != 0)
				{
					if (((compositionData.m_Flags.m_Left | compositionData.m_Flags.m_Right) & (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) == 0)
					{
						float2 = compositionData.m_HeightRange.min;
					}
					float3 = -1000000f;
				}
				else if ((compositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
				{
					if ((compositionData.m_Flags.m_Left & CompositionFlags.Side.HighTransition) != 0)
					{
						float4.xy = math.min(float4.xy, float2.xy);
					}
					if ((compositionData.m_Flags.m_Right & CompositionFlags.Side.HighTransition) != 0)
					{
						float4.yz = math.min(float4.yz, float2.yz);
					}
					if (((compositionData.m_Flags.m_Left | compositionData.m_Flags.m_Right) & (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) == 0)
					{
						float2 = 1000000f;
						float3 = compositionData.m_HeightRange.max + 1f;
					}
					else
					{
						float5 = netGeometry.m_ElevationLimit * 3f;
						float6 = compositionData.m_HeightRange.max + 1f;
						float2 = math.max(float2, netGeometry.m_ElevationLimit * 3f);
						clipOffset.y = math.max(clipOffset.y, netGeometry.m_ElevationLimit * 3f);
					}
				}
				else
				{
					if ((compositionData.m_Flags.m_Left & CompositionFlags.Side.Lowered) != 0)
					{
						if ((compositionData.m_Flags.m_Left & CompositionFlags.Side.LowTransition) != 0)
						{
							float4.xy = math.min(float4.xy, float2.xy);
						}
						float2.xy = math.max(float2.xy, netGeometry.m_ElevationLimit * 3f);
						clipOffset.y = math.max(clipOffset.y, netGeometry.m_ElevationLimit * 3f);
					}
					else if ((compositionData.m_Flags.m_Left & CompositionFlags.Side.Raised) != 0)
					{
						float3.xy = -1000000f;
					}
					if ((compositionData.m_Flags.m_Right & CompositionFlags.Side.Lowered) != 0)
					{
						if ((compositionData.m_Flags.m_Right & CompositionFlags.Side.LowTransition) != 0)
						{
							float4.yz = math.min(float4.yz, float2.yz);
						}
						float2.yz = math.max(float2.yz, netGeometry.m_ElevationLimit * 3f);
						clipOffset.y = math.max(clipOffset.y, netGeometry.m_ElevationLimit * 3f);
					}
					else if ((compositionData.m_Flags.m_Right & CompositionFlags.Side.Raised) != 0)
					{
						float3.yz = -1000000f;
					}
				}
				Bounds3 bounds = MathUtils.Bounds(segment.m_Left) | MathUtils.Bounds(segment.m_Right);
				bounds.min.xz -= terrainSmoothingWidth;
				bounds.max.xz += terrainSmoothingWidth;
				bounds.min.y += math.cmin(math.min(float2, float3));
				bounds.max.y += math.cmax(math.max(float2, float3));
				LaneSection laneSection = default(LaneSection);
				laneSection.m_Bounds = bounds.xz;
				laneSection.m_Left = new float4x3(segment.m_Left.a.x, segment.m_Left.a.y, segment.m_Left.a.z, segment.m_Left.b.x, segment.m_Left.b.y, segment.m_Left.b.z, segment.m_Left.c.x, segment.m_Left.c.y, segment.m_Left.c.z, segment.m_Left.d.x, segment.m_Left.d.y, segment.m_Left.d.z);
				laneSection.m_Right = new float4x3(segment.m_Right.a.x, segment.m_Right.a.y, segment.m_Right.a.z, segment.m_Right.b.x, segment.m_Right.b.y, segment.m_Right.b.z, segment.m_Right.c.x, segment.m_Right.c.y, segment.m_Right.c.z, segment.m_Right.d.x, segment.m_Right.d.y, segment.m_Right.d.z);
				laneSection.m_MinOffset = float2;
				laneSection.m_MaxOffset = float3;
				laneSection.m_ClipOffset = clipOffset;
				laneSection.m_WidthOffset = terrainSmoothingWidth;
				laneSection.m_Flags = flags;
				LaneSection value = laneSection;
				this.Result.AddNoResize(value);
				if (math.any(float4 != 1000000f) && (flags & LaneFlags.ShiftTerrain) != 0)
				{
					Bounds1 t3 = new Bounds1(0f, 1f);
					Bounds1 t4 = new Bounds1(0f, 1f);
					MathUtils.ClampLengthInverse(segment.m_Left.xz, ref t3, 3f);
					MathUtils.ClampLengthInverse(segment.m_Right.xz, ref t4, 3f);
					Segment segment3 = segment;
					segment3.m_Left = MathUtils.Cut(segment.m_Left, t3);
					segment3.m_Right = MathUtils.Cut(segment.m_Right, t4);
					bounds = MathUtils.Bounds(segment3.m_Left) | MathUtils.Bounds(segment3.m_Right);
					bounds.min.xz -= terrainSmoothingWidth;
					bounds.max.xz += terrainSmoothingWidth;
					bounds.min.y += math.cmin(math.min(float4, float3));
					bounds.max.y += math.cmax(math.max(float4, float3));
					laneSection = default(LaneSection);
					laneSection.m_Bounds = bounds.xz;
					laneSection.m_Left = new float4x3(segment3.m_Left.a.x, segment3.m_Left.a.y, segment3.m_Left.a.z, segment3.m_Left.b.x, segment3.m_Left.b.y, segment3.m_Left.b.z, segment3.m_Left.c.x, segment3.m_Left.c.y, segment3.m_Left.c.z, segment3.m_Left.d.x, segment3.m_Left.d.y, segment3.m_Left.d.z);
					laneSection.m_Right = new float4x3(segment3.m_Right.a.x, segment3.m_Right.a.y, segment3.m_Right.a.z, segment3.m_Right.b.x, segment3.m_Right.b.y, segment3.m_Right.b.z, segment3.m_Right.c.x, segment3.m_Right.c.y, segment3.m_Right.c.z, segment3.m_Right.d.x, segment3.m_Right.d.y, segment3.m_Right.d.z);
					laneSection.m_MinOffset = float4;
					laneSection.m_MaxOffset = float3;
					laneSection.m_ClipOffset = clipOffset;
					laneSection.m_WidthOffset = terrainSmoothingWidth;
					laneSection.m_Flags = flags & ~LaneFlags.ClipTerrain;
					value = laneSection;
					this.Result.AddNoResize(value);
				}
				if ((math.any(float5 != 1000000f) || math.any(float6 != 1000000f)) && (flags & LaneFlags.ShiftTerrain) != 0)
				{
					float3 value2 = MathUtils.StartTangent(segment.m_Left);
					float3 value3 = MathUtils.StartTangent(segment.m_Right);
					value2 = MathUtils.Normalize(value2, value2.xz);
					value3 = MathUtils.Normalize(value3, value3.xz);
					Segment segment4 = segment;
					segment4.m_Left = NetUtils.StraightCurve(segment.m_Left.a + value2 * 2f, segment.m_Left.a - value2 * 2f);
					segment4.m_Right = NetUtils.StraightCurve(segment.m_Right.a + value3 * 2f, segment.m_Right.a - value3 * 2f);
					bounds = MathUtils.Bounds(segment4.m_Left) | MathUtils.Bounds(segment4.m_Right);
					bounds.min.xz -= terrainSmoothingWidth;
					bounds.max.xz += terrainSmoothingWidth;
					bounds.min.y += math.cmin(math.min(float5, float3));
					bounds.max.y += math.cmax(math.max(float5, float3));
					laneSection = default(LaneSection);
					laneSection.m_Bounds = bounds.xz;
					laneSection.m_Left = new float4x3(segment4.m_Left.a.x, segment4.m_Left.a.y, segment4.m_Left.a.z, segment4.m_Left.b.x, segment4.m_Left.b.y, segment4.m_Left.b.z, segment4.m_Left.c.x, segment4.m_Left.c.y, segment4.m_Left.c.z, segment4.m_Left.d.x, segment4.m_Left.d.y, segment4.m_Left.d.z);
					laneSection.m_Right = new float4x3(segment4.m_Right.a.x, segment4.m_Right.a.y, segment4.m_Right.a.z, segment4.m_Right.b.x, segment4.m_Right.b.y, segment4.m_Right.b.z, segment4.m_Right.c.x, segment4.m_Right.c.y, segment4.m_Right.c.z, segment4.m_Right.d.x, segment4.m_Right.d.y, segment4.m_Right.d.z);
					laneSection.m_MinOffset = float5;
					laneSection.m_MaxOffset = float6;
					laneSection.m_ClipOffset = clipOffset;
					laneSection.m_WidthOffset = terrainSmoothingWidth;
					laneSection.m_Flags = flags & ~LaneFlags.ClipTerrain;
					value = laneSection;
					this.Result.AddNoResize(value);
				}
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct DequeBuildingDrawsJob : IJob
		{
			[ReadOnly]
			public NativeQueue<BuildingLotDraw> m_Queue;

			public NativeList<BuildingLotDraw> m_List;

			public void Execute()
			{
				NativeArray<BuildingLotDraw> other = this.m_Queue.ToArray(Allocator.Temp);
				this.m_List.CopyFrom(in other);
				other.Dispose();
			}
		}

		[BurstCompile]
		private struct CullBuildingsCascadeJob : IJobParallelForDefer
		{
			[ReadOnly]
			public NativeList<BuildingUtils.LotInfo> m_LotsToCull;

			[ReadOnly]
			public float4 m_Area;

			public NativeQueue<BuildingLotDraw>.ParallelWriter Result;

			public void Execute(int index)
			{
				if (index < this.m_LotsToCull.Length)
				{
					BuildingUtils.LotInfo lotInfo = this.m_LotsToCull[index];
					if (!(lotInfo.m_Position.x + lotInfo.m_Radius < this.m_Area.x) && !(lotInfo.m_Position.x - lotInfo.m_Radius > this.m_Area.z) && !(lotInfo.m_Position.z + lotInfo.m_Radius < this.m_Area.y) && !(lotInfo.m_Position.z - lotInfo.m_Radius > this.m_Area.w))
					{
						float2 @float = 0.5f / math.max(0.01f, lotInfo.m_Extents);
						BuildingLotDraw buildingLotDraw = default(BuildingLotDraw);
						buildingLotDraw.m_HeightsX = math.transpose(new float4x2(new float4(lotInfo.m_RightHeights, lotInfo.m_BackHeights.x), new float4(lotInfo.m_FrontHeights.x, lotInfo.m_LeftHeights.zyx)));
						buildingLotDraw.m_HeightsZ = math.transpose(new float4x2(new float4(lotInfo.m_RightHeights.x, lotInfo.m_FrontHeights.zyx), new float4(lotInfo.m_BackHeights, lotInfo.m_LeftHeights.x)));
						buildingLotDraw.m_FlatX0 = lotInfo.m_FlatX0 * @float.x + 0.5f;
						buildingLotDraw.m_FlatZ0 = lotInfo.m_FlatZ0 * @float.y + 0.5f;
						buildingLotDraw.m_FlatX1 = lotInfo.m_FlatX1 * @float.x + 0.5f;
						buildingLotDraw.m_FlatZ1 = lotInfo.m_FlatZ1 * @float.y + 0.5f;
						buildingLotDraw.m_Position = lotInfo.m_Position;
						buildingLotDraw.m_AxisX = math.mul(lotInfo.m_Rotation, new float3(1f, 0f, 0f));
						buildingLotDraw.m_AxisZ = math.mul(lotInfo.m_Rotation, new float3(0f, 0f, 1f));
						buildingLotDraw.m_Size = lotInfo.m_Extents;
						buildingLotDraw.m_MinLimit = lotInfo.m_MinLimit * @float.xyxy + 0.5f;
						buildingLotDraw.m_MaxLimit = lotInfo.m_MaxLimit * @float.xyxy + 0.5f;
						buildingLotDraw.m_Circular = lotInfo.m_Circular;
						buildingLotDraw.m_SmoothingWidth = ObjectUtils.GetTerrainSmoothingWidth(lotInfo.m_Extents * 2f);
						BuildingLotDraw value = buildingLotDraw;
						this.Result.Enqueue(value);
					}
				}
			}
		}

		[BurstCompile]
		private struct CullTrianglesJob : IJob
		{
			[ReadOnly]
			public NativeList<AreaTriangle> m_Triangles;

			[ReadOnly]
			public float4 m_Area;

			public NativeList<AreaTriangle> Result;

			public void Execute()
			{
				for (int i = 0; i < this.m_Triangles.Length; i++)
				{
					AreaTriangle value = this.m_Triangles[i];
					float2 @float = math.min(value.m_PositionA, math.min(value.m_PositionB, value.m_PositionC));
					float2 float2 = math.max(value.m_PositionA, math.max(value.m_PositionB, value.m_PositionC));
					if (!(float2.x < this.m_Area.x) && !(@float.x > this.m_Area.z) && !(float2.y < this.m_Area.y) && !(@float.y > this.m_Area.w))
					{
						this.Result.Add(in value);
					}
				}
			}
		}

		[BurstCompile]
		private struct CullEdgesJob : IJob
		{
			[ReadOnly]
			public NativeList<AreaEdge> m_Edges;

			[ReadOnly]
			public float4 m_Area;

			public NativeList<AreaEdge> Result;

			public void Execute()
			{
				for (int i = 0; i < this.m_Edges.Length; i++)
				{
					AreaEdge value = this.m_Edges[i];
					float2 @float = math.min(value.m_PositionA, value.m_PositionB) - value.m_SideOffset;
					float2 float2 = math.max(value.m_PositionA, value.m_PositionB) + value.m_SideOffset;
					if (!(float2.x < this.m_Area.x) && !(@float.x > this.m_Area.z) && !(float2.y < this.m_Area.y) && !(@float.y > this.m_Area.w))
					{
						this.Result.Add(in value);
					}
				}
			}
		}

		[BurstCompile]
		private struct GenerateClipDataJob : IJobParallelForDefer
		{
			[ReadOnly]
			public NativeList<LaneSection> m_RoadsToCull;

			public NativeList<ClipMapDraw>.ParallelWriter Result;

			public void Execute(int index)
			{
				LaneSection laneSection = this.m_RoadsToCull[index];
				if ((laneSection.m_Flags & LaneFlags.ClipTerrain) != 0)
				{
					laneSection.m_ClipOffset.x -= 0.3f;
					laneSection.m_ClipOffset.y += 0.3f;
					laneSection.m_Left.c1 += laneSection.m_ClipOffset.x;
					laneSection.m_Right.c1 += laneSection.m_ClipOffset.x;
					ClipMapDraw clipMapDraw = default(ClipMapDraw);
					clipMapDraw.m_Left = laneSection.m_Left;
					clipMapDraw.m_Right = laneSection.m_Right;
					clipMapDraw.m_Height = laneSection.m_ClipOffset.y - laneSection.m_ClipOffset.x;
					clipMapDraw.m_OffsetFactor = math.select(1f, -1f, (laneSection.m_Flags & LaneFlags.InverseClipOffset) != 0);
					ClipMapDraw value = clipMapDraw;
					this.Result.AddNoResize(value);
				}
			}
		}

		[BurstCompile]
		private struct CullAreasJob : IJobChunk
		{
			[ReadOnly]
			public ComponentTypeHandle<Clip> m_ClipType;

			[ReadOnly]
			public ComponentTypeHandle<Area> m_AreaType;

			[ReadOnly]
			public ComponentTypeHandle<Geometry> m_GeometryType;

			[ReadOnly]
			public ComponentTypeHandle<Storage> m_StorageType;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

			[ReadOnly]
			public BufferTypeHandle<Game.Areas.Node> m_NodeType;

			[ReadOnly]
			public BufferTypeHandle<Triangle> m_TriangleType;

			[ReadOnly]
			public ComponentLookup<TerrainAreaData> m_PrefabTerrainAreaData;

			[ReadOnly]
			public ComponentLookup<StorageAreaData> m_PrefabStorageAreaData;

			[ReadOnly]
			public float4 m_Area;

			public NativeQueue<AreaTriangle>.ParallelWriter m_Triangles;

			public NativeQueue<AreaEdge>.ParallelWriter m_Edges;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				if (chunk.Has(ref this.m_ClipType))
				{
					return;
				}
				NativeArray<Area> nativeArray = chunk.GetNativeArray(ref this.m_AreaType);
				NativeArray<Geometry> nativeArray2 = chunk.GetNativeArray(ref this.m_GeometryType);
				NativeArray<Storage> nativeArray3 = chunk.GetNativeArray(ref this.m_StorageType);
				NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref this.m_PrefabRefType);
				BufferAccessor<Game.Areas.Node> bufferAccessor = chunk.GetBufferAccessor(ref this.m_NodeType);
				BufferAccessor<Triangle> bufferAccessor2 = chunk.GetBufferAccessor(ref this.m_TriangleType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Area area = nativeArray[i];
					Geometry geometry = nativeArray2[i];
					PrefabRef prefabRef = nativeArray4[i];
					DynamicBuffer<Game.Areas.Node> dynamicBuffer = bufferAccessor[i];
					DynamicBuffer<Triangle> dynamicBuffer2 = bufferAccessor2[i];
					if (geometry.m_Bounds.max.x < this.m_Area.x || geometry.m_Bounds.min.x > this.m_Area.z || geometry.m_Bounds.max.z < this.m_Area.y || geometry.m_Bounds.min.z > this.m_Area.w || dynamicBuffer2.Length == 0 || !this.m_PrefabTerrainAreaData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						continue;
					}
					float2 noiseSize = new float2(componentData.m_NoiseFactor, componentData.m_NoiseScale);
					float num = componentData.m_HeightOffset;
					float num2 = componentData.m_SlopeWidth;
					if (nativeArray3.Length != 0 && this.m_PrefabStorageAreaData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
					{
						Storage storage = nativeArray3[i];
						int y = AreaUtils.CalculateStorageCapacity(geometry, componentData2);
						float num3 = (float)(int)((long)storage.m_Amount * 100L / math.max(1, y)) * 0.015f;
						float num4 = math.min(1f, num3);
						noiseSize.x *= math.clamp(2f - num3, 0.5f, 1f);
						num *= num4;
						num2 *= math.sqrt(num4);
					}
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						Triangle triangle = dynamicBuffer2[j];
						this.m_Triangles.Enqueue(new AreaTriangle
						{
							m_PositionA = dynamicBuffer[triangle.m_Indices.x].m_Position.xz,
							m_PositionB = dynamicBuffer[triangle.m_Indices.y].m_Position.xz,
							m_PositionC = dynamicBuffer[triangle.m_Indices.z].m_Position.xz,
							m_NoiseSize = noiseSize,
							m_HeightDelta = num
						});
					}
					if ((area.m_Flags & AreaFlags.CounterClockwise) != 0)
					{
						float2 xz = dynamicBuffer[0].m_Position.xz;
						float2 @float = dynamicBuffer[1].m_Position.xz;
						float2 xz2 = dynamicBuffer[2].m_Position.xz;
						float2 float2 = math.normalizesafe(@float - xz);
						float2 float3 = math.normalizesafe(xz2 - @float);
						float num5 = MathUtils.RotationAngleRight(-float2, float3);
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							int num6 = k + 3;
							num6 -= math.select(0, dynamicBuffer.Length, num6 >= dynamicBuffer.Length);
							xz = @float;
							@float = xz2;
							xz2 = dynamicBuffer[num6].m_Position.xz;
							float2 = float3;
							float3 = math.normalizesafe(xz2 - @float);
							float y2 = num5;
							num5 = MathUtils.RotationAngleRight(-float2, float3);
							this.m_Edges.Enqueue(new AreaEdge
							{
								m_PositionA = @float,
								m_PositionB = xz,
								m_Angles = new float2(num5, y2),
								m_SideOffset = num2
							});
						}
					}
					else
					{
						float2 xz3 = dynamicBuffer[0].m_Position.xz;
						float2 float4 = dynamicBuffer[1].m_Position.xz;
						float2 xz4 = dynamicBuffer[2].m_Position.xz;
						float2 float5 = math.normalizesafe(float4 - xz3);
						float2 float6 = math.normalizesafe(xz4 - float4);
						float num7 = MathUtils.RotationAngleLeft(-float5, float6);
						for (int l = 0; l < dynamicBuffer.Length; l++)
						{
							int num8 = l + 3;
							num8 -= math.select(0, dynamicBuffer.Length, num8 >= dynamicBuffer.Length);
							xz3 = float4;
							float4 = xz4;
							xz4 = dynamicBuffer[num8].m_Position.xz;
							float5 = float6;
							float6 = math.normalizesafe(xz4 - float4);
							float x = num7;
							num7 = MathUtils.RotationAngleLeft(-float5, float6);
							this.m_Edges.Enqueue(new AreaEdge
							{
								m_PositionA = xz3,
								m_PositionB = float4,
								m_Angles = new float2(x, num7),
								m_SideOffset = num2
							});
						}
					}
				}
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct DequeTrianglesJob : IJob
		{
			[ReadOnly]
			public NativeQueue<AreaTriangle> m_Queue;

			public NativeList<AreaTriangle> m_List;

			public void Execute()
			{
				NativeArray<AreaTriangle> other = this.m_Queue.ToArray(Allocator.Temp);
				this.m_List.CopyFrom(in other);
				other.Dispose();
			}
		}

		[BurstCompile]
		private struct DequeEdgesJob : IJob
		{
			[ReadOnly]
			public NativeQueue<AreaEdge> m_Queue;

			public NativeList<AreaEdge> m_List;

			public void Execute()
			{
				NativeArray<AreaEdge> other = this.m_Queue.ToArray(Allocator.Temp);
				this.m_List.CopyFrom(in other);
				other.Dispose();
			}
		}

		[BurstCompile]
		private struct GenerateAreaClipMeshJob : IJob
		{
			[ReadOnly]
			public NativeList<ArchetypeChunk> m_Chunks;

			[ReadOnly]
			public ComponentTypeHandle<Clip> m_ClipType;

			[ReadOnly]
			public ComponentTypeHandle<Area> m_AreaType;

			[ReadOnly]
			public BufferTypeHandle<Game.Areas.Node> m_NodeType;

			[ReadOnly]
			public BufferTypeHandle<Triangle> m_TriangleType;

			public Mesh.MeshDataArray m_MeshData;

			public void Execute()
			{
				int num = 0;
				int num2 = 0;
				for (int i = 0; i < this.m_Chunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = this.m_Chunks[i];
					if (archetypeChunk.Has(ref this.m_ClipType))
					{
						BufferAccessor<Game.Areas.Node> bufferAccessor = archetypeChunk.GetBufferAccessor(ref this.m_NodeType);
						BufferAccessor<Triangle> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref this.m_TriangleType);
						for (int j = 0; j < bufferAccessor.Length; j++)
						{
							DynamicBuffer<Game.Areas.Node> dynamicBuffer = bufferAccessor[j];
							DynamicBuffer<Triangle> dynamicBuffer2 = bufferAccessor2[j];
							num += dynamicBuffer.Length * 2;
							num2 += dynamicBuffer2.Length * 6 + dynamicBuffer.Length * 6;
						}
					}
				}
				Mesh.MeshData meshData = this.m_MeshData[0];
				NativeArray<VertexAttributeDescriptor> attributes = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory) { [0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4) };
				meshData.SetVertexBufferParams(num, attributes);
				meshData.SetIndexBufferParams(num2, IndexFormat.UInt32);
				attributes.Dispose();
				meshData.subMeshCount = 1;
				meshData.SetSubMesh(0, new SubMeshDescriptor
				{
					vertexCount = num,
					indexCount = num2,
					topology = MeshTopology.Triangles
				}, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
				NativeArray<float4> vertexData = meshData.GetVertexData<float4>();
				NativeArray<uint> indexData = meshData.GetIndexData<uint>();
				SubMeshDescriptor subMesh = meshData.GetSubMesh(0);
				Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
				int num3 = 0;
				int num4 = 0;
				for (int k = 0; k < this.m_Chunks.Length; k++)
				{
					ArchetypeChunk archetypeChunk2 = this.m_Chunks[k];
					if (!archetypeChunk2.Has(ref this.m_ClipType))
					{
						continue;
					}
					NativeArray<Area> nativeArray = archetypeChunk2.GetNativeArray(ref this.m_AreaType);
					BufferAccessor<Game.Areas.Node> bufferAccessor3 = archetypeChunk2.GetBufferAccessor(ref this.m_NodeType);
					BufferAccessor<Triangle> bufferAccessor4 = archetypeChunk2.GetBufferAccessor(ref this.m_TriangleType);
					for (int l = 0; l < nativeArray.Length; l++)
					{
						Area area = nativeArray[l];
						DynamicBuffer<Game.Areas.Node> dynamicBuffer3 = bufferAccessor3[l];
						DynamicBuffer<Triangle> dynamicBuffer4 = bufferAccessor4[l];
						int4 @int = num3 + new int4(0, 1, dynamicBuffer3.Length, dynamicBuffer3.Length + 1);
						float num5 = 0f;
						float num6 = 0f;
						for (int m = 0; m < dynamicBuffer4.Length; m++)
						{
							Triangle triangle = dynamicBuffer4[m];
							int3 indices = triangle.m_Indices;
							num5 = math.min(num5, triangle.m_HeightRange.min);
							num6 = math.max(num6, triangle.m_HeightRange.max);
							int3 int2 = indices + @int.x;
							indexData[num4++] = (uint)int2.z;
							indexData[num4++] = (uint)int2.y;
							indexData[num4++] = (uint)int2.x;
							int3 int3 = indices + @int.z;
							indexData[num4++] = (uint)int3.x;
							indexData[num4++] = (uint)int3.y;
							indexData[num4++] = (uint)int3.z;
						}
						if ((area.m_Flags & AreaFlags.CounterClockwise) != 0)
						{
							for (int n = 0; n < dynamicBuffer3.Length; n++)
							{
								int4 int4 = n + @int;
								int4.yw -= math.select(0, dynamicBuffer3.Length, n == dynamicBuffer3.Length - 1);
								indexData[num4++] = (uint)int4.x;
								indexData[num4++] = (uint)int4.y;
								indexData[num4++] = (uint)int4.w;
								indexData[num4++] = (uint)int4.w;
								indexData[num4++] = (uint)int4.z;
								indexData[num4++] = (uint)int4.x;
							}
						}
						else
						{
							for (int num7 = 0; num7 < dynamicBuffer3.Length; num7++)
							{
								int4 int5 = num7 + @int;
								int5.yw -= math.select(0, dynamicBuffer3.Length, num7 == dynamicBuffer3.Length - 1);
								indexData[num4++] = (uint)int5.x;
								indexData[num4++] = (uint)int5.z;
								indexData[num4++] = (uint)int5.w;
								indexData[num4++] = (uint)int5.w;
								indexData[num4++] = (uint)int5.y;
								indexData[num4++] = (uint)int5.x;
							}
						}
						num5 -= 0.3f;
						num6 += 0.3f;
						for (int num8 = 0; num8 < dynamicBuffer3.Length; num8++)
						{
							float3 position = dynamicBuffer3[num8].m_Position;
							position.y += num5;
							bounds |= position;
							vertexData[num3++] = new float4(position, 0f);
						}
						for (int num9 = 0; num9 < dynamicBuffer3.Length; num9++)
						{
							float3 position2 = dynamicBuffer3[num9].m_Position;
							position2.y += num6;
							bounds |= position2;
							vertexData[num3++] = new float4(position2, 1f);
						}
					}
				}
				subMesh.bounds = RenderingUtils.ToBounds(bounds);
				meshData.SetSubMesh(0, subMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
			}
		}

		[BurstCompile]
		private struct CullRoadsCacscadeJob : IJobParallelForDefer
		{
			[ReadOnly]
			public NativeList<LaneSection> m_RoadsToCull;

			[ReadOnly]
			public float4 m_Area;

			[ReadOnly]
			public float m_Scale;

			public NativeList<LaneDraw>.ParallelWriter Result;

			public void Execute(int index)
			{
				LaneSection laneSection = this.m_RoadsToCull[index];
				if ((laneSection.m_Flags & LaneFlags.ShiftTerrain) != 0 && !math.any(laneSection.m_Bounds.max < this.m_Area.xy) && !math.any(laneSection.m_Bounds.min > this.m_Area.zw))
				{
					float4 minOffset;
					float4 maxOffset;
					float2 widthOffset;
					if ((laneSection.m_Flags & (LaneFlags.MiddleLeft | LaneFlags.MiddleRight)) == (LaneFlags.MiddleLeft | LaneFlags.MiddleRight))
					{
						minOffset = new float4(laneSection.m_MinOffset.yyy, 1f);
						maxOffset = new float4(laneSection.m_MaxOffset.yyy, 1f);
						widthOffset = 0f;
					}
					else if ((laneSection.m_Flags & LaneFlags.MiddleLeft) != 0)
					{
						minOffset = new float4(laneSection.m_MinOffset.yyz, 0.6f);
						maxOffset = new float4(laneSection.m_MaxOffset.yyz, 0.6f);
						widthOffset = new float2(0f, laneSection.m_WidthOffset);
					}
					else if ((laneSection.m_Flags & LaneFlags.MiddleRight) != 0)
					{
						minOffset = new float4(laneSection.m_MinOffset.xyy, 0.6f);
						maxOffset = new float4(laneSection.m_MaxOffset.xyy, 0.6f);
						widthOffset = new float2(laneSection.m_WidthOffset, 0f);
					}
					else
					{
						minOffset = new float4(laneSection.m_MinOffset, 0.8f);
						maxOffset = new float4(laneSection.m_MaxOffset, 0.8f);
						widthOffset = laneSection.m_WidthOffset;
					}
					LaneDraw laneDraw = default(LaneDraw);
					laneDraw.m_Left = laneSection.m_Left;
					laneDraw.m_Right = laneSection.m_Right;
					laneDraw.m_MinOffset = minOffset;
					laneDraw.m_MaxOffset = maxOffset;
					laneDraw.m_WidthOffset = widthOffset;
					LaneDraw value = laneDraw;
					this.Result.AddNoResize(value);
				}
			}
		}

		private struct TypeHandle
		{
			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Updated> __Game_Common_Updated_RO_ComponentTypeHandle;

			[ReadOnly]
			public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

			[ReadOnly]
			public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

			[ReadOnly]
			public ComponentTypeHandle<Game.Areas.Terrain> __Game_Areas_Terrain_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Clip> __Game_Areas_Clip_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Geometry> __Game_Areas_Geometry_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Buildings.Lot> __Game_Buildings_Lot_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Stack> __Game_Objects_Stack_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

			[ReadOnly]
			public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<AssetStampData> __Game_Prefabs_AssetStampData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<AdditionalBuildingTerraformElement> __Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup;

			[ReadOnly]
			public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<TerrainComposition> __Game_Prefabs_TerrainComposition_RO_ComponentLookup;

			[ReadOnly]
			public ComponentTypeHandle<Area> __Game_Areas_Area_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Storage> __Game_Areas_Storage_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<Game.Areas.Node> __Game_Areas_Node_RO_BufferTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

			[ReadOnly]
			public ComponentLookup<TerrainAreaData> __Game_Prefabs_TerrainAreaData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<StorageAreaData> __Game_Prefabs_StorageAreaData_RO_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				this.__Game_Common_Updated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Updated>(isReadOnly: true);
				this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
				this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
				this.__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
				this.__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
				this.__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
				this.__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
				this.__Game_Net_NodeGeometry_RO_ComponentLookup = state.GetComponentLookup<NodeGeometry>(isReadOnly: true);
				this.__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
				this.__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
				this.__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
				this.__Game_Areas_Terrain_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Areas.Terrain>(isReadOnly: true);
				this.__Game_Areas_Clip_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Clip>(isReadOnly: true);
				this.__Game_Areas_Geometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Geometry>(isReadOnly: true);
				this.__Game_Buildings_Lot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Lot>(isReadOnly: true);
				this.__Game_Objects_Elevation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Elevation>(isReadOnly: true);
				this.__Game_Objects_Stack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stack>(isReadOnly: true);
				this.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
				this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
				this.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
				this.__Game_Prefabs_AssetStampData_RO_ComponentLookup = state.GetComponentLookup<AssetStampData>(isReadOnly: true);
				this.__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup = state.GetComponentLookup<BuildingTerraformData>(isReadOnly: true);
				this.__Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup = state.GetBufferLookup<AdditionalBuildingTerraformElement>(isReadOnly: true);
				this.__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
				this.__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
				this.__Game_Prefabs_TerrainComposition_RO_ComponentLookup = state.GetComponentLookup<TerrainComposition>(isReadOnly: true);
				this.__Game_Areas_Area_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Area>(isReadOnly: true);
				this.__Game_Areas_Storage_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Storage>(isReadOnly: true);
				this.__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.Node>(isReadOnly: true);
				this.__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
				this.__Game_Prefabs_TerrainAreaData_RO_ComponentLookup = state.GetComponentLookup<TerrainAreaData>(isReadOnly: true);
				this.__Game_Prefabs_StorageAreaData_RO_ComponentLookup = state.GetComponentLookup<StorageAreaData>(isReadOnly: true);
			}
		}

		private const float kShiftTerrainAmount = 2000f;

		private const float kSoftenTerrainAmount = 1000f;

		private const float kSlopeAndLevelTerrainAmount = 4000f;

		public static readonly int kDefaultHeightmapWidth = 4096;

		public static readonly int kDefaultHeightmapHeight = TerrainSystem.kDefaultHeightmapWidth;

		//private static readonly float2 kDefaultMapSize = new float2(14336f, 14336f);//
        private static readonly float2 kDefaultMapSize = new float2(57344f, 57344f);

        private static readonly float2 kDefaultMapOffset = TerrainSystem.kDefaultMapSize * -0.5f;

		private static readonly float2 kDefaultWorldSize = TerrainSystem.kDefaultMapSize * 4f;

		private static readonly float2 kDefaultWorldOffset = TerrainSystem.kDefaultWorldSize * -0.5f;

		private static readonly float2 kDefaultHeightScaleOffset = new float2(4096f, 0f);

		private AsyncGPUReadbackHelper m_AsyncGPUReadback;

		private NativeArray<ushort> m_CPUHeights;

		private JobHandle m_CPUHeightReaders;

		private RenderTexture m_Heightmap;

		private RenderTexture m_HeightmapCascade;

		private RenderTexture m_HeightmapDepth;

		private RenderTexture m_WorldMapEditable;

		private Vector4 m_MapOffsetScale;

		private bool m_HeightMapChanged;

		private int4 m_LastPreviewWrite;

		private int4 m_LastWorldPreviewWrite;

		private int4 m_LastWrite;

		private int4 m_LastWorldWrite;

		private int4 m_LastRequest;

		private int m_FailCount;

		private Vector4 m_WorldOffsetScale;

		private bool m_NewMap;

		private bool m_NewMapThisFrame;

		private bool m_Loaded;

		private bool m_UpdateOutOfDate;

		private ComputeShader m_AdjustTerrainCS;

		private int m_ShiftTerrainKernal;

		private int m_BlurHorzKernal;

		private int m_BlurVertKernal;

		private int m_SmoothTerrainKernal;

		private int m_LevelTerrainKernal;

		private int m_SlopeTerrainKernal;

		private CommandBuffer m_CommandBuffer;

		private CommandBuffer m_CascadeCB;

		private Material m_TerrainBlit;

		private Material m_ClipMaterial;

		private EntityQuery m_BrushQuery;

		private NativeList<BuildingUtils.LotInfo> m_BuildingCullList;

		private NativeList<LaneSection> m_LaneCullList;

		private NativeList<AreaTriangle> m_TriangleCullList;

		private NativeList<AreaEdge> m_EdgeCullList;

		private JobHandle m_BuildingCull;

		private JobHandle m_LaneCull;

		private JobHandle m_AreaCull;

		private JobHandle m_ClipMapCull;

		private JobHandle m_CullFinished;

		private NativeParallelHashMap<Entity, Entity> m_BuildingUpgrade;

		private JobHandle m_BuildingUpgradeDependencies;

		public const int kCascadeMax = 4;

		private float4 m_LastCullArea;

		private float4[] m_CascadeRanges;

		private Vector4[] m_ShaderCascadeRanges;

		private float4 m_UpdateArea;

		private float4 m_TerrainChangeArea;

		private bool m_CascadeReset;

		private bool m_RoadUpdate;

		private bool m_AreaUpdate;

		private bool m_TerrainChange;

		private EntityQuery m_BuildingsChanged;

		private EntityQuery m_BuildingGroup;

		private EntityQuery m_RoadsChanged;

		private EntityQuery m_RoadsGroup;

		private EntityQuery m_EditorLotQuery;

		private EntityQuery m_AreasChanged;

		private EntityQuery m_AreasQuery;

		private List<CascadeCullInfo> m_CascadeCulling;

		private ManagedStructuredBuffers<BuildingLotDraw> m_BuildingInstanceData;

		private ManagedStructuredBuffers<LaneDraw> m_LaneInstanceData;

		private ManagedStructuredBuffers<AreaTriangle> m_TriangleInstanceData;

		private ManagedStructuredBuffers<AreaEdge> m_EdgeInstanceData;

		private Material m_MasterBuildingLotMaterial;

		private Material m_MasterLaneMaterial;

		private Material m_MasterAreaMaterial;

		private Mesh m_LaneMesh;

		private ToolSystem m_ToolSystem;

		private CameraUpdateSystem m_CameraUpdateSystem;

		private GroundHeightSystem m_GroundHeightSystem;

		private RenderingSystem m_RenderingSystem;

		private WaterSystem m_WaterSystem;

		private NativeList<ClipMapDraw> m_ClipMapList;

		private ManagedStructuredBuffers<ClipMapDraw> m_ClipMapBuffer;

		private ComputeBuffer m_CurrentClipMap;

		private Mesh m_ClipMesh;

		private Mesh m_AreaClipMesh;

		private Mesh.MeshDataArray m_AreaClipMeshData;

		private bool m_HasAreaClipMeshData;

		private JobHandle m_AreaClipMeshDataDeps;

		private TerrainMinMaxMap m_TerrainMinMax;

		private TypeHandle __TypeHandle;

		public Vector4 VTScaleOffset => new Vector4(this.m_WorldOffsetScale.z, this.m_WorldOffsetScale.w, this.m_WorldOffsetScale.x, this.m_WorldOffsetScale.y);

		public bool NewMap => this.m_NewMapThisFrame;

		public Texture heightmap => this.m_Heightmap;

		public Vector4 mapOffsetScale => this.m_MapOffsetScale;

		public float2 heightScaleOffset { get; set; }

		public TextureAsset worldMapAsset { get; set; }

		public Texture worldHeightmap { get; set; }

		public Colossal.Hash128 mapReference { get; set; }

		public float2 playableArea { get; private set; }

		public float2 playableOffset { get; private set; }

		public float2 worldSize { get; private set; }

		public float2 worldOffset { get; private set; }

		public float2 worldHeightMinMax { get; private set; }

		public float3 positionOffset => new float3(this.playableOffset.x, this.heightScaleOffset.y, this.playableOffset.y);

		public bool heightMapRenderRequired { get; private set; }

		public bool[] heightMapSliceUpdated { get; private set; }

		public float4[] heightMapViewport { get; private set; }

		public float4[] heightMapViewportUpdated { get; private set; }

		public float4[] heightMapSliceArea => this.m_CascadeRanges;

		public float4[] heightMapCullArea { get; private set; }

		public bool freezeCascadeUpdates { get; set; }

		public bool[] heightMapSliceUpdatedLast { get; private set; }

		public float4 lastCullArea => this.m_LastCullArea;

		public static int baseLod { get; private set; }

		private ComputeBuffer clipMapBuffer
		{
			get
			{
				if (this.m_CurrentClipMap == null)
				{
					this.m_ClipMapCull.Complete();
					if (this.m_ClipMapList.Length > 0)
					{
						NativeArray<ClipMapDraw> data = this.m_ClipMapList.AsArray();
						this.m_ClipMapBuffer.StartFrame();
						this.m_CurrentClipMap = this.m_ClipMapBuffer.Request(data.Length);
						this.m_CurrentClipMap.SetData(data);
						this.m_ClipMapBuffer.EndFrame();
					}
				}
				return this.m_CurrentClipMap;
			}
		}

		private int clipMapInstances
		{
			get
			{
				this.m_ClipMapCull.Complete();
				return this.m_ClipMapList.Length;
			}
		}

		public Mesh areaClipMesh
		{
			get
			{
				if (this.m_AreaClipMesh == null)
				{
					this.m_AreaClipMesh = new Mesh();
				}
				if (this.m_HasAreaClipMeshData)
				{
					this.m_HasAreaClipMeshData = false;
					this.m_AreaClipMeshDataDeps.Complete();
					Mesh.ApplyAndDisposeWritableMeshData(this.m_AreaClipMeshData, this.m_AreaClipMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
				}
				return this.m_AreaClipMesh;
			}
			private set
			{
				this.m_AreaClipMesh = value;
			}
		}

		private float GetTerrainAdjustmentSpeed(TerraformingType type)
		{
			return type switch
			{
				TerraformingType.Soften => 1000f, 
				TerraformingType.Shift => 2000f, 
				_ => 4000f, 
			};
		}

		public Bounds GetTerrainBounds()
		{
			float3 @float = new float3(0f, (0f - this.heightScaleOffset.y) * 0.5f, 0f);
			return new Bounds(size: new float3(14336f, this.heightScaleOffset.x, 14336f), center: @float);
		}

		public TerrainHeightData GetHeightData(bool waitForPending = false)
		{
			if (waitForPending && this.m_HeightMapChanged)
			{
				this.m_AsyncGPUReadback.WaitForCompletion();
				this.m_CPUHeightReaders.Complete();
				this.m_CPUHeightReaders = default(JobHandle);
				this.UpdateGPUReadback();
			}
			int3 resolution = ((this.m_CPUHeights.IsCreated && !(this.m_HeightmapCascade == null) && this.m_CPUHeights.Length == this.m_HeightmapCascade.width * this.m_HeightmapCascade.height) ? new int3(this.m_HeightmapCascade.width, 65536, this.m_HeightmapCascade.height) : new int3(2, 2, 2));
			float3 @float = new float3(14336f, math.max(1f, this.heightScaleOffset.x), 14336f);
			float3 scale = new float3(resolution.x, resolution.y - 1, resolution.z) / @float;
			float3 offset = -this.positionOffset;
			offset.xz -= 0.5f / scale.xz;
			return new TerrainHeightData(this.m_CPUHeights, resolution, scale, offset);
		}

		public void AddCPUHeightReader(JobHandle handle)
		{
			this.m_CPUHeightReaders = JobHandle.CombineDependencies(this.m_CPUHeightReaders, handle);
		}

		public NativeList<LaneSection> GetRoads()
		{
			this.m_LaneCull.Complete();
			return this.m_LaneCullList;
		}

		public bool GetTerrainBrushUpdate(out float4 viewport)
		{
			viewport = this.m_TerrainChangeArea;
			if (this.m_TerrainChange)
			{
				this.m_TerrainChange = false;
				viewport = new float4(this.m_TerrainChangeArea.x - this.m_CascadeRanges[TerrainSystem.baseLod].x, this.m_TerrainChangeArea.y - this.m_CascadeRanges[TerrainSystem.baseLod].y, this.m_TerrainChangeArea.z - this.m_CascadeRanges[TerrainSystem.baseLod].x, this.m_TerrainChangeArea.w - this.m_CascadeRanges[TerrainSystem.baseLod].y);
				viewport /= new float4(this.m_CascadeRanges[TerrainSystem.baseLod].z - this.m_CascadeRanges[TerrainSystem.baseLod].x, this.m_CascadeRanges[TerrainSystem.baseLod].w - this.m_CascadeRanges[TerrainSystem.baseLod].y, this.m_CascadeRanges[TerrainSystem.baseLod].z - this.m_CascadeRanges[TerrainSystem.baseLod].x, this.m_CascadeRanges[TerrainSystem.baseLod].w - this.m_CascadeRanges[TerrainSystem.baseLod].y);
				viewport.zw -= viewport.xy;
				viewport = this.ClipViewport(viewport);
				this.m_TerrainChangeArea = viewport;
				return true;
			}
			return false;
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_LastCullArea = float4.zero;
			this.freezeCascadeUpdates = false;
			this.m_CPUHeights = new NativeArray<ushort>(4, Allocator.Persistent);
			this.m_AdjustTerrainCS = Resources.Load<ComputeShader>("AdjustTerrain");
			this.m_ShiftTerrainKernal = this.m_AdjustTerrainCS.FindKernel("ShiftTerrain");
			this.m_BlurHorzKernal = this.m_AdjustTerrainCS.FindKernel("HorzBlur");
			this.m_BlurVertKernal = this.m_AdjustTerrainCS.FindKernel("VertBlur");
			this.m_SmoothTerrainKernal = this.m_AdjustTerrainCS.FindKernel("SmoothTerrain");
			this.m_LevelTerrainKernal = this.m_AdjustTerrainCS.FindKernel("LevelTerrain");
			this.m_SlopeTerrainKernal = this.m_AdjustTerrainCS.FindKernel("SlopeTerrain");
			this.m_BuildingUpgrade = new NativeParallelHashMap<Entity, Entity>(1024, Allocator.Persistent);
			this.m_CommandBuffer = new CommandBuffer();
			this.m_CommandBuffer.name = "TerrainAdjust";
			this.m_CascadeCB = new CommandBuffer();
			this.m_CascadeCB.name = "Terrain Cascade";
			Shader shader = Resources.Load<Shader>("BuildingLot");
			this.m_MasterBuildingLotMaterial = new Material(shader);
			Shader shader2 = Resources.Load<Shader>("Lane");
			this.m_MasterLaneMaterial = new Material(shader2);
			Shader shader3 = Resources.Load<Shader>("Area");
			this.m_MasterAreaMaterial = new Material(shader3);
			this.m_TerrainBlit = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("TerrainCascadeBlit"));
			this.m_ClipMaterial = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("RoadClip"));
			this.m_TerrainMinMax = new TerrainMinMaxMap();
			this.m_MapOffsetScale = new Vector4(0f, 0f, 1f, 1f);
			this.m_UpdateArea = float4.zero;
			this.m_TerrainChangeArea = float4.zero;
			this.m_TerrainChange = false;
			this.m_BuildingCullList = new NativeList<BuildingUtils.LotInfo>(1000, Allocator.Persistent);
			this.m_LaneCullList = new NativeList<LaneSection>(1000, Allocator.Persistent);
			this.m_TriangleCullList = new NativeList<AreaTriangle>(100, Allocator.Persistent);
			this.m_EdgeCullList = new NativeList<AreaEdge>(100, Allocator.Persistent);
			this.m_ClipMapList = new NativeList<ClipMapDraw>(1000, Allocator.Persistent);
			this.m_CascadeCulling = new List<CascadeCullInfo>(4);
			for (int i = 0; i < 4; i++)
			{
				this.m_CascadeCulling.Add(new CascadeCullInfo(this.m_MasterBuildingLotMaterial, this.m_MasterLaneMaterial, this.m_MasterAreaMaterial));
			}
			this.m_BuildingInstanceData = new ManagedStructuredBuffers<BuildingLotDraw>(10000);
			this.m_LaneInstanceData = new ManagedStructuredBuffers<LaneDraw>(10000);
			this.m_TriangleInstanceData = new ManagedStructuredBuffers<AreaTriangle>(1000);
			this.m_EdgeInstanceData = new ManagedStructuredBuffers<AreaEdge>(1000);
			this.m_LastPreviewWrite = int4.zero;
			this.m_LastWorldPreviewWrite = int4.zero;
			this.m_LastWorldWrite = int4.zero;
			this.m_LastWrite = int4.zero;
			this.m_LastRequest = int4.zero;
			this.m_FailCount = 0;
			TerrainSystem.baseLod = 0;
			this.m_NewMap = true;
			this.m_NewMapThisFrame = true;
			this.m_CascadeReset = true;
			this.m_RoadUpdate = false;
			this.m_AreaUpdate = false;
			this.m_ClipMapBuffer = new ManagedStructuredBuffers<ClipMapDraw>(10000);
			this.m_CurrentClipMap = null;
			this.heightMapRenderRequired = false;
			this.heightMapSliceUpdated = new bool[4];
			this.heightMapSliceUpdatedLast = new bool[4];
			this.heightMapViewport = new float4[4];
			this.heightMapViewportUpdated = new float4[4];
			this.heightMapCullArea = new float4[4];
			this.m_BrushQuery = base.GetEntityQuery(ComponentType.ReadOnly<Brush>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
			this.m_BuildingsChanged = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[2]
				{
					ComponentType.ReadOnly<Created>(),
					ComponentType.ReadOnly<Game.Objects.Object>()
				},
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Game.Buildings.Lot>(),
					ComponentType.ReadOnly<AssetStamp>(),
					ComponentType.ReadOnly<Pillar>()
				},
				None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
			}, new EntityQueryDesc
			{
				All = new ComponentType[2]
				{
					ComponentType.ReadOnly<Deleted>(),
					ComponentType.ReadOnly<Game.Objects.Object>()
				},
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Game.Buildings.Lot>(),
					ComponentType.ReadOnly<AssetStamp>(),
					ComponentType.ReadOnly<Pillar>()
				},
				None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
			}, new EntityQueryDesc
			{
				All = new ComponentType[2]
				{
					ComponentType.ReadOnly<Updated>(),
					ComponentType.ReadOnly<Game.Objects.Object>()
				},
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Game.Buildings.Lot>(),
					ComponentType.ReadOnly<AssetStamp>(),
					ComponentType.ReadOnly<Pillar>()
				},
				None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
			});
			this.m_BuildingGroup = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.Object>() },
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Game.Buildings.Lot>(),
					ComponentType.ReadOnly<AssetStamp>(),
					ComponentType.ReadOnly<Pillar>()
				},
				None = new ComponentType[2]
				{
					ComponentType.ReadOnly<Deleted>(),
					ComponentType.ReadOnly<Temp>()
				}
			});
			this.m_RoadsChanged = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<EdgeGeometry>() },
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Created>(),
					ComponentType.ReadOnly<Updated>(),
					ComponentType.ReadOnly<Deleted>()
				},
				None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
			}, new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<NodeGeometry>() },
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Created>(),
					ComponentType.ReadOnly<Updated>(),
					ComponentType.ReadOnly<Deleted>()
				},
				None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
			});
			this.m_RoadsGroup = base.GetEntityQuery(new EntityQueryDesc
			{
				Any = new ComponentType[2]
				{
					ComponentType.ReadOnly<EdgeGeometry>(),
					ComponentType.ReadOnly<NodeGeometry>()
				},
				None = new ComponentType[2]
				{
					ComponentType.ReadOnly<Deleted>(),
					ComponentType.ReadOnly<Temp>()
				}
			});
			this.m_AreasChanged = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<Clip>() },
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Created>(),
					ComponentType.ReadOnly<Updated>(),
					ComponentType.ReadOnly<Deleted>()
				},
				None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
			}, new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<Game.Areas.Terrain>() },
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Created>(),
					ComponentType.ReadOnly<Updated>(),
					ComponentType.ReadOnly<Deleted>()
				},
				None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
			});
			this.m_AreasQuery = base.GetEntityQuery(new EntityQueryDesc
			{
				Any = new ComponentType[2]
				{
					ComponentType.ReadOnly<Clip>(),
					ComponentType.ReadOnly<Game.Areas.Terrain>()
				},
				None = new ComponentType[2]
				{
					ComponentType.ReadOnly<Deleted>(),
					ComponentType.ReadOnly<Temp>()
				}
			});
			this.m_EditorLotQuery = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[3]
				{
					ComponentType.ReadOnly<Game.Buildings.Lot>(),
					ComponentType.ReadOnly<Game.Objects.Transform>(),
					ComponentType.ReadOnly<PrefabRef>()
				},
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Temp>(),
					ComponentType.ReadOnly<Error>(),
					ComponentType.ReadOnly<Warning>()
				},
				None = new ComponentType[2]
				{
					ComponentType.ReadOnly<Hidden>(),
					ComponentType.ReadOnly<Deleted>()
				}
			}, new EntityQueryDesc
			{
				All = new ComponentType[3]
				{
					ComponentType.ReadOnly<AssetStamp>(),
					ComponentType.ReadOnly<Game.Objects.Transform>(),
					ComponentType.ReadOnly<PrefabRef>()
				},
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<Temp>(),
					ComponentType.ReadOnly<Error>(),
					ComponentType.ReadOnly<Warning>()
				},
				None = new ComponentType[2]
				{
					ComponentType.ReadOnly<Hidden>(),
					ComponentType.ReadOnly<Deleted>()
				}
			});
			this.m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
			this.m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
			this.m_GroundHeightSystem = base.World.GetOrCreateSystemManaged<GroundHeightSystem>();
			this.m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
			this.m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
			this.CreateRoadMeshes();
			this.m_Heightmap = null;
			this.m_HeightmapCascade = null;
			this.m_HeightmapDepth = null;
			this.m_WorldMapEditable = null;
		}

		[Preserve]
		protected override void OnDestroy()
		{
			CoreUtils.Destroy(this.m_TerrainBlit);
			CoreUtils.Destroy(this.m_ClipMaterial);
			if (this.m_CPUHeights.IsCreated)
			{
				this.m_CPUHeights.Dispose();
			}
			CoreUtils.Destroy(this.m_Heightmap);
			CoreUtils.Destroy(this.m_HeightmapCascade);
			CoreUtils.Destroy(this.m_WorldMapEditable);
			this.worldMapAsset?.Unload();
			CoreUtils.Destroy(this.m_HeightmapDepth);
			if (this.m_BuildingCullList.IsCreated)
			{
				this.m_CullFinished.Complete();
				this.m_BuildingCullList.Dispose();
			}
			if (this.m_LaneCullList.IsCreated)
			{
				this.m_CullFinished.Complete();
				this.m_LaneCullList.Dispose();
			}
			if (this.m_TriangleCullList.IsCreated)
			{
				this.m_CullFinished.Complete();
				this.m_TriangleCullList.Dispose();
			}
			if (this.m_EdgeCullList.IsCreated)
			{
				this.m_CullFinished.Complete();
				this.m_EdgeCullList.Dispose();
			}
			if (this.m_ClipMapList.IsCreated)
			{
				this.m_ClipMapCull.Complete();
				this.m_ClipMapList.Dispose();
			}
			if (this.m_BuildingInstanceData != null)
			{
				this.m_BuildingInstanceData.Dispose();
				this.m_BuildingInstanceData = null;
			}
			if (this.m_LaneInstanceData != null)
			{
				this.m_LaneInstanceData.Dispose();
				this.m_LaneInstanceData = null;
			}
			if (this.m_TriangleInstanceData != null)
			{
				this.m_TriangleInstanceData.Dispose();
				this.m_TriangleInstanceData = null;
			}
			if (this.m_EdgeInstanceData != null)
			{
				this.m_EdgeInstanceData.Dispose();
				this.m_EdgeInstanceData = null;
			}
			if (this.m_ClipMapBuffer != null)
			{
				this.m_ClipMapBuffer.Dispose();
				this.m_ClipMapBuffer = null;
			}
			for (int i = 0; i < 4; i++)
			{
				if (!this.m_CascadeCulling[i].m_BuildingHandle.IsCompleted)
				{
					this.m_CascadeCulling[i].m_BuildingHandle.Complete();
				}
				if (this.m_CascadeCulling[i].m_BuildingRenderList.IsCreated)
				{
					this.m_CascadeCulling[i].m_BuildingRenderList.Dispose();
				}
				if (!this.m_CascadeCulling[i].m_LaneHandle.IsCompleted)
				{
					this.m_CascadeCulling[i].m_LaneHandle.Complete();
				}
				if (this.m_CascadeCulling[i].m_LaneRenderList.IsCreated)
				{
					this.m_CascadeCulling[i].m_LaneRenderList.Dispose();
				}
				if (!this.m_CascadeCulling[i].m_AreaHandle.IsCompleted)
				{
					this.m_CascadeCulling[i].m_AreaHandle.Complete();
				}
				if (this.m_CascadeCulling[i].m_TriangleRenderList.IsCreated)
				{
					this.m_CascadeCulling[i].m_TriangleRenderList.Dispose();
				}
				if (this.m_CascadeCulling[i].m_EdgeRenderList.IsCreated)
				{
					this.m_CascadeCulling[i].m_EdgeRenderList.Dispose();
				}
			}
			if (this.m_BuildingUpgrade.IsCreated)
			{
				this.m_BuildingUpgradeDependencies.Complete();
				this.m_BuildingUpgrade.Dispose();
			}
			this.m_CascadeCB.Dispose();
			this.m_CommandBuffer.Dispose();
			this.m_TerrainMinMax.Dispose();
			base.OnDestroy();
		}

		private unsafe static void SerializeHeightmap<TWriter>(TWriter writer, Texture heightmap) where TWriter : IWriter
		{
			if (heightmap == null)
			{
				writer.Write(0);
				writer.Write(0);
				return;
			}
			writer.Write(heightmap.width);
			writer.Write(heightmap.height);
			NativeArray<ushort> output = new NativeArray<ushort>(heightmap.width * heightmap.height, Allocator.Persistent);
			AsyncGPUReadback.RequestIntoNativeArray(ref output, heightmap).WaitForCompletion();
			NativeArray<byte> nativeArray = new NativeArray<byte>(output.Length * 2, Allocator.Temp);
			NativeCompression.FilterDataBeforeWrite((IntPtr)output.GetUnsafeReadOnlyPtr(), (IntPtr)nativeArray.GetUnsafePtr(), nativeArray.Length, 2);
			output.Dispose();
			writer.Write(nativeArray);
			nativeArray.Dispose();
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			writer.Write(this.mapReference);
			TerrainSystem.SerializeHeightmap(writer, this.worldHeightmap);
			TerrainSystem.SerializeHeightmap(writer, this.m_Heightmap);
			writer.Write(this.heightScaleOffset);
			writer.Write(this.playableOffset);
			writer.Write(this.playableArea);
			writer.Write(this.worldOffset);
			writer.Write(this.worldSize);
			writer.Write(this.worldHeightMinMax);
		}

		private unsafe static Texture2D DeserializeHeightmap<TReader>(TReader reader, string name, ref NativeArray<ushort> unfiltered, bool makeNoLongerReadable) where TReader : IReader
		{
			reader.Read(out int value);
			reader.Read(out int value2);
			if (value != 0 && value2 != 0)
			{
				Texture2D texture2D = new Texture2D(value, value2, GraphicsFormat.R16_UNorm, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate)
				{
					hideFlags = HideFlags.HideAndDontSave,
					name = name,
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp
				};
				using NativeArray<ushort> nativeArray = texture2D.GetRawTextureData<ushort>();
				if (reader.context.version >= Game.Version.terrainWaterSnowCompression)
				{
					if (unfiltered.Length != nativeArray.Length)
					{
						ArrayExtensions.ResizeArray(ref unfiltered, nativeArray.Length);
					}
					NativeArray<byte> nativeArray2 = unfiltered.Reinterpret<byte>(2);
					reader.Read(nativeArray2);
					NativeCompression.UnfilterDataAfterRead((IntPtr)nativeArray2.GetUnsafePtr(), (IntPtr)nativeArray.GetUnsafePtr(), nativeArray2.Length, 2);
				}
				else
				{
					reader.Read(nativeArray);
				}
				texture2D.Apply(updateMipmaps: false, makeNoLongerReadable);
				return texture2D;
			}
			return null;
		}

		private TextureAsset LegacyLoadWorldMap()
		{
			MapMetadata asset = AssetDatabase.global.GetAsset<MapMetadata>(this.mapReference);
			if (asset != null)
			{
				TerrainDesc terrainDesc = asset.LoadAs<TerrainDesc>();
				switch (this.mapReference.ToString())
				{
				case "bf8036b291428535b986c757fda3e627":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("4ea9a2ffbdf3f2e5eaca4b25864d01cd");
					break;
				case "3ca4d7fcb0152c951a9c64b073ae20eb":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("4e96721be8b17b15e83baba2bbf6dde6");
					break;
				case "5b0804a2f2200b950acca980ed0e79c4":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("a4f3ee3c39a4d2a54b638952caed902f");
					break;
				case "3a84420e77337665c8e4e20cfc3f8e82":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("4281ab9d49876d65aab7cb0860c7e181");
					break;
				case "09976b87610b2235e871ef990fe1d641":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("bf0a154278bf44d5ca4615542c6a8328");
					break;
				case "f9612145c1e3be953a425a1a50ae2331":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("5924ad730abd94854821258372d4fb51");
					break;
				case "fc36d01de91f7f2558871a788caca83e":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("b18abfca21d528f549bf2ca158497023");
					break;
				case "28793380017d5135995a603dda66f808":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("41eb8bd1626d000568fc39f052c32869");
					break;
				case "0de682325fee4145d8f6b476abc1b49f":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("bbb43ef6eeb2c9a58bcd8f088e86ea4d");
					break;
				case "419f928408010f45fa6bff8926656ffe":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("7030399925b76835cb458e9eba8959d0");
					break;
				case "3e902647f5783aa50afa3a45d4bd567f":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("47af40991307d3055b8317f3d8de2986");
					break;
				case "e5740c2468c1bc85fa21203c46ac140d":
					terrainDesc.worldHeightMapGuid = new Colossal.Hash128("22b3607ae209c6a568e68664db333db3");
					break;
				}
				return AssetDatabase.global.GetAsset<TextureAsset>(terrainDesc.worldHeightMapGuid);
			}
			return null;
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			this.m_Loaded = true;
			if (reader.context.version >= Game.Version.terrainGuidToHash)
			{
				reader.Read(out Colossal.Hash128 value);
				this.mapReference = value;
			}
			else
			{
				reader.Read(out string value2);
				this.mapReference = Guid.Parse(value2);
			}
			if (reader.context.version >= Game.Version.terrainInSaves)
			{
				Texture2D texture2D = null;
				TextureAsset textureAsset = null;
				if (reader.context.version >= Game.Version.worldmapInSaves)
				{
					texture2D = TerrainSystem.DeserializeHeightmap(reader, "LoadedWorldHeightMap", ref this.m_CPUHeights, makeNoLongerReadable: true);
				}
				else
				{
					textureAsset = this.LegacyLoadWorldMap();
					texture2D = textureAsset?.Load(0) as Texture2D;
				}
				Texture2D texture2D2 = TerrainSystem.DeserializeHeightmap(reader, "LoadedHeightmap", ref this.m_CPUHeights, makeNoLongerReadable: false);
				reader.Read(out float2 value3);
				reader.Read(out float2 value4);
				reader.Read(out float2 value5);
				reader.Read(out float2 value6);
				reader.Read(out float2 value7);
				reader.Read(out float2 value8);
				this.InitializeTerrainData(texture2D2, texture2D, value3, value4, value5, value6, value7, value8);
				if (textureAsset != this.worldMapAsset)
				{
					this.worldMapAsset?.Unload();
				}
				this.worldMapAsset = textureAsset;
				UnityEngine.Object.Destroy(texture2D2);
				return;
			}
			throw new NotSupportedException($"Saves prior to {Game.Version.terrainInSaves} are no longer supported");
		}

		public void SetDefaults(Context context)
		{
			this.m_Loaded = true;
			this.mapReference = default(Colossal.Hash128);
			this.LoadTerrain();
		}

		public void Clear()
		{
			CoreUtils.Destroy(this.m_Heightmap);
			this.mapReference = default(Colossal.Hash128);
		}

		private void LoadTerrain()
		{
			this.InitializeTerrainData(null, null, TerrainSystem.kDefaultHeightScaleOffset, TerrainSystem.kDefaultMapOffset, TerrainSystem.kDefaultMapSize, TerrainSystem.kDefaultWorldOffset, TerrainSystem.kDefaultWorldSize, float2.zero);
		}

		private void InitializeTerrainData(Texture2D inMap, Texture2D worldMap, float2 heightScaleOffset, float2 inMapCorner, float2 inMapSize, float2 inWorldCorner, float2 inWorldSize, float2 inWorldHeightMinMax)
		{
			Texture2D texture2D = ((inMap != null) ? inMap : this.CreateDefaultHeightmap((worldMap != null) ? worldMap.width : TerrainSystem.kDefaultHeightmapWidth, (worldMap != null) ? worldMap.height : TerrainSystem.kDefaultHeightmapHeight));
			this.SetHeightmap(texture2D);
			this.SetWorldHeightmap(worldMap, this.m_ToolSystem.actionMode.IsEditor());
			this.FinalizeTerrainData(texture2D, worldMap, heightScaleOffset, inMapCorner, inMapSize, inWorldCorner, inWorldSize, inWorldHeightMinMax);
			if (texture2D != inMap)
			{
				UnityEngine.Object.Destroy(texture2D);
			}
		}

		public void ReplaceHeightmap(Texture2D inMap)
		{
			Texture2D texture2D = ((inMap != null) ? inMap : this.CreateDefaultHeightmap((this.worldHeightmap != null) ? this.worldHeightmap.width : TerrainSystem.kDefaultHeightmapWidth, (this.worldHeightmap != null) ? this.worldHeightmap.height : TerrainSystem.kDefaultHeightmapHeight));
			Texture2D texture2D2 = TerrainSystem.ToR16(texture2D);
			this.SetHeightmap(texture2D2);
			this.FinalizeTerrainData(texture2D2, null, this.heightScaleOffset, TerrainSystem.kDefaultMapOffset, TerrainSystem.kDefaultMapSize, TerrainSystem.kDefaultWorldOffset, TerrainSystem.kDefaultWorldSize, this.worldHeightMinMax);
			if (texture2D2 != texture2D)
			{
				UnityEngine.Object.Destroy(texture2D2);
			}
			if (texture2D != inMap)
			{
				UnityEngine.Object.Destroy(texture2D);
			}
		}

		public void ReplaceWorldHeightmap(Texture2D inMap)
		{
			Texture2D texture2D = TerrainSystem.ToR16(inMap);
			this.SetWorldHeightmap(texture2D, this.m_ToolSystem.actionMode.IsEditor());
			this.FinalizeTerrainData(null, texture2D, this.heightScaleOffset, TerrainSystem.kDefaultMapOffset, TerrainSystem.kDefaultMapSize, TerrainSystem.kDefaultWorldOffset, TerrainSystem.kDefaultWorldSize, float2.zero);
			if (texture2D != inMap && texture2D != this.worldHeightmap)
			{
				UnityEngine.Object.Destroy(texture2D);
			}
		}

		public void SetTerrainProperties(float2 heightScaleOffset)
		{
			this.FinalizeTerrainData(null, null, heightScaleOffset, this.playableOffset, this.playableArea, this.worldOffset, this.worldSize, this.worldHeightMinMax);
		}

		private void SetHeightmap(Texture2D map)
		{
			if (this.m_Heightmap == null || this.m_Heightmap.width != map.width || this.m_Heightmap.height != map.height)
			{
				if (this.m_Heightmap != null)
				{
					this.m_Heightmap.Release();
					UnityEngine.Object.Destroy(this.m_Heightmap);
				}
				this.m_Heightmap = new RenderTexture(map.width, map.height, 0, GraphicsFormat.R16_UNorm)
				{
					hideFlags = HideFlags.HideAndDontSave,
					enableRandomWrite = true,
					name = "TerrainHeights",
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp
				};
				this.m_Heightmap.Create();
			}
			Graphics.CopyTexture(map, this.m_Heightmap);
			if (this.worldHeightmap != null && (this.worldHeightmap.width != this.m_Heightmap.width || this.worldHeightmap.height != this.m_Heightmap.height))
			{
				this.DestroyWorldMap();
			}
		}

		private void SetWorldHeightmap(Texture2D map, bool isEditor)
		{
			if (map == null || map.width != this.m_Heightmap.width || map.height != this.m_Heightmap.height)
			{
				this.DestroyWorldMap();
			}
			else if (isEditor)
			{
				if (this.m_WorldMapEditable == null || this.worldHeightmap != this.m_WorldMapEditable || this.m_WorldMapEditable.width != map.width || this.m_WorldMapEditable.height != map.height)
				{
					this.DestroyWorldMap();
					this.m_WorldMapEditable = new RenderTexture(map.width, map.height, 0, GraphicsFormat.R16_UNorm)
					{
						hideFlags = HideFlags.HideAndDontSave,
						enableRandomWrite = true,
						name = "TerrainWorldHeights",
						filterMode = FilterMode.Bilinear,
						wrapMode = TextureWrapMode.Clamp
					};
					this.m_WorldMapEditable.Create();
					this.worldHeightmap = this.m_WorldMapEditable;
				}
				Graphics.CopyTexture(map, this.m_WorldMapEditable);
			}
			else
			{
				if (map != this.worldHeightmap && (this.m_WorldMapEditable != null || this.worldHeightmap != null))
				{
					this.DestroyWorldMap();
				}
				this.worldHeightmap = map;
			}
		}

		private void FinalizeTerrainData(Texture2D map, Texture2D worldMap, float2 heightScaleOffset, float2 inMapCorner, float2 inMapSize, float2 inWorldCorner, float2 inWorldSize, float2 inWorldHeightMinMax)
		{
			this.heightScaleOffset = heightScaleOffset;
			if (math.all(inWorldSize == inMapSize) || this.worldHeightmap == null)
			{
				TerrainSystem.baseLod = 0;
				this.playableArea = inMapSize;
				this.worldSize = inMapSize;
				this.playableOffset = inMapCorner;
				this.worldOffset = inMapCorner;
			}
			else
			{
				TerrainSystem.baseLod = 1;
				this.playableArea = inMapSize;
				this.worldSize = inWorldSize;
				this.playableOffset = inMapCorner;
				this.worldOffset = inWorldCorner;
			}
			this.m_NewMap = true;
			this.m_NewMapThisFrame = true;
			this.m_CascadeReset = true;
			this.worldHeightMinMax = inWorldHeightMinMax;
			this.m_WorldOffsetScale = new float4((this.playableOffset - this.worldOffset) / this.worldSize, this.playableArea / this.worldSize);
			float3 @float = new float3(this.playableArea.x, heightScaleOffset.x, this.playableArea.y);
			float3 xyz = 1f / @float;
			float3 xyz2 = -this.positionOffset;
			this.m_MapOffsetScale = new Vector4(0f - this.positionOffset.x, 0f - this.positionOffset.z, 1f / @float.x, 1f / @float.z);
			if (this.m_HeightmapCascade == null || this.m_HeightmapCascade.width != this.heightmap.width || this.m_HeightmapCascade.height != this.heightmap.height)
			{
				if (this.m_HeightmapCascade != null)
				{
					this.m_HeightmapCascade.Release();
					UnityEngine.Object.Destroy(this.m_HeightmapCascade);
					this.m_HeightmapCascade = null;
				}
				this.m_HeightmapCascade = new RenderTexture(this.heightmap.width, this.heightmap.height, 0, GraphicsFormat.R16_UNorm)
				{
					hideFlags = HideFlags.HideAndDontSave,
					enableRandomWrite = false,
					name = "TerrainHeightsCascade",
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp,
					dimension = TextureDimension.Tex2DArray,
					volumeDepth = 4
				};
				this.m_HeightmapCascade.Create();
			}
			if (this.m_HeightmapDepth == null || this.m_HeightmapDepth.width != this.heightmap.width || this.m_HeightmapDepth.height != this.heightmap.height)
			{
				if (this.m_HeightmapDepth != null)
				{
					this.m_HeightmapDepth.Release();
					UnityEngine.Object.Destroy(this.m_HeightmapDepth);
					this.m_HeightmapDepth = null;
				}
				this.m_HeightmapDepth = new RenderTexture(this.heightmap.width, this.heightmap.height, 16, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear)
				{
					name = "HeightmapDepth"
				};
				this.m_HeightmapDepth.Create();
			}
			if (map != null)
			{
				Graphics.CopyTexture(map, 0, 0, this.m_HeightmapCascade, TerrainSystem.baseLod, 0);
			}
			this.m_CascadeRanges = new float4[4];
			this.m_ShaderCascadeRanges = new Vector4[4];
			for (int i = 0; i < 4; i++)
			{
				this.m_CascadeRanges[i] = new float4(0f, 0f, 0f, 0f);
			}
			this.m_CascadeRanges[TerrainSystem.baseLod] = new float4(this.playableOffset, this.playableOffset + this.playableArea);
			if (TerrainSystem.baseLod > 0)
			{
				this.m_CascadeRanges[0] = new float4(this.worldOffset, this.worldOffset + this.worldSize);
				if (worldMap != null)
				{
					Graphics.CopyTexture(worldMap, 0, 0, this.m_HeightmapCascade, 0, 0);
				}
			}
			this.m_UpdateArea = new float4(this.m_CascadeRanges[TerrainSystem.baseLod]);
			Shader.SetGlobalTexture("colossal_TerrainTexture", this.m_Heightmap);
			Shader.SetGlobalVector("colossal_TerrainScale", new float4(xyz, 0f));
			Shader.SetGlobalVector("colossal_TerrainOffset", new float4(xyz2, 0f));
			Shader.SetGlobalVector("colossal_TerrainCascadeLimit", new float4(0.5f / (float)this.m_HeightmapCascade.width, 0.5f / (float)this.m_HeightmapCascade.height, 0f, 0f));
			Shader.SetGlobalTexture("colossal_TerrainTextureArray", this.m_HeightmapCascade);
			Shader.SetGlobalInt("colossal_TerrainTextureArrayBaseLod", TerrainSystem.baseLod);
			if (map != null)
			{
				this.m_CPUHeightReaders.Complete();
				this.m_CPUHeightReaders = default(JobHandle);
				this.WriteCPUHeights(map.GetRawTextureData<ushort>());
			}
			this.m_TerrainMinMax.Init((this.worldHeightmap != null) ? 1024 : 512, (this.worldHeightmap != null) ? this.worldHeightmap.width : this.m_Heightmap.width);
			this.m_TerrainMinMax.UpdateMap(this, this.m_Heightmap, this.worldHeightmap);
		}

		private void DestroyWorldMap()
		{
			if (this.worldHeightmap != null)
			{
				if (this.worldHeightmap is RenderTexture renderTexture)
				{
					renderTexture.Release();
				}
				UnityEngine.Object.Destroy(this.worldHeightmap);
				this.worldHeightmap = null;
			}
			if (this.m_WorldMapEditable != null)
			{
				this.m_WorldMapEditable.Release();
				UnityEngine.Object.Destroy(this.m_WorldMapEditable);
				this.m_WorldMapEditable = null;
			}
			if (this.worldMapAsset != null)
			{
				this.worldMapAsset.Unload();
				this.worldMapAsset = null;
			}
		}

		private Texture2D CreateDefaultHeightmap(int width, int height)
		{
			Texture2D obj = new Texture2D(width, height, GraphicsFormat.R16_UNorm, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate)
			{
				hideFlags = HideFlags.HideAndDontSave,
				name = "DefaultHeightmap",
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			TerrainSystem.SetDefaultHeights(obj);
			return obj;
		}

		private static void SetDefaultHeights(Texture2D targetHeightmap)
		{
			NativeArray<ushort> rawTextureData = targetHeightmap.GetRawTextureData<ushort>();
			ushort value = 8191;
			for (int i = 0; i < rawTextureData.Length; i++)
			{
				rawTextureData[i] = value;
			}
			targetHeightmap.Apply(updateMipmaps: false, makeNoLongerReadable: false);
		}

		private static Texture2D ToR16(Texture2D textureRGBA64)
		{
			if (textureRGBA64 != null && textureRGBA64.graphicsFormat != GraphicsFormat.R16_UNorm)
			{
				NativeArray<ushort> rawTextureData = textureRGBA64.GetRawTextureData<ushort>();
				NativeArray<ushort> data = new NativeArray<ushort>(textureRGBA64.width * textureRGBA64.height, Allocator.Temp);
				for (int i = 0; i < data.Length; i++)
				{
					data[i] = rawTextureData[i * 4];
				}
				Texture2D texture2D = new Texture2D(textureRGBA64.width, textureRGBA64.height, GraphicsFormat.R16_UNorm, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
				texture2D.SetPixelData(data, 0);
				texture2D.Apply();
				return texture2D;
			}
			return textureRGBA64;
		}

		public static bool IsValidHeightmapFormat(Texture2D tex)
		{
			if (tex.width == TerrainSystem.kDefaultHeightmapWidth && tex.height == TerrainSystem.kDefaultHeightmapHeight)
			{
				if (tex.graphicsFormat != GraphicsFormat.R16_UNorm)
				{
					return tex.graphicsFormat == GraphicsFormat.R16G16B16A16_UNorm;
				}
				return true;
			}
			return false;
		}

		private void SaveBitmap(NativeArray<ushort> buffer, int width, int height)
		{
			using System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(File.OpenWrite("heightmapResult.raw"));
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					binaryWriter.Write(buffer[j + i * width]);
				}
			}
		}

		private void EnsureCPUHeights(int length)
		{
			if (this.m_CPUHeights.IsCreated)
			{
				if (this.m_CPUHeights.Length != length)
				{
					this.m_CPUHeights.Dispose();
					this.m_CPUHeights = new NativeArray<ushort>(length, Allocator.Persistent);
				}
			}
			else
			{
				this.m_CPUHeights = new NativeArray<ushort>(length, Allocator.Persistent);
			}
		}

		private void WriteCPUHeights(NativeArray<ushort> buffer)
		{
			this.EnsureCPUHeights(buffer.Length);
			this.m_CPUHeights.CopyFrom(buffer);
			this.m_GroundHeightSystem.AfterReadHeights();
		}

		private void WriteCPUHeights(NativeArray<ushort> buffer, int4 offsets)
		{
			for (int i = 0; i < offsets.w; i++)
			{
				int dstIndex = (offsets.y + i) * this.m_HeightmapCascade.width + offsets.x;
				NativeArray<ushort>.Copy(buffer, i * offsets.z, this.m_CPUHeights, dstIndex, offsets.z);
			}
			this.m_GroundHeightSystem.AfterReadHeights();
		}

		private void UpdateGPUReadback()
		{
			this.m_TerrainMinMax.Update();
			if (this.m_AsyncGPUReadback.isPending)
			{
				if (!this.m_AsyncGPUReadback.hasError)
				{
					if (this.m_AsyncGPUReadback.done)
					{
						NativeArray<ushort> data = this.m_AsyncGPUReadback.GetData<ushort>();
						this.WriteCPUHeights(data, this.m_LastRequest);
						if (this.m_UpdateOutOfDate)
						{
							this.m_UpdateOutOfDate = false;
							this.OnHeightsChanged();
						}
						else
						{
							this.m_HeightMapChanged = false;
						}
						this.m_FailCount = 0;
					}
					this.m_AsyncGPUReadback.IncrementFrame();
				}
				else if (++this.m_FailCount < 10)
				{
					this.m_GroundHeightSystem.BeforeReadHeights();
					this.m_AsyncGPUReadback.Request(this.m_HeightmapCascade, 0, this.m_LastRequest.x, this.m_LastRequest.z, this.m_LastRequest.y, this.m_LastRequest.w, TerrainSystem.baseLod, 1);
				}
				else
				{
					COSystemBase.baseLog.Error("m_AsyncGPUReadback.hasError");
					this.m_LastRequest = new int4(0, 0, this.m_HeightmapCascade.width, this.m_HeightmapCascade.height);
					this.m_GroundHeightSystem.BeforeReadHeights();
					this.m_AsyncGPUReadback.Request(this.m_HeightmapCascade, 0, 0, this.m_HeightmapCascade.width, 0, this.m_HeightmapCascade.height, TerrainSystem.baseLod, 1);
				}
			}
			else
			{
				this.m_HeightMapChanged = false;
			}
		}

		public void TriggerAsyncChange()
		{
			this.m_UpdateOutOfDate = this.m_AsyncGPUReadback.isPending;
			this.m_HeightMapChanged = true;
			if (!this.m_UpdateOutOfDate)
			{
				this.OnHeightsChanged();
			}
		}

		public void HandleNewMap()
		{
			this.m_NewMap = false;
		}

		private void OnHeightsChanged()
		{
			this.m_LastRequest = this.m_LastWrite;
			this.m_LastWrite = int4.zero;
			if (this.m_LastRequest.z == 0 || this.m_LastRequest.w == 0)
			{
				this.m_LastRequest = new int4(0, 0, this.m_HeightmapCascade.width, this.m_HeightmapCascade.height);
			}
			this.m_GroundHeightSystem.BeforeReadHeights();
			this.m_AsyncGPUReadback.Request(this.m_HeightmapCascade, 0, this.m_LastRequest.x, this.m_LastRequest.z, this.m_LastRequest.y, this.m_LastRequest.w, TerrainSystem.baseLod, 1);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			this.m_NewMapThisFrame = this.m_NewMap;
			if (!(this.m_Heightmap == null))
			{
				this.m_CPUHeightReaders.Complete();
				this.m_CPUHeightReaders = default(JobHandle);
				if (!this.freezeCascadeUpdates)
				{
					this.UpdateCascades(this.m_Loaded);
					this.m_Loaded = false;
				}
				this.UpdateGPUReadback();
				this.UpdateGPUTerrain();
			}
		}

		private void UpdateGPUTerrain()
		{
			TerrainSurface validSurface = TerrainSurface.GetValidSurface();
			if (!(validSurface != null))
			{
				return;
			}
			validSurface.UsesCascade = true;
			this.GetCascadeInfo(out var _, out validSurface.BaseLOD, out var areas, out var ranges, out var size);
			validSurface.CascadeArea = areas;
			validSurface.CascadeRanges = ranges;
			validSurface.CascadeSizes = size;
			validSurface.CascadeTexture = this.m_HeightmapCascade;
			validSurface.TerrainHeightOffset = this.heightScaleOffset.y;
			validSurface.TerrainHeightScale = this.heightScaleOffset.x;
			if (validSurface.RenderClipAreas != null)
			{
				return;
			}
			validSurface.RenderClipAreas = delegate(RenderGraphContext ctx, HDCamera hdCamera)
			{
				Camera camera = hdCamera.camera;
				bool flag = false;
				float w = math.tan(math.radians(camera.fieldOfView) * 0.5f) * 0.002f;
				this.m_ClipMaterial.SetBuffer(ShaderID._RoadData, this.clipMapBuffer);
				this.m_ClipMaterial.SetVector(ShaderID._ClipOffset, new float4(camera.transform.position, w));
				if (this.clipMapInstances > 0)
				{
					ctx.cmd.DrawMeshInstancedProcedural(this.m_ClipMesh, 0, this.m_ClipMaterial, 0, this.clipMapInstances);
				}
				if (this.m_RenderingSystem.hideOverlay || this.m_ToolSystem.activeTool == null || (this.m_ToolSystem.activeTool.requireAreas & AreaTypeMask.Surfaces) == 0)
				{
					ctx.cmd.DrawMesh(this.areaClipMesh, Matrix4x4.identity, this.m_ClipMaterial, 0, 2);
				}
				ctx.cmd.DrawProcedural(Matrix4x4.identity, this.m_ClipMaterial, flag ? 4 : 3, MeshTopology.Triangles, 3, 1);
			};
		}

		private void ApplyToTerrain(RenderTexture target, RenderTexture source, float delta, TerraformingType type, Bounds2 area, Brush brush, Texture texture, bool worldMap)
		{
			if (target == null || !target.IsCreated())
			{
				return;
			}
			if (delta == 0f || brush.m_Strength == 0f)
			{
				if (worldMap && source != null && this.m_LastWorldPreviewWrite.z != 0)
				{
					this.m_CommandBuffer.Clear();
					this.m_CommandBuffer.CopyTexture(source, 0, 0, this.m_LastWorldPreviewWrite.x, this.m_LastWorldPreviewWrite.y, this.m_LastWorldPreviewWrite.z, this.m_LastWorldPreviewWrite.w, target, 0, 0, this.m_LastWorldPreviewWrite.x, this.m_LastWorldPreviewWrite.y);
					Graphics.ExecuteCommandBuffer(this.m_CommandBuffer);
					this.m_LastWorldPreviewWrite = Unity.Mathematics.int4.zero;
				}
				if (!worldMap && source != null && this.m_LastPreviewWrite.z != 0)
				{
					this.m_CommandBuffer.Clear();
					this.m_CommandBuffer.CopyTexture(source, 0, 0, this.m_LastPreviewWrite.x, this.m_LastPreviewWrite.y, this.m_LastPreviewWrite.z, this.m_LastPreviewWrite.w, target, 0, 0, this.m_LastPreviewWrite.x, this.m_LastPreviewWrite.y);
					Graphics.ExecuteCommandBuffer(this.m_CommandBuffer);
					this.m_LastPreviewWrite = Unity.Mathematics.int4.zero;
				}
				return;
			}
			float x = delta * brush.m_Strength * this.GetTerrainAdjustmentSpeed(type) / this.heightScaleOffset.x;
			float2 @float = (worldMap ? this.worldSize : this.playableArea);
			float2 float2 = (worldMap ? this.worldOffset : this.playableOffset);
			float num = math.max(@float.x, @float.y);
			float2 float3 = (brush.m_Position.xz - float2) / @float;
			this.m_GroundHeightSystem.GetUpdateBuffer().Add(in area);
			if (math.lengthsq(this.m_UpdateArea) > 0f)
			{
				this.m_UpdateArea.xy = math.min(this.m_UpdateArea.xy, area.min);
				this.m_UpdateArea.zw = math.max(this.m_UpdateArea.zw, area.max);
			}
			else
			{
				this.m_UpdateArea = new float4(area.min, area.max);
			}
			if (!this.m_TerrainChange)
			{
				this.m_TerrainChange = true;
				this.m_TerrainChangeArea = new float4(area.min, area.max);
			}
			else
			{
				this.m_TerrainChangeArea.xy = math.min(this.m_TerrainChangeArea.xy, area.min);
				this.m_TerrainChangeArea.zw = math.max(this.m_TerrainChangeArea.zw, area.max);
			}
			area.min -= float2;
			area.max -= float2;
			area.min /= @float;
			area.max /= @float;
			int4 @int = new int4((int)math.max(math.floor(area.min.x * (float)target.width), 0f), (int)math.max(math.floor(area.min.y * (float)target.height), 0f), (int)math.min(math.ceil(area.max.x * (float)target.width), target.width - 1), (int)math.min(math.ceil(area.max.y * (float)target.height), target.height - 1));
			Vector4 val = new Vector4(float3.x, float3.y, brush.m_Size / num * 0.5f, brush.m_Angle);
			int num2 = @int.z - @int.x + 1;
			int num3 = @int.w - @int.y + 1;
			int threadGroupsX = (num2 + 7) / 8;
			int threadGroupsY = (num3 + 7) / 8;
			this.m_CommandBuffer.Clear();
			int4 int2 = new int4(math.max(@int.x - 2, 0), math.max(@int.y - 2, 0), num2 + 4, num3 + 4);
			if (int2.x + int2.z < 0 || int2.x > target.width || int2.y + int2.w < 0 || int2.y > target.height || num2 <= 0 || num3 <= 0)
			{
				return;
			}
			if (int2.x + int2.z > target.width)
			{
				int2.z = target.width - int2.x;
			}
			if (int2.y + int2.w > target.height)
			{
				int2.w = target.height - int2.y;
			}
			if (source != null)
			{
				if (worldMap)
				{
					if (this.m_LastWorldPreviewWrite.z == 0)
					{
						this.m_CommandBuffer.CopyTexture(source, target);
					}
					else
					{
						this.m_CommandBuffer.CopyTexture(source, 0, 0, this.m_LastWorldPreviewWrite.x, this.m_LastWorldPreviewWrite.y, this.m_LastWorldPreviewWrite.z, this.m_LastWorldPreviewWrite.w, target, 0, 0, this.m_LastWorldPreviewWrite.x, this.m_LastWorldPreviewWrite.y);
					}
					this.m_LastWorldPreviewWrite = int2;
				}
				else
				{
					if (this.m_LastPreviewWrite.z == 0)
					{
						this.m_CommandBuffer.CopyTexture(source, target);
					}
					else
					{
						this.m_CommandBuffer.CopyTexture(source, 0, 0, this.m_LastPreviewWrite.x, this.m_LastPreviewWrite.y, this.m_LastPreviewWrite.z, this.m_LastPreviewWrite.w, target, 0, 0, this.m_LastPreviewWrite.x, this.m_LastPreviewWrite.y);
						float4 float4 = new float4((float)this.m_LastPreviewWrite.x * (1f / (float)target.width), (float)this.m_LastPreviewWrite.y * (1f / (float)target.width), (float)this.m_LastPreviewWrite.z * (1f / (float)target.width), (float)this.m_LastPreviewWrite.w * (1f / (float)target.width));
						float4 float5 = new float4(float2 + float4.xy * @float, float2 + (float4.xy + float4.zw) * @float);
						this.m_UpdateArea.xy = math.min(this.m_UpdateArea.xy, float5.xy);
						this.m_UpdateArea.zw = math.max(this.m_UpdateArea.zw, float5.zw);
					}
					this.m_LastPreviewWrite = int2;
				}
			}
			else if (worldMap)
			{
				if (this.m_LastWorldWrite.z == 0)
				{
					this.m_LastWorldWrite = int2;
				}
				else
				{
					int2 int3 = new int2(math.min(this.m_LastWorldWrite.x, int2.x), math.min(this.m_LastWorldWrite.y, int2.y));
					int2 int4 = new int2(math.max(this.m_LastWorldWrite.x + this.m_LastWorldWrite.z, int2.x + int2.z), math.max(this.m_LastWorldWrite.y + this.m_LastWorldWrite.w, int2.y + int2.w));
					this.m_LastWorldWrite.xy = int3;
					this.m_LastWorldWrite.zw = int4 - int3;
				}
			}
			else if (this.m_LastWrite.z == 0)
			{
				this.m_LastWrite = int2;
			}
			else
			{
				int2 int5 = new int2(math.min(this.m_LastWrite.x, int2.x), math.min(this.m_LastWrite.y, int2.y));
				int2 int6 = new int2(math.max(this.m_LastWrite.x + this.m_LastWrite.z, int2.x + int2.z), math.max(this.m_LastWrite.y + this.m_LastWrite.w, int2.y + int2.w));
				this.m_LastWrite.xy = int5;
				this.m_LastWrite.zw = int6 - int5;
			}
			this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._CenterSizeRotation, val);
			this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._Dims, new Vector4(num, target.width, target.height, 0f));
			int num4 = 0;
			Vector4 val2 = new Vector4(x, 0f, 0f, 0f);
			Vector4 val3 = Vector4.zero;
			switch (type)
			{
			case TerraformingType.Shift:
				num4 = this.m_ShiftTerrainKernal;
				break;
			case TerraformingType.Level:
				num4 = this.m_LevelTerrainKernal;
				val2.y = (brush.m_Target.y - this.positionOffset.y) / this.heightScaleOffset.x;
				break;
			case TerraformingType.Slope:
			{
				num4 = this.m_SlopeTerrainKernal;
				float3 float6 = brush.m_Target - brush.m_Start;
				val2.y = (brush.m_Target.y - this.positionOffset.y) / this.heightScaleOffset.x;
				val2.z = (brush.m_Start.y - this.positionOffset.y) / this.heightScaleOffset.x;
				val2.w = float6.y / this.heightScaleOffset.x;
				float4 zero = float4.zero;
				zero.xy = math.normalize(float6.xz);
				zero.z = 0f - math.dot((brush.m_Start.xz - float2) / @float, zero.xy);
				zero.w = math.length(float6.xz) / num;
				val3 = zero;
				break;
			}
			case TerraformingType.Soften:
			{
				RenderTextureDescriptor renderTextureDescriptor = default(RenderTextureDescriptor);
				renderTextureDescriptor.autoGenerateMips = false;
				renderTextureDescriptor.bindMS = false;
				renderTextureDescriptor.depthBufferBits = 0;
				renderTextureDescriptor.dimension = TextureDimension.Tex2D;
				renderTextureDescriptor.enableRandomWrite = true;
				renderTextureDescriptor.graphicsFormat = GraphicsFormat.R16_UNorm;
				renderTextureDescriptor.memoryless = RenderTextureMemoryless.None;
				renderTextureDescriptor.height = num3 + 8;
				renderTextureDescriptor.width = num2 + 8;
				renderTextureDescriptor.volumeDepth = 1;
				renderTextureDescriptor.mipCount = 1;
				renderTextureDescriptor.msaaSamples = 1;
				renderTextureDescriptor.sRGB = false;
				renderTextureDescriptor.useDynamicScale = false;
				renderTextureDescriptor.useMipMap = false;
				RenderTextureDescriptor desc = renderTextureDescriptor;
				this.m_CommandBuffer.GetTemporaryRT(ShaderID._AvgTerrainHeightsTemp, desc);
				this.m_CommandBuffer.GetTemporaryRT(ShaderID._BlurTempHorz, desc);
				num4 = this.m_SmoothTerrainKernal;
				val2.y = desc.width;
				val2.z = desc.height;
				val3.x = 4f;
				val3.y = 4f;
				this.m_CommandBuffer.SetComputeTextureParam(this.m_AdjustTerrainCS, this.m_BlurHorzKernal, ShaderID._Heightmap, target);
				this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._BrushData, val2);
				this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._Range, new Vector4(@int.x - 4, @int.y - 4, @int.z + 4, @int.w + 4));
				int threadGroupsX2 = (num2 + 15) / 8;
				int threadGroupsY2 = num3 + 8;
				this.m_CommandBuffer.DispatchCompute(this.m_AdjustTerrainCS, this.m_BlurHorzKernal, threadGroupsX2, threadGroupsY2, 1);
				int threadGroupsX3 = num2 + 8;
				int threadGroupsY3 = (num3 + 15) / 8;
				this.m_CommandBuffer.DispatchCompute(this.m_AdjustTerrainCS, this.m_BlurVertKernal, threadGroupsX3, threadGroupsY3, 1);
				break;
			}
			default:
				num4 = this.m_ShiftTerrainKernal;
				break;
			}
			int num5 = 2;
			float4 float7 = ((this.worldHeightmap != null && !this.m_ToolSystem.actionMode.IsEditor()) ? new float4(num5, num5, target.width - num5, target.height - num5) : new float4(-1f, -1f, target.width + 1, target.height + 1));
			float val4 = 10f / this.heightScaleOffset.x;
			this.m_CommandBuffer.SetComputeTextureParam(this.m_AdjustTerrainCS, num4, ShaderID._Heightmap, target);
			this.m_CommandBuffer.SetComputeTextureParam(this.m_AdjustTerrainCS, num4, ShaderID._BrushTexture, texture);
			this.m_CommandBuffer.SetComputeTextureParam(this.m_AdjustTerrainCS, num4, ShaderID._WorldTexture, (this.worldHeightmap != null) ? this.worldHeightmap : Texture2D.whiteTexture);
			this.m_CommandBuffer.SetComputeTextureParam(this.m_AdjustTerrainCS, num4, ShaderID._WaterTexture, this.m_WaterSystem.WaterTexture);
			this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._HeightScaleOffset, new float4(this.heightScaleOffset.x, this.heightScaleOffset.y, 0f, 0f));
			this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._Range, new Vector4(@int.x, @int.y, @int.z, @int.w));
			this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._BrushData, val2);
			this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._BrushData2, val3);
			this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._ClampArea, float7);
			this.m_CommandBuffer.SetComputeVectorParam(this.m_AdjustTerrainCS, ShaderID._WorldOffsetScale, this.m_WorldOffsetScale);
			this.m_CommandBuffer.SetComputeFloatParam(this.m_AdjustTerrainCS, ShaderID._EdgeMaxDifference, val4);
			this.m_CommandBuffer.DispatchCompute(this.m_AdjustTerrainCS, num4, threadGroupsX, threadGroupsY, 1);
			if (type == TerraformingType.Soften)
			{
				this.m_CommandBuffer.ReleaseTemporaryRT(ShaderID._AvgTerrainHeightsTemp);
				this.m_CommandBuffer.ReleaseTemporaryRT(ShaderID._BlurTempHorz);
			}
			Graphics.ExecuteCommandBuffer(this.m_CommandBuffer);
		}

		public void PreviewBrush(TerraformingType type, Bounds2 area, Brush brush, Texture texture)
		{
		}

		public void ApplyBrush(TerraformingType type, Bounds2 area, Brush brush, Texture texture)
		{
			this.m_WaterSystem.TerrainWillChangeFromBrush(area);
			this.ApplyToTerrain(this.m_Heightmap, null, UnityEngine.Time.unscaledDeltaTime, type, area, brush, texture, worldMap: false);
			this.ApplyToTerrain(this.m_WorldMapEditable, null, UnityEngine.Time.unscaledDeltaTime, type, area, brush, texture, worldMap: true);
			this.UpdateMinMax(brush, area);
			this.TriggerAsyncChange();
		}

		public void UpdateMinMax(Brush brush, Bounds2 area)
		{
			if (this.worldHeightmap != null)
			{
				area.min -= this.worldOffset;
				area.max -= this.worldOffset;
				area.min /= this.worldSize;
				area.max /= this.worldSize;
			}
			else
			{
				area.min -= this.playableOffset;
				area.max -= this.playableOffset;
				area.min /= this.playableArea;
				area.max /= this.playableArea;
			}
			int4 area2 = new int4((int)math.max(math.floor(area.min.x * (float)this.m_Heightmap.width) - 1f, 0f), (int)math.max(math.floor(area.min.y * (float)this.m_Heightmap.height) - 1f, 0f), (int)math.min(math.ceil(area.max.x * (float)this.m_Heightmap.width) + 1f, this.m_Heightmap.width - 1), (int)math.min(math.ceil(area.max.y * (float)this.m_Heightmap.height) + 1f, this.m_Heightmap.height - 1));
			area2.zw -= area2.xy;
			area2.zw = math.clamp(area2.zw, new int2(this.m_Heightmap.width / this.m_TerrainMinMax.size, this.m_Heightmap.height / this.m_TerrainMinMax.size), new int2(this.m_Heightmap.width, this.m_Heightmap.height));
			this.m_TerrainMinMax.RequestUpdate(this, this.m_Heightmap, this.worldHeightmap, area2);
		}

		public void GetCascadeInfo(out int LODCount, out int baseLOD, out float4x4 areas, out float4 ranges, out float4 size)
		{
			LODCount = 4;
			baseLOD = TerrainSystem.baseLod;
			if (this.m_CascadeRanges != null)
			{
				areas = new float4x4(this.m_CascadeRanges[0].x, this.m_CascadeRanges[0].y, this.m_CascadeRanges[0].z, this.m_CascadeRanges[0].w, this.m_CascadeRanges[1].x, this.m_CascadeRanges[1].y, this.m_CascadeRanges[1].z, this.m_CascadeRanges[1].w, this.m_CascadeRanges[2].x, this.m_CascadeRanges[2].y, this.m_CascadeRanges[2].z, this.m_CascadeRanges[2].w, this.m_CascadeRanges[3].x, this.m_CascadeRanges[3].y, this.m_CascadeRanges[3].z, this.m_CascadeRanges[3].w);
				ranges = new float4(math.min(this.m_CascadeRanges[0].z - this.m_CascadeRanges[0].x, this.m_CascadeRanges[0].w - this.m_CascadeRanges[0].y) * 0.75f, math.min(this.m_CascadeRanges[1].z - this.m_CascadeRanges[1].x, this.m_CascadeRanges[1].w - this.m_CascadeRanges[1].y) * 0.75f, math.min(this.m_CascadeRanges[2].z - this.m_CascadeRanges[2].x, this.m_CascadeRanges[2].w - this.m_CascadeRanges[2].y) * 0.75f, math.min(this.m_CascadeRanges[3].z - this.m_CascadeRanges[3].x, this.m_CascadeRanges[3].w - this.m_CascadeRanges[3].y) * 0.75f);
				size = new float4(math.max(this.m_CascadeRanges[0].z - this.m_CascadeRanges[0].x, this.m_CascadeRanges[0].w - this.m_CascadeRanges[0].y), math.max(this.m_CascadeRanges[1].z - this.m_CascadeRanges[1].x, this.m_CascadeRanges[1].w - this.m_CascadeRanges[1].y), math.max(this.m_CascadeRanges[2].z - this.m_CascadeRanges[2].x, this.m_CascadeRanges[2].w - this.m_CascadeRanges[2].y), math.max(this.m_CascadeRanges[3].z - this.m_CascadeRanges[3].x, this.m_CascadeRanges[3].w - this.m_CascadeRanges[3].y));
			}
			else
			{
				areas = default(float4x4);
				ranges = default(float4);
				size = default(float4);
			}
		}

		public Texture GetCascadeTexture()
		{
			return this.m_HeightmapCascade;
		}

		private bool Overlap(ref float4 A, ref float4 B)
		{
			if (A.x > B.z || B.x > A.z || A.z < B.x || B.z < A.x || A.y > B.w || B.y > A.w || A.w < B.y || B.w < A.y)
			{
				return false;
			}
			return true;
		}

		private float4 ClipViewport(float4 Viewport)
		{
			if (Viewport.x < 0f)
			{
				Viewport.z = math.max(Viewport.z + Viewport.x, 0f);
				Viewport.x = 0f;
			}
			else if (Viewport.x > 1f)
			{
				Viewport.x = 1f;
				Viewport.z = 0f;
			}
			if (Viewport.x + Viewport.z > 1f)
			{
				Viewport.z = math.max(1f - Viewport.x, 0f);
			}
			if (Viewport.y < 0f)
			{
				Viewport.w = math.max(Viewport.w + Viewport.y, 0f);
				Viewport.y = 0f;
			}
			else if (Viewport.y > 1f)
			{
				Viewport.y = 1f;
				Viewport.w = 0f;
			}
			if (Viewport.y + Viewport.w > 1f)
			{
				Viewport.w = math.max(1f - Viewport.y, 0f);
			}
			return Viewport;
		}

		private void UpdateCascades(bool isLoaded)
		{
			float3 position = this.m_CameraUpdateSystem.position;
			float4 @float = new float4(0);
			float4 A = this.m_UpdateArea;
			this.heightMapRenderRequired = math.lengthsq(A) > 0f;
			this.m_UpdateArea = float4.zero;
			this.m_RoadUpdate = this.m_CascadeReset;
			this.m_AreaUpdate = this.m_CascadeReset;
			if (this.m_CascadeReset)
			{
				this.heightMapRenderRequired = true;
				A = this.m_CascadeRanges[TerrainSystem.baseLod];
			}
			NativeList<Bounds2> updateBuffer = this.m_GroundHeightSystem.GetUpdateBuffer();
			bool flag = isLoaded || !this.m_BuildingsChanged.IsEmptyIgnoreFilter;
			if (flag || (this.m_ToolSystem.actionMode.IsEditor() && !this.m_EditorLotQuery.IsEmpty))
			{
				this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<Game.Objects.Transform> typeHandle = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle;
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<PrefabRef> typeHandle2 = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
				this.__TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<Updated> typeHandle3 = this.__TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle;
				this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
				EntityTypeHandle _Unity_Entities_Entity_TypeHandle = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
				this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<ObjectGeometryData> _Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
				float4 area;
				if (flag)
				{
					this.m_BuildingUpgradeDependencies.Complete();
					this.m_BuildingUpgradeDependencies = default(JobHandle);
					NativeArray<ArchetypeChunk> nativeArray = (isLoaded ? this.m_BuildingGroup : this.m_BuildingsChanged).ToArchetypeChunkArray(Allocator.Temp);
					base.CompleteDependency();
					for (int i = 0; i < nativeArray.Length; i++)
					{
						NativeArray<Entity> nativeArray2 = nativeArray[i].GetNativeArray(_Unity_Entities_Entity_TypeHandle);
						NativeArray<Game.Objects.Transform> nativeArray3 = nativeArray[i].GetNativeArray(ref typeHandle);
						NativeArray<PrefabRef> nativeArray4 = nativeArray[i].GetNativeArray(ref typeHandle2);
						bool flag2 = nativeArray[i].Has(ref typeHandle3);
						if (isLoaded)
						{
							this.heightMapRenderRequired = true;
							this.m_WaterSystem.TerrainWillChange();
							A = this.m_CascadeRanges[TerrainSystem.baseLod];
							break;
						}
						for (int j = 0; j < nativeArray3.Length; j++)
						{
							PrefabRef prefabRef = nativeArray4[j];
							if (this.CalculateBuildingCullArea(nativeArray3[j], prefabRef.m_Prefab, _Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, out area))
							{
								Bounds2 value = new Bounds2(area.xy, area.zw);
								updateBuffer.Add(in value);
								if (!this.heightMapRenderRequired)
								{
									this.heightMapRenderRequired = true;
									this.m_WaterSystem.TerrainWillChange();
									A = area;
								}
								else
								{
									A.xy = math.min(A.xy, area.xy);
									A.zw = math.max(A.zw, area.zw);
								}
							}
							if (!flag2 || !this.m_BuildingUpgrade.TryGetValue(nativeArray2[j], out var item))
							{
								continue;
							}
							if (item != prefabRef.m_Prefab && this.CalculateBuildingCullArea(nativeArray3[j], item, _Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, out area))
							{
								if (!this.heightMapRenderRequired)
								{
									this.heightMapRenderRequired = true;
									A = area;
								}
								else
								{
									A.xy = math.min(A.xy, area.xy);
									A.zw = math.max(A.zw, area.zw);
								}
							}
							this.m_BuildingUpgrade.Remove(nativeArray2[j]);
						}
					}
					nativeArray.Dispose();
				}
				if (this.m_ToolSystem.actionMode.IsEditor() && !this.m_EditorLotQuery.IsEmpty)
				{
					NativeArray<ArchetypeChunk> nativeArray5 = this.m_EditorLotQuery.ToArchetypeChunkArray(Allocator.Temp);
					base.CompleteDependency();
					for (int k = 0; k < nativeArray5.Length; k++)
					{
						NativeArray<Game.Objects.Transform> nativeArray6 = nativeArray5[k].GetNativeArray(ref typeHandle);
						NativeArray<PrefabRef> nativeArray7 = nativeArray5[k].GetNativeArray(ref typeHandle2);
						for (int l = 0; l < nativeArray6.Length; l++)
						{
							PrefabRef prefabRef2 = nativeArray7[l];
							if (this.CalculateBuildingCullArea(nativeArray6[l], prefabRef2.m_Prefab, _Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, out area))
							{
								if (!this.heightMapRenderRequired)
								{
									this.heightMapRenderRequired = true;
									this.m_WaterSystem.TerrainWillChange();
									A = area;
								}
								else
								{
									A.xy = math.min(A.xy, area.xy);
									A.zw = math.max(A.zw, area.zw);
								}
							}
						}
					}
					nativeArray5.Dispose();
				}
				this.m_BuildingUpgrade.Clear();
			}
			if (isLoaded || !this.m_RoadsChanged.IsEmptyIgnoreFilter)
			{
				NativeArray<ArchetypeChunk> nativeArray8 = (isLoaded ? this.m_RoadsGroup : this.m_RoadsChanged).ToArchetypeChunkArray(Allocator.Temp);
				this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
				EntityTypeHandle _Unity_Entities_Entity_TypeHandle2 = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<PrefabRef> typeHandle4 = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
				this.__TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<NetData> _Game_Prefabs_NetData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup;
				this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<NetGeometryData> _Game_Prefabs_NetGeometryData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup;
				this.__TypeHandle.__Game_Net_Composition_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<Composition> _Game_Net_Composition_RO_ComponentLookup = this.__TypeHandle.__Game_Net_Composition_RO_ComponentLookup;
				this.__TypeHandle.__Game_Net_Orphan_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<Orphan> _Game_Net_Orphan_RO_ComponentLookup = this.__TypeHandle.__Game_Net_Orphan_RO_ComponentLookup;
				this.__TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<NodeGeometry> _Game_Net_NodeGeometry_RO_ComponentLookup = this.__TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup;
				this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<EdgeGeometry> _Game_Net_EdgeGeometry_RO_ComponentLookup = this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup;
				this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<StartNodeGeometry> _Game_Net_StartNodeGeometry_RO_ComponentLookup = this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup;
				this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<EndNodeGeometry> _Game_Net_EndNodeGeometry_RO_ComponentLookup = this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup;
				base.CompleteDependency();
				for (int m = 0; m < nativeArray8.Length; m++)
				{
					NativeArray<Entity> nativeArray9 = nativeArray8[m].GetNativeArray(_Unity_Entities_Entity_TypeHandle2);
					NativeArray<PrefabRef> nativeArray10 = nativeArray8[m].GetNativeArray(ref typeHandle4);
					if (isLoaded)
					{
						this.heightMapRenderRequired = true;
						A = this.m_CascadeRanges[TerrainSystem.baseLod];
						this.m_WaterSystem.TerrainWillChange();
						break;
					}
					for (int n = 0; n < nativeArray9.Length; n++)
					{
						Entity entity = nativeArray9[n];
						if (!_Game_Prefabs_NetGeometryData_RO_ComponentLookup.TryGetComponent(nativeArray10[n].m_Prefab, out var componentData) || (componentData.m_Flags & (Game.Net.GeometryFlags.FlattenTerrain | Game.Net.GeometryFlags.ClipTerrain)) == 0)
						{
							continue;
						}
						this.m_RoadUpdate = true;
						if ((componentData.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) == 0)
						{
							continue;
						}
						Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
						if (_Game_Net_Composition_RO_ComponentLookup.HasComponent(entity))
						{
							EdgeGeometry edgeGeometry = _Game_Net_EdgeGeometry_RO_ComponentLookup[entity];
							StartNodeGeometry startNodeGeometry = _Game_Net_StartNodeGeometry_RO_ComponentLookup[entity];
							EndNodeGeometry endNodeGeometry = _Game_Net_EndNodeGeometry_RO_ComponentLookup[entity];
							if (math.any(edgeGeometry.m_Start.m_Length + edgeGeometry.m_End.m_Length > 0.1f))
							{
								bounds |= edgeGeometry.m_Bounds;
							}
							if (math.any(startNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(startNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
							{
								bounds |= startNodeGeometry.m_Geometry.m_Bounds;
							}
							if (math.any(endNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(endNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
							{
								bounds |= endNodeGeometry.m_Geometry.m_Bounds;
							}
						}
						else if (_Game_Net_Orphan_RO_ComponentLookup.HasComponent(entity))
						{
							bounds |= _Game_Net_NodeGeometry_RO_ComponentLookup[entity].m_Bounds;
						}
						if (bounds.min.x <= bounds.max.x)
						{
							NetData netData = _Game_Prefabs_NetData_RO_ComponentLookup[nativeArray10[n].m_Prefab];
							bounds = MathUtils.Expand(bounds, NetUtils.GetTerrainSmoothingWidth(netData) - 8f);
							Bounds2 value = bounds.xz;
							updateBuffer.Add(in value);
							if (!this.heightMapRenderRequired)
							{
								this.heightMapRenderRequired = true;
								this.m_WaterSystem.TerrainWillChange();
								A = new float4(bounds.min.xz, bounds.max.xz);
							}
							else
							{
								A.xy = math.min(A.xy, bounds.min.xz);
								A.zw = math.max(A.zw, bounds.max.xz);
							}
						}
					}
				}
				nativeArray8.Dispose();
			}
			bool num = isLoaded || !this.m_AreasChanged.IsEmptyIgnoreFilter;
			bool flag3 = false;
			if (num)
			{
				NativeArray<ArchetypeChunk> nativeArray11 = (isLoaded ? this.m_AreasQuery : this.m_AreasChanged).ToArchetypeChunkArray(Allocator.Temp);
				this.__TypeHandle.__Game_Areas_Terrain_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<Game.Areas.Terrain> typeHandle5 = this.__TypeHandle.__Game_Areas_Terrain_RO_ComponentTypeHandle;
				this.__TypeHandle.__Game_Areas_Clip_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<Clip> typeHandle6 = this.__TypeHandle.__Game_Areas_Clip_RO_ComponentTypeHandle;
				this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<Geometry> typeHandle7 = this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle;
				base.CompleteDependency();
				for (int num2 = 0; num2 < nativeArray11.Length; num2++)
				{
					flag3 |= nativeArray11[num2].Has(ref typeHandle6);
					if (!nativeArray11[num2].Has(ref typeHandle5))
					{
						continue;
					}
					this.m_AreaUpdate = true;
					NativeArray<Geometry> nativeArray12 = nativeArray11[num2].GetNativeArray(ref typeHandle7);
					if (isLoaded)
					{
						this.heightMapRenderRequired = true;
						A = this.m_CascadeRanges[TerrainSystem.baseLod];
						break;
					}
					for (int num3 = 0; num3 < nativeArray12.Length; num3++)
					{
						Bounds3 bounds2 = nativeArray12[num3].m_Bounds;
						if (bounds2.min.x <= bounds2.max.x)
						{
							Bounds2 value = bounds2.xz;
							updateBuffer.Add(in value);
							if (!this.heightMapRenderRequired)
							{
								this.heightMapRenderRequired = true;
								A = new float4(bounds2.min.xz, bounds2.max.xz);
							}
							else
							{
								A.xy = math.min(A.xy, bounds2.min.xz);
								A.zw = math.max(A.zw, bounds2.max.xz);
							}
						}
					}
				}
				nativeArray11.Dispose();
			}
			if (this.heightMapRenderRequired)
			{
				A += new float4(-10f, -10f, 10f, 10f);
			}
			float4 area2 = A;
			for (int num4 = 0; num4 <= TerrainSystem.baseLod; num4++)
			{
				if (this.heightMapRenderRequired)
				{
					this.heightMapViewport[num4] = new float4(A.x - this.m_CascadeRanges[num4].x, A.y - this.m_CascadeRanges[num4].y, A.z - this.m_CascadeRanges[num4].x, A.w - this.m_CascadeRanges[num4].y);
					this.heightMapViewport[num4] /= new float4(this.m_CascadeRanges[num4].z - this.m_CascadeRanges[num4].x, this.m_CascadeRanges[num4].w - this.m_CascadeRanges[num4].y, this.m_CascadeRanges[num4].z - this.m_CascadeRanges[num4].x, this.m_CascadeRanges[num4].w - this.m_CascadeRanges[num4].y);
					this.heightMapViewport[num4].zw -= this.heightMapViewport[num4].xy;
					this.heightMapViewport[num4] = this.ClipViewport(this.heightMapViewport[num4]);
					this.heightMapSliceUpdated[num4] = this.heightMapViewport[num4].w > 0f && this.heightMapViewport[num4].z > 0f;
					area2.xy = math.min(area2.xy, this.m_CascadeRanges[num4].xy + this.heightMapViewport[num4].xy * (this.m_CascadeRanges[num4].zw - this.m_CascadeRanges[num4].xy));
					area2.zw = math.max(area2.zw, this.m_CascadeRanges[num4].xy + (this.heightMapViewport[num4].xy + this.heightMapViewport[num4].zw) * (this.m_CascadeRanges[num4].zw - this.m_CascadeRanges[num4].xy));
				}
				else
				{
					this.heightMapViewport[num4] = float4.zero;
					this.heightMapSliceUpdated[num4] = false;
				}
			}
			for (int num5 = TerrainSystem.baseLod + 1; num5 < 4; num5++)
			{
				float2 float2 = this.m_CascadeRanges[TerrainSystem.baseLod].zw - this.m_CascadeRanges[TerrainSystem.baseLod].xy;
				float2 /= math.pow(2f, num5 - TerrainSystem.baseLod);
				float num6 = math.min(float2.x, float2.y) / 4f;
				@float.xy = position.xz - float2 * 0.5f;
				@float.zw = position.xz + float2 * 0.5f;
				if (@float.x < this.m_CascadeRanges[0].x)
				{
					float num7 = this.m_CascadeRanges[0].x - @float.x;
					@float.x += num7;
					@float.z += num7;
				}
				if (@float.y < this.m_CascadeRanges[0].y)
				{
					float num8 = this.m_CascadeRanges[0].y - @float.y;
					@float.y += num8;
					@float.w += num8;
				}
				if (@float.z > this.m_CascadeRanges[0].z)
				{
					float num9 = this.m_CascadeRanges[0].z - @float.z;
					@float.x += num9;
					@float.z += num9;
				}
				if (@float.w > this.m_CascadeRanges[0].w)
				{
					float num10 = this.m_CascadeRanges[0].w - @float.w;
					@float.y += num10;
					@float.w += num10;
				}
				float2 float3 = math.abs(@float.xy - new float2(this.m_CascadeRanges[num5].x, this.m_CascadeRanges[num5].y));
				if (math.lengthsq(this.m_CascadeRanges[num5]) == 0f || float3.x > num6 || float3.y > num6)
				{
					this.heightMapSliceUpdated[num5] = true;
					this.heightMapViewport[num5] = new float4(0f, 0f, 1f, 1f);
					this.m_CascadeRanges[num5] = @float;
					if (this.heightMapRenderRequired)
					{
						A.xy = math.min(A.xy, this.m_CascadeRanges[num5].xy);
						A.zw = math.max(A.zw, this.m_CascadeRanges[num5].zw);
						area2.xy = math.min(area2.xy, this.m_CascadeRanges[num5].xy);
						area2.zw = math.max(area2.zw, this.m_CascadeRanges[num5].zw);
					}
					else
					{
						this.heightMapRenderRequired = true;
						A = this.m_CascadeRanges[num5];
						area2 = A;
					}
				}
				else if (math.lengthsq(A) > 0f && this.Overlap(ref A, ref this.m_CascadeRanges[num5]))
				{
					this.heightMapViewport[num5] = new float4(math.clamp(A.x, this.m_CascadeRanges[num5].x, this.m_CascadeRanges[num5].z) - this.m_CascadeRanges[num5].x, math.clamp(A.y, this.m_CascadeRanges[num5].y, this.m_CascadeRanges[num5].w) - this.m_CascadeRanges[num5].y, math.clamp(A.z, this.m_CascadeRanges[num5].x, this.m_CascadeRanges[num5].z) - this.m_CascadeRanges[num5].x, math.clamp(A.w, this.m_CascadeRanges[num5].y, this.m_CascadeRanges[num5].w) - this.m_CascadeRanges[num5].y);
					this.heightMapViewport[num5] /= new float4(this.m_CascadeRanges[num5].z - this.m_CascadeRanges[num5].x, this.m_CascadeRanges[num5].w - this.m_CascadeRanges[num5].y, this.m_CascadeRanges[num5].z - this.m_CascadeRanges[num5].x, this.m_CascadeRanges[num5].w - this.m_CascadeRanges[num5].y);
					this.heightMapViewport[num5].zw -= this.heightMapViewport[num5].xy;
					this.heightMapViewport[num5] = this.ClipViewport(this.heightMapViewport[num5]);
					this.heightMapSliceUpdated[num5] = this.heightMapViewport[num5].w > 0f && this.heightMapViewport[num5].z > 0f;
					area2.xy = math.min(area2.xy, this.m_CascadeRanges[num5].xy + this.heightMapViewport[num5].xy * (this.m_CascadeRanges[num5].zw - this.m_CascadeRanges[num5].xy));
					area2.zw = math.max(area2.zw, this.m_CascadeRanges[num5].xy + (this.heightMapViewport[num5].xy + this.heightMapViewport[num5].zw) * (this.m_CascadeRanges[num5].zw - this.m_CascadeRanges[num5].xy));
				}
				else
				{
					this.heightMapSliceUpdated[num5] = false;
					this.heightMapViewport[num5] = float4.zero;
				}
			}
			if (this.heightMapRenderRequired || this.m_RoadUpdate || flag3)
			{
				if (this.heightMapRenderRequired)
				{
					area2 += new float4(-10f, -10f, 10f, 10f);
					this.m_LastCullArea = area2;
					this.heightMapSliceUpdatedLast = this.heightMapSliceUpdated;
					this.heightMapViewportUpdated = this.heightMapViewport;
				}
				this.CullForCascades(area2, this.heightMapRenderRequired, this.m_RoadUpdate, this.m_AreaUpdate, flag3, out var laneCount);
				if (this.heightMapRenderRequired)
				{
					for (int num11 = 3; num11 >= TerrainSystem.baseLod; num11--)
					{
						if (this.heightMapSliceUpdated[num11])
						{
							this.CullCascade(num11, this.m_CascadeRanges[num11], this.heightMapViewport[num11], laneCount);
						}
						else
						{
							this.heightMapCullArea[num11] = float4.zero;
						}
					}
				}
				JobHandle.ScheduleBatchedJobs();
			}
			for (int num12 = 0; num12 < 4; num12++)
			{
				float4 float4 = this.m_CascadeRanges[num12];
				float4.zw = 1f / math.max(0.001f, float4.zw - float4.xy);
				float4.xy *= float4.zw;
				this.m_ShaderCascadeRanges[num12] = float4;
			}
			Shader.SetGlobalVectorArray(ShaderID._CascadeRangesID, this.m_ShaderCascadeRanges);
			this.m_CascadeReset = false;
		}

		public void RenderCascades()
		{
			if (this.heightMapRenderRequired)
			{
				this.m_GroundHeightSystem.BeforeUpdateHeights();
				this.m_CascadeCB.Clear();
				this.m_BuildingInstanceData.StartFrame();
				this.m_LaneInstanceData.StartFrame();
				this.m_TriangleInstanceData.StartFrame();
				this.m_EdgeInstanceData.StartFrame();
				if (TerrainSystem.baseLod != 0)
				{
					Texture value = ((this.m_WorldMapEditable != null) ? this.m_WorldMapEditable : this.worldHeightmap);
					this.m_TerrainBlit.SetTexture("_WorldMap", value);
				}
				for (int num = 3; num >= TerrainSystem.baseLod; num--)
				{
					if (this.heightMapSliceUpdated[num])
					{
						this.RenderCascade(num, this.m_CascadeRanges[num], this.heightMapViewport[num], ref this.m_CascadeCB);
					}
				}
				if (TerrainSystem.baseLod > 0 && this.heightMapSliceUpdated[0])
				{
					int4 lastWorldWrite = new int4((int)(this.heightMapViewport[0].x * (float)this.m_HeightmapCascade.width), (int)(this.heightMapViewport[0].y * (float)this.m_HeightmapCascade.height), (int)(this.heightMapViewport[0].z * (float)this.m_HeightmapCascade.width), (int)(this.heightMapViewport[0].w * (float)this.m_HeightmapCascade.height));
					if (this.m_LastWorldWrite.z == 0)
					{
						this.m_LastWorldWrite = lastWorldWrite;
					}
					else
					{
						int2 @int = new int2(math.min(this.m_LastWorldWrite.x, lastWorldWrite.x), math.min(this.m_LastWorldWrite.y, lastWorldWrite.y));
						int2 int2 = new int2(math.max(this.m_LastWorldWrite.x + this.m_LastWorldWrite.z, lastWorldWrite.x + lastWorldWrite.z), math.max(this.m_LastWorldWrite.y + this.m_LastWorldWrite.w, lastWorldWrite.y + lastWorldWrite.w));
						this.m_LastWorldWrite.xy = @int;
						this.m_LastWorldWrite.zw = int2 - @int;
					}
					this.RenderWorldMapToCascade(this.m_CascadeRanges[0], this.heightMapViewport[0], ref this.m_CascadeCB);
				}
				this.m_BuildingInstanceData.EndFrame();
				this.m_LaneInstanceData.EndFrame();
				this.m_TriangleInstanceData.EndFrame();
				this.m_EdgeInstanceData.EndFrame();
				Graphics.ExecuteCommandBuffer(this.m_CascadeCB);
				if (this.heightMapSliceUpdated[TerrainSystem.baseLod])
				{
					int4 lastWrite = new int4((int)(this.heightMapViewport[TerrainSystem.baseLod].x * (float)this.m_HeightmapCascade.width), (int)(this.heightMapViewport[TerrainSystem.baseLod].y * (float)this.m_HeightmapCascade.height), (int)(this.heightMapViewport[TerrainSystem.baseLod].z * (float)this.m_HeightmapCascade.width), (int)(this.heightMapViewport[TerrainSystem.baseLod].w * (float)this.m_HeightmapCascade.height));
					if (this.m_LastWrite.z == 0)
					{
						this.m_LastWrite = lastWrite;
					}
					else
					{
						int2 int3 = new int2(math.min(this.m_LastWrite.x, lastWrite.x), math.min(this.m_LastWrite.y, lastWrite.y));
						int2 int4 = new int2(math.max(this.m_LastWrite.x + this.m_LastWrite.z, lastWrite.x + lastWrite.z), math.max(this.m_LastWrite.y + this.m_LastWrite.w, lastWrite.y + lastWrite.w));
						this.m_LastWrite.xy = int3;
						this.m_LastWrite.zw = int4 - int3;
					}
					this.TriggerAsyncChange();
				}
			}
			this.m_CascadeReset = false;
		}

		private void CullForCascades(float4 area, bool heightMapRenderRequired, bool roadsChanged, bool terrainAreasChanged, bool clipAreasChanged, out int laneCount)
		{
			this.m_CullFinished.Complete();
			if (roadsChanged)
			{
				this.m_ClipMapCull.Complete();
				this.m_LaneCullList.Clear();
				this.m_ClipMapList.Clear();
				laneCount = this.m_RoadsGroup.CalculateEntityCountWithoutFiltering() * 6;
				if (laneCount > this.m_LaneCullList.Capacity)
				{
					this.m_LaneCullList.Capacity = laneCount + math.max(laneCount / 4, 250);
					this.m_ClipMapList.Capacity = this.m_LaneCullList.Capacity;
				}
			}
			else
			{
				laneCount = this.m_LaneCullList.Length;
			}
			if (heightMapRenderRequired)
			{
				NativeQueue<BuildingUtils.LotInfo> queue = new NativeQueue<BuildingUtils.LotInfo>(Allocator.TempJob);
				this.__TypeHandle.__Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Objects_Elevation_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Buildings_Lot_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				CullBuildingLotsJob cullBuildingLotsJob = default(CullBuildingLotsJob);
				cullBuildingLotsJob.m_LotHandle = this.__TypeHandle.__Game_Buildings_Lot_RO_ComponentTypeHandle;
				cullBuildingLotsJob.m_TransformHandle = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle;
				cullBuildingLotsJob.m_ElevationHandle = this.__TypeHandle.__Game_Objects_Elevation_RO_ComponentTypeHandle;
				cullBuildingLotsJob.m_StackHandle = this.__TypeHandle.__Game_Objects_Stack_RO_ComponentTypeHandle;
				cullBuildingLotsJob.m_PrefabRefHandle = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
				cullBuildingLotsJob.m_InstalledUpgradeHandle = this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;
				cullBuildingLotsJob.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
				cullBuildingLotsJob.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
				cullBuildingLotsJob.m_PrefabBuildingData = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
				cullBuildingLotsJob.m_PrefabBuildingExtensionData = this.__TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;
				cullBuildingLotsJob.m_PrefabAssetStampData = this.__TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup;
				cullBuildingLotsJob.m_OverrideTerraform = this.__TypeHandle.__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup;
				cullBuildingLotsJob.m_ObjectGeometryData = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
				cullBuildingLotsJob.m_AdditionalLots = this.__TypeHandle.__Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup;
				cullBuildingLotsJob.m_Area = area;
				cullBuildingLotsJob.Result = queue.AsParallelWriter();
				CullBuildingLotsJob jobData = cullBuildingLotsJob;
				DequeBuildingLotsJob jobData2 = new DequeBuildingLotsJob
				{
					m_Queue = queue,
					m_List = this.m_BuildingCullList
				};
				JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(jobData, this.m_BuildingGroup, base.Dependency);
				this.m_BuildingCull = IJobExtensions.Schedule(jobData2, dependsOn);
				this.m_CullFinished = this.m_BuildingCull;
				queue.Dispose(this.m_BuildingCull);
			}
			if (roadsChanged)
			{
				this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_TerrainComposition_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Net_Orphan_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Net_Composition_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
				CullRoadsJob cullRoadsJob = default(CullRoadsJob);
				cullRoadsJob.m_EntityHandle = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
				cullRoadsJob.m_CompositionData = this.__TypeHandle.__Game_Net_Composition_RO_ComponentLookup;
				cullRoadsJob.m_EdgeGeometryData = this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup;
				cullRoadsJob.m_StartNodeGeometryData = this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup;
				cullRoadsJob.m_EndNodeGeometryData = this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup;
				cullRoadsJob.m_NodeData = this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup;
				cullRoadsJob.m_NodeGeometryData = this.__TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup;
				cullRoadsJob.m_OrphanData = this.__TypeHandle.__Game_Net_Orphan_RO_ComponentLookup;
				cullRoadsJob.m_PrefabCompositionData = this.__TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup;
				cullRoadsJob.m_TerrainCompositionData = this.__TypeHandle.__Game_Prefabs_TerrainComposition_RO_ComponentLookup;
				cullRoadsJob.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
				cullRoadsJob.m_NetData = this.__TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup;
				cullRoadsJob.m_NetGeometryData = this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup;
				cullRoadsJob.m_Area = this.m_CascadeRanges[TerrainSystem.baseLod];
				cullRoadsJob.Result = this.m_LaneCullList.AsParallelWriter();
				CullRoadsJob jobData3 = cullRoadsJob;
				this.m_LaneCull = JobChunkExtensions.ScheduleParallel(jobData3, this.m_RoadsGroup, base.Dependency);
				this.m_CullFinished = JobHandle.CombineDependencies(this.m_CullFinished, this.m_LaneCull);
				GenerateClipDataJob generateClipDataJob = default(GenerateClipDataJob);
				generateClipDataJob.m_RoadsToCull = this.m_LaneCullList;
				generateClipDataJob.Result = this.m_ClipMapList.AsParallelWriter();
				GenerateClipDataJob jobData4 = generateClipDataJob;
				this.m_CurrentClipMap = null;
				this.m_ClipMapCull = jobData4.Schedule(this.m_LaneCullList, 128, this.m_LaneCull);
				this.m_CullFinished = JobHandle.CombineDependencies(this.m_CullFinished, this.m_ClipMapCull);
			}
			if (terrainAreasChanged)
			{
				NativeQueue<AreaTriangle> queue2 = new NativeQueue<AreaTriangle>(Allocator.TempJob);
				NativeQueue<AreaEdge> queue3 = new NativeQueue<AreaEdge>(Allocator.TempJob);
				this.__TypeHandle.__Game_Prefabs_StorageAreaData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_TerrainAreaData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Storage_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Clip_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				CullAreasJob cullAreasJob = default(CullAreasJob);
				cullAreasJob.m_ClipType = this.__TypeHandle.__Game_Areas_Clip_RO_ComponentTypeHandle;
				cullAreasJob.m_AreaType = this.__TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle;
				cullAreasJob.m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
				cullAreasJob.m_GeometryType = this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle;
				cullAreasJob.m_StorageType = this.__TypeHandle.__Game_Areas_Storage_RO_ComponentTypeHandle;
				cullAreasJob.m_NodeType = this.__TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle;
				cullAreasJob.m_TriangleType = this.__TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle;
				cullAreasJob.m_PrefabTerrainAreaData = this.__TypeHandle.__Game_Prefabs_TerrainAreaData_RO_ComponentLookup;
				cullAreasJob.m_PrefabStorageAreaData = this.__TypeHandle.__Game_Prefabs_StorageAreaData_RO_ComponentLookup;
				cullAreasJob.m_Area = this.m_CascadeRanges[TerrainSystem.baseLod];
				cullAreasJob.m_Triangles = queue2.AsParallelWriter();
				cullAreasJob.m_Edges = queue3.AsParallelWriter();
				CullAreasJob jobData5 = cullAreasJob;
				DequeTrianglesJob dequeTrianglesJob = default(DequeTrianglesJob);
				dequeTrianglesJob.m_Queue = queue2;
				dequeTrianglesJob.m_List = this.m_TriangleCullList;
				DequeTrianglesJob jobData6 = dequeTrianglesJob;
				DequeEdgesJob dequeEdgesJob = default(DequeEdgesJob);
				dequeEdgesJob.m_Queue = queue3;
				dequeEdgesJob.m_List = this.m_EdgeCullList;
				DequeEdgesJob jobData7 = dequeEdgesJob;
				JobHandle dependsOn2 = JobChunkExtensions.ScheduleParallel(jobData5, this.m_AreasQuery, base.Dependency);
				JobHandle job = IJobExtensions.Schedule(jobData6, dependsOn2);
				JobHandle job2 = IJobExtensions.Schedule(jobData7, dependsOn2);
				this.m_AreaCull = JobHandle.CombineDependencies(job, job2);
				this.m_CullFinished = JobHandle.CombineDependencies(this.m_CullFinished, this.m_AreaCull);
				queue2.Dispose(this.m_AreaCull);
				queue3.Dispose(this.m_AreaCull);
			}
			if (clipAreasChanged)
			{
				if (!this.m_HasAreaClipMeshData)
				{
					this.m_HasAreaClipMeshData = true;
					this.m_AreaClipMeshData = Mesh.AllocateWritableMeshData(1);
				}
				this.__TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Clip_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				GenerateAreaClipMeshJob generateAreaClipMeshJob = default(GenerateAreaClipMeshJob);
				generateAreaClipMeshJob.m_Chunks = this.m_AreasQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out var outJobHandle);
				generateAreaClipMeshJob.m_ClipType = this.__TypeHandle.__Game_Areas_Clip_RO_ComponentTypeHandle;
				generateAreaClipMeshJob.m_AreaType = this.__TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle;
				generateAreaClipMeshJob.m_NodeType = this.__TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle;
				generateAreaClipMeshJob.m_TriangleType = this.__TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle;
				generateAreaClipMeshJob.m_MeshData = this.m_AreaClipMeshData;
				GenerateAreaClipMeshJob jobData8 = generateAreaClipMeshJob;
				this.m_AreaClipMeshDataDeps = IJobExtensions.Schedule(jobData8, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
				jobData8.m_Chunks.Dispose(this.m_AreaClipMeshDataDeps);
				this.m_CullFinished = JobHandle.CombineDependencies(this.m_CullFinished, this.m_AreaClipMeshDataDeps);
			}
			base.Dependency = this.m_CullFinished;
		}

		public void CullClipMapForView(Viewer viewer)
		{
		}

		private void CullCascade(int cascadeIndex, float4 area, float4 viewport, int laneCount)
		{
			if (viewport.z == 0f || viewport.w == 0f)
			{
				UnityEngine.Debug.LogError("Invalid Viewport");
			}
			CascadeCullInfo cascadeCullInfo = this.m_CascadeCulling[cascadeIndex];
			cascadeCullInfo.m_BuildingHandle.Complete();
			cascadeCullInfo.m_BuildingRenderList = new NativeList<BuildingLotDraw>(Allocator.TempJob);
			float2 xy = area.xy;
			float2 @float = area.zw - area.xy;
			area = new float4(xy.x + @float.x * viewport.x, xy.y + @float.y * viewport.y, xy.x + @float.x * (viewport.x + viewport.z), xy.y + @float.y * (viewport.y + viewport.w));
			area += new float4(-10f, -10f, 10f, 10f);
			this.heightMapCullArea[cascadeIndex] = area;
			NativeQueue<BuildingLotDraw> queue = new NativeQueue<BuildingLotDraw>(Allocator.TempJob);
			CullBuildingsCascadeJob cullBuildingsCascadeJob = default(CullBuildingsCascadeJob);
			cullBuildingsCascadeJob.m_LotsToCull = this.m_BuildingCullList;
			cullBuildingsCascadeJob.m_Area = area;
			cullBuildingsCascadeJob.Result = queue.AsParallelWriter();
			CullBuildingsCascadeJob jobData = cullBuildingsCascadeJob;
			DequeBuildingDrawsJob jobData2 = new DequeBuildingDrawsJob
			{
				m_Queue = queue,
				m_List = cascadeCullInfo.m_BuildingRenderList
			};
			JobHandle dependsOn = jobData.Schedule(this.m_BuildingCullList, 128, this.m_BuildingCull);
			cascadeCullInfo.m_BuildingHandle = IJobExtensions.Schedule(jobData2, dependsOn);
			queue.Dispose(cascadeCullInfo.m_BuildingHandle);
			cascadeCullInfo.m_LaneHandle.Complete();
			cascadeCullInfo.m_LaneRenderList = new NativeList<LaneDraw>(laneCount, Allocator.TempJob);
			CullRoadsCacscadeJob cullRoadsCacscadeJob = default(CullRoadsCacscadeJob);
			cullRoadsCacscadeJob.m_RoadsToCull = this.m_LaneCullList;
			cullRoadsCacscadeJob.m_Area = area;
			cullRoadsCacscadeJob.m_Scale = 1f / this.heightScaleOffset.x;
			cullRoadsCacscadeJob.Result = cascadeCullInfo.m_LaneRenderList.AsParallelWriter();
			CullRoadsCacscadeJob jobData3 = cullRoadsCacscadeJob;
			cascadeCullInfo.m_LaneHandle = jobData3.Schedule(this.m_LaneCullList, 128, this.m_LaneCull);
			cascadeCullInfo.m_AreaHandle.Complete();
			cascadeCullInfo.m_TriangleRenderList = new NativeList<AreaTriangle>(Allocator.TempJob);
			cascadeCullInfo.m_EdgeRenderList = new NativeList<AreaEdge>(Allocator.TempJob);
			CullTrianglesJob cullTrianglesJob = default(CullTrianglesJob);
			cullTrianglesJob.m_Triangles = this.m_TriangleCullList;
			cullTrianglesJob.m_Area = area;
			cullTrianglesJob.Result = cascadeCullInfo.m_TriangleRenderList;
			CullTrianglesJob jobData4 = cullTrianglesJob;
			CullEdgesJob cullEdgesJob = default(CullEdgesJob);
			cullEdgesJob.m_Edges = this.m_EdgeCullList;
			cullEdgesJob.m_Area = area;
			cullEdgesJob.Result = cascadeCullInfo.m_EdgeRenderList;
			CullEdgesJob jobData5 = cullEdgesJob;
			JobHandle job = IJobExtensions.Schedule(jobData4, this.m_AreaCull);
			JobHandle job2 = IJobExtensions.Schedule(jobData5, this.m_AreaCull);
			cascadeCullInfo.m_AreaHandle = JobHandle.CombineDependencies(job, job2);
			this.m_CullFinished = JobHandle.CombineDependencies(this.m_CullFinished, JobHandle.CombineDependencies(cascadeCullInfo.m_BuildingHandle, cascadeCullInfo.m_LaneHandle, cascadeCullInfo.m_AreaHandle));
		}

		private void DrawHeightAdjustments(ref CommandBuffer cmdBuffer, int cascade, float4 area, float4 viewport, RenderTargetBinding binding, ref NativeArray<BuildingLotDraw> lots, ref NativeArray<LaneDraw> lanes, ref NativeArray<AreaTriangle> triangles, ref NativeArray<AreaEdge> edges, ref Material lotMaterial, ref Material laneMaterial, ref Material areaMaterial)
		{
			float4 @float = new float4(-area.xy, 1f / (area.zw - area.xy));
			Rect scissor = new Rect(viewport.x * (float)this.m_HeightmapCascade.width, viewport.y * (float)this.m_HeightmapCascade.height, viewport.z * (float)this.m_HeightmapCascade.width, viewport.w * (float)this.m_HeightmapCascade.height);
			if (lots.Length > 0)
			{
				ComputeBuffer computeBuffer = this.m_BuildingInstanceData.Request(lots.Length);
				computeBuffer.SetData(lots);
				computeBuffer.name = $"BuildingLot Buffer Cascade{cascade}";
				lotMaterial.SetVector(ShaderID._TerrainScaleOffsetID, new Vector4(this.heightScaleOffset.x, this.heightScaleOffset.y, 0f, 0f));
				lotMaterial.SetVector(ShaderID._MapOffsetScaleID, this.m_MapOffsetScale);
				lotMaterial.SetVector(ShaderID._CascadeOffsetScale, @float);
				lotMaterial.SetTexture(ShaderID._HeightmapID, this.heightmap);
				lotMaterial.SetBuffer(ShaderID._BuildingLotID, computeBuffer);
			}
			if (lanes.Length > 0)
			{
				ComputeBuffer computeBuffer2 = this.m_LaneInstanceData.Request(lanes.Length);
				computeBuffer2.SetData(lanes);
				computeBuffer2.name = $"Lane Buffer Cascade{cascade}";
				laneMaterial.SetVector(ShaderID._TerrainScaleOffsetID, new Vector4(this.heightScaleOffset.x, this.heightScaleOffset.y, 0f, 0f));
				laneMaterial.SetVector(ShaderID._MapOffsetScaleID, this.m_MapOffsetScale);
				laneMaterial.SetVector(ShaderID._CascadeOffsetScale, @float);
				laneMaterial.SetTexture(ShaderID._HeightmapID, this.heightmap);
				laneMaterial.SetBuffer(ShaderID._LanesID, computeBuffer2);
			}
			if (triangles.Length > 0 || edges.Length > 0)
			{
				ComputeBuffer computeBuffer3 = this.m_TriangleInstanceData.Request(triangles.Length);
				computeBuffer3.SetData(triangles);
				computeBuffer3.name = $"Triangle Buffer Cascade{cascade}";
				ComputeBuffer computeBuffer4 = this.m_EdgeInstanceData.Request(edges.Length);
				computeBuffer4.SetData(edges);
				computeBuffer4.name = $"Edge Buffer Cascade{cascade}";
				areaMaterial.SetVector(ShaderID._TerrainScaleOffsetID, new Vector4(this.heightScaleOffset.x, this.heightScaleOffset.y, 0f, 0f));
				areaMaterial.SetVector(ShaderID._MapOffsetScaleID, this.m_MapOffsetScale);
				areaMaterial.SetVector(ShaderID._CascadeOffsetScale, @float);
				areaMaterial.SetTexture(ShaderID._HeightmapID, this.heightmap);
				areaMaterial.SetBuffer(ShaderID._TrianglesID, computeBuffer3);
				areaMaterial.SetBuffer(ShaderID._EdgesID, computeBuffer4);
			}
			if (lots.Length > 0)
			{
				cmdBuffer.DrawProcedural(Matrix4x4.identity, lotMaterial, 1, MeshTopology.Triangles, 6, lots.Length);
			}
			if (lanes.Length > 0)
			{
				cmdBuffer.DrawMeshInstancedProcedural(this.m_LaneMesh, 0, laneMaterial, 1, lanes.Length);
			}
			int num = Shader.PropertyToID("_CascadeMinHeights");
			cmdBuffer.GetTemporaryRT(num, this.m_HeightmapCascade.width, this.m_HeightmapCascade.height, 0, FilterMode.Point, this.m_HeightmapCascade.graphicsFormat);
			int num2 = math.max(0, Mathf.FloorToInt(scissor.xMin));
			int num3 = math.max(0, Mathf.FloorToInt(scissor.yMin));
			int srcWidth = math.min(this.m_HeightmapCascade.width, Mathf.CeilToInt(scissor.xMax)) - num2;
			int srcHeight = math.min(this.m_HeightmapCascade.height, Mathf.CeilToInt(scissor.yMax)) - num3;
			cmdBuffer.CopyTexture(this.m_HeightmapCascade, cascade, 0, num2, num3, srcWidth, srcHeight, num, 0, 0, num2, num3);
			cmdBuffer.SetRenderTarget(binding, 0, CubemapFace.Unknown, cascade);
			cmdBuffer.EnableScissorRect(scissor);
			if (triangles.Length > 0)
			{
				cmdBuffer.DrawProcedural(Matrix4x4.identity, areaMaterial, 0, MeshTopology.Triangles, 3, triangles.Length);
			}
			if (edges.Length > 0)
			{
				cmdBuffer.DrawProcedural(Matrix4x4.identity, areaMaterial, 1, MeshTopology.Triangles, 6, edges.Length);
			}
			if (lots.Length > 0)
			{
				cmdBuffer.DrawProcedural(Matrix4x4.identity, lotMaterial, 0, MeshTopology.Triangles, 6, lots.Length);
			}
			if (lanes.Length > 0)
			{
				cmdBuffer.DrawMeshInstancedProcedural(this.m_LaneMesh, 0, laneMaterial, 0, lanes.Length);
			}
			cmdBuffer.ReleaseTemporaryRT(num);
			if (lots.Length > 0)
			{
				cmdBuffer.DrawProcedural(Matrix4x4.identity, lotMaterial, 2, MeshTopology.Triangles, 6, lots.Length);
			}
			if (lanes.Length > 0)
			{
				cmdBuffer.DrawMeshInstancedProcedural(this.m_LaneMesh, 0, laneMaterial, 2, lanes.Length);
			}
		}

		private void RenderWorldMapToCascade(float4 area, float4 viewport, ref CommandBuffer cmdBuffer)
		{
			if (this.m_WorldMapEditable != null)
			{
				bool flag = viewport.x == 0f && viewport.y == 0f && viewport.z == 1f && viewport.w == 1f;
				Texture worldMapEditable = this.m_WorldMapEditable;
				Rect scissor = new Rect(viewport.x * (float)this.m_HeightmapCascade.width, viewport.y * (float)this.m_HeightmapCascade.height, viewport.z * (float)this.m_HeightmapCascade.width, viewport.w * (float)this.m_HeightmapCascade.height);
				RenderTargetBinding binding = new RenderTargetBinding(this.m_HeightmapCascade, flag ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, this.m_HeightmapDepth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
				cmdBuffer.SetRenderTarget(binding, 0, CubemapFace.Unknown, 0);
				cmdBuffer.ClearRenderTarget(clearDepth: true, clearColor: false, UnityEngine.Color.black, 1f);
				cmdBuffer.EnableScissorRect(scissor);
				Vector2 scale = new Vector2(1f, 1f);
				Vector2 offset = default(Vector2);
				offset.x = (area.x - this.worldOffset.x) / this.worldSize.x;
				offset.y = (area.y - this.worldOffset.y) / this.worldSize.y;
				cmdBuffer.Blit(worldMapEditable, BuiltinRenderTextureType.CurrentActive, scale, offset);
			}
		}

		private void RenderCascade(int cascadeIndex, float4 area, float4 viewport, ref CommandBuffer cmdBuffer)
		{
			bool flag = true;
			bool flag2 = viewport.x == 0f && viewport.y == 0f && viewport.z == 1f && viewport.w == 1f;
			Rect scissor = new Rect(viewport.x * (float)this.m_HeightmapCascade.width, viewport.y * (float)this.m_HeightmapCascade.height, viewport.z * (float)this.m_HeightmapCascade.width, viewport.w * (float)this.m_HeightmapCascade.height);
			RenderTargetBinding binding = new RenderTargetBinding(this.m_HeightmapCascade, (flag2 && flag) ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, this.m_HeightmapDepth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
			cmdBuffer.SetRenderTarget(binding, 0, CubemapFace.Unknown, cascadeIndex);
			cmdBuffer.ClearRenderTarget(clearDepth: true, clearColor: false, UnityEngine.Color.black, 1f);
			cmdBuffer.EnableScissorRect(scissor);
			if (flag)
			{
				float num = ((cascadeIndex < TerrainSystem.baseLod) ? math.pow(2f, -(cascadeIndex - TerrainSystem.baseLod)) : (1f / math.pow(2f, cascadeIndex - TerrainSystem.baseLod)));
				float2 @float = new Vector2(num, num);
				float2 float2 = (area.xy - this.playableOffset) / this.playableArea;
				if (cascadeIndex == TerrainSystem.baseLod || TerrainSystem.baseLod == 0)
				{
					cmdBuffer.Blit(this.heightmap, BuiltinRenderTextureType.CurrentActive, @float, float2);
				}
				else
				{
					cmdBuffer.SetGlobalVector("_CascadeHeightmapOffsetScale", new float4(float2, @float));
					@float = (area.zw - area.xy) / this.worldSize;
					float2 = (area.xy - this.worldOffset) / this.worldSize;
					cmdBuffer.SetGlobalVector("_CascadeWorldOffsetScale", new float4(float2, @float));
					cmdBuffer.Blit(this.heightmap, BuiltinRenderTextureType.CurrentActive, this.m_TerrainBlit);
				}
			}
			Matrix4x4 proj = Matrix4x4.Ortho(area.x, area.z, area.w, area.y, this.heightScaleOffset.x + this.heightScaleOffset.y, this.heightScaleOffset.y);
			proj.m02 *= -1f;
			proj.m12 *= -1f;
			proj.m22 *= -1f;
			proj.m32 *= -1f;
			cmdBuffer.SetViewProjectionMatrices(GL.GetGPUProjectionMatrix(proj, renderIntoTexture: true), Matrix4x4.identity);
			CascadeCullInfo cascadeCullInfo = this.m_CascadeCulling[cascadeIndex];
			cascadeCullInfo.m_BuildingHandle.Complete();
			cascadeCullInfo.m_LaneHandle.Complete();
			cascadeCullInfo.m_AreaHandle.Complete();
			if (cascadeCullInfo.m_BuildingRenderList.IsCreated || cascadeCullInfo.m_LaneRenderList.IsCreated || cascadeCullInfo.m_TriangleRenderList.IsCreated || cascadeCullInfo.m_EdgeRenderList.IsCreated)
			{
				NativeArray<BuildingLotDraw> lots = default(NativeArray<BuildingLotDraw>);
				NativeArray<LaneDraw> lanes = default(NativeArray<LaneDraw>);
				NativeArray<AreaTriangle> triangles = default(NativeArray<AreaTriangle>);
				NativeArray<AreaEdge> edges = default(NativeArray<AreaEdge>);
				if (cascadeCullInfo.m_BuildingRenderList.IsCreated)
				{
					lots = cascadeCullInfo.m_BuildingRenderList.AsArray();
				}
				if (cascadeCullInfo.m_LaneRenderList.IsCreated)
				{
					lanes = cascadeCullInfo.m_LaneRenderList.AsArray();
				}
				if (cascadeCullInfo.m_TriangleRenderList.IsCreated)
				{
					triangles = cascadeCullInfo.m_TriangleRenderList.AsArray();
				}
				if (cascadeCullInfo.m_EdgeRenderList.IsCreated)
				{
					edges = cascadeCullInfo.m_EdgeRenderList.AsArray();
				}
				this.DrawHeightAdjustments(ref cmdBuffer, cascadeIndex, area, viewport, binding, ref lots, ref lanes, ref triangles, ref edges, ref cascadeCullInfo.m_LotMaterial, ref cascadeCullInfo.m_LaneMaterial, ref cascadeCullInfo.m_AreaMaterial);
				if (cascadeCullInfo.m_BuildingRenderList.IsCreated)
				{
					cascadeCullInfo.m_BuildingRenderList.Dispose();
				}
				if (cascadeCullInfo.m_LaneRenderList.IsCreated)
				{
					cascadeCullInfo.m_LaneRenderList.Dispose();
				}
				if (cascadeCullInfo.m_TriangleRenderList.IsCreated)
				{
					cascadeCullInfo.m_TriangleRenderList.Dispose();
				}
				if (cascadeCullInfo.m_EdgeRenderList.IsCreated)
				{
					cascadeCullInfo.m_EdgeRenderList.Dispose();
				}
			}
			cmdBuffer.DisableScissorRect();
		}

		private void CreateRoadMeshes()
		{
			this.m_LaneMesh = new Mesh
			{
				name = "Lane Mesh"
			};
			int num = 1;
			int num2 = 8;
			int num3 = (num + 1) * (num2 + 1);
			int num4 = num * num2 * 2 * 3;
			Vector3[] array = new Vector3[num3];
			Vector2[] array2 = new Vector2[num3];
			int[] array3 = new int[num4];
			for (int i = 0; i <= num2; i++)
			{
				for (int j = 0; j <= num; j++)
				{
					array[j + (num + 1) * i] = new Vector3((float)j / (float)num, 0f, (float)i / (float)num2);
					array2[j + (num + 1) * i] = new Vector2(array[j + (num + 1) * i].x, array[j + (num + 1) * i].z);
				}
			}
			int num5 = num + 1;
			int num6 = 0;
			for (int k = 0; k < num2; k++)
			{
				for (int l = 0; l < num; l++)
				{
					array3[num6++] = l + num5 * (k + 1);
					array3[num6++] = l + 1 + num5 * (k + 1);
					array3[num6++] = l + 1 + num5 * k;
					array3[num6++] = l + num5 * (k + 1);
					array3[num6++] = l + 1 + num5 * k;
					array3[num6++] = l + num5 * k;
				}
			}
			this.m_LaneMesh.vertices = array;
			this.m_LaneMesh.uv = array2;
			this.m_LaneMesh.subMeshCount = 1;
			this.m_LaneMesh.SetTriangles(array3, 0);
			this.m_LaneMesh.UploadMeshData(markNoLongerReadable: true);
			this.m_ClipMesh = new Mesh
			{
				name = "Clip Mesh"
			};
			int num7 = num3;
			num3 *= 2;
			num4 = num4 * 2 + num2 * 2 * 3 * 2 + num * 2 * 3 * 2;
			array = new Vector3[num3];
			array2 = new Vector2[num3];
			array3 = new int[num4];
			for (int m = 0; m <= num2; m++)
			{
				for (int n = 0; n <= num; n++)
				{
					array[n + (num + 1) * m] = new Vector3((float)n / (float)num, 1f, (float)m / (float)num2);
					array2[n + (num + 1) * m] = new Vector2(array[n + (num + 1) * m].x, array[n + (num + 1) * m].z);
					array[num7 + n + (num + 1) * m] = array[n + (num + 1) * m];
					array[num7 + n + (num + 1) * m].y = 0f;
					array2[num7 + n + (num + 1) * m] = array2[n + (num + 1) * m];
				}
			}
			num5 = num + 1;
			num6 = 0;
			for (int num8 = 0; num8 < num2; num8++)
			{
				for (int num9 = 0; num9 < num; num9++)
				{
					array3[num6++] = num9 + num5 * (num8 + 1);
					array3[num6++] = num9 + 1 + num5 * (num8 + 1);
					array3[num6++] = num9 + 1 + num5 * num8;
					array3[num6++] = num9 + num5 * (num8 + 1);
					array3[num6++] = num9 + 1 + num5 * num8;
					array3[num6++] = num9 + num5 * num8;
				}
			}
			for (int num10 = 0; num10 < num2; num10++)
			{
				for (int num11 = 0; num11 < num; num11++)
				{
					array3[num6++] = num7 + (num11 + 1 + num5 * (num10 + 1));
					array3[num6++] = num7 + (num11 + num5 * (num10 + 1));
					array3[num6++] = num7 + (num11 + 1 + num5 * num10);
					array3[num6++] = num7 + (num11 + 1 + num5 * num10);
					array3[num6++] = num7 + (num11 + num5 * (num10 + 1));
					array3[num6++] = num7 + (num11 + num5 * num10);
				}
			}
			int num12 = 0;
			for (int num13 = 0; num13 < num2; num13++)
			{
				array3[num6++] = num12 + num5 * (num13 + 1);
				array3[num6++] = num12 + num5 * num13;
				array3[num6++] = num7 + num12 + num5 * num13;
				array3[num6++] = num7 + num12 + num5 * num13;
				array3[num6++] = num7 + num12 + num5 * (num13 + 1);
				array3[num6++] = num12 + num5 * (num13 + 1);
			}
			num12 = num;
			for (int num14 = 0; num14 < num2; num14++)
			{
				array3[num6++] = num12 + num5 * num14;
				array3[num6++] = num12 + num5 * (num14 + 1);
				array3[num6++] = num7 + num12 + num5 * num14;
				array3[num6++] = num7 + num12 + num5 * (num14 + 1);
				array3[num6++] = num7 + num12 + num5 * num14;
				array3[num6++] = num12 + num5 * (num14 + 1);
			}
			for (int num15 = 0; num15 < num; num15++)
			{
				array3[num6++] = num15;
				array3[num6++] = num15 + num7;
				array3[num6++] = num15 + num7 + 1;
				array3[num6++] = num15 + num7 + 1;
				array3[num6++] = num15 + 1;
				array3[num6++] = num15;
			}
			for (int num16 = 1; num16 <= num; num16++)
			{
				array3[num6++] = num3 - num16;
				array3[num6++] = num3 - num16 - 1;
				array3[num6++] = num3 - num16 - num7 - 1;
				array3[num6++] = num3 - num16 - num7 - 1;
				array3[num6++] = num3 - num16 - num7;
				array3[num6++] = num3 - num16;
			}
			this.m_ClipMesh.vertices = array;
			this.m_ClipMesh.uv = array2;
			this.m_ClipMesh.subMeshCount = 1;
			this.m_ClipMesh.SetTriangles(array3, 0);
			this.m_ClipMesh.UploadMeshData(markNoLongerReadable: true);
		}

		public bool CalculateBuildingCullArea(Game.Objects.Transform transform, Entity prefab, ComponentLookup<ObjectGeometryData> geometryData, out float4 area)
		{
			area = float4.zero;
			if (geometryData.TryGetComponent(prefab, out var componentData))
			{
				Bounds3 bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, componentData);
				bounds = MathUtils.Expand(bounds, ObjectUtils.GetTerrainSmoothingWidth(componentData) - 8f);
				area.xy = bounds.min.xz;
				area.zw = bounds.max.xz;
				return true;
			}
			return false;
		}

		public void OnBuildingMoved(Entity entity)
		{
			this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			ComponentLookup<Game.Objects.Transform> _Game_Objects_Transform_RO_ComponentLookup = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			ComponentLookup<PrefabRef> _Game_Prefabs_PrefabRef_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
			this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			ComponentLookup<ObjectGeometryData> _Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
			base.CompleteDependency();
			float4 area = float4.zero;
			if (!_Game_Prefabs_PrefabRef_RO_ComponentLookup.HasComponent(entity) || !_Game_Objects_Transform_RO_ComponentLookup.HasComponent(entity))
			{
				return;
			}
			PrefabRef prefabRef = _Game_Prefabs_PrefabRef_RO_ComponentLookup[entity];
			Game.Objects.Transform transform = _Game_Objects_Transform_RO_ComponentLookup[entity];
			if (this.CalculateBuildingCullArea(transform, prefabRef.m_Prefab, _Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, out area))
			{
				NativeList<Bounds2> updateBuffer = this.m_GroundHeightSystem.GetUpdateBuffer();
				Bounds2 value = new Bounds2(area.xy, area.zw);
				updateBuffer.Add(in value);
				if (math.lengthsq(this.m_UpdateArea) > 0f)
				{
					this.m_UpdateArea.xy = math.min(this.m_UpdateArea.xy, area.xy);
					this.m_UpdateArea.zw = math.max(this.m_UpdateArea.zw, area.zw);
				}
				else
				{
					this.m_UpdateArea = area;
				}
				this.m_UpdateArea += new float4(-10f, -10f, 10f, 10f);
			}
		}

		public void GetLastMinMaxUpdate(out float3 min, out float3 max)
		{
			int4 updateArea = this.m_TerrainMinMax.UpdateArea;
			float2 minMax = this.m_TerrainMinMax.GetMinMax(updateArea);
			float4 @float = new float4((float)updateArea.x / (float)this.m_TerrainMinMax.size, (float)updateArea.y / (float)this.m_TerrainMinMax.size, (float)(updateArea.x + updateArea.z) / (float)this.m_TerrainMinMax.size, (float)(updateArea.y + updateArea.w) / (float)this.m_TerrainMinMax.size);
			@float *= this.worldSize.xyxy;
			@float += this.worldOffset.xyxy;
			min = new float3(@float.x, minMax.x, @float.y);
			max = new float3(@float.z, minMax.y, @float.w);
		}

		public NativeParallelHashMap<Entity, Entity>.ParallelWriter GetBuildingUpgradeWriter(int ExpectedAmount)
		{
			this.m_BuildingUpgradeDependencies.Complete();
			if (ExpectedAmount > this.m_BuildingUpgrade.Capacity)
			{
				this.m_BuildingUpgrade.Capacity = ExpectedAmount;
			}
			return this.m_BuildingUpgrade.AsParallelWriter();
		}

		public void SetBuildingUpgradeWriterDependency(JobHandle handle)
		{
			this.m_BuildingUpgradeDependencies = handle;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void __AssignQueries(ref SystemState state)
		{
		}

		protected override void OnCreateForCompiler()
		{
			base.OnCreateForCompiler();
			this.__AssignQueries(ref base.CheckedStateRef);
			this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
		}

		[Preserve]
		public TerrainSystem()
		{
		}
	}
}
