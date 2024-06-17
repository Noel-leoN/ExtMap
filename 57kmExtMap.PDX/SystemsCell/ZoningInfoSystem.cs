using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.UI;

namespace Game.Tools
{
	[CompilerGenerated]
	public partial class ZoningInfoSystem : GameSystemBase, IZoningInfoSystem
	{
		private struct TypeHandle
		{
			[ReadOnly]
			public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

			[ReadOnly]
			public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
				this.__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
				this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			}
		}

		private EntityQuery m_ZoningPreferenceGroup;

		private EntityQuery m_ProcessQuery;

		private NativeList<ZoneEvaluationUtils.ZoningEvaluationResult> m_EvaluationResults;

		private ZoneToolSystem m_ZoneToolSystem;

		private IndustrialDemandSystem m_IndustrialDemandSystem;

		private GroundPollutionSystem m_GroundPollutionSystem;

		private AirPollutionSystem m_AirPollutionSystem;

		private NoisePollutionSystem m_NoisePollutionSystem;

		private PrefabSystem m_PrefabSystem;

		private ResourceSystem m_ResourceSystem;

		private ToolRaycastSystem m_ToolRaycastSystem;

		private TypeHandle __TypeHandle;

		public NativeList<ZoneEvaluationUtils.ZoningEvaluationResult> evaluationResults => this.m_EvaluationResults;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_ZoneToolSystem = base.World.GetOrCreateSystemManaged<ZoneToolSystem>();
			this.m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
			this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
			this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
			this.m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
			this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
			this.m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
			this.m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
			this.m_ProcessQuery = base.GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>());
			this.m_ZoningPreferenceGroup = base.GetEntityQuery(ComponentType.ReadOnly<ZonePreferenceData>());
			this.m_EvaluationResults = new NativeList<ZoneEvaluationUtils.ZoningEvaluationResult>(Allocator.Persistent);
			base.RequireForUpdate(this.m_ProcessQuery);
		}

		[Preserve]
		protected override void OnDestroy()
		{
			this.m_EvaluationResults.Dispose();
			base.OnDestroy();
		}

		[Preserve]
		protected override void OnUpdate()
		{
			this.m_EvaluationResults.Clear();
			if (this.m_ToolRaycastSystem.GetRaycastResult(out var result) && base.EntityManager.TryGetComponent<Block>(result.m_Owner, out var component) && base.EntityManager.TryGetComponent<Owner>(result.m_Owner, out var component2))
			{
				base.Dependency.Complete();
				this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup.Update(ref base.CheckedStateRef);
				BufferLookup<ResourceAvailability> _Game_Net_ResourceAvailability_RO_BufferLookup = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup;
				this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<LandValue> _Game_Net_LandValue_RO_ComponentLookup = this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup;
				NativeArray<ZonePreferenceData> nativeArray = this.m_ZoningPreferenceGroup.ToComponentDataArray<ZonePreferenceData>(Allocator.TempJob);
				JobHandle deps;
				NativeArray<int> industrialResourceZoningDemands = this.m_IndustrialDemandSystem.GetIndustrialResourceZoningDemands(out deps);
				ResourcePrefabs prefabs = this.m_ResourceSystem.GetPrefabs();
				this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ComponentLookup<ResourceData> _Game_Prefabs_ResourceData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
				ZonePreferenceData preferences = nativeArray[0];
				Entity owner = component2.m_Owner;
				AreaType areaType = this.m_ZoneToolSystem.prefab.m_AreaType;
				JobHandle dependencies;
				NativeArray<GroundPollution> map = this.m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies);
				JobHandle dependencies2;
				NativeArray<AirPollution> map2 = this.m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2);
				JobHandle dependencies3;
				NativeArray<NoisePollution> map3 = this.m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3);
				deps.Complete();
				dependencies.Complete();
				dependencies2.Complete();
				dependencies3.Complete();
				GroundPollution pollution = GroundPollutionSystem.GetPollution(component.m_Position, map);
				float2 pollution2 = new float2(pollution.m_Pollution, pollution.m_Pollution - pollution.m_Previous);
				pollution2.x += AirPollutionSystem.GetPollution(component.m_Position, map2).m_Pollution;
				pollution2.x += NoisePollutionSystem.GetPollution(component.m_Position, map3).m_Pollution;
				float num = _Game_Net_LandValue_RO_ComponentLookup[owner].m_LandValue;
				Entity entity = this.m_PrefabSystem.GetEntity(this.m_ZoneToolSystem.prefab);
				DynamicBuffer<ProcessEstimate> buffer = base.World.EntityManager.GetBuffer<ProcessEstimate>(entity, isReadOnly: true);
				if (base.World.EntityManager.HasComponent<ZonePropertiesData>(entity))
				{
					ZonePropertiesData componentData = base.World.EntityManager.GetComponentData<ZonePropertiesData>(entity);
					float num2 = ((areaType != AreaType.Residential) ? componentData.m_SpaceMultiplier : (componentData.m_ScaleResidentials ? componentData.m_ResidentialProperties : (componentData.m_ResidentialProperties / 8f)));
					num /= num2;
				}
				JobHandle outJobHandle;
				NativeList<IndustrialProcessData> processes = this.m_ProcessQuery.ToComponentDataListAsync<IndustrialProcessData>(Allocator.TempJob, out outJobHandle);
				outJobHandle.Complete();
				ZoneEvaluationUtils.GetFactors(areaType, this.m_ZoneToolSystem.prefab.m_Office, _Game_Net_ResourceAvailability_RO_BufferLookup[owner], result.m_Hit.m_CurvePosition, ref preferences, this.m_EvaluationResults, industrialResourceZoningDemands, pollution2, num, buffer, processes, prefabs, _Game_Prefabs_ResourceData_RO_ComponentLookup);
				processes.Dispose();
				nativeArray.Dispose();
				this.m_EvaluationResults.Sort();
			}
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
		public ZoningInfoSystem()
		{
		}
	}
}
