using UnityEngine;

namespace EnderChest.Components
{
	// 挂上这个组件的建筑，才会显示末影网络的独立左侧面板。
	public class EnderChestLeftPanelTarget : KMonoBehaviour
	{
		// 当前面板只服务末影收集器，后续要支持别的建筑时可以在这里扩展目标数据。
		[MyCmpGet]
		private EnderCollector collector;

		public EnderCollector Collector => collector;
	}
}
