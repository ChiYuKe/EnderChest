using KSerialization;
using UnityEngine;

namespace EnderChest.Components
{
	[SerializationConfig(MemberSerialization.OptIn)]
	public class EnderCollector: KMonoBehaviour, IUserControlledCapacity, IEnderNetworkMember
	{
		public Storage LocalStorage => this.storage;

		public Storage IntakeStorage => this.storage;

		public EnderNetworkMemberRole Role => EnderNetworkMemberRole.Collector;

		public int BoundNetworkInstanceId => this.boundNetworkInstanceId;

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
		public int boundNetworkInstanceId;

		private EnderNetwork boundNetwork;

		public bool IsOperational()
		{
			Operational operational = this.GetComponent<Operational>();
			return operational == null || operational.IsOperational;
		}

		public bool ControlEnabled() => true;

		public int GetBoundNetworkInstanceId() => this.boundNetworkInstanceId;

		public void BindToNetwork(EnderNetwork network)
		{
			int newId = network != null ? network.InstanceID : 0;
			if (this.boundNetworkInstanceId == newId)
			{
				return;
			}
			if (this.boundNetwork != null)
			{
				this.boundNetwork.UnregisterMember(this);
				this.boundNetwork = null;
			}
			this.boundNetworkInstanceId = newId;
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
			this.boundNetwork = EnderNetworkRegistry.Resolve(this.boundNetworkInstanceId);
			this.boundNetwork?.RegisterMember(this);
		}
	}
}

