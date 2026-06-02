using System;
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

		public string BoundNetworkId => this.boundNetworkId;

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
		public string boundNetworkId;

		private EnderNetwork boundNetwork;

		public bool IsOperational()
		{
			Operational operational = this.GetComponent<Operational>();
			return operational == null || operational.IsOperational;
		}

		public bool ControlEnabled() => true;

		public string GetBoundNetworkId() => this.boundNetworkId;

		public void BindToNetwork(EnderNetwork network)
		{
			// 收集器只序列化核心 InstanceID；真正的核心对象通过 EnderNetworkRegistry.Resolve 再取回。
			string oldId = boundNetwork != null ? boundNetwork.networkId : "";
			string newId = network != null ? network.networkId : "";
			if (oldId == newId)
			{
				return;
			}
			if (this.boundNetwork != null)
			{
				this.boundNetwork.UnregisterMember(this);
				this.boundNetwork = null;
			}
			this.boundNetworkId = newId;
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
			// 注册到网络表，侧屏统计绑定数量时就不用全局查找所有收集器。
			EnderNetworkRegistry.RegisterCollector(this);
			this.TryRegisterWithNetwork();
		}

		protected override void OnCleanUp()
		{
			// 清理时先从统计缓存移除，防止 UI 继续把这个收集器算进绑定数量。
			EnderNetworkRegistry.UnregisterCollector(this);
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
			// 存档加载或重新绑定后，根据保存的 InstanceID 找回核心对象并注册为网络成员。
			this.boundNetwork = EnderNetworkRegistry.Resolve(this.boundNetworkId);

			if (!string.IsNullOrEmpty(this.boundNetworkId) && this.boundNetwork == null) {
				EnderNetworkRegistry.Pending(this.boundNetworkId, this);
			}
			else
			{
				this.boundNetwork?.RegisterMember(this);
			}
		}
	}
}

