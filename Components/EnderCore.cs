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
		[Serialize]
		public string networkName = "";

		[MyCmpGet]
		private UserNameable nameable;

		private Storage hubStorage;

		private EnderNetwork network;

		private int nameChangedHandle = -1;

		public int InstanceID => this.GetComponent<KPrefabID>().InstanceID;

		public Storage HubStorage => this.hubStorage;

		public EnderNetwork Network => this.network;

		protected override void OnSpawn()
		{
			base.OnSpawn();
			this.hubStorage = this.GetComponent<Storage>();
			this.network = this.GetComponent<EnderNetwork>();
			this.SyncNetworkNameFromNameable();
			this.nameChangedHandle = this.Subscribe((int)GameHashes.NameChanged, new System.Action<object>(this.OnNameChanged));
			EnderNetworkRegistry.Register(this, this.network);
		}

		protected override void OnCleanUp()
		{
			if (this.nameChangedHandle != -1)
			{
				this.Unsubscribe(this.nameChangedHandle);
				this.nameChangedHandle = -1;
			}
			EnderNetworkRegistry.Unregister(this);
			base.OnCleanUp();
		}

		private void OnNameChanged(object data)
		{
			this.SyncNetworkNameFromNameable();
			EnderNetworkRegistry.NotifyCoresChanged();
		}

		private void SyncNetworkNameFromNameable()
		{
			if (this.nameable != null && !string.IsNullOrEmpty(this.nameable.savedName))
			{
				this.networkName = this.nameable.savedName;
			}
		}

		public string GetDisplayName()
		{
			if (this.nameable != null && !string.IsNullOrEmpty(this.nameable.savedName))
			{
				return this.nameable.savedName;
			}
			if (!string.IsNullOrEmpty(this.networkName))
			{
				return this.networkName;
			}
			return this.gameObject.GetProperName();
		}

		public bool IsOperational()
		{
			Operational operational = this.GetComponent<Operational>();
			return operational == null || operational.IsOperational;
		}
	}
}
