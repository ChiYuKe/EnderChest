using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSerialization;
using UnityEngine;

namespace EnderChest.Components
{
	public enum EnderNetworkMemberRole
	{
		Collector,
		Consumer
	}
	public interface IEnderNetworkMember
	{
		EnderNetworkMemberRole Role { get; }

		Storage LocalStorage { get; }

		[Serialize]
		string BoundNetworkId { get; }

		void BindToNetwork(EnderNetwork network);

		bool IsOperational();
	}

	[SerializationConfig(MemberSerialization.OptIn)]
	public class EnderNetwork : KMonoBehaviour, ISim1000ms
	{

		public const float TransferPerSecondKg = 1000f; 
		
		[Serialize]
		public string networkName = "";

		[Serialize]
		private string uuid;

		[MyCmpGet]
		private UserNameable nameable;

		private Storage hubStorage; 
		
		private int nameChangedHandle = -1;

		public string networkId => this.uuid;

		public Storage HubStorage => this.hubStorage;

		private readonly List<IEnderNetworkMember> collectors = new List<IEnderNetworkMember>();

		private readonly List<IEnderNetworkMember> consumers = new List<IEnderNetworkMember>();
		protected override void OnSpawn()
		{
			base.OnSpawn();
			this.hubStorage = this.GetComponent<Storage>(); 
			this.SyncNetworkNameFromNameable();
			this.nameChangedHandle = this.Subscribe((int)GameHashes.NameChanged, new System.Action<object>(this.OnNameChanged));
			if (string.IsNullOrEmpty(uuid)) {
				uuid = Guid.NewGuid().ToString();
				Debug.Log($"[EN] Assign new uuid {uuid} to new network");
			}
			EnderNetworkRegistry.Register(this);
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
		public void RegisterMember(IEnderNetworkMember member)
		{
			if (member == null || member.BoundNetworkId != this.networkId)
			{
				return;
			}
			var list = member.Role == EnderNetworkMemberRole.Collector ? this.collectors : this.consumers;
			if (!list.Contains(member))
			{
				Debug.Log($"[EN] Add member to network {name}");
				list.Add(member);
			}
		}
		public void UnregisterMember(IEnderNetworkMember member)
		{
			if (member == null)
			{
				return;
			}
			this.collectors.Remove(member);
			this.consumers.Remove(member);
		}
		public void Sim1000ms(float dt)
		{
			if (this.hubStorage == null || !this.IsOperational())
			{
				return;
			}
			for (int i = 0; i < this.collectors.Count; i++)
			{
				this.ProcessCollector(this.collectors[i]);
			}
		}
		private void ProcessCollector(IEnderNetworkMember member)
		{
			if (!member.IsOperational())
			{
				return;
			}
			Storage local = member.LocalStorage;
			if (local == null || local.MassStored() <= 0f)
			{
				return;
			}
			float remaining = this.hubStorage.RemainingCapacity();
			if (remaining <= 0f)
			{
				return;
			}
			float budget = Mathf.Min(TransferPerSecondKg, remaining);
			this.TransferUpToMass(local, this.hubStorage, budget);
		}

		private void TransferUpToMass(Storage source, Storage destination, float maxMass)
		{
			float moved = 0f;
			int safety = source.items.Count + 4;
			while (moved < maxMass && source.items.Count > 0 && safety-- > 0)
			{
				GameObject item = source.items[0];
				if (item == null)
				{
					source.items.RemoveAt(0);
					continue;
				}
				PrimaryElement pe = item.GetComponent<PrimaryElement>();
				if (pe == null)
				{
					break;
				}
				float amount = pe.Mass;
				if (amount <= 0f)
				{
					break;
				}
				if (moved + amount > maxMass)
				{
					Tag tag = pe.Element != null ? pe.Element.tag : Tag.Invalid;
					if (!tag.IsValid)
					{
						break;
					}
					moved += source.Transfer(destination, tag, maxMass - moved, false, true);
					break;
				}
				if (!source.Transfer(item, destination, false, true))
				{
					break;
				}
				moved += amount;
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
