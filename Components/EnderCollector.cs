using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using KSerialization;
using UnityEngine;

namespace EnderChest.Components
{
	[SerializationConfig(MemberSerialization.OptIn)]
	public class EnderCollector: KMonoBehaviour, IUserControlledCapacity
	{
		public int EnderNetworkID = 0;

		public Storage LocalStorage => this.storage;

		public Storage IntakeStorage => this.storage;

		public int BoundCoreInstanceId => this.boundCoreInstanceId;

		[MyCmpGet]
		private Storage storage;

		[MyCmpGet]
		private SolidConduitConsumer conduitConsumer;

		public SolidConduitConsumer ConduitConsumer => this.conduitConsumer;

		public virtual float UserMaxCapacity
		{
			get { return Mathf.Min(this.userMaxCapacity, this.storage.capacityKg); }
			set
			{
				this.userMaxCapacity = value;
				this.filteredStorage.FilterChanged();
			}
		}

		public float AmountStored => this.storage.MassStored();

		public float MinCapacity => 0f;

		public float MaxCapacity => this.storage.capacityKg;

		public bool WholeValues => false;

		public LocString CapacityUnits => GameUtil.GetCurrentMassUnit(false);

		protected FilteredStorage filteredStorage;

		[Serialize]
		private float userMaxCapacity = float.PositiveInfinity;

		[Serialize]
		public string lockerName = "";

		[Serialize]
		public int boundCoreInstanceId;

		private EnderNetwork boundNetwork;

		public bool IsOperational()
		{
			Operational operational = this.GetComponent<Operational>();
			return operational == null || operational.IsOperational;
		}

		public bool ControlEnabled() => true;

		public int GetBoundCoreInstanceId() => this.boundCoreInstanceId;

		public void BindToStorageCore(EnderCore core)
		{
			int newId = core != null ? core.InstanceID : 0;
			if (this.boundCoreInstanceId == newId)
			{
				return;
			}
			if (this.boundNetwork != null)
			{
				this.boundNetwork.UnregisterMember(this);
				this.boundNetwork = null;
			}
			this.boundCoreInstanceId = newId;
			this.TryRegisterWithNetwork();
		}

		protected override void OnPrefabInit()
		{
			base.OnPrefabInit();
			ChoreType fetchChoreType = Db.Get().ChoreTypes.Get(Db.Get().ChoreTypes.StorageFetch.Id);
			this.filteredStorage = new FilteredStorage(this, null, this, false, fetchChoreType);
		}

		protected override void OnSpawn()
		{
			base.OnSpawn();
			this.TryRegisterWithNetwork();
		}

		protected override void OnCleanUp()
		{
			if (this.boundNetwork != null)
			{
				this.boundNetwork.UnregisterMember(this);
				this.boundNetwork = null;
			}
			this.filteredStorage?.CleanUp();
			base.OnCleanUp();
		}

		private void TryRegisterWithNetwork()
		{
			this.boundNetwork = ProxyChestRegistry.Resolve(this.boundCoreInstanceId);
			this.boundNetwork?.RegisterMember(this);
		}
	}
}

