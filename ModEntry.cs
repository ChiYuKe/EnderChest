using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using EnderChest.Core;
using KMod;

namespace EnderChest
{
	internal class ModEntry: UserMod2
	{
		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
			// 注册本模组的本地化字符串，避免 UI 和建筑描述显示 MISSING.STRINGS。
			myLocalization.SetModPath(base.mod.ContentPath);
			myLocalization.Translate(typeof(STRINGS));
			//mySpriteLoader.SetModPath(base.mod.ContentPath);
		}
	}
}
