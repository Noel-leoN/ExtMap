using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace ExtMap57km.Systems
{
    //[CompilerGenerated]
    public partial class AirPollutionSystem : CellMapSystem<AirPollution>, IJobSerializable
	{
		[BurstCompile]
		private struct AirPollutionMoveJob : IJob
		{
			public NativeArray<AirPollution> m_PollutionMap;

			[ReadOnly]
			public NativeArray<Wind> m_WindMap;

			public PollutionParameterData m_PollutionParameters;

			public RandomSeed m_Random;

			public uint m_Frame;

			public void Execute()
			{
				NativeArray<AirPollution> nativeArray = new NativeArray<AirPollution>(this.m_PollutionMap.Length, Allocator.Temp);
				Random random = this.m_Random.GetRandom((int)this.m_Frame);
				for (int i = 0; i < this.m_PollutionMap.Length; i++)
				{
					float3 cellCenter = AirPollutionSystem.GetCellCenter(i);
					Wind wind = WindSystem.GetWind(cellCenter, this.m_WindMap);
					short pollution = AirPollutionSystem.GetPollution(cellCenter - this.m_PollutionParameters.m_WindAdvectionSpeed * new float3(wind.m_Wind.x, 0f, wind.m_Wind.y), this.m_PollutionMap).m_Pollution;
					nativeArray[i] = new AirPollution
					{
						m_Pollution = pollution
					};
				}
				float value = (float)this.m_PollutionParameters.m_AirFade / (float)AirPollutionSystem.kUpdatesPerDay;
				for (int j = 0; j < AirPollutionSystem.kTextureSize; j++)
				{
					for (int k = 0; k < AirPollutionSystem.kTextureSize; k++)
					{
						int num = j * AirPollutionSystem.kTextureSize + k;
						int pollution2 = nativeArray[num].m_Pollution;
						pollution2 += ((k > 0) ? (nativeArray[num - 1].m_Pollution >> AirPollutionSystem.kSpread) : 0);
						pollution2 += ((k < AirPollutionSystem.kTextureSize - 1) ? (nativeArray[num + 1].m_Pollution >> AirPollutionSystem.kSpread) : 0);
						pollution2 += ((j > 0) ? (nativeArray[num - AirPollutionSystem.kTextureSize].m_Pollution >> AirPollutionSystem.kSpread) : 0);
						pollution2 += ((j < AirPollutionSystem.kTextureSize - 1) ? (nativeArray[num + AirPollutionSystem.kTextureSize].m_Pollution >> AirPollutionSystem.kSpread) : 0);
						pollution2 -= (nativeArray[num].m_Pollution >> AirPollutionSystem.kSpread - 2) + MathUtils.RoundToIntRandom(ref random, value);
						pollution2 = math.clamp(pollution2, 0, 32767);
						this.m_PollutionMap[num] = new AirPollution
						{
							m_Pollution = (short)pollution2
						};
					}
				}
				nativeArray.Dispose();
			}
		}

		private static readonly int kSpread = 3;

		public static readonly int kTextureSize = 256;

		public static readonly int kUpdatesPerDay = 128;

		private WindSystem m_WindSystem;

		private SimulationSystem m_SimulationSystem;

		private EntityQuery m_PollutionParameterQuery;

		public int2 TextureSize => new int2(AirPollutionSystem.kTextureSize, AirPollutionSystem.kTextureSize);

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 262144 / AirPollutionSystem.kUpdatesPerDay;
		}

		public static float3 GetCellCenter(int index)
		{
			return CellMapSystem<AirPollution>.GetCellCenter(index, AirPollutionSystem.kTextureSize);
		}

		public static AirPollution GetPollution(float3 position, NativeArray<AirPollution> pollutionMap)
		{
			AirPollution result = default(AirPollution);
			float num = (float)CellMapSystem<AirPollution>.kMapSize / (float)AirPollutionSystem.kTextureSize;
			int2 cell = CellMapSystem<AirPollution>.GetCell(position - new float3(num / 2f, 0f, num / 2f), CellMapSystem<AirPollution>.kMapSize, AirPollutionSystem.kTextureSize);
			float2 @float = CellMapSystem<AirPollution>.GetCellCoords(position, CellMapSystem<AirPollution>.kMapSize, AirPollutionSystem.kTextureSize) - new float2(0.5f, 0.5f);
			cell = math.clamp(cell, 0, AirPollutionSystem.kTextureSize - 2);
			short pollution = pollutionMap[cell.x + AirPollutionSystem.kTextureSize * cell.y].m_Pollution;
			short pollution2 = pollutionMap[cell.x + 1 + AirPollutionSystem.kTextureSize * cell.y].m_Pollution;
			short pollution3 = pollutionMap[cell.x + AirPollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
			short pollution4 = pollutionMap[cell.x + 1 + AirPollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
			result.m_Pollution = (short)math.round(math.lerp(math.lerp(pollution, pollution2, @float.x - (float)cell.x), math.lerp(pollution3, pollution4, @float.x - (float)cell.x), @float.y - (float)cell.y));
			return result;
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			base.CreateTextures(AirPollutionSystem.kTextureSize);
			this.m_WindSystem = base.World.GetOrCreateSystemManaged<WindSystem>();
			this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			this.m_PollutionParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
			base.RequireForUpdate(this.m_PollutionParameterQuery);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			AirPollutionMoveJob airPollutionMoveJob = default(AirPollutionMoveJob);
			airPollutionMoveJob.m_PollutionMap = base.m_Map;
			airPollutionMoveJob.m_WindMap = this.m_WindSystem.GetMap(readOnly: true, out var dependencies);
			airPollutionMoveJob.m_PollutionParameters = this.m_PollutionParameterQuery.GetSingleton<PollutionParameterData>();
			airPollutionMoveJob.m_Random = RandomSeed.Next();
			airPollutionMoveJob.m_Frame = this.m_SimulationSystem.frameIndex;
			AirPollutionMoveJob jobData = airPollutionMoveJob;
			base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(dependencies, base.m_WriteDependencies, base.m_ReadDependencies, base.Dependency));
			this.m_WindSystem.AddReader(base.Dependency);
			base.AddWriter(base.Dependency);
			base.Dependency = JobHandle.CombineDependencies(base.m_ReadDependencies, base.m_WriteDependencies, base.Dependency);
		}

		[Preserve]
		public AirPollutionSystem()
		{
		}
	}
}
