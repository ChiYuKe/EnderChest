using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnderChest.Core;
using HarmonyLib;
using KMod;

namespace EnderChest
{
	internal class ModEntry: UserMod2
	{
		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
			myLocalization.SetModPath(base.mod.ContentPath);
			mySpriteLoader.SetModPath(base.mod.ContentPath);
		}
	}
}
