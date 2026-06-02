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
		private static RectTransform cachedDetailsSideScreen;
		private static GameObject cachedGeoTunerRowPrefab;
		private static bool resolvedGeoTunerRowPrefab;

		private EnderChestLeftPanelTarget panelTarget;
		private EnderCollector collector;
		private GameObject panelObject;
		private LocText currentCoreLabel;
		private GameObject emptyRow;
		private GameObject rowsRoot;
		private bool refreshScheduled;

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
			RequestRefreshUi();
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
			EnderNetworkRegistry.CoresChanged += RequestRefreshUi;
			if (panelObject == null)
			{
				// 提前创建并隐藏，避免第一次选中建筑时把 PLib 构建成本压到同一帧。
				panelObject = BuildPanel();
				panelObject.SetActive(false);
			}
		}

		protected override void OnCleanUp()
		{
			EnderNetworkRegistry.CoresChanged -= RequestRefreshUi;
			CancelPendingRefresh();
			DestroyPanel();
			base.OnCleanUp();
		}

		private void OpenPanel()
		{
			// 正常情况下 OnSpawn 已经预建面板；这里保留兜底，防止特殊加载顺序下没有 UI 可显示。
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
			PPanel bindContent = new PPanel("EnderChestBindContent")
			{
				Direction = PanelDirection.Vertical,
				Spacing = 6,
				Alignment = TextAnchor.UpperCenter,
				BackColor = PUITuning.Colors.BackgroundLight,
				FlexSize = Vector2.right,
				Margin = new RectOffset(8, 8, 8, 8)
			}
				.AddChild(new PLabel("EnderChestCurrentCore")
				{
					Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.CURRENT_PREFIX) + STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.EMPTY),
					TextAlignment = TextAnchor.MiddleLeft,
					TextStyle = PUITuning.Fonts.UIDarkStyle,
					DynamicSize = true,
					FlexSize = Vector2.right,
					Margin = new RectOffset(4, 4, 2, 2)
				}.AddOnRealize(delegate (GameObject obj)
				{
					currentCoreLabel = obj.GetComponentInChildren<LocText>();
					SetLayoutSize(obj, PanelWidth - 32f, 24f, 0f);
				}))
				.AddChild(new PPanel("EnderChestBindButtons")
				{
					Direction = PanelDirection.Horizontal,
					Spacing = 8,
					Alignment = TextAnchor.MiddleCenter,
					DynamicSize = true,
					FlexSize = Vector2.right,
					Margin = new RectOffset(0, 0, 2, 2)
				}.AddOnRealize(delegate(GameObject obj)
				{
					SetLayoutSize(obj, PanelWidth - 32f, 32f, 0f);
				})
					.AddChild(new PButton("EnderChestRefresh")
					{
						Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.REFRESH),
						Margin = new RectOffset(8, 8, 5, 5),
						OnClick = delegate { RefreshUi(); }
					}.SetKleiBlueStyle())
					.AddChild(new PButton("EnderChestUnbind")
					{
						Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.UNBIND),
						Margin = new RectOffset(8, 8, 5, 5),
						OnClick = delegate { Unbind(); }
					}.SetKleiPinkStyle()));

			PPanel rowsPanel = new PPanel("EnderChestCoreRows")
			{
				Direction = PanelDirection.Vertical,
				Spacing = 8,
				Alignment = TextAnchor.UpperCenter,
				FlexSize = Vector2.right,
				Margin = new RectOffset(6, 6, 6, 6)
			}.AddOnRealize(delegate(GameObject obj)
			{
				rowsRoot = obj;
			});

			PScrollPane rowsScroll = new PScrollPane("EnderChestCoreScroll")
			{
				Child = rowsPanel,
				ScrollHorizontal = false,
				ScrollVertical = true,
				AlwaysShowHorizontal = false,
				AlwaysShowVertical = false,
				TrackSize = 8f,
				FlexSize = Vector2.one
			};

			PPanel listContent = new PPanel("EnderChestListContent")
			{
				Direction = PanelDirection.Vertical,
				Spacing = 0,
				Alignment = TextAnchor.UpperCenter,
				BackColor = PUITuning.Colors.BackgroundLight,
				FlexSize = Vector2.one,
				Margin = new RectOffset(0, 0, 0, 0)
			}
				.AddChild(new PLabel("EnderChestEmptyRow")
				{
					Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.NO_CORES),
					TextAlignment = TextAnchor.MiddleCenter,
					TextStyle = PUITuning.Fonts.UIDarkStyle,
					DynamicSize = true,
					FlexSize = Vector2.right,
					Margin = new RectOffset(8, 8, 8, 8)
				}.AddOnRealize(delegate(GameObject obj)
				{
					emptyRow = obj;
					SetLayoutSize(obj, PanelWidth - 32f, 34f, 0f);
					SetBoxBorderImage(obj, PUITuning.Colors.BackgroundLight);
				}))
				.AddChild(rowsScroll.AddOnRealize(delegate(GameObject obj)
				{
					SetLayoutSize(obj, PanelWidth - 16f, 520f, 1f);
				}));

			GameObject panel = new PPanel("EnderChestLeftPanel")
			{
				Direction = PanelDirection.Vertical,
				Spacing = 0,
				Alignment = TextAnchor.UpperCenter,
				Margin = new RectOffset(0, 0, 0, 0),
				FlexSize = Vector2.one,
				BackColor = PUITuning.Colors.BackgroundLight
			}
				.AddChild(new PLabel("EnderChestBindTitle")
				{
					Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.TITLE),
					TextAlignment = TextAnchor.MiddleCenter,
					DynamicSize = true,
					FlexSize = Vector2.right,
					Margin = new RectOffset(6, 6, 2, 2)
				}.SetKleiPinkColor().AddOnRealize(delegate(GameObject obj)
				{
					SetLayoutSize(obj, PanelWidth, 26f, 0f);
					SetKleiTitleBarImage(obj);
				}))
				.AddChild(new PLabel("EnderChestBindSection")
				{
					Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.TITLE),
					TextAlignment = TextAnchor.MiddleLeft,
					DynamicSize = true,
					FlexSize = Vector2.right,
					Margin = new RectOffset(6, 6, 2, 2)
				}.SetKleiPinkColor().AddOnRealize(delegate(GameObject obj)
				{
					SetLayoutSize(obj, PanelWidth, 24f, 0f);
				}))
				.AddChild(bindContent)
				.AddChild(new PLabel("EnderChestCoreSection")
				{
					Text = STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.CORE_LIST_TITLE),
					TextAlignment = TextAnchor.MiddleLeft,
					DynamicSize = true,
					FlexSize = Vector2.right,
					Margin = new RectOffset(6, 6, 2, 2)
				}.SetKleiPinkColor().AddOnRealize(delegate(GameObject obj)
				{
					SetLayoutSize(obj, PanelWidth, 24f, 0f);
				}))
				.AddChild(listContent.AddOnRealize(delegate(GameObject obj)
				{
					SetLayoutSize(obj, PanelWidth, 585f, 1f);
				}))
				.BuildWithFixedSize(new Vector2(PanelWidth, PanelHeight));

			SetBoxBorderImage(panel, PUITuning.Colors.BackgroundLight);

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
			CancelPendingRefresh();
			if (panelObject != null)
			{
				// 关闭时只隐藏 UI，避免下一次选中建筑时重新创建整棵控件树导致掉帧。
				panelObject.SetActive(false);
			}
		}

		private void DestroyPanel()
		{
			// 组件销毁时才真正释放面板对象；普通切换目标只隐藏复用。
			if (panelObject != null)
			{
				Destroy(panelObject);
				panelObject = null;
			}
			currentCoreLabel = null;
			emptyRow = null;
			rowsRoot = null;
		}

		private void RequestRefreshUi()
		{
			if (panelObject == null || !panelObject.activeSelf)
			{
				return;
			}

			CancelPendingRefresh();
			refreshScheduled = true;
			// SideScreenContent 自身可能是 inactive，不能 StartCoroutine；用 ONI 的 UI 调度器延迟到下一帧刷新。
			UIScheduler.Instance.ScheduleNextFrame("EnderChestRefreshBindSideScreen", delegate(object data)
			{
				if (!refreshScheduled)
				{
					return;
				}
				refreshScheduled = false;
				RefreshUi();
			});
		}

		private void CancelPendingRefresh()
		{
			// ScheduleNextFrame 不能直接取消，用标记让回调到达后自行跳过。
			refreshScheduled = false;
		}

		private void RefreshUi()
		{
			if (panelObject == null || !panelObject.activeSelf)
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

			// 刷新时复用已有行，只更新内容和显隐，减少打开面板时的对象创建。
			List<EnderNetwork> networks = GetAvailableNetworks();
			if (emptyRow != null)
			{
				emptyRow.SetActive(networks.Count == 0);
			}

			GameObject prefab = GetGeoTunerRowPrefab();
			for (int i = 0; i < networks.Count; i++)
			{
				SetNetworkRow(i, networks[i], prefab);
			}

			for (int i = networks.Count; i < rowsRoot.transform.childCount; i++)
			{
				rowsRoot.transform.GetChild(i).gameObject.SetActive(false);
			}
		}

		private void SetNetworkRow(int index, EnderNetwork network, GameObject prefab)
		{
			// 优先克隆地质调谐仪的原版列表行，复用它的 MultiToggle 选中态和行样式。
			if (prefab == null)
			{
				SetFallbackNetworkRow(index, network);
				return;
			}

			GameObject row = index < rowsRoot.transform.childCount
				? rowsRoot.transform.GetChild(index).gameObject
				: Util.KInstantiateUI(prefab, rowsRoot, true);
			row.SetActive(true);
			row.name = "EnderNetwork_" + network.networkId;

			// 地质调谐仪行预制件通过 HierarchyReferences 暴露 label/icon/amount，这里只替换内容，不改原版结构。
			HierarchyReferences references = row.GetComponent<HierarchyReferences>();
			if (references != null)
			{
				LocText label = references.GetReference<LocText>("label");
				if (label != null)
				{
					label.text = network.GetDisplayName();
					label.textStyleSetting = PUITuning.Fonts.UIDarkStyle;
					label.ApplySettings();
				}

				Image icon = references.GetReference<Image>("icon");
				if (icon != null)
				{
					icon.sprite = Def.GetUISprite(network.gameObject, "ui", false).first;
					icon.color = Color.white;
				}

				LocText amount = references.GetReference<LocText>("amount");
				if (amount != null)
				{
					// 右侧计数显示当前绑定到这个核心的收集器数量。
					int boundCollectors = EnderNetworkRegistry.CountCollectorsBoundTo(network.networkId);
					amount.SetText(boundCollectors.ToString());
					if (amount.transform.parent != null)
					{
						GameObject amountGroup = amount.transform.parent.gameObject;
						amountGroup.SetActive(true);
						SetCollectorCountIcon(amountGroup);
					}
				}
			}

			ToolTip tooltip = row.GetComponentInChildren<ToolTip>();
			if (tooltip != null)
			{
				tooltip.SetSimpleTooltip(GetNetworkDetail(network));
			}

			MultiToggle toggle = row.GetComponent<MultiToggle>();
			if (toggle != null)
			{
				toggle.ChangeState(IsBoundTo(network) ? 1 : 0);
				toggle.onClick = delegate
				{
					Bind(network);
				};
				toggle.onDoubleClick = delegate
				{
					GameUtil.FocusCamera(network.transform.GetPosition(), 2f, true, true);
					return true;
				};
			}
			else
			{
				// 兜底：如果克隆出来的行没有 MultiToggle，就挂一个轻量点击代理，避免重复添加匿名事件。
				row.AddOrGet<KButton>();
				row.AddOrGet<EnderNetworkRowClickHandler>().Configure(this, network);
			}
		}

		private void SetFallbackNetworkRow(int index, EnderNetwork network)
		{
			GameObject row;
			if (index < rowsRoot.transform.childCount)
			{
				// 列表行数量不会每次重建，复用已有行能减少打开/刷新时的 GC 和布局抖动。
				row = rowsRoot.transform.GetChild(index).gameObject;
				row.SetActive(true);
			}
			else
			{
				row = new PButton("EnderNetwork_" + network.networkId)
				{
					TextAlignment = TextAnchor.MiddleLeft,
					TextStyle = PUITuning.Fonts.UIDarkStyle,
					Margin = new RectOffset(10, 10, 8, 8)
				}.AddOnRealize(delegate(GameObject obj)
				{
					SetLayoutSize(obj, PanelWidth - 28f, 48f, 0f);
				}).AddTo(rowsRoot, -2);
			}

			PButton.SetButtonEnabled(row, true);
			row.AddOrGet<EnderNetworkRowClickHandler>().Configure(this, network);
			LocText label = row.GetComponentInChildren<LocText>();
			if (label != null)
			{
				label.SetText(network.GetDisplayName() + "\n" + GetNetworkDetail(network));
			}
			SetBoxBorderImage(row, IsBoundTo(network) ? new Color32(232, 210, 222, 255) : new Color32(245, 245, 245, 255));
		}

		private GameObject GetGeoTunerRowPrefab()
		{
			if (resolvedGeoTunerRowPrefab)
			{
				return cachedGeoTunerRowPrefab;
			}

			resolvedGeoTunerRowPrefab = true;
			// 只全局查找一次地质调谐仪侧屏，后续刷新直接复用缓存，避免打开面板时反复扫描 Resources。
			GeoTunerSideScreen[] screens = Resources.FindObjectsOfTypeAll<GeoTunerSideScreen>();
			for (int i = 0; i < screens.Length; i++)
			{
				if (screens[i] != null && screens[i].rowPrefab != null)
				{
					cachedGeoTunerRowPrefab = screens[i].rowPrefab;
					return cachedGeoTunerRowPrefab;
				}
			}
			return null;
		}

		private static void SetLayoutSize(GameObject obj, float width, float height, float flexHeight)
		{
			// PLib 负责创建控件树；LayoutElement 只用来声明固定区和滚动区的布局尺寸。
			LayoutElement layout = obj.AddOrGet<LayoutElement>();
			layout.minWidth = width;
			layout.preferredWidth = width;
			layout.minHeight = height;
			layout.preferredHeight = height;
			layout.flexibleWidth = 0f;
			layout.flexibleHeight = flexHeight;
		}

		private static void SetKleiTitleBarImage(GameObject obj)
		{
			// 复用 PLib.PDialog 标题栏的边框图，保持和 PLib 窗口标题一致。
			SetBoxBorderImage(obj, PUITuning.Colors.ButtonPinkStyle.inactiveColor);
		}

		private static void SetBoxBorderImage(GameObject obj, Color color)
		{
			Image image = obj.AddOrGet<Image>();
			image.color = color;
			image.sprite = PUITuning.Images.BoxBorder;
			image.type = Image.Type.Sliced;
		}

		private static void SetCollectorCountIcon(GameObject amountGroup)
		{
			if (amountGroup == null)
			{
				return;
			}

			Image countIcon = amountGroup.GetComponentInChildren<Image>(true);
			if (countIcon == null)
			{
				return;
			}

			// 地质调谐仪预制件自带的计数图标含义不对，这里换成末影收集器建筑图标。
			GameObject collectorPrefab = Assets.GetPrefab("EnderCollector");
			if (collectorPrefab != null)
			{
				countIcon.sprite = Def.GetUISprite(collectorPrefab, "ui", false).first;
				countIcon.color = Color.white;
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
			// 当前绑定只保存核心 InstanceID，显示名称前先从注册表找回核心对象。
			if (collector == null || collector.BoundNetworkId == "")
			{
				return STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.EMPTY);
			}

			EnderNetwork network = EnderNetworkRegistry.Resolve(collector.BoundNetworkId);
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
			int collectors = EnderNetworkRegistry.CountCollectorsBoundTo(network.networkId);
			
			return string.Format(STRINGS.Get(STRINGS.SIDESCREEN.ENDERNETWORKBIND.CORE_DETAIL), GetWorldDisplayName(network.GetMyWorldId()), collectors);
		}

		private bool IsBoundTo(EnderNetwork network)
		{
			return collector != null && network != null && collector.BoundNetworkId == network.networkId;
		}

		private void Bind(EnderNetwork network)
		{
			// 点击核心行后，把当前收集器绑定到该核心并立即刷新选中态。
			if (collector == null)
			{
				return;
			}

			collector.BindToNetwork(network);
			RefreshUi();
		}

		private void Unbind()
		{
			// 解绑按钮会清空当前收集器保存的核心 InstanceID。
			if (collector == null)
			{
				return;
			}

			collector.BindToNetwork(null);
			RefreshUi();
		}

		private static RectTransform FindRightmostDetailsSideScreen()
		{
			if (cachedDetailsSideScreen != null && cachedDetailsSideScreen.gameObject.activeInHierarchy && IsUnderDetailsScreen(cachedDetailsSideScreen))
			{
				return cachedDetailsSideScreen;
			}

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

			cachedDetailsSideScreen = best;
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

		private sealed class EnderNetworkRowClickHandler : MonoBehaviour
		{
			private EnderNetworkBindSideScreen owner;
			private EnderNetwork network;
			private KButton button;

			public void Configure(EnderNetworkBindSideScreen newOwner, EnderNetwork newNetwork)
			{
				// 行对象会被复用，点击处理器只注册一次事件，后续刷新只替换 owner/network 引用。
				owner = newOwner;
				network = newNetwork;
				if (button == null)
				{
					button = GetComponent<KButton>();
					if (button != null)
					{
						button.onClick += OnClick;
					}
				}
			}

			private void OnClick()
			{
				if (owner != null && network != null)
				{
					owner.Bind(network);
				}
			}

			private void OnDestroy()
			{
				if (button != null)
				{
					button.onClick -= OnClick;
				}
			}
		}
	}
}
