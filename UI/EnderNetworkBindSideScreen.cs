using System.Collections.Generic;
using EnderChest.Components;
using PeterHan.PLib.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EnderChest.UI
{
	// 原版侧屏只作为目标检测入口；真正的末影网络面板会作为 DetailsScreen 的同级子物体显示在左侧。
	public class EnderNetworkBindSideScreen : SideScreenContent
	{
		// 这里的尺寸和坐标对应游戏内调试窗口里 RectTransform 的手调结果。
		private const float PanelWidth = 280f;
		private const float PanelHeight = 715f;
		private const float PanelGap = 7.114f;
		private const float PanelY = 173.761f;

		private EnderChestLeftPanelTarget panelTarget;
		private EnderCollector collector;
		private GameObject panelObject;
		private LocText currentCoreLabel;
		private LocText emptyLabel;
		private GameObject rowsRoot;

		protected override void OnPrefabInit()
		{
			this.ContentContainer = base.gameObject;
			base.OnPrefabInit();
		}

		public override string GetTitle()
		{
			return STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.TITLE);
		}

		public override bool IsValidForTarget(GameObject target)
		{
			// 只有带标记组件的建筑才显示左侧末影网络面板。
			return target != null && target.GetComponent<EnderChestLeftPanelTarget>() != null;
		}

		public override void SetTarget(GameObject target)
		{
			base.SetTarget(target);
			panelTarget = target != null ? target.GetComponent<EnderChestLeftPanelTarget>() : null;
			collector = panelTarget != null ? panelTarget.Collector : null;
			OpenPanel();
			RefreshUi();
		}

		public override void ClearTarget()
		{
			base.ClearTarget();
			panelTarget = null;
			collector = null;
			ClosePanel();
		}

		protected override void OnSpawn()
		{
			base.OnSpawn();
			EnderNetworkRegistry.CoresChanged += RefreshUi;
			if (panelTarget != null)
			{
				OpenPanel();
				RefreshUi();
			}
		}

		protected override void OnCleanUp()
		{
			EnderNetworkRegistry.CoresChanged -= RefreshUi;
			ClosePanel();
			base.OnCleanUp();
		}

		private void OpenPanel()
		{
			// 延迟创建面板，避免没有选中目标时提前污染 DetailsScreen 层级。
			if (panelObject == null)
			{
				panelObject = BuildPanel();
			}
			PositionPanel();
			panelObject.SetActive(true);
		}

		private GameObject BuildPanel()
		{
			// 用 PLib 构建独立 UI，不再依赖 AssetBundle 或 Unity 预制件。
			GameObject panel = new PPanel("EnderChestLeftPanel")
			{
				Direction = PanelDirection.Vertical,
				Spacing = 6,
				Alignment = TextAnchor.UpperCenter,
				Margin = new RectOffset(6, 6, 6, 6)
			}
				.SetKleiPinkColor()
				.AddChild(new PLabel("EnderChestBindTitle")
				{
					Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.TITLE),
					TextAlignment = TextAnchor.MiddleCenter,
					DynamicSize = true
				}.SetKleiPinkColor())
				.AddChild(new PLabel("EnderChestCurrentCore")
				{
					Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.CURRENT_PREFIX) + STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.EMPTY),
					TextAlignment = TextAnchor.MiddleLeft,
					DynamicSize = true
				}.AddOnRealize(delegate (GameObject obj)
				{
					currentCoreLabel = obj.GetComponentInChildren<LocText>();
				}))
				.AddChild(new PPanel("EnderChestBindButtons")
				{
					Direction = PanelDirection.Horizontal,
					Spacing = 6,
					Alignment = TextAnchor.MiddleCenter,
					DynamicSize = true
				}
					.AddChild(new PButton("EnderChestRefresh")
					{
						Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.REFRESH),
						OnClick = delegate { RefreshUi(); }
					}.SetKleiBlueStyle())
					.AddChild(new PButton("EnderChestUnbind")
					{
						Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.UNBIND),
						OnClick = delegate { Unbind(); }
					}.SetKleiPinkStyle()))
				.AddChild(new PLabel("EnderChestEmpty")
				{
					Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.NO_CORES),
					TextAlignment = TextAnchor.MiddleCenter,
					DynamicSize = true
				}.AddOnRealize(delegate(GameObject obj)
				{
					emptyLabel = obj.GetComponentInChildren<LocText>();
				}))
				.AddChild(new PPanel("EnderChestCoreRows")
				{
					Direction = PanelDirection.Vertical,
					Spacing = 4,
					Alignment = TextAnchor.UpperCenter,
					DynamicSize = true,
				}.AddOnRealize(delegate (GameObject obj)
				{
					rowsRoot = obj;
					LayoutElement le = obj.AddOrGet<LayoutElement>();

					le.minWidth = PanelWidth - 10;
					le.preferredWidth = PanelWidth - 10;
				}))
				.BuildWithFixedSize(new Vector2(PanelWidth, PanelHeight));

			RectTransform sideRect = FindRightmostDetailsSideScreen();
			// 面板挂到 SideScreen 的父节点下，才能和原版详情侧屏并排显示。
			Transform parent = sideRect != null ? sideRect.parent : base.transform.parent;
			panel.transform.SetParent(parent, false);
			IgnoreParentLayout(panel);
			return panel;
		}

		private void PositionPanel()
		{
			if (panelObject == null)
			{
				return;
			}

			RectTransform panelRect = panelObject.GetComponent<RectTransform>();
			if (panelRect == null)
			{
				return;
			}

			RectTransform sideRect = FindRightmostDetailsSideScreen();
			RectTransform parentRect = sideRect != null ? sideRect.parent as RectTransform : panelRect.parent as RectTransform;
			if (parentRect == null)
			{
				return;
			}

			if (panelRect.parent != parentRect)
			{
				panelRect.SetParent(parentRect, false);
			}

			// 以右侧原版 SideScreen 为基准，把新面板贴到它左边。
			float panelX = sideRect != null ? sideRect.anchoredPosition.x - sideRect.rect.width - PanelGap : -294.014f;
			panelRect.anchorMin = new Vector2(0f, 1f);
			panelRect.anchorMax = new Vector2(0f, 1f);
			panelRect.pivot = new Vector2(1f, 1f);
			panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
			panelRect.anchoredPosition3D = new Vector3(panelX, PanelY, 0f);
			panelRect.SetAsLastSibling();
		}

		private static void IgnoreParentLayout(GameObject panel)
		{
			// DetailsScreen 自带布局组件；必须忽略布局，否则独立面板会被压缩或挪位。
			LayoutElement layout = panel.GetComponent<LayoutElement>();
			if (layout == null)
			{
				layout = panel.AddComponent<LayoutElement>();
			}
			layout.ignoreLayout = true;
		}

		private void ClosePanel()
		{
			if (panelObject != null)
			{
				Destroy(panelObject);
				panelObject = null;
			}
			currentCoreLabel = null;
			emptyLabel = null;
			rowsRoot = null;
		}

		private void RefreshUi()
		{
			if (panelObject == null)
			{
				return;
			}

			if (currentCoreLabel != null)
			{
				currentCoreLabel.SetText(STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.CURRENT_PREFIX) + GetCurrentCoreName());
			}
			RefreshRows();
		}

		private void RefreshRows()
		{
			if (rowsRoot == null)
			{
				return;
			}

			// 核心列表数量很少，刷新时直接重建按钮可以保持逻辑简单。
			for (int i = rowsRoot.transform.childCount - 1; i >= 0; i--)
			{
				Destroy(rowsRoot.transform.GetChild(i).gameObject);
			}

			List<EnderNetwork> networks = GetAvailableNetworks();
			if (emptyLabel != null)
			{
				emptyLabel.transform.parent.gameObject.SetActive(networks.Count == 0);
			}

			for (int i = 0; i < networks.Count; i++)
			{
				EnderNetwork network = networks[i];
				string marker = IsBoundTo(network) ? STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.SELECTED_MARKER) : "";
				new PButton("EnderNetwork_" + network.InstanceID)
				{
					Text = marker + network.GetDisplayName() + "\n" + GetNetworkDetail(network),
					TextAlignment = TextAnchor.MiddleLeft,
					OnClick = delegate { Bind(network); }
				}.SetKleiBlueStyle().AddTo(rowsRoot, -2);
			}
		}

		private List<EnderNetwork> GetAvailableNetworks()
		{
			int worldId = collector != null ? collector.GetMyWorldId() : -1;
			return worldId >= 0
				? EnderNetworkRegistry.GetCoresForWorld(worldId)
				: EnderNetworkRegistry.GetAllNetworks();
		}

		private string GetCurrentCoreName()
		{
			if (collector == null || collector.BoundNetworkInstanceId == 0)
			{
				return STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.EMPTY);
			}

			EnderNetwork network = EnderNetworkRegistry.Resolve(collector.BoundNetworkInstanceId);
			return network != null ? network.GetDisplayName() : STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.INVALID_CORE);
		}

		internal static string GetWorldDisplayName(int worldId)
		{
			if (ClusterManager.Instance == null)
			{
				return string.Empty;
			}
			WorldContainer world = ClusterManager.Instance.GetWorld(worldId);
			if (world == null)
			{
				return string.Empty;
			}
			ClusterGridEntity clusterEntity = world.GetComponent<ClusterGridEntity>();
			if (clusterEntity != null && !string.IsNullOrEmpty(clusterEntity.Name))
			{
				return clusterEntity.Name;
			}
			return world.worldName ?? string.Empty;
		}
		private string GetNetworkDetail(EnderNetwork network)
		{
			int collectors = EnderNetworkRegistry.CountCollectorsBoundTo(network.InstanceID);
			
			return string.Format(STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.CORE_DETAIL), GetWorldDisplayName(network.GetMyWorldId()), collectors);
		}

		private bool IsBoundTo(EnderNetwork network)
		{
			return collector != null && network != null && collector.BoundNetworkInstanceId == network.InstanceID;
		}

		private void Bind(EnderNetwork network)
		{
			if (collector == null)
			{
				return;
			}

			collector.BindToNetwork(network);
			RefreshUi();
		}

		private void Unbind()
		{
			if (collector == null)
			{
				return;
			}

			collector.BindToNetwork(null);
			RefreshUi();
		}

		private static RectTransform FindRightmostDetailsSideScreen()
		{
			// 游戏里可能同时存在筛选侧屏和详情侧屏，取最靠右的那个作为定位基准。
			RectTransform best = null;
			float bestRight = float.MinValue;
			RectTransform[] rects = Resources.FindObjectsOfTypeAll<RectTransform>();

			for (int i = 0; i < rects.Length; i++)
			{
				RectTransform candidate = rects[i];
				if (candidate == null || !candidate.gameObject.activeInHierarchy)
				{
					continue;
				}
				if (!candidate.name.StartsWith("SideScreen") || !IsUnderDetailsScreen(candidate))
				{
					continue;
				}

				Vector3[] corners = new Vector3[4];
				candidate.GetWorldCorners(corners);
				float right = corners[2].x;
				if (right > bestRight)
				{
					bestRight = right;
					best = candidate;
				}
			}

			return best;
		}

		private static bool IsUnderDetailsScreen(Transform transform)
		{
			Transform current = transform.parent;
			while (current != null)
			{
				if (current.name == "DetailsScreen")
				{
					return true;
				}
				current = current.parent;
			}
			return false;
		}
	}
}
