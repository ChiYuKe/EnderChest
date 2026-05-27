using System.Collections.Generic;
using EnderChest.Components;
using STRINGS;
using TUNING;
using UnityEngine;

namespace EnderChest.Buildings
{
	public class EnderCollectorConfig : IBuildingConfig
	{

		public override BuildingDef CreateBuildingDef()
		{
			string id = "EnderCollector";
			BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
				id, 1, 2, "storagelocker_kanim", 30, 10f,
				TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
				MATERIALS.RAW_MINERALS_OR_METALS, 1600f,
				BuildLocationRule.Anywhere,
				TUNING.BUILDINGS.DECOR.PENALTY.TIER1,
				NOISE_POLLUTION.NONE, 0.2f);
			buildingDef.Floodable = false;
			buildingDef.AudioCategory = "Metal";
			buildingDef.Overheatable = false;
			buildingDef.AddSearchTerms(SEARCH_TERMS.STORAGE);
			buildingDef.ViewMode = OverlayModes.SolidConveyor.ID;
			buildingDef.InputConduitType = ConduitType.Solid;
			buildingDef.UtilityInputOffset = new CellOffset(0, 0);
			buildingDef.PermittedRotations = PermittedRotations.R360;
			GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SolidConveyorIDs, id);
			return buildingDef;
		}

		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
			GeneratedBuildings.MakeBuildingAlwaysOperational(go);
			SoundEventVolumeCache.instance.AddVolume("storagelocker_kanim", "StorageLocker_Hit_metallic_low", NOISE_POLLUTION.NOISY.TIER1);
			Prioritizable.AddRef(go);
			Storage storage = go.AddOrGet<Storage>();
			storage.showInUI = true;
			storage.allowItemRemoval = true;
			storage.showDescriptor = true;
			storage.storageFilters = new List<Tag>(STORAGEFILTERS.SOLID_TRANSFER_ARM_CONVEYABLE);
			storage.storageFullMargin = STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
			storage.fetchCategory = Storage.FetchCategory.GeneralStorage;
			storage.showCapacityStatusItem = true;
			storage.showCapacityAsMainStatus = true;
			storage.capacityKg = 20000f;
			go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.StorageLocker;
			SolidConduitConsumer conduitConsumer = go.AddOrGet<SolidConduitConsumer>();
			conduitConsumer.capacityTag = GameTags.Any;
			conduitConsumer.capacityKG = float.PositiveInfinity;
			conduitConsumer.alwaysConsume = false;
			go.AddOrGet<SimpleVent>();
			go.AddOrGet<EnderCollector>();
			go.AddOrGet<TreeFilterable>();
		}

		public override void DoPostConfigureUnderConstruction(GameObject go)
		{
			base.DoPostConfigureUnderConstruction(go);
			go.GetComponent<Constructable>().requiredSkillPerk = Db.Get().SkillPerks.ConveyorBuild.Id;
		}

		public override void DoPostConfigureComplete(GameObject go)
		{
			go.AddOrGet<Automatable>();
		}
	}
}
