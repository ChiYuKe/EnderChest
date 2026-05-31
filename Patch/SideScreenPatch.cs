using EnderChest.UI;
using HarmonyLib;
using PeterHan.PLib.UI;

namespace EnderChest.Patch
{
	[HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
	public static class DetailsScreen_OnPrefabInit_Patch
	{
		public static void Postfix()
		{
			PUIUtils.AddSideScreenContent<EnderNetworkBindSideScreen>(null);
		}
	}
}
