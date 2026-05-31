using EnderChest.Components;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EnderChest.UI
{
	public class EnderNetworkBindSideScreen : SideScreenContent
	{

		public IEnderNetworkMember target;
		private PButton button;

		protected override void OnPrefabInit() {
			button = new PButton("Bind to Network")
			{
				Text = "绑定到网络"
			};
			button.SetKleiPinkStyle();
			button.AddTo(base.gameObject, -2);
			this.ContentContainer = base.gameObject;
			base.OnPrefabInit();
		}

		public override string GetTitle()
		{
			return "testTitle";
		}

		public override bool IsValidForTarget(GameObject target)
		{
			return (target != null) && (target.GetComponent<IEnderNetworkMember>() != null);
		}

	}
}
