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
	public partial class ZoneAmbienceSystem : CellMapSystem<ZoneAmbienceCell>, IJobSerializable
	{
		[BurstCompile]
		private struct ZoneAmbienceUpdateJob : IJobParallelFor
		{
			public NativeArray<ZoneAmbienceCell> m_ZoneMap;

			public void Execute(int index)
			{
				ZoneAmbienceCell zoneAmbienceCell = this.m_ZoneMap[index];
				this.m_ZoneMap[index] = new ZoneAmbienceCell
				{
					m_Value = zoneAmbienceCell.m_Accumulator,
					m_Accumulator = default(ZoneAmbiences)
				};
			}
		}

		public static readonly int kTextureSize = 64;

		public static readonly int kUpdatesPerDay = 128;

		public int2 TextureSize => new int2(ZoneAmbienceSystem.kTextureSize, ZoneAmbienceSystem.kTextureSize);

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 262144 / ZoneAmbienceSystem.kUpdatesPerDay;
		}

		public static float3 GetCellCenter(int index)
		{
			return CellMapSystem<ZoneAmbienceCell>.GetCellCenter(index, ZoneAmbienceSystem.kTextureSize);
		}

		public static float GetZoneAmbienceNear(GroupAmbienceType type, float3 position, NativeArray<ZoneAmbienceCell> zoneAmbienceMap, float nearWeight, float maxPerCell)
		{
			int2 cell = CellMapSystem<ZoneAmbienceCell>.GetCell(position, CellMapSystem<ZoneAmbienceCell>.kMapSize, ZoneAmbienceSystem.kTextureSize);
			float num = 0f;
			float num2 = 0f;
			for (int i = cell.x - 2; i <= cell.x + 2; i++)
			{
				for (int j = cell.y - 2; j <= cell.y + 2; j++)
				{
					if (i >= 0 && i < ZoneAmbienceSystem.kTextureSize && j >= 0 && j < ZoneAmbienceSystem.kTextureSize)
					{
						int index = i + ZoneAmbienceSystem.kTextureSize * j;
						float num3 = math.max(1f, math.pow(math.distance(ZoneAmbienceSystem.GetCellCenter(index), position) / 10f, 1f + nearWeight));
						num += math.min(maxPerCell, zoneAmbienceMap[index].m_Value.GetAmbience(type)) / num3;
						num2 += 1f / num3;
					}
				}
			}
			return num / num2;
		}

		public static float GetZoneAmbience(GroupAmbienceType type, float3 position, NativeArray<ZoneAmbienceCell> zoneAmbienceMap, float maxPerCell)
		{
			int2 cell = CellMapSystem<ZoneAmbienceCell>.GetCell(position, CellMapSystem<ZoneAmbienceCell>.kMapSize, ZoneAmbienceSystem.kTextureSize);
			float num = 0f;
			float num2 = 0f;
			for (int i = cell.x - 2; i <= cell.x + 2; i++)
			{
				for (int j = cell.y - 2; j <= cell.y + 2; j++)
				{
					if (i >= 0 && i < ZoneAmbienceSystem.kTextureSize && j >= 0 && j < ZoneAmbienceSystem.kTextureSize)
					{
						int index = i + ZoneAmbienceSystem.kTextureSize * j;
						float num3 = math.max(1f, math.distancesq(ZoneAmbienceSystem.GetCellCenter(index), position) / 10f);
						num += math.min(maxPerCell, zoneAmbienceMap[index].m_Value.GetAmbience(type)) / num3;
						num2 += 1f / num3;
					}
				}
			}
			return num / num2;
		}

		public static ZoneAmbienceCell GetZoneAmbience(float3 position, NativeArray<ZoneAmbienceCell> zoneAmbienceMap)
		{
			ZoneAmbienceCell result = default(ZoneAmbienceCell);
			int2 cell = CellMapSystem<ZoneAmbienceCell>.GetCell(position, CellMapSystem<ZoneAmbienceCell>.kMapSize, ZoneAmbienceSystem.kTextureSize);
			ZoneAmbiences zoneAmbiences = default(ZoneAmbiences);
			float num = 0f;
			for (int i = cell.x - 2; i <= cell.x + 2; i++)
			{
				for (int j = cell.y - 2; j <= cell.y + 2; j++)
				{
					if (i >= 0 && i < ZoneAmbienceSystem.kTextureSize && j >= 0 && j < ZoneAmbienceSystem.kTextureSize)
					{
						int index = i + ZoneAmbienceSystem.kTextureSize * j;
						float num2 = math.max(1f, math.distancesq(ZoneAmbienceSystem.GetCellCenter(index), position) / 10f);
						zoneAmbiences += zoneAmbienceMap[index].m_Value / num2;
						num += 1f / num2;
					}
				}
			}
			result.m_Value = zoneAmbiences / num;
			return result;
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			base.CreateTextures(ZoneAmbienceSystem.kTextureSize);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			ZoneAmbienceUpdateJob zoneAmbienceUpdateJob = default(ZoneAmbienceUpdateJob);
			zoneAmbienceUpdateJob.m_ZoneMap = base.m_Map;
			ZoneAmbienceUpdateJob jobData = zoneAmbienceUpdateJob;
			base.Dependency = IJobParallelForExtensions.Schedule(jobData, ZoneAmbienceSystem.kTextureSize * ZoneAmbienceSystem.kTextureSize, ZoneAmbienceSystem.kTextureSize, JobHandle.CombineDependencies(base.m_WriteDependencies, base.m_ReadDependencies, base.Dependency));
			base.AddWriter(base.Dependency);
			base.Dependency = JobHandle.CombineDependencies(base.m_ReadDependencies, base.m_WriteDependencies, base.Dependency);
		}

		[Preserve]
		public ZoneAmbienceSystem()
		{
		}
	}
}
