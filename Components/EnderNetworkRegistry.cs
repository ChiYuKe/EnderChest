using System.Collections.Generic;
using UnityEngine;

namespace EnderChest.Components
{
	public static class EnderNetworkRegistry
	{
		private static readonly List<EnderNetwork> allNetworks = new List<EnderNetwork>();

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

		public static List<EnderNetwork> GetAllNetworks() {
			var result = new List<EnderNetwork>(EnderNetworkRegistry.allNetworks);
			return result;
		}
		public static List<EnderNetwork> GetCoresForWorld(int worldId)
		{
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

		public static EnderNetwork Resolve(int networkInstanceId) {
			return EnderNetworkRegistry.allNetworks.Find(w => w.InstanceID == networkInstanceId);
		}
		public static int CountCollectorsBoundTo(int networkInstanceId)
		{
			if (networkInstanceId == 0)
			{
				return 0;
			}
			int count = 0;
			EnderCollector[] collectors = Object.FindObjectsOfType<EnderCollector>();
			for (int i = 0; i < collectors.Length; i++)
			{
				EnderCollector collector = collectors[i];
				if (collector != null && collector.gameObject != null
					&& collector.BoundNetworkInstanceId == networkInstanceId)
				{
					count++;
				}
			}
			return count;
		}
	}
}
