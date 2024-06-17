using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
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
	public partial class ObjectPolluteSystem : GameSystemBase
	{
		[BurstCompile]
		private struct ObjectPolluteJob : IJobChunk
		{
			[ReadOnly]
			public EntityTypeHandle m_EntityType;

			[ReadOnly]
			public ComponentTypeHandle<Transform> m_TransformType;

			public ComponentTypeHandle<Plant> m_PlantType;

			[ReadOnly]
			public NativeArray<GroundPollution> m_GroundPollutionMap;

			[ReadOnly]
			public NativeArray<AirPollution> m_AirPollutionMap;

			public PollutionParameterData m_PollutionParameters;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
				NativeArray<Plant> nativeArray2 = chunk.GetNativeArray(ref this.m_PlantType);
				NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref this.m_TransformType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					float3 position = nativeArray3[i].m_Position;
					GroundPollution pollution = GroundPollutionSystem.GetPollution(position, this.m_GroundPollutionMap);
					AirPollution pollution2 = AirPollutionSystem.GetPollution(position, this.m_AirPollutionMap);
					Plant value = nativeArray2[i];
					value.m_Pollution = math.saturate(value.m_Pollution + (this.m_PollutionParameters.m_PlantGroundMultiplier * (float)pollution.m_Pollution + this.m_PollutionParameters.m_PlantAirMultiplier * (float)pollution2.m_Pollution - this.m_PollutionParameters.m_PlantFade) / (float)ObjectPolluteSystem.kUpdatesPerDay);
					nativeArray2[i] = value;
				}
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		private struct TypeHandle
		{
			[ReadOnly]
			public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

			public ComponentTypeHandle<Plant> __Game_Objects_Plant_RW_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
				this.__Game_Objects_Plant_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Plant>();
				this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			}
		}

		public static readonly int kUpdatesPerDay = 32;

		private GroundPollutionSystem m_GroundPollutionSystem;

		private AirPollutionSystem m_AirPollutionSystem;

		private SimulationSystem m_SimulationSystem;

		private EntityQuery m_PollutionParameterQuery;

		private EntityQuery m_PollutableObjectQuery;

		private TypeHandle __TypeHandle;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 262144 / (ObjectPolluteSystem.kUpdatesPerDay * 16);
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
			this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
			this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			this.m_PollutionParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
			this.m_PollutableObjectQuery = base.GetEntityQuery(ComponentType.ReadOnly<Plant>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		}

		[Preserve]
		protected override void OnUpdate()
		{
			uint updateFrame = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, ObjectPolluteSystem.kUpdatesPerDay, 16);
			this.m_PollutableObjectQuery.ResetFilter();
			this.m_PollutableObjectQuery.SetSharedComponentFilter(new UpdateFrame(updateFrame));
			this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Plant_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
			ObjectPolluteJob jobData = default(ObjectPolluteJob);
			jobData.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
			jobData.m_PlantType = this.__TypeHandle.__Game_Objects_Plant_RW_ComponentTypeHandle;
			jobData.m_TransformType = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle;
			jobData.m_GroundPollutionMap = this.m_GroundPollutionSystem.GetMap(readOnly: true, out var dependencies);
			jobData.m_AirPollutionMap = this.m_AirPollutionSystem.GetMap(readOnly: true, out var dependencies2);
			jobData.m_PollutionParameters = this.m_PollutionParameterQuery.GetSingleton<PollutionParameterData>();
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, this.m_PollutableObjectQuery, JobHandle.CombineDependencies(dependencies2, base.Dependency, dependencies));
			this.m_GroundPollutionSystem.AddReader(jobHandle);
			this.m_AirPollutionSystem.AddReader(jobHandle);
			base.Dependency = jobHandle;
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
		public ObjectPolluteSystem()
		{
		}
	}
}
