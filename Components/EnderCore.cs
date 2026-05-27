using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSerialization;

namespace EnderChest.Components
{
	[SerializationConfig(MemberSerialization.OptIn)]
	public class EnderCore : KMonoBehaviour
	{
		public int InstanceID;

		public List<EnderCollector> collectorList;
	}
}
