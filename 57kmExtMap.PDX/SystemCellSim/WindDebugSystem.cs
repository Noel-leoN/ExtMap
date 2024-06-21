using Colossal;
using Game.Simulation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game.Debug;

namespace ExtMap57km.SystemsCell
{
	public partial class WindDebugSystem : BaseDebugSystem
	{
		private struct WindGizmoJob : IJob
		{
			[ReadOnly]
			public NativeArray<WindSimulationSystem.WindCell> m_WindMap;

			public GizmoBatcher m_GizmoBatcher;

			public float2 m_TerrainRange;

			public void Execute()
			{
				for (int i = 0; i < WindSimulationSystem.kResolution.x; i++)
				{
					for (int j = 0; j < WindSimulationSystem.kResolution.y; j++)
					{
						for (int k = 0; k < WindSimulationSystem.kResolution.z; k++)
						{
							if (i == 0 || j == 0 || k == 0 || i == WindSimulationSystem.kResolution.x - 1 || j == WindSimulationSystem.kResolution.y - 1)
							{
								int index = i + j * WindSimulationSystem.kResolution.x + k * WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y;
								WindSimulationSystem.WindCell windCell = this.m_WindMap[index];
								float3 cellCenter = WindSimulationSystem.GetCellCenter(index);
								cellCenter.y = math.lerp(this.m_TerrainRange.x, this.m_TerrainRange.y, ((float)k + 0.5f) / (float)WindSimulationSystem.kResolution.z);
								Color white = Color.white;
								if (math.abs(windCell.m_Velocities.x) > 0.001f)
								{
									float3 @float = cellCenter + new float3(0.5f * (float)CellMapSystem<Wind>.kMapSize / (float)WindSimulationSystem.kResolution.x, 0f, 0f);
									this.m_GizmoBatcher.DrawArrow(@float, @float + 50f * new float3(windCell.m_Velocities.x, 0f, 0f), white, 1f);
								}
								if (math.abs(windCell.m_Velocities.y) > 0.001f)
								{
									float3 @float = cellCenter + new float3(0f, 0f, 0.5f * (float)CellMapSystem<Wind>.kMapSize / (float)WindSimulationSystem.kResolution.y);
									this.m_GizmoBatcher.DrawArrow(@float, @float + 50f * new float3(0f, 0f, windCell.m_Velocities.y), white, 1f);
								}
								if (math.abs(windCell.m_Velocities.z) > 0.001f)
								{
									float3 @float = cellCenter + new float3(0f, 0.5f * (float)CellMapSystem<Wind>.kMapSize / (float)WindSimulationSystem.kResolution.x, 0f);
									this.m_GizmoBatcher.DrawArrow(@float, @float + 50f * new float3(0f, windCell.m_Velocities.z, 0f), white, 1f);
								}
								white = ((!(windCell.m_Pressure < 0f)) ? new Color(0f, math.lerp(0f, 1f, math.saturate(10f * windCell.m_Pressure)), 0f) : new Color(math.lerp(0f, 1f, math.saturate(-10f * windCell.m_Pressure)), 0f, 0f));
								this.m_GizmoBatcher.DrawWireCube(cellCenter, new float3(10f, 10f * windCell.m_Pressure, 10f), white);
							}
						}
					}
				}
			}
		}

		private WindSimulationSystem m_WindSimulationSystem;

		private TerrainSystem m_TerrainSystem;

		private GizmosSystem m_GizmosSystem;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
			this.m_WindSimulationSystem = base.World.GetOrCreateSystemManaged<WindSimulationSystem>();
			this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
			base.Enabled = false;
		}

		[Preserve]
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			TerrainHeightData data = this.m_TerrainSystem.GetHeightData();
			float2 terrainRange = new float2(TerrainUtils.ToWorldSpace(ref data, 0f), TerrainUtils.ToWorldSpace(ref data, 65535f));
			WindGizmoJob jobData = default(WindGizmoJob);
			jobData.m_WindMap = this.m_WindSimulationSystem.GetCells(out var deps);
			jobData.m_GizmoBatcher = this.m_GizmosSystem.GetGizmosBatcher(out var dependencies);
			jobData.m_TerrainRange = terrainRange;
			JobHandle jobHandle = jobData.Schedule(JobHandle.CombineDependencies(inputDeps, dependencies, deps));
			this.m_WindSimulationSystem.AddReader(jobHandle);
			this.m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
			return jobHandle;
		}

		[Preserve]
		public WindDebugSystem()
		{
		}
	}
}
