using EnderChest.Components;
using System.Collections.Generic;
using STRINGS;
using TUNING;
using UnityEngine;

namespace EnderChest.Buildings
{
	public class EnderCoreConfig : IBuildingConfig
	{
		public override BuildingDef CreateBuildingDef()
		{
			string id = "EnderCore";
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

			return buildingDef;
		}

		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
			SoundEventVolumeCache.instance.AddVolume("storagelocker_kanim", "StorageLocker_Hit_metallic_low", NOISE_POLLUTION.NOISY.TIER1);
			Prioritizable.AddRef(go);
			Storage storage = go.AddOrGet<Storage>();
			storage.showInUI = true;
			storage.allowItemRemoval = true;
			storage.showDescriptor = true;
			storage.storageFilters = new List<Tag> (STORAGEFILTERS.SOLID_TRANSFER_ARM_CONVEYABLE);
			storage.storageFullMargin = STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
			storage.fetchCategory = Storage.FetchCategory.GeneralStorage;
			storage.showCapacityStatusItem = true;
			storage.showCapacityAsMainStatus = true;
			storage.capacityKg = float.PositiveInfinity;
			storage.SetDefaultStoredItemModifiers(new List<Storage.StoredItemModifier>
		{
			Storage.StoredItemModifier.Seal,
			Storage.StoredItemModifier.Insulate,
			Storage.StoredItemModifier.Preserve,
			Storage.StoredItemModifier.Hide
		});
			go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.StorageLocker;
			go.AddOrGet<EnderCore>();
			go.AddOrGet<UserNameable>();
		}

		public override void DoPostConfigureComplete(GameObject go)
		{
		}
	}
}
