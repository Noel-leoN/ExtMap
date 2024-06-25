using System;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Colossal.UI;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Widgets;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game.UI.Editor;
using Game.UI;

namespace ExtMap57km.Systems
{
	public partial class ResourcePanelSystem : EditorPanelSystemBase
	{
		private static readonly string kTextureImportFolder = "Heightmaps";

		private PrefabSystem m_PrefabSystem;

		private ToolSystem m_ToolSystem;

		private NaturalResourceSystem m_NaturalResourceSystem;

		private GroundWaterSystem m_GroundWaterSystem;

		private EntityQuery m_PrefabQuery;

		private IconButtonGroup m_ToolButtonGroup;

		private EditorSection m_TextureImportButtons;

		private List<PrefabBase> m_ToolPrefabs = new List<PrefabBase>();

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
			this.m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
			this.m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
			this.m_GroundWaterSystem = base.World.GetExistingSystemManaged<GroundWaterSystem>();
			this.m_PrefabQuery = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[3]
				{
					ComponentType.ReadOnly<PrefabData>(),
					ComponentType.ReadOnly<TerraformingData>(),
					ComponentType.ReadOnly<UIObjectData>()
				},
				None = new ComponentType[2]
				{
					ComponentType.ReadOnly<Deleted>(),
					ComponentType.ReadOnly<Temp>()
				}
			});
			this.title = "Editor.RESOURCES";
			IWidget[] array = new IWidget[1];
			IWidget[] obj = new IWidget[2]
			{
				new EditorSection
				{
					displayName = "Editor.RESOURCE_TOOLS",
					expanded = true,
					children = new IWidget[1] { this.m_ToolButtonGroup = new IconButtonGroup() }
				},
				null
			};
			EditorSection obj2 = new EditorSection
			{
				displayName = "Editor.RESOURCE_TEXTURE_LABEL",
				expanded = true
			};
			EditorSection editorSection = obj2;
			this.m_TextureImportButtons = obj2;
			obj[1] = editorSection;
			array[0] = Scrollable.WithChildren(obj);
			this.children = array;
		}

		protected override void OnGameLoaded(Context serializationContext)
		{
			base.OnGameLoaded(serializationContext);
			this.m_ToolPrefabs.Clear();
			List<IconButton> list = new List<IconButton>();
			List<IWidget> list2 = new List<IWidget>();
			using NativeList<UIObjectInfo> nativeList = UIObjectInfo.GetSortedObjects(this.m_PrefabQuery, Allocator.Temp);
			foreach (UIObjectInfo item in nativeList)
			{
				TerraformingPrefab prefab = this.m_PrefabSystem.GetPrefab<TerraformingPrefab>(item.prefabData);
				if (ResourcePanelSystem.IsResourceTerraformingPrefab(prefab))
				{
					this.m_ToolPrefabs.Add(prefab);
					list.Add(new IconButton
					{
						icon = (ImageSystem.GetIcon(prefab) ?? "Media/Editor/Terrain.svg"),
						tooltip = LocalizedString.Id("Assets.NAME[" + prefab.name + "]"),
						action = delegate
						{
							this.m_ToolSystem.ActivatePrefabTool((this.m_ToolSystem.activePrefab != prefab) ? prefab : null);
						},
						selected = () => this.m_ToolSystem.activePrefab == prefab
					});
					list2.Add(new ButtonRow
					{
						children = new Button[2]
						{
							new Button
							{
								displayName = $"Editor.IMPORT_RESOURCE[{prefab.m_Target}]",
								action = delegate
								{
									this.ImportTexture(prefab.m_Target);
								}
							},
							new Button
							{
								displayName = $"Editor.CLEAR_RESOURCE[{prefab.m_Target}]",
								action = delegate
								{
									this.Clear(prefab.m_Target);
								}
							}
						}
					});
				}
			}
			this.m_ToolButtonGroup.children = list.ToArray();
			this.m_TextureImportButtons.children = list2;
		}

		private static bool IsResourceTerraformingPrefab(TerraformingPrefab prefab)
		{
			if (prefab.m_Target != 0 && prefab.m_Target != TerraformingTarget.Material)
			{
				return prefab.m_Target != TerraformingTarget.None;
			}
			return false;
		}

		[Preserve]
		protected override void OnStopRunning()
		{
			base.OnStopRunning();
			if (this.m_ToolPrefabs.Contains(this.m_ToolSystem.activePrefab))
			{
				this.m_ToolSystem.ActivatePrefabTool(null);
			}
		}

		protected override bool OnCancel()
		{
			if (this.m_ToolPrefabs.Contains(this.m_ToolSystem.activePrefab))
			{
				this.m_ToolSystem.ActivatePrefabTool(null);
				return false;
			}
			return base.OnCancel();
		}

		private void ImportTexture(TerraformingTarget target)
		{
			base.activeSubPanel = new LoadAssetPanel("Import " + target, ResourcePanelSystem.GetTextures(), delegate(Colossal.Hash128 guid)
			{
				this.OnLoadTexture(guid, target);
			}, base.CloseSubPanel);
		}

		private static IEnumerable<AssetItem> GetTextures()
		{
			foreach (ImageAsset asset in AssetDatabase.global.GetAssets(SearchFilter<ImageAsset>.ByCondition((ImageAsset a) => a.GetMeta().subPath?.StartsWith(ResourcePanelSystem.kTextureImportFolder) ?? false)))
			{
				yield return new AssetItem
				{
					guid = asset.guid,
					fileName = asset.name,
					displayName = asset.name,
					image = asset.ToUri()
				};
			}
		}

		private void OnLoadTexture(Colossal.Hash128 guid, TerraformingTarget target)
		{
			base.CloseSubPanel();
			if (AssetDatabase.global.TryGetAsset(guid, out ImageAsset assetData))
			{
				using (assetData)
				{
					Texture2D texture = assetData.Load();
					this.ApplyTexture(texture, target);
				}
			}
		}

		private void ApplyTexture(Texture2D texture, TerraformingTarget target)
		{
			switch (target)
			{
			case TerraformingTarget.GroundWater:
				this.ApplyTexture(texture, this.m_GroundWaterSystem, ApplyGroundWater, null);
				break;
			case TerraformingTarget.Ore:
				this.ApplyTexture(texture, this.m_NaturalResourceSystem, ApplyResource, ApplyOre);
				break;
			case TerraformingTarget.Oil:
				this.ApplyTexture(texture, this.m_NaturalResourceSystem, ApplyResource, ApplyOil);
				break;
			case TerraformingTarget.FertileLand:
				this.ApplyTexture(texture, this.m_NaturalResourceSystem, ApplyResource, ApplyFertile);
				break;
			}
		}

		private void ApplyTexture<TCell>(Texture2D texture, CellMapSystem<TCell> cellMapSystem, Action<Texture2D, CellMapData<TCell>, int, int, Func<TCell, ushort, TCell>> applyCallback, Func<TCell, ushort, TCell> resourceCallback) where TCell : struct, ISerializable
		{
			JobHandle dependencies;
			CellMapData<TCell> data = cellMapSystem.GetData(readOnly: false, out dependencies);
			dependencies.Complete();
			for (int i = 0; i < data.m_TextureSize.y; i++)
			{
				for (int j = 0; j < data.m_TextureSize.x; j++)
				{
					applyCallback(texture, data, j, i, resourceCallback);
				}
			}
		}

		private void ApplyResource<TCell>(Texture2D texture, CellMapData<TCell> data, int x, int y, Func<TCell, ushort, TCell> applyCallback) where TCell : struct, ISerializable
		{
			int index = y * data.m_TextureSize.x + x;
			TCell arg = data.m_Buffer[index];
			ushort arg2 = (ushort)this.Sample(texture, data, x, y, 10000);
			data.m_Buffer[index] = applyCallback(arg, arg2);
		}

		private void ApplyGroundWater(Texture2D texture, CellMapData<GroundWater> data, int x, int y, Func<GroundWater, ushort, GroundWater> _)
		{
			int index = y * data.m_TextureSize.x + x;
			short num = (short)this.Sample(texture, data, x, y, 10000);
			data.m_Buffer[index] = new GroundWater
			{
				m_Amount = num,
				m_Max = num
			};
		}

		private NaturalResourceCell ApplyOre(NaturalResourceCell cell, ushort amount)
		{
			cell.m_Ore = new NaturalResourceAmount
			{
				m_Base = amount
			};
			return cell;
		}

		private NaturalResourceCell ApplyOil(NaturalResourceCell cell, ushort amount)
		{
			cell.m_Oil = new NaturalResourceAmount
			{
				m_Base = amount
			};
			return cell;
		}

		private NaturalResourceCell ApplyFertile(NaturalResourceCell cell, ushort amount)
		{
			cell.m_Fertility = new NaturalResourceAmount
			{
				m_Base = amount
			};
			return cell;
		}

		private int Sample<TCell>(Texture2D texture, CellMapData<TCell> data, int x, int y, int max) where TCell : struct, ISerializable
		{
			return Mathf.RoundToInt(math.saturate(texture.GetPixelBilinear((float)x / (float)(data.m_TextureSize.x - 1), (float)y / (float)(data.m_TextureSize.y - 1)).r) * (float)max);
		}

		private void Clear(TerraformingTarget target)
		{
			switch (target)
			{
			case TerraformingTarget.GroundWater:
				this.ClearMap(this.m_GroundWaterSystem, ClearGroundWater);
				break;
			case TerraformingTarget.Ore:
				this.ClearMap(this.m_NaturalResourceSystem, ClearOre);
				break;
			case TerraformingTarget.Oil:
				this.ClearMap(this.m_NaturalResourceSystem, ClearOil);
				break;
			case TerraformingTarget.FertileLand:
				this.ClearMap(this.m_NaturalResourceSystem, ClearFertile);
				break;
			}
		}

		private void ClearMap<TCell>(CellMapSystem<TCell> cellMapSystem, Func<TCell, TCell> clearCallback) where TCell : struct, ISerializable
		{
			JobHandle dependencies;
			CellMapData<TCell> data = cellMapSystem.GetData(readOnly: false, out dependencies);
			dependencies.Complete();
			for (int i = 0; i < data.m_Buffer.Length; i++)
			{
				data.m_Buffer[i] = clearCallback(data.m_Buffer[i]);
			}
		}

		private NaturalResourceCell ClearOre(NaturalResourceCell cell)
		{
			cell.m_Ore = default(NaturalResourceAmount);
			return cell;
		}

		private NaturalResourceCell ClearOil(NaturalResourceCell cell)
		{
			cell.m_Oil = default(NaturalResourceAmount);
			return cell;
		}

		private NaturalResourceCell ClearFertile(NaturalResourceCell cell)
		{
			cell.m_Fertility = default(NaturalResourceAmount);
			return cell;
		}

		private GroundWater ClearGroundWater(GroundWater _)
		{
			return default(GroundWater);
		}

		[Preserve]
		public ResourcePanelSystem()
		{
		}
	}
}
