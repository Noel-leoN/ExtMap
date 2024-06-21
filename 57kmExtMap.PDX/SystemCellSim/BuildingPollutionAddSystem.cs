using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace ExtMap57km.Systems
{
	//[CompilerGenerated]
	//需要重写系统；
	public partial class BuildingPollutionAddSystem : GameSystemBase
	{
		private struct PollutionItem
		{
			public int amount;

			public float2 position;
		}

		[BurstCompile]
		private struct ApplyBuildingPollutionJob<T> : IJob where T : struct, IPollution
		{
			public NativeArray<T> m_PollutionMap;

			public NativeQueue<PollutionItem> m_PollutionQueue;

			public int m_MapSize;

			public int m_TextureSize;

			public float m_MaxRadiusSq;

			public float m_Radius;

			public float m_Multiplier;

			public NativeArray<float> m_WeightCache;

			[ReadOnly]
			public NativeArray<float> m_DistanceWeightCache;

			public PollutionParameterData m_PollutionParameters;

			private float GetWeight(int2 cell, float2 position, float radiusSq, float offset, int cellSize)
			{
				float2 @float = new float2(0f - offset + ((float)cell.x + 0.5f) * (float)cellSize, 0f - offset + ((float)cell.y + 0.5f) * (float)cellSize);
				float num = math.lengthsq(position - @float);
				if (num < radiusSq)
				{
					float num2 = 255f * num / this.m_MaxRadiusSq;
					int num3 = Mathf.FloorToInt(num2);
					return math.lerp(this.m_DistanceWeightCache[num3], this.m_DistanceWeightCache[num3 + 1], math.frac(num2));
				}
				return 0f;
			}

			private void AddSingle(int pollution, int mapSize, int textureSize, float2 position, float radius, NativeArray<T> map)
			{
				int num = mapSize / textureSize;
				float num2 = (float)mapSize / 2f;
				float radiusSq = radius * radius;
				int2 @int = new int2(math.max(0, Mathf.FloorToInt((position.x + num2 - radius) / (float)num)), math.max(0, Mathf.FloorToInt((position.y + num2 - radius) / (float)num)));
				int2 int2 = new int2(math.min(textureSize - 1, Mathf.CeilToInt((position.x + num2 + radius) / (float)num)), math.min(textureSize - 1, Mathf.CeilToInt((position.y + num2 + radius) / (float)num)));
				float num3 = 0f;
				int num4 = 0;
				int2 cell = default(int2);
				cell.x = @int.x;
				while (cell.x <= int2.x)
				{
					cell.y = @int.y;
					while (cell.y <= int2.y)
					{
						float weight = this.GetWeight(cell, position, radiusSq, 0.5f * (float)mapSize, num);
						num3 += weight;
						this.m_WeightCache[num4] = weight;
						num4++;
						cell.y++;
					}
					cell.x++;
				}
				num4 = 0;
				float num5 = 1f / (num3 * (float)BuildingPollutionAddSystem.kUpdatesPerDay);
				cell.x = @int.x;
				while (cell.x <= int2.x)
				{
					int num6 = cell.x + textureSize * @int.y;
					cell.y = @int.y;
					while (cell.y <= int2.y)
					{
						float num7 = (float)pollution * num5 * this.m_WeightCache[num4];
						num4++;
						if (num7 > 0.2f)
						{
							int num8 = Mathf.CeilToInt(num7);
							T value = map[num6];
							value.Add((short)num8);
							map[num6] = value;
						}
						num6 += textureSize;
						cell.y++;
					}
					cell.x++;
				}
			}

			public void Execute()
			{
				PollutionItem item;
				while (this.m_PollutionQueue.TryDequeue(out item))
				{
					this.AddSingle((int)(this.m_Multiplier * (float)item.amount), this.m_MapSize, this.m_TextureSize, item.position, this.m_Radius, this.m_PollutionMap);
				}
			}
		}

		[BurstCompile]
		private struct BuildingPolluteJob : IJobChunk
		{
			[ReadOnly]
			public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

			[ReadOnly]
			public ComponentTypeHandle<Destroyed> m_DestroyedType;

			[ReadOnly]
			public ComponentTypeHandle<Abandoned> m_AbandonedType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;

			[ReadOnly]
			public BufferTypeHandle<Efficiency> m_BuildingEfficiencyType;

			[ReadOnly]
			public BufferTypeHandle<Renter> m_RenterType;

			[ReadOnly]
			public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

			[ReadOnly]
			public ComponentLookup<PrefabRef> m_Prefabs;

			[ReadOnly]
			public ComponentLookup<BuildingData> m_BuildingDatas;

			[ReadOnly]
			public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

			[ReadOnly]
			public ComponentLookup<PollutionData> m_PollutionDatas;

			[ReadOnly]
			public ComponentLookup<PollutionModifierData> m_PollutionModifierDatas;

			[ReadOnly]
			public ComponentLookup<ZoneData> m_ZoneDatas;

			[ReadOnly]
			public BufferLookup<Employee> m_Employees;

			[ReadOnly]
			public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

			[ReadOnly]
			public ComponentLookup<Citizen> m_Citizens;

			[ReadOnly]
			public BufferLookup<CityModifier> m_CityModifiers;

			[ReadOnly]
			public PollutionParameterData m_PollutionParameters;

			public NativeQueue<PollutionItem>.ParallelWriter m_GroundPollutionQueue;

			public NativeQueue<PollutionItem>.ParallelWriter m_AirPollutionQueue;

			public NativeQueue<PollutionItem>.ParallelWriter m_NoisePollutionQueue;

			public Entity m_City;

			public uint m_UpdateFrameIndex;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				if (chunk.GetSharedComponent(this.m_UpdateFrameType).m_Index != this.m_UpdateFrameIndex)
				{
					return;
				}
				NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref this.m_PrefabRefType);
				NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref this.m_TransformType);
				bool destroyed = chunk.Has(ref this.m_DestroyedType);
				bool abandoned = chunk.Has(ref this.m_AbandonedType);
				bool isPark = chunk.Has(ref this.m_ParkType);
				BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref this.m_BuildingEfficiencyType);
				BufferAccessor<Renter> bufferAccessor2 = chunk.GetBufferAccessor(ref this.m_RenterType);
				BufferAccessor<InstalledUpgrade> bufferAccessor3 = chunk.GetBufferAccessor(ref this.m_InstalledUpgradeType);
				DynamicBuffer<CityModifier> cityModifiers = this.m_CityModifiers[this.m_City];
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity prefab = nativeArray[i].m_Prefab;
					float3 position = nativeArray2[i].m_Position;
					float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
					DynamicBuffer<Renter> renters = ((bufferAccessor2.Length != 0) ? bufferAccessor2[i] : default(DynamicBuffer<Renter>));
					DynamicBuffer<InstalledUpgrade> installedUpgrades = ((bufferAccessor3.Length != 0) ? bufferAccessor3[i] : default(DynamicBuffer<InstalledUpgrade>));
					PollutionData buildingPollution = BuildingPollutionAddSystem.GetBuildingPollution(prefab, destroyed, abandoned, isPark, efficiency, renters, installedUpgrades, this.m_PollutionParameters, cityModifiers, ref this.m_Prefabs, ref this.m_BuildingDatas, ref this.m_SpawnableDatas, ref this.m_PollutionDatas, ref this.m_PollutionModifierDatas, ref this.m_ZoneDatas, ref this.m_Employees, ref this.m_HouseholdCitizens, ref this.m_Citizens);
					if (buildingPollution.m_GroundPollution > 0f)
					{
						this.m_GroundPollutionQueue.Enqueue(new PollutionItem
						{
							amount = (int)buildingPollution.m_GroundPollution,
							position = position.xz
						});
					}
					if (buildingPollution.m_AirPollution > 0f)
					{
						this.m_AirPollutionQueue.Enqueue(new PollutionItem
						{
							amount = (int)buildingPollution.m_AirPollution,
							position = position.xz
						});
					}
					if (buildingPollution.m_NoisePollution > 0f)
					{
						this.m_NoisePollutionQueue.Enqueue(new PollutionItem
						{
							amount = (int)buildingPollution.m_NoisePollution,
							position = position.xz
						});
					}
				}
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		private struct TypeHandle
		{
			public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Abandoned> __Game_Buildings_Abandoned_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

			[ReadOnly]
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PollutionData> __Game_Prefabs_PollutionData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PollutionModifierData> __Game_Prefabs_PollutionModifierData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

			[ReadOnly]
			public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
				this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
				this.__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
				this.__Game_Buildings_Abandoned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Abandoned>(isReadOnly: true);
				this.__Game_Buildings_Park_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>(isReadOnly: true);
				this.__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
				this.__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
				this.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
				this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
				this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
				this.__Game_Prefabs_PollutionData_RO_ComponentLookup = state.GetComponentLookup<PollutionData>(isReadOnly: true);
				this.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup = state.GetComponentLookup<PollutionModifierData>(isReadOnly: true);
				this.__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
				this.__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
				this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
				this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
				this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			}
		}

		public static readonly int kUpdatesPerDay = 128;

		private SimulationSystem m_SimulationSystem;

		private GroundPollutionSystem m_GroundPollutionSystem;

		private AirPollutionSystem m_AirPollutionSystem;

		private NoisePollutionSystem m_NoisePollutionSystem;

		private CitySystem m_CitySystem;

		private EntityQuery m_PolluterQuery;

		private NativeArray<float> m_GroundWeightCache;

		private NativeArray<float> m_AirWeightCache;

		private NativeArray<float> m_NoiseWeightCache;

		private NativeArray<float> m_DistanceWeightCache;

		private NativeQueue<PollutionItem> m_GroundPollutionQueue;

		private NativeQueue<PollutionItem> m_AirPollutionQueue;

		private NativeQueue<PollutionItem> m_NoisePollutionQueue;

		private TypeHandle __TypeHandle;

		private EntityQuery __query_985639355_0;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 262144 / (16 * BuildingPollutionAddSystem.kUpdatesPerDay);
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
			this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
			this.m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
			this.m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
			this.m_GroundPollutionQueue = new NativeQueue<PollutionItem>(Allocator.Persistent);
			this.m_AirPollutionQueue = new NativeQueue<PollutionItem>(Allocator.Persistent);
			this.m_NoisePollutionQueue = new NativeQueue<PollutionItem>(Allocator.Persistent);
			this.m_PolluterQuery = base.GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Placeholder>());
		}

		[Preserve]
		protected override void OnDestroy()
		{
			if (this.m_GroundWeightCache.IsCreated)
			{
				this.m_GroundWeightCache.Dispose();
			}
			if (this.m_AirWeightCache.IsCreated)
			{
				this.m_AirWeightCache.Dispose();
			}
			if (this.m_NoiseWeightCache.IsCreated)
			{
				this.m_NoiseWeightCache.Dispose();
			}
			if (this.m_DistanceWeightCache.IsCreated)
			{
				this.m_DistanceWeightCache.Dispose();
			}
			this.m_GroundPollutionQueue.Dispose();
			this.m_AirPollutionQueue.Dispose();
			this.m_NoisePollutionQueue.Dispose();
			base.OnDestroy();
		}

		[Preserve]
		protected override void OnUpdate()
		{
			PollutionParameterData singleton = this.__query_985639355_0.GetSingleton<PollutionParameterData>();
			float num = math.max(math.max(singleton.m_GroundRadius, singleton.m_AirRadius), singleton.m_NoiseRadius);
			num *= num;
			if (!this.m_GroundWeightCache.IsCreated)
			{
				int num2 = 3 + Mathf.CeilToInt(2f * singleton.m_GroundRadius * (float)GroundPollutionSystem.kTextureSize / (float)CellMapSystem<GroundPollution>.kMapSize);
				this.m_GroundWeightCache = new NativeArray<float>(num2 * num2, Allocator.Persistent);
				num2 = 3 + Mathf.CeilToInt(2f * singleton.m_AirRadius * (float)AirPollutionSystem.kTextureSize / (float)CellMapSystem<AirPollution>.kMapSize);
				this.m_AirWeightCache = new NativeArray<float>(num2 * num2, Allocator.Persistent);
				num2 = 3 + Mathf.CeilToInt(2f * singleton.m_NoiseRadius * (float)NoisePollutionSystem.kTextureSize / (float)CellMapSystem<NoisePollution>.kMapSize);
				this.m_NoiseWeightCache = new NativeArray<float>(num2 * num2, Allocator.Persistent);
				this.m_DistanceWeightCache = new NativeArray<float>(256, Allocator.Persistent);
				for (int i = 0; i < 256; i++)
				{
					this.m_DistanceWeightCache[i] = BuildingPollutionAddSystem.GetWeight(math.sqrt(num * (float)i / 256f), singleton.m_DistanceExponent);
				}
			}
			uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
			this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
			BuildingPolluteJob jobData = default(BuildingPolluteJob);
			jobData.m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
			jobData.m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
			jobData.m_TransformType = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle;
			jobData.m_DestroyedType = this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle;
			jobData.m_AbandonedType = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle;
			jobData.m_ParkType = this.__TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle;
			jobData.m_BuildingEfficiencyType = this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle;
			jobData.m_RenterType = this.__TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle;
			jobData.m_InstalledUpgradeType = this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;
			jobData.m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
			jobData.m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
			jobData.m_SpawnableDatas = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
			jobData.m_PollutionDatas = this.__TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentLookup;
			jobData.m_PollutionModifierDatas = this.__TypeHandle.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup;
			jobData.m_ZoneDatas = this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup;
			jobData.m_Employees = this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup;
			jobData.m_HouseholdCitizens = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup;
			jobData.m_Citizens = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup;
			jobData.m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
			jobData.m_PollutionParameters = singleton;
			jobData.m_GroundPollutionQueue = this.m_GroundPollutionQueue.AsParallelWriter();
			jobData.m_AirPollutionQueue = this.m_AirPollutionQueue.AsParallelWriter();
			jobData.m_NoisePollutionQueue = this.m_NoisePollutionQueue.AsParallelWriter();
			jobData.m_City = this.m_CitySystem.City;
			jobData.m_UpdateFrameIndex = updateFrameWithInterval;
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, this.m_PolluterQuery, base.Dependency);
			ApplyBuildingPollutionJob<GroundPollution> jobData2 = default(ApplyBuildingPollutionJob<GroundPollution>);
			jobData2.m_PollutionMap = this.m_GroundPollutionSystem.GetMap(readOnly: false, out var dependencies);
			jobData2.m_MapSize = CellMapSystem<GroundPollution>.kMapSize;
			jobData2.m_TextureSize = GroundPollutionSystem.kTextureSize;
			jobData2.m_PollutionParameters = singleton;
			jobData2.m_MaxRadiusSq = num;
			jobData2.m_Radius = singleton.m_GroundRadius;
			jobData2.m_PollutionQueue = this.m_GroundPollutionQueue;
			jobData2.m_WeightCache = this.m_GroundWeightCache;
			jobData2.m_DistanceWeightCache = this.m_DistanceWeightCache;
			jobData2.m_Multiplier = singleton.m_GroundMultiplier;
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle, dependencies));
			this.m_GroundPollutionSystem.AddWriter(jobHandle2);
			ApplyBuildingPollutionJob<AirPollution> jobData3 = default(ApplyBuildingPollutionJob<AirPollution>);
			jobData3.m_PollutionMap = this.m_AirPollutionSystem.GetMap(readOnly: false, out var dependencies2);
			jobData3.m_MapSize = CellMapSystem<AirPollution>.kMapSize;
			jobData3.m_TextureSize = AirPollutionSystem.kTextureSize;
			jobData3.m_PollutionParameters = singleton;
			jobData3.m_MaxRadiusSq = num;
			jobData3.m_Radius = singleton.m_AirRadius;
			jobData3.m_PollutionQueue = this.m_AirPollutionQueue;
			jobData3.m_WeightCache = this.m_AirWeightCache;
			jobData3.m_DistanceWeightCache = this.m_DistanceWeightCache;
			jobData3.m_Multiplier = singleton.m_AirMultiplier;
			JobHandle jobHandle3 = IJobExtensions.Schedule(jobData3, JobHandle.CombineDependencies(dependencies2, jobHandle));
			this.m_AirPollutionSystem.AddWriter(jobHandle3);
			ApplyBuildingPollutionJob<NoisePollution> jobData4 = default(ApplyBuildingPollutionJob<NoisePollution>);
			jobData4.m_PollutionMap = this.m_NoisePollutionSystem.GetMap(readOnly: false, out var dependencies3);
			jobData4.m_MapSize = CellMapSystem<NoisePollution>.kMapSize;
			jobData4.m_TextureSize = NoisePollutionSystem.kTextureSize;
			jobData4.m_PollutionParameters = singleton;
			jobData4.m_MaxRadiusSq = num;
			jobData4.m_Radius = singleton.m_NoiseRadius;
			jobData4.m_PollutionQueue = this.m_NoisePollutionQueue;
			jobData4.m_WeightCache = this.m_NoiseWeightCache;
			jobData4.m_DistanceWeightCache = this.m_DistanceWeightCache;
			jobData4.m_Multiplier = singleton.m_NoiseMultiplier;
			JobHandle jobHandle4 = IJobExtensions.Schedule(jobData4, JobHandle.CombineDependencies(dependencies3, jobHandle));
			this.m_NoisePollutionSystem.AddWriter(jobHandle4);
			base.Dependency = JobHandle.CombineDependencies(jobHandle2, jobHandle3, jobHandle4);
		}

		private static float GetWeight(float distance, float exponent)
		{
			return 1f / math.max(20f, math.pow(distance, exponent));
		}

		public static PollutionData GetBuildingPollution(Entity prefab, bool destroyed, bool abandoned, bool isPark, float efficiency, DynamicBuffer<Renter> renters, DynamicBuffer<InstalledUpgrade> installedUpgrades, PollutionParameterData pollutionParameters, DynamicBuffer<CityModifier> cityModifiers, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<SpawnableBuildingData> spawnableDatas, ref ComponentLookup<PollutionData> pollutionDatas, ref ComponentLookup<PollutionModifierData> pollutionModifierDatas, ref ComponentLookup<ZoneData> zoneDatas, ref BufferLookup<Employee> employees, ref BufferLookup<HouseholdCitizen> householdCitizens, ref ComponentLookup<Citizen> citizens)
		{
			PollutionData componentData;
			if (!(destroyed || abandoned))
			{
				if (efficiency > 0f && pollutionDatas.TryGetComponent(prefab, out componentData))
				{
					if (installedUpgrades.IsCreated)
					{
						UpgradeUtils.CombineStats(ref componentData, installedUpgrades, ref prefabRefs, ref pollutionDatas);
					}
					SpawnableBuildingData componentData2;
					if (componentData.m_ScaleWithRenters && !isPark && renters.IsCreated)
					{
						BuildingPollutionAddSystem.CountRenters(out var count, out var education, renters, ref employees, ref householdCitizens, ref citizens, ignoreEmployees: false);
						float num = (spawnableDatas.TryGetComponent(prefab, out componentData2) ? ((float)(int)componentData2.m_Level) : 5f);
						float num2 = ((count > 0) ? (5f * (float)count / (num + 0.5f * (float)(education / count))) : 0f);
						componentData.m_GroundPollution *= num2;
						componentData.m_AirPollution *= num2;
						componentData.m_NoisePollution *= num2;
					}
					if (cityModifiers.IsCreated && spawnableDatas.TryGetComponent(prefab, out componentData2))
					{
						ZoneData zoneData = zoneDatas[componentData2.m_ZonePrefab];
						if (zoneData.m_AreaType == AreaType.Industrial && (zoneData.m_ZoneFlags & ZoneFlags.Office) == 0)
						{
							CityUtils.ApplyModifier(ref componentData.m_GroundPollution, cityModifiers, CityModifierType.IndustrialGroundPollution);
							CityUtils.ApplyModifier(ref componentData.m_AirPollution, cityModifiers, CityModifierType.IndustrialAirPollution);
						}
					}
					if (installedUpgrades.IsCreated)
					{
						PollutionModifierData result = default(PollutionModifierData);
						UpgradeUtils.CombineStats(ref result, installedUpgrades, ref prefabRefs, ref pollutionModifierDatas);
						componentData.m_GroundPollution *= math.max(0f, 1f + result.m_GroundPollutionMultiplier);
						componentData.m_AirPollution *= math.max(0f, 1f + result.m_AirPollutionMultiplier);
						componentData.m_NoisePollution *= math.max(0f, 1f + result.m_NoisePollutionMultiplier);
					}
				}
				else
				{
					componentData = default(PollutionData);
				}
			}
			else
			{
				BuildingData buildingData = buildingDatas[prefab];
				PollutionData pollutionData = default(PollutionData);
				pollutionData.m_GroundPollution = 0f;
				pollutionData.m_AirPollution = 0f;
				pollutionData.m_NoisePollution = 5f * (float)(buildingData.m_LotSize.x * buildingData.m_LotSize.y) * pollutionParameters.m_AbandonedNoisePollutionMultiplier;
				componentData = pollutionData;
			}
			if ((abandoned || isPark) && renters.IsCreated)
			{
				BuildingPollutionAddSystem.CountRenters(out var count2, out var _, renters, ref employees, ref householdCitizens, ref citizens, ignoreEmployees: true);
				componentData.m_NoisePollution += count2 * pollutionParameters.m_HomelessNoisePollution;
			}
			return componentData;
		}

		//改为public;
		public static void CountRenters(out int count, out int education, DynamicBuffer<Renter> renters, ref BufferLookup<Employee> employees, ref BufferLookup<HouseholdCitizen> householdCitizens, ref ComponentLookup<Citizen> citizens, bool ignoreEmployees)
		{
			count = 0;
			education = 0;
			foreach (Renter item in renters)
			{
				if (householdCitizens.TryGetBuffer(item, out var bufferData))
				{
					foreach (HouseholdCitizen item2 in bufferData)
					{
						if (citizens.TryGetComponent(item2, out var componentData))
						{
							education += componentData.GetEducationLevel();
							count++;
						}
					}
				}
				else
				{
					if (ignoreEmployees || !employees.TryGetBuffer(item, out var bufferData2))
					{
						continue;
					}
					foreach (Employee item3 in bufferData2)
					{
						if (citizens.TryGetComponent(item3.m_Worker, out var componentData2))
						{
							education += componentData2.GetEducationLevel();
							count++;
						}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void __AssignQueries(ref SystemState state)
		{
			this.__query_985639355_0 = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<PollutionParameterData>() },
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
		public BuildingPollutionAddSystem()
		{
		}
	}
}
