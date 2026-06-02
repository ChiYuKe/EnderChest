using System.Collections.Generic;
using System.Linq;
namespace EnderChest.Components
{
	public static class EnderNetworkRegistry
	{
		// 运行中的末影核心缓存；侧屏列表直接从这里取核心对象，避免每次刷新都扫描场景。
		private static readonly List<EnderNetwork> allNetworks = new List<EnderNetwork>();

		// 运行中的末影收集器缓存；用于统计绑定数量，替代开 UI 时 Object.FindObjectsOfType 的全局查找。
		private static readonly List<EnderCollector> allCollectors = new List<EnderCollector>();

		private static readonly Dictionary<string, List<IEnderNetworkMember>> pendingList = new Dictionary<string, List<IEnderNetworkMember>>();

		public static event System.Action CoresChanged;

		public static void Register(EnderNetwork network)
		{
			if (network == null)
			{
				return;
			}
			if (!allNetworks.Contains(network))
			{
				allNetworks.Add(network);
				if (pendingList.TryGetValue(network.networkId, out var list))
				{
					foreach (IEnderNetworkMember member in list)
					{
						Debug.Log($"[EN] Pending process {network.name}");
						member.BindToNetwork(network);
					}
				}
				NotifyCoresChanged();
			}
		}

		public static void Unregister(EnderNetwork network)
		{
			if (network == null)
			{
				return;
			}
			if (allNetworks.Remove(network))
			{
				NotifyCoresChanged();
			}
		}

		public static void NotifyCoresChanged()
		{
			CoresChanged?.Invoke();
		}

		public static void RegisterCollector(EnderCollector collector)
		{
			// 收集器生成时登记到缓存，供 UI 统计每个核心绑定了几个收集器。
			if (collector == null)
			{
				return;
			}
			if (!allCollectors.Contains(collector))
			{
				allCollectors.Add(collector);
				NotifyCoresChanged();
			}
		}

		public static void UnregisterCollector(EnderCollector collector)
		{
			// 收集器销毁时从缓存移除，避免侧屏统计到已经不存在的建筑。
			if (collector == null)
			{
				return;
			}
			if (allCollectors.Remove(collector))
			{
				NotifyCoresChanged();
			}
		}

		public static List<EnderNetwork> GetAllNetworks() {
			// 返回副本，避免 UI 刷新时直接改到注册表内部列表。
			var result = new List<EnderNetwork>(EnderNetworkRegistry.allNetworks);
			return result;
		}

		public static List<EnderNetwork> GetCoresForWorld(int worldId)
		{
			// 左侧面板只显示当前收集器所在世界的核心，跨世界核心先过滤掉。
			var result = new List<EnderNetwork>();
			for (int i = 0; i < allNetworks.Count; i++)
			{
				EnderNetwork network = allNetworks[i];
				if (network != null && network.gameObject != null && network.GetMyWorldId() == worldId)
				{
					result.Add(network);
				}
			}
			return result;
		}

		public static EnderNetwork Resolve(string networkId) {
			// 收集器保存的是核心 InstanceID，实际使用前需要从运行中核心缓存反查对象。
			return EnderNetworkRegistry.allNetworks.Find(w => w.networkId == networkId);
		}

		public static void Pending(string networkId, IEnderNetworkMember member) {
			if (!pendingList.TryGetValue(networkId, out var list))
			{
				list = new List<IEnderNetworkMember>();
				pendingList.Add(networkId, list);
			}
			list.Add(member);
		}

		public static int CountCollectorsBoundTo(string networkId)
		{
			if (networkId == "")
			{
				return 0;
			}
			int count = 0;
			// UI 刷新会频繁查询绑定数量，这里使用注册表缓存，避免打开侧屏时全局扫描所有建筑。
			for (int i = allCollectors.Count - 1; i >= 0; i--)
			{
				EnderCollector collector = allCollectors[i];
				if (collector == null || collector.gameObject == null)
				{
					allCollectors.RemoveAt(i);
					continue;
				}
				if (collector.BoundNetworkId == networkId)
				{
					count++;
				}
			}
			return count;
		}
	}
}
