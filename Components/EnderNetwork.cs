using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

		int BoundCoreInstanceId { get; }

		bool IsOperational();
	}
	public class EnderNetwork : KMonoBehaviour
	{

		public const float TransferPerTickKg = 200f;

		private EnderCore core;

		private Storage hubStorage;

		private readonly List<IEnderNetworkMember> collectors = new List<IEnderNetworkMember>();

		private readonly List<IEnderNetworkMember> consumers = new List<IEnderNetworkMember>();
		protected override void OnSpawn()
		{
			base.OnSpawn();
			this.core = this.GetComponent<EnderCore>();
			this.hubStorage = this.GetComponent<Storage>();
		}
		public void RegisterMember(IEnderNetworkMember member)
		{
			if (member == null || member.BoundCoreInstanceId != this.core.InstanceID)
			{
				return;
			}
			var list = member.Role == EnderNetworkMemberRole.Collector ? this.collectors : this.consumers;
			if (!list.Contains(member))
			{
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
		public void Sim200ms(float dt)
		{
			if (this.hubStorage == null || this.core == null || !this.core.IsOperational())
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
			float budget = Mathf.Min(TransferPerTickKg, remaining);
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


	}
}
