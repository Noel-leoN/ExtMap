using Colossal.Serialization.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace ExtMap57km.Systems
{
	public partial class TrafficAmbienceSystem : CellMapSystem<TrafficAmbienceCell>, IJobSerializable
	{
		[BurstCompile]
		private struct TrafficAmbienceUpdateJob : IJobParallelFor
		{
			public NativeArray<TrafficAmbienceCell> m_TrafficMap;

			public void Execute(int index)
			{
				TrafficAmbienceCell trafficAmbienceCell = this.m_TrafficMap[index];
				this.m_TrafficMap[index] = new TrafficAmbienceCell
				{
					m_Traffic = trafficAmbienceCell.m_Accumulator,
					m_Accumulator = 0f
				};
			}
		}

		public static readonly int kTextureSize = 64;

		public static readonly int kUpdatesPerDay = 1024;

		public int2 TextureSize => new int2(TrafficAmbienceSystem.kTextureSize, TrafficAmbienceSystem.kTextureSize);

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 262144 / TrafficAmbienceSystem.kUpdatesPerDay;
		}

		public static float3 GetCellCenter(int index)
		{
			return CellMapSystem<TrafficAmbienceCell>.GetCellCenter(index, TrafficAmbienceSystem.kTextureSize);
		}

		public static TrafficAmbienceCell GetTrafficAmbience2(float3 position, NativeArray<TrafficAmbienceCell> trafficAmbienceMap, float maxPerCell)
		{
			TrafficAmbienceCell result = default(TrafficAmbienceCell);
			int2 cell = CellMapSystem<TrafficAmbienceCell>.GetCell(position, CellMapSystem<TrafficAmbienceCell>.kMapSize, TrafficAmbienceSystem.kTextureSize);
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
			return result;
		}

		public static TrafficAmbienceCell GetTrafficAmbience(float3 position, NativeArray<TrafficAmbienceCell> trafficAmbienceMap)
		{
			TrafficAmbienceCell result = default(TrafficAmbienceCell);
			int2 cell = CellMapSystem<TrafficAmbienceCell>.GetCell(position, CellMapSystem<TrafficAmbienceCell>.kMapSize, TrafficAmbienceSystem.kTextureSize);
			if (cell.x < 0 || cell.x >= TrafficAmbienceSystem.kTextureSize || cell.y < 0 || cell.y >= TrafficAmbienceSystem.kTextureSize)
			{
				TrafficAmbienceCell result2 = default(TrafficAmbienceCell);
				result2.m_Accumulator = 0f;
				result2.m_Traffic = 0f;
				return result2;
			}
			float2 cellCoords = CellMapSystem<TrafficAmbienceCell>.GetCellCoords(position, CellMapSystem<TrafficAmbienceCell>.kMapSize, TrafficAmbienceSystem.kTextureSize);
			float traffic = trafficAmbienceMap[cell.x + TrafficAmbienceSystem.kTextureSize * cell.y].m_Traffic;
			float y = ((cell.x < TrafficAmbienceSystem.kTextureSize - 1) ? trafficAmbienceMap[cell.x + 1 + TrafficAmbienceSystem.kTextureSize * cell.y].m_Traffic : 0f);
			float x = ((cell.y < TrafficAmbienceSystem.kTextureSize - 1) ? trafficAmbienceMap[cell.x + TrafficAmbienceSystem.kTextureSize * (cell.y + 1)].m_Traffic : 0f);
			float y2 = ((cell.x < TrafficAmbienceSystem.kTextureSize - 1 && cell.y < TrafficAmbienceSystem.kTextureSize - 1) ? trafficAmbienceMap[cell.x + 1 + TrafficAmbienceSystem.kTextureSize * (cell.y + 1)].m_Traffic : 0f);
			result.m_Traffic = math.lerp(math.lerp(traffic, y, cellCoords.x - (float)cell.x), math.lerp(x, y2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
			return result;
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			base.CreateTextures(TrafficAmbienceSystem.kTextureSize);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			TrafficAmbienceUpdateJob trafficAmbienceUpdateJob = default(TrafficAmbienceUpdateJob);
			trafficAmbienceUpdateJob.m_TrafficMap = base.m_Map;
			TrafficAmbienceUpdateJob jobData = trafficAmbienceUpdateJob;
			base.Dependency = IJobParallelForExtensions.Schedule(jobData, TrafficAmbienceSystem.kTextureSize * TrafficAmbienceSystem.kTextureSize, TrafficAmbienceSystem.kTextureSize, JobHandle.CombineDependencies(base.m_WriteDependencies, base.m_ReadDependencies, base.Dependency));
			base.AddWriter(base.Dependency);
			base.Dependency = JobHandle.CombineDependencies(base.m_ReadDependencies, base.m_WriteDependencies, base.Dependency);
		}

		[Preserve]
		public TrafficAmbienceSystem()
		{
		}
	}
}
