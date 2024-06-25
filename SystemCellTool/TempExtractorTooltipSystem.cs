using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game.UI.Tooltip;
using Game.UI;

namespace ExtMap57km.Systems
{
	//[CompilerGenerated]
	public partial class TempExtractorTooltipSystem : TooltipSystemBase
	{
		private struct TreeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public Circle2 m_Circle;

			public ComponentLookup<Overridden> m_OverriddenData;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<TreeData> m_PrefabTreeData;

			public float m_Result;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, this.m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (this.m_OverriddenData.HasComponent(entity))
				{
					return;
				}
				Transform transform = this.m_TransformData[entity];
				if (!MathUtils.Intersect(this.m_Circle, transform.m_Position.xz))
				{
					return;
				}
				PrefabRef prefabRef = this.m_PrefabRefData[entity];
				if (this.m_PrefabTreeData.HasComponent(prefabRef.m_Prefab))
				{
					TreeData treeData = this.m_PrefabTreeData[prefabRef.m_Prefab];
					if (treeData.m_WoodAmount >= 1f)
					{
						this.m_Result += treeData.m_WoodAmount;
					}
				}
			}
		}

		private struct TypeHandle
		{
			[ReadOnly]
			public ComponentLookup<Overridden> __Game_Common_Overridden_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PlaceholderBuildingData> __Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<LotData> __Game_Prefabs_LotData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Common_Overridden_RO_ComponentLookup = state.GetComponentLookup<Overridden>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
				this.__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
				this.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderBuildingData>(isReadOnly: true);
				this.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
				this.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = state.GetComponentLookup<ExtractorAreaData>(isReadOnly: true);
				this.__Game_Prefabs_LotData_RO_ComponentLookup = state.GetComponentLookup<LotData>(isReadOnly: true);
				this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
				this.__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
				this.__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			}
		}

		private NaturalResourceSystem m_NaturalResourceSystem;

		private ResourceSystem m_ResourceSystem;

		private CitySystem m_CitySystem;

		private IndustrialDemandSystem m_IndustrialDemandSystem;

		private CountCompanyDataSystem m_CountCompanyDataSystem;

		private ClimateSystem m_ClimateSystem;

		private PrefabSystem m_PrefabSystem;

		private Game.Objects.SearchSystem m_SearchSystem;

		private EntityQuery m_ErrorQuery;

		private EntityQuery m_TempQuery;

		private EntityQuery m_ExtractorParameterQuery;

		private StringTooltip m_ResourceAvailable;

		private StringTooltip m_ResourceUnavailable;

		private IntTooltip m_Surplus;

		private IntTooltip m_Deficit;

		private StringTooltip m_ClimateAvailable;

		private StringTooltip m_ClimateUnavailable;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
			this.m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
			this.m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
			this.m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
			this.m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
			this.m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
			this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
			this.m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
			this.m_ErrorQuery = base.GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Error>());
			this.m_TempQuery = base.GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Placeholder>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
			this.m_ExtractorParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<ExtractorParameterData>());
			this.m_ResourceAvailable = new StringTooltip
			{
				path = "extractorMapFeatureAvailable"
			};
			this.m_ResourceUnavailable = new StringTooltip
			{
				path = "extractorMapFeatureUnavailable",
				color = TooltipColor.Warning
			};
			this.m_ClimateAvailable = new StringTooltip
			{
				path = "extractorClimateAvailable",
				value = LocalizedString.Id("Tools.EXTRACTOR_CLIMATE_REQUIRED_AVAILABLE")
			};
			this.m_ClimateUnavailable = new StringTooltip
			{
				path = "extractorClimateUnavailable",
				value = LocalizedString.Id("Tools.EXTRACTOR_CLIMATE_REQUIRED_UNAVAILABLE"),
				color = TooltipColor.Warning
			};
			this.m_Surplus = new IntTooltip
			{
				path = "extractorCityProductionSurplus",
				label = LocalizedString.Id("Tools.EXTRACTOR_PRODUCTION_SURPLUS"),
				unit = "weightPerMonth"
			};
			this.m_Deficit = new IntTooltip
			{
				path = "extractorCityProductionDeficit",
				label = LocalizedString.Id("Tools.EXTRACTOR_PRODUCTION_DEFICIT"),
				unit = "weightPerMonth"
			};
			base.RequireForUpdate(this.m_TempQuery);
		}

		private bool FindWoodResource(Circle2 circle)
		{
			this.__TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Common_Overridden_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			TreeIterator treeIterator = default(TreeIterator);
			treeIterator.m_OverriddenData = this.__TypeHandle.__Game_Common_Overridden_RO_ComponentLookup;
			treeIterator.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
			treeIterator.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
			treeIterator.m_PrefabTreeData = this.__TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup;
			treeIterator.m_Bounds = new Bounds2(circle.position - circle.radius, circle.position + circle.radius);
			treeIterator.m_Circle = circle;
			treeIterator.m_Result = 0f;
			TreeIterator iterator = treeIterator;
			JobHandle dependencies;
			NativeQuadTree<Entity, QuadTreeBoundsXZ> staticSearchTree = this.m_SearchSystem.GetStaticSearchTree(readOnly: true, out dependencies);
			dependencies.Complete();
			staticSearchTree.Iterate(ref iterator);
			return iterator.m_Result > 0f;
		}

		private bool FindResource(Circle2 circle, MapFeature requiredFeature, CellMapData<NaturalResourceCell> resourceMap, DynamicBuffer<CityModifier> cityModifiers)
		{
			int2 cell = CellMapSystem<NaturalResourceCell>.GetCell(new float3(circle.position.x - circle.radius, 0f, circle.position.y - circle.radius), CellMapSystem<NaturalResourceCell>.kMapSize, resourceMap.m_TextureSize.x);
			int2 cell2 = CellMapSystem<NaturalResourceCell>.GetCell(new float3(circle.position.x + circle.radius, 0f, circle.position.y + circle.radius), CellMapSystem<NaturalResourceCell>.kMapSize, resourceMap.m_TextureSize.x);
			cell = math.max(new int2(0, 0), cell);
			cell2 = math.min(new int2(resourceMap.m_TextureSize.x - 1, resourceMap.m_TextureSize.y - 1), cell2);
			int2 cell3 = default(int2);
			cell3.x = cell.x;
			while (cell3.x <= cell2.x)
			{
				cell3.y = cell.y;
				while (cell3.y <= cell2.y)
				{
					if (MathUtils.Intersect(circle, CellMapSystem<NaturalResourceCell>.GetCellCenter(cell3, resourceMap.m_TextureSize.x).xz))
					{
						NaturalResourceCell naturalResourceCell = resourceMap.m_Buffer[cell3.x + cell3.y * resourceMap.m_TextureSize.x];
						float num = 0f;
						switch (requiredFeature)
						{
						case MapFeature.FertileLand:
							num = (int)naturalResourceCell.m_Fertility.m_Base;
							num -= (float)(int)naturalResourceCell.m_Fertility.m_Used;
							break;
						case MapFeature.Ore:
							num = (int)naturalResourceCell.m_Ore.m_Base;
							if (cityModifiers.IsCreated)
							{
								CityUtils.ApplyModifier(ref num, cityModifiers, CityModifierType.OreResourceAmount);
							}
							num -= (float)(int)naturalResourceCell.m_Ore.m_Used;
							break;
						case MapFeature.Oil:
							num = (int)naturalResourceCell.m_Oil.m_Base;
							if (cityModifiers.IsCreated)
							{
								CityUtils.ApplyModifier(ref num, cityModifiers, CityModifierType.OilResourceAmount);
							}
							num -= (float)(int)naturalResourceCell.m_Oil.m_Used;
							break;
						default:
							num = 0f;
							break;
						}
						if (num > 0f)
						{
							return true;
						}
					}
					cell3.y++;
				}
				cell3.x++;
			}
			return false;
		}

		[Preserve]
		protected override void OnUpdate()
		{
			if (!this.m_ErrorQuery.IsEmptyIgnoreFilter || !base.EntityManager.TryGetBuffer(this.m_CitySystem.City, isReadOnly: true, out DynamicBuffer<CityModifier> buffer))
			{
				return;
			}
			base.CompleteDependency();
			JobHandle dependencies;
			CellMapData<NaturalResourceCell> data = this.m_NaturalResourceSystem.GetData(readOnly: true, out dependencies);
			dependencies.Complete();
			this.__TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			ComponentLookup<PlaceholderBuildingData> _Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup;
			this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			ComponentLookup<BuildingPropertyData> _Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
			this.__TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			ComponentLookup<ExtractorAreaData> _Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;
			this.__TypeHandle.__Game_Prefabs_LotData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			ComponentLookup<LotData> _Game_Prefabs_LotData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_LotData_RO_ComponentLookup;
			this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			ComponentLookup<ResourceData> _Game_Prefabs_ResourceData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
			this.__TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup.Update(ref base.CheckedStateRef);
			BufferLookup<Game.Prefabs.SubArea> _Game_Prefabs_SubArea_RO_BufferLookup = this.__TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup;
			ResourcePrefabs prefabs = this.m_ResourceSystem.GetPrefabs();
			NativeArray<ArchetypeChunk> nativeArray = this.m_TempQuery.ToArchetypeChunkArray(Allocator.TempJob);
			MapFeature mapFeature = MapFeature.None;
			bool flag = false;
			bool flag2 = false;
			Resource resource = Resource.NoResource;
			try
			{
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<PrefabRef> typeHandle = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
				this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<Transform> typeHandle2 = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle;
				this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				ComponentTypeHandle<Temp> typeHandle3 = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle;
				dependencies.Complete();
				foreach (ArchetypeChunk item in nativeArray)
				{
					NativeArray<Temp> nativeArray2 = item.GetNativeArray(ref typeHandle3);
					NativeArray<PrefabRef> nativeArray3 = item.GetNativeArray(ref typeHandle);
					NativeArray<Transform> nativeArray4 = item.GetNativeArray(ref typeHandle2);
					for (int i = 0; i < nativeArray3.Length; i++)
					{
						if ((nativeArray2[i].m_Flags & (TempFlags.Create | TempFlags.Upgrade)) == 0)
						{
							continue;
						}
						Entity prefab = nativeArray3[i].m_Prefab;
						if (!_Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup.HasComponent(prefab) || !_Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.HasComponent(prefab) || _Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup[prefab].m_Type != BuildingType.ExtractorBuilding)
						{
							continue;
						}
						resource = _Game_Prefabs_BuildingPropertyData_RO_ComponentLookup[prefab].m_AllowedManufactured;
						DynamicBuffer<Game.Prefabs.SubArea> dynamicBuffer = _Game_Prefabs_SubArea_RO_BufferLookup[prefab];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							Entity prefab2 = dynamicBuffer[j].m_Prefab;
							if (_Game_Prefabs_ExtractorAreaData_RO_ComponentLookup.HasComponent(prefab2) && _Game_Prefabs_LotData_RO_ComponentLookup.HasComponent(prefab2))
							{
								flag2 = true;
								float maxRadius = _Game_Prefabs_LotData_RO_ComponentLookup[prefab2].m_MaxRadius;
								mapFeature = ExtractorCompanySystem.GetRequiredMapFeature(resource, prefab2, prefabs, _Game_Prefabs_ResourceData_RO_ComponentLookup, _Game_Prefabs_ExtractorAreaData_RO_ComponentLookup);
								if (mapFeature != MapFeature.None)
								{
									Circle2 circle = new Circle2(maxRadius, nativeArray4[i].m_Position.xz);
									flag = ((mapFeature != MapFeature.Forest) ? this.FindResource(circle, mapFeature, data, buffer) : this.FindWoodResource(circle));
								}
							}
						}
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
			}
			if (!flag2)
			{
				return;
			}
			JobHandle deps;
			NativeArray<int> production = this.m_CountCompanyDataSystem.GetProduction(out deps);
			JobHandle deps2;
			NativeArray<int> consumption = this.m_IndustrialDemandSystem.GetConsumption(out deps2);
			int resourceIndex = EconomyUtils.GetResourceIndex(resource);
			deps.Complete();
			deps2.Complete();
			int num = production[resourceIndex] - consumption[resourceIndex];
			Entity entity = prefabs[resource];
			ResourceData resourceData = _Game_Prefabs_ResourceData_RO_ComponentLookup[entity];
			string icon = ImageSystem.GetIcon(this.m_PrefabSystem.GetPrefab<PrefabBase>(entity));
			if (num > 0)
			{
				this.m_Surplus.value = num;
				this.m_Surplus.icon = icon;
				base.AddMouseTooltip(this.m_Surplus);
			}
			else
			{
				this.m_Deficit.value = -num;
				this.m_Deficit.icon = icon;
				base.AddMouseTooltip(this.m_Deficit);
			}
			if (mapFeature != MapFeature.None)
			{
				string mapFeatureIconName = AreaTools.GetMapFeatureIconName(mapFeature);
				if (flag)
				{
					this.m_ResourceAvailable.icon = "Media/Game/Icons/" + mapFeatureIconName + ".svg";
					this.m_ResourceAvailable.value = LocalizedString.Id("Tools.EXTRACTOR_MAP_FEATURE_REQUIRED_AVAILABLE");
					base.AddMouseTooltip(this.m_ResourceAvailable);
				}
				else
				{
					this.m_ResourceUnavailable.icon = "Media/Game/Icons/" + mapFeatureIconName + ".svg";
					this.m_ResourceUnavailable.value = LocalizedString.Id("Tools.EXTRACTOR_MAP_FEATURE_REQUIRED_MISSING");
					base.AddMouseTooltip(this.m_ResourceUnavailable);
				}
			}
			if (resourceData.m_RequireTemperature)
			{
				if (this.m_ClimateSystem.averageTemperature >= resourceData.m_RequiredTemperature)
				{
					base.AddMouseTooltip(this.m_ClimateAvailable);
				}
				else
				{
					base.AddMouseTooltip(this.m_ClimateUnavailable);
				}
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
		public TempExtractorTooltipSystem()
		{
		}
	}
}
