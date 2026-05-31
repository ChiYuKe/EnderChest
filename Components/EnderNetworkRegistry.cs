using System.Collections.Generic;
using UnityEngine;

namespace EnderChest.Components
{
	public class EnderNetworkRegistry
	{
		private static readonly Dictionary<int, EnderNetwork> networksByCoreId = new Dictionary<int, EnderNetwork>();

		private static readonly List<EnderCore> allCores = new List<EnderCore>();

		public static event System.Action CoresChanged;

		public static void Register(EnderCore core, EnderNetwork network)
		{
			if (core == null || network == null)
			{
				return;
			}
			networksByCoreId[core.InstanceID] = network;
			if (!allCores.Contains(core))
			{
				allCores.Add(core);
				NotifyCoresChanged();
			}
		}

		public static void Unregister(EnderCore core)
		{
			if (core == null)
			{
				return;
			}
			networksByCoreId.Remove(core.InstanceID);
			if (allCores.Remove(core))
			{
				NotifyCoresChanged();
			}
		}

		public static void NotifyCoresChanged()
		{
			CoresChanged?.Invoke();
		}

		public static EnderNetwork Resolve(int coreInstanceId)
		{
			EnderNetwork network;
			if (coreInstanceId != 0 && networksByCoreId.TryGetValue(coreInstanceId, out network))
			{
				return network;
			}
			return null;
		}

		public static List<EnderCore> GetAllCores()
		{
			var result = new List<EnderCore>();
			for (int i = 0; i < allCores.Count; i++)
			{
				EnderCore core = allCores[i];
				if (core != null && core.gameObject != null)
				{
					result.Add(core);
				}
			}
			result.Sort(CompareCoresForDisplay);
			return result;
		}

		public static List<EnderCore> GetCoresForWorld(int worldId)
		{
			var result = new List<EnderCore>();
			for (int i = 0; i < allCores.Count; i++)
			{
				EnderCore core = allCores[i];
				if (core != null && core.gameObject != null && core.GetMyWorldId() == worldId)
				{
					result.Add(core);
				}
			}
			result.Sort(CompareCoresForDisplay);
			return result;
		}

		public static int CountCollectorsBoundTo(int coreInstanceId)
		{
			if (coreInstanceId == 0)
			{
				return 0;
			}
			int count = 0;
			EnderCollector[] collectors = Object.FindObjectsOfType<EnderCollector>();
			for (int i = 0; i < collectors.Length; i++)
			{
				EnderCollector collector = collectors[i];
				if (collector != null && collector.gameObject != null
					&& collector.BoundCoreInstanceId == coreInstanceId)
				{
					count++;
				}
			}
			return count;
		}

		private static int CompareCoresForDisplay(EnderCore a, EnderCore b)
		{
			int worldCompare = a.GetMyWorldId().CompareTo(b.GetMyWorldId());
			if (worldCompare != 0)
			{
				return worldCompare;
			}
			return string.Compare(a.GetDisplayName(), b.GetDisplayName(), System.StringComparison.OrdinalIgnoreCase);
		}
	}
}
