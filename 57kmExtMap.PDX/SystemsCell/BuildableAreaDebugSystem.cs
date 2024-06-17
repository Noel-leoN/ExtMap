using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Debug;

namespace ExtMap57km.Systems
{
	//[CompilerGenerated]
    public partial class BuildableAreaDebugSystem : BaseDebugSystem
	{
		private struct BuildableAreaGizmoJob : IJobParallelFor
		{
			[ReadOnly]
			public CellMapData<NaturalResourceCell> m_NaturalResourceData;

			[ReadOnly]
			public TerrainHeightData m_TerrainHeightData;

			[ReadOnly]
			public WaterSurfaceData m_WaterSurfaceData;

			public GizmoBatcher m_GizmoBatcher;

			public NativeAccumulator<AverageFloat>.ParallelWriter m_Average;

			public Bounds1 m_BuildableLandMaxSlope;

			public void Execute(int index)
			{
				float3 cellCenter = CellMapSystem<NaturalResourceCell>.GetCellCenter(index, NaturalResourceSystem.kTextureSize);
				float num = AreaResourceSystem.CalculateBuildable(cellCenter, this.m_NaturalResourceData.m_CellSize, this.m_WaterSurfaceData, this.m_TerrainHeightData, this.m_BuildableLandMaxSlope);
				this.m_Average.Accumulate(new AverageFloat
				{
					m_Total = num,
					m_Count = 1
				});
				if (num > 0f)
				{
					Color color = Color.Lerp(Color.red, Color.green, num);
					float2 @float = 0.5f * math.sqrt(num) * this.m_NaturalResourceData.m_CellSize;
					this.DrawLine(cellCenter + new float3(0f - @float.x, 0f, 0f - @float.y), cellCenter + new float3(@float.x, 0f, 0f - @float.y), color);
					this.DrawLine(cellCenter + new float3(@float.x, 0f, 0f - @float.y), cellCenter + new float3(@float.x, 0f, @float.y), color);
					this.DrawLine(cellCenter + new float3(@float.x, 0f, @float.y), cellCenter + new float3(0f - @float.x, 0f, @float.y), color);
					this.DrawLine(cellCenter + new float3(0f - @float.x, 0f, @float.y), cellCenter + new float3(0f - @float.x, 0f, 0f - @float.y), color);
				}
			}

			private void DrawLine(float3 a, float3 b, Color color)
			{
				a.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, a);
				b.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, b);
				this.m_GizmoBatcher.DrawLine(a, b, color);
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct TypeHandle
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
			}
		}

		private TerrainSystem m_TerrainSystem;

		private WaterSystem m_WaterSystem;

		private NaturalResourceSystem m_NaturalResourceSystem;

		private GizmosSystem m_GizmosSystem;

		private Option m_StrictOption;

		private NativeAccumulator<AverageFloat> m_BuildableArea;

		private float m_LastBuildableArea;

		private TypeHandle __TypeHandle;

		private EntityQuery __query_1438325908_0;

		public float buildableArea => this.m_LastBuildableArea;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
			this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
			this.m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
			this.m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
			base.RequireForUpdate<AreasConfigurationData>();
			this.m_BuildableArea = new NativeAccumulator<AverageFloat>(Allocator.Persistent);
			this.m_StrictOption = base.AddOption("Strict", defaultEnabled: false);
			base.Enabled = false;
		}

		[Preserve]
		protected override void OnDestroy()
		{
			this.m_BuildableArea.Dispose();
			base.OnDestroy();
		}

		[Preserve]
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			this.m_LastBuildableArea = this.m_BuildableArea.GetResult().average;
			this.m_BuildableArea.Clear();
			BuildableAreaGizmoJob jobData = default(BuildableAreaGizmoJob);
			jobData.m_GizmoBatcher = this.m_GizmosSystem.GetGizmosBatcher(out var dependencies);
			jobData.m_NaturalResourceData = this.m_NaturalResourceSystem.GetData(readOnly: true, out var dependencies2);
			jobData.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData();
			jobData.m_WaterSurfaceData = this.m_WaterSystem.GetSurfaceData(out var deps);
			jobData.m_BuildableLandMaxSlope = (this.m_StrictOption.enabled ? new Bounds1(0f, 0.3f) : this.__query_1438325908_0.GetSingleton<AreasConfigurationData>().m_BuildableLandMaxSlope);
			jobData.m_Average = this.m_BuildableArea.AsParallelWriter();
			JobHandle jobHandle = IJobParallelForExtensions.Schedule(jobData, NaturalResourceSystem.kTextureSize * NaturalResourceSystem.kTextureSize, NaturalResourceSystem.kTextureSize, JobUtils.CombineDependencies(inputDeps, dependencies, dependencies2, deps));
			this.m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
			this.m_TerrainSystem.AddCPUHeightReader(jobHandle);
			this.m_WaterSystem.AddSurfaceReader(jobHandle);
			this.m_NaturalResourceSystem.AddReader(jobHandle);
			return jobHandle;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void __AssignQueries(ref SystemState state)
		{
			this.__query_1438325908_0 = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<AreasConfigurationData>() },
				Any = new ComponentType[0],
				None = new ComponentType[0],
				Disabled = new ComponentType[0],
				Absent = new ComponentType[0],
				Options = EntityQueryOptions.IncludeSystems
			});
		}

		protected override void OnCreateForCompiler()
		{
			base.OnCreateForCompiler();
			this.__AssignQueries(ref base.CheckedStateRef);
			this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
		}

		[Preserve]
		public BuildableAreaDebugSystem()
		{
		}
	}
}
