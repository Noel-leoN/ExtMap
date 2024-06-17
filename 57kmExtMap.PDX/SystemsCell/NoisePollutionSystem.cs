using Colossal.Serialization.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace ExtMap57km.Systems
{
	public partial class NoisePollutionSystem : Game.Simulation.CellMapSystem<NoisePollution>, IJobSerializable
	{
		[BurstCompile]
		private struct NoisePollutionSwapJob : IJobParallelFor
		{
			[NativeDisableParallelForRestriction]
			public NativeArray<NoisePollution> m_PollutionMap;

			public void Execute(int index)
			{
				NoisePollution value = this.m_PollutionMap[index];
				int num = index % NoisePollutionSystem.kTextureSize;
				int num2 = index / NoisePollutionSystem.kTextureSize;
				short num3 = (short)((num > 0) ? this.m_PollutionMap[index - 1].m_PollutionTemp : 0);
				short num4 = (short)((num < NoisePollutionSystem.kTextureSize - 1) ? this.m_PollutionMap[index + 1].m_PollutionTemp : 0);
				short num5 = (short)((num2 > 0) ? this.m_PollutionMap[index - NoisePollutionSystem.kTextureSize].m_PollutionTemp : 0);
				short num6 = (short)((num2 < NoisePollutionSystem.kTextureSize - 1) ? this.m_PollutionMap[index + NoisePollutionSystem.kTextureSize].m_PollutionTemp : 0);
				short num7 = (short)((num > 0 && num2 > 0) ? this.m_PollutionMap[index - 1 - NoisePollutionSystem.kTextureSize].m_PollutionTemp : 0);
				short num8 = (short)((num < NoisePollutionSystem.kTextureSize - 1 && num2 > 0) ? this.m_PollutionMap[index + 1 - NoisePollutionSystem.kTextureSize].m_PollutionTemp : 0);
				short num9 = (short)((num > 0 && num2 < NoisePollutionSystem.kTextureSize - 1) ? this.m_PollutionMap[index - 1 + NoisePollutionSystem.kTextureSize].m_PollutionTemp : 0);
				short num10 = (short)((num < NoisePollutionSystem.kTextureSize - 1 && num2 < NoisePollutionSystem.kTextureSize - 1) ? this.m_PollutionMap[index + 1 + NoisePollutionSystem.kTextureSize].m_PollutionTemp : 0);
				value.m_Pollution = (short)(value.m_PollutionTemp / 4 + (num3 + num4 + num5 + num6) / 8 + (num7 + num8 + num9 + num10) / 16);
				this.m_PollutionMap[index] = value;
			}
		}

		[BurstCompile]
		private struct NoisePollutionClearJob : IJobParallelFor
		{
			public NativeArray<NoisePollution> m_PollutionMap;

			public void Execute(int index)
			{
				NoisePollution value = this.m_PollutionMap[index];
				value.m_PollutionTemp = 0;
				this.m_PollutionMap[index] = value;
			}
		}

		public static readonly int kTextureSize = 256;

		public static readonly int kUpdatesPerDay = 128;

		public int2 TextureSize => new int2(NoisePollutionSystem.kTextureSize, NoisePollutionSystem.kTextureSize);

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 262144 / NoisePollutionSystem.kUpdatesPerDay;
		}

		public static float3 GetCellCenter(int index)
		{
			return Game.Simulation.CellMapSystem<NoisePollution>.GetCellCenter(index, NoisePollutionSystem.kTextureSize);
		}

		public static NoisePollution GetPollution(float3 position, NativeArray<NoisePollution> pollutionMap)
		{
			NoisePollution result = default(NoisePollution);
			float num = (float)Game.Simulation.CellMapSystem<NoisePollution>.kMapSize / (float)NoisePollutionSystem.kTextureSize;
            int2 cell = Game.Simulation.CellMapSystem<NoisePollution>.GetCell(position - new float3(num / 2f, 0f, num / 2f), Game.Simulation.CellMapSystem<NoisePollution>.kMapSize, NoisePollutionSystem.kTextureSize);
            float2 @float = Game.Simulation.CellMapSystem<NoisePollution>.GetCellCoords(position, Game.Simulation.CellMapSystem<NoisePollution>.kMapSize, NoisePollutionSystem.kTextureSize) - new float2(0.5f, 0.5f);
			cell = math.clamp(cell, 0, NoisePollutionSystem.kTextureSize - 2);
			short pollution = pollutionMap[cell.x + NoisePollutionSystem.kTextureSize * cell.y].m_Pollution;
			short pollution2 = pollutionMap[cell.x + 1 + NoisePollutionSystem.kTextureSize * cell.y].m_Pollution;
			short pollution3 = pollutionMap[cell.x + NoisePollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
			short pollution4 = pollutionMap[cell.x + 1 + NoisePollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
			result.m_Pollution = (short)Mathf.RoundToInt(math.lerp(math.lerp(pollution, pollution2, @float.x - (float)cell.x), math.lerp(pollution3, pollution4, @float.x - (float)cell.x), @float.y - (float)cell.y));
			return result;
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			base.CreateTextures(NoisePollutionSystem.kTextureSize);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			NoisePollutionSwapJob noisePollutionSwapJob = default(NoisePollutionSwapJob);
			noisePollutionSwapJob.m_PollutionMap = base.GetMap(readOnly: false, out var dependencies);
			NoisePollutionSwapJob jobData = noisePollutionSwapJob;
			NoisePollutionClearJob noisePollutionClearJob = default(NoisePollutionClearJob);
			noisePollutionClearJob.m_PollutionMap = jobData.m_PollutionMap;
			dependencies = noisePollutionClearJob.Schedule(dependsOn: IJobParallelForExtensions.Schedule(jobData, base.m_Map.Length, 4, dependencies), arrayLength: base.m_Map.Length, innerloopBatchCount: 64);
			base.AddWriter(dependencies);
		}

		[Preserve]
		public NoisePollutionSystem()
		{
		}
	}
}
