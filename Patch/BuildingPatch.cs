using HarmonyLib;

namespace EnderChest.Patch
{
	public static class BuildingPatch
	{
		[HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
		public static class LoadGeneratedBuildingsPatch
		{
			public static void Prefix()
			{
				ModUtil.AddBuildingToPlanScreen("Base", "EnderCollector");
				ModUtil.AddBuildingToPlanScreen("Base", "EnderCore");
			}
		}
	}
}
