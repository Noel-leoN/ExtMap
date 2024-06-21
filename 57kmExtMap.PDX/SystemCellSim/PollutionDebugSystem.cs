using Colossal;
using Game.Simulation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;
using Game.Debug;

namespace Game.Debug
{
	public partial class PollutionDebugSystem : BaseDebugSystem
	{
		private struct PollutionGizmoJob : IJob
		{
			[ReadOnly]
			public NativeArray<GroundPollution> m_PollutionMap;

			[ReadOnly]
			public NativeArray<AirPollution> m_AirPollutionMap;

			[ReadOnly]
			public NativeArray<NoisePollution> m_NoisePollutionMap;

			public GizmoBatcher m_GizmoBatcher;

			public bool m_GroundOption;

			public bool m_AirOption;

			public bool m_NoiseOption;

			public float m_BaseHeight;

			public void Execute()
			{
				float3 @float = new float3(0f, this.m_BaseHeight, 0f);
				if (this.m_GroundOption)
				{
					for (int i = 0; i < this.m_PollutionMap.Length; i++)
					{
						GroundPollution groundPollution = this.m_PollutionMap[i];
						if (groundPollution.m_Pollution > 0)
						{
							float3 cellCenter = GroundPollutionSystem.GetCellCenter(i);
							cellCenter.y += (float)groundPollution.m_Pollution / 400f;
							Color color = ((groundPollution.m_Pollution >= 8000) ? Color.Lerp(Color.yellow, Color.red, math.saturate((float)(groundPollution.m_Pollution - 8000) / 8000f)) : Color.Lerp(Color.green, Color.yellow, (float)groundPollution.m_Pollution / 8000f));
							this.m_GizmoBatcher.DrawWireCube(cellCenter + @float, new float3(10f, (float)groundPollution.m_Pollution / 200f, 10f), color);
						}
					}
				}
				if (this.m_AirOption)
				{
					for (int j = 0; j < this.m_AirPollutionMap.Length; j++)
					{
						AirPollution airPollution = this.m_AirPollutionMap[j];
						if (airPollution.m_Pollution > 0)
						{
							float3 cellCenter2 = AirPollutionSystem.GetCellCenter(j);
							cellCenter2.y += 200f;
							Color color2 = ((airPollution.m_Pollution >= 8000) ? Color.Lerp(Color.yellow, Color.red, math.saturate((float)(airPollution.m_Pollution - 8000) / 8000f)) : Color.Lerp(Color.green, Color.yellow, (float)airPollution.m_Pollution / 8000f));
							this.m_GizmoBatcher.DrawWireCone(cellCenter2 + @float, 10f, cellCenter2 + @float + new float3(0f, (float)airPollution.m_Pollution / 50f, 0f), 10f, color2);
						}
					}
				}
				if (!this.m_NoiseOption)
				{
					return;
				}
				for (int k = 0; k < this.m_NoisePollutionMap.Length; k++)
				{
					NoisePollution noisePollution = this.m_NoisePollutionMap[k];
					if (noisePollution.m_Pollution > 0)
					{
						float3 cellCenter3 = NoisePollutionSystem.GetCellCenter(k);
						cellCenter3.y += 50f + (float)noisePollution.m_Pollution / 400f;
						Color color3 = ((noisePollution.m_Pollution >= 8000) ? Color.Lerp(Color.yellow, Color.red, math.saturate((float)(noisePollution.m_Pollution - 8000) / 8000f)) : Color.Lerp(Color.green, Color.yellow, (float)noisePollution.m_Pollution / 8000f));
						this.m_GizmoBatcher.DrawWireCube(cellCenter3 + @float, new float3(10f, (float)noisePollution.m_Pollution / 200f, 10f), color3);
					}
				}
			}
		}

		private GroundPollutionSystem m_GroundPollutionSystem;

		private AirPollutionSystem m_AirPollutionSystem;

		private NoisePollutionSystem m_NoisePollutionSystem;

		private ClimateSystem m_ClimateSystem;

		private GizmosSystem m_GizmosSystem;

		private Option m_GroundOption;

		private Option m_AirOption;

		private Option m_NoiseOption;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
			this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
			this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
			this.m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
			this.m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
			this.m_GroundOption = base.AddOption("Ground pollution", defaultEnabled: true);
			this.m_AirOption = base.AddOption("Air pollution", defaultEnabled: true);
			this.m_NoiseOption = base.AddOption("Noise pollution", defaultEnabled: true);
			base.Enabled = false;
		}

		[Preserve]
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			PollutionGizmoJob jobData = default(PollutionGizmoJob);
			jobData.m_PollutionMap = this.m_GroundPollutionSystem.GetMap(readOnly: true, out var dependencies);
			jobData.m_AirPollutionMap = this.m_AirPollutionSystem.GetMap(readOnly: true, out var dependencies2);
			jobData.m_NoisePollutionMap = this.m_NoisePollutionSystem.GetMap(readOnly: true, out var dependencies3);
			jobData.m_GizmoBatcher = this.m_GizmosSystem.GetGizmosBatcher(out var dependencies4);
			jobData.m_AirOption = this.m_AirOption.enabled;
			jobData.m_GroundOption = this.m_GroundOption.enabled;
			jobData.m_NoiseOption = this.m_NoiseOption.enabled;
			jobData.m_BaseHeight = this.m_ClimateSystem.temperatureBaseHeight;
			JobHandle jobHandle = jobData.Schedule(JobHandle.CombineDependencies(dependencies2, dependencies3, JobHandle.CombineDependencies(inputDeps, dependencies4, dependencies)));
			this.m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
			this.m_GroundPollutionSystem.AddReader(jobHandle);
			this.m_AirPollutionSystem.AddReader(jobHandle);
			this.m_NoisePollutionSystem.AddReader(jobHandle);
			return jobHandle;
		}

		[Preserve]
		public PollutionDebugSystem()
		{
		}
	}
}
