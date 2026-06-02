namespace EnderChest
{
	internal class STRINGS
	{
		public static string Get(LocString value)
		{
			return value.ToString();
		}

		// 末影网络侧屏及独立左侧面板显示的文本。
		public class SIDESCREEN
		{
			public class ENDERNETWORKBIND
			{
				public static LocString TITLE = "末影网络";
				public static LocString CURRENT_PREFIX = "当前：";
				public static LocString EMPTY = "空";
				public static LocString INVALID_CORE = "无效核心";
				public static LocString REFRESH = "刷新";
				public static LocString UNBIND = "解绑";
				public static LocString NO_CORES = "没有可用核心";
				public static LocString CORE_LIST_TITLE = "末影核心列表";
				public static LocString SELECTED_MARKER = " → ";
				public static LocString CORE_DETAIL = "({0}) 已绑定 {1}";
			}
		}

		// 两个自定义建筑的名称、说明和效果文本。
		public class BUILDINGS
		{
			public class PREFABS
			{
				public class ENDERCORE
				{
					public static LocString NAME = "末影核心";
					public static LocString DESC = "储存末影网络中的物品，并作为收集器绑定的网络核心。";
					public static LocString EFFECT = "作为末影物流网络的核心仓库，可被末影收集器绑定。";
				}

				public class ENDERCOLLECTOR
				{
					public static LocString NAME = "末影收集器";
					public static LocString DESC = "接收固体运输轨道中的物品，并将其转移到绑定的末影核心。";
					public static LocString EFFECT = "可绑定一个末影核心，把输入的固体物品送入对应的末影网络。";
				}
			}
		}
	}
}
