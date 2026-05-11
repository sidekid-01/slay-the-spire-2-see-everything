using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.addons.mega_text;
using PartyObserver.Networking;
using PartyObserver.Services;

namespace PartyObserver.UI;

[ScriptPath("res://UI/PartyObserverOverlay.cs")]
public class PartyObserverOverlay : CanvasLayer
{
	private sealed class AnchorBinding
	{
		public required NMultiplayerPlayerState State { get; init; }

		public required Action MouseEntered { get; init; }

		public required Action MouseExited { get; init; }
	}

	public class MethodName : MethodName
	{
		public static readonly StringName BumpFont = StringName.op_Implicit("BumpFont");

		public static readonly StringName RegisterPlayerState = StringName.op_Implicit("RegisterPlayerState");

		public static readonly StringName UnregisterPlayerState = StringName.op_Implicit("UnregisterPlayerState");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName _Input = StringName.op_Implicit("_Input");

		public static readonly StringName CreateRoot = StringName.op_Implicit("CreateRoot");

		public static readonly StringName CreateHoverPanel = StringName.op_Implicit("CreateHoverPanel");

		public static readonly StringName CreateDetailPanel = StringName.op_Implicit("CreateDetailPanel");

		public static readonly StringName ApplyPanelOpacity = StringName.op_Implicit("ApplyPanelOpacity");

		public static readonly StringName OnAnchorMouseEntered = StringName.op_Implicit("OnAnchorMouseEntered");

		public static readonly StringName OnAnchorMouseExited = StringName.op_Implicit("OnAnchorMouseExited");

		public static readonly StringName OnHoverPanelGuiInput = StringName.op_Implicit("OnHoverPanelGuiInput");

		public static readonly StringName ShowDetailPanel = StringName.op_Implicit("ShowDetailPanel");

		public static readonly StringName HideDetailPanel = StringName.op_Implicit("HideDetailPanel");

		public static readonly StringName RefreshHoverCard = StringName.op_Implicit("RefreshHoverCard");

		public static readonly StringName RefreshDetailPanel = StringName.op_Implicit("RefreshDetailPanel");

		public static readonly StringName CreateEmptyStateLabel = StringName.op_Implicit("CreateEmptyStateLabel");

		public static readonly StringName GetPlayerScreenLabel = StringName.op_Implicit("GetPlayerScreenLabel");

		public static readonly StringName GetPlayerState = StringName.op_Implicit("GetPlayerState");

		public static readonly StringName LoadTexture = StringName.op_Implicit("LoadTexture");

		public static readonly StringName RefreshLocalizedChrome = StringName.op_Implicit("RefreshLocalizedChrome");

		public static readonly StringName HideAllPanels = StringName.op_Implicit("HideAllPanels");

		public static readonly StringName PositionPanel = StringName.op_Implicit("PositionPanel");

		public static readonly StringName ShouldKeepPanelClusterVisible = StringName.op_Implicit("ShouldKeepPanelClusterVisible");

		public static readonly StringName ExpandRect = StringName.op_Implicit("ExpandRect");

		public static readonly StringName IsPointInsideVisiblePanel = StringName.op_Implicit("IsPointInsideVisiblePanel");

		public static readonly StringName OnSnapshotChanged = StringName.op_Implicit("OnSnapshotChanged");

		public static readonly StringName OnScreenChanged = StringName.op_Implicit("OnScreenChanged");

		public static readonly StringName CreateCompactPanelStyle = StringName.op_Implicit("CreateCompactPanelStyle");

		public static readonly StringName CreateDetailPanelStyle = StringName.op_Implicit("CreateDetailPanelStyle");

		public static readonly StringName CreatePreviewFrameStyle = StringName.op_Implicit("CreatePreviewFrameStyle");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _root = StringName.op_Implicit("_root");

		public static readonly StringName _hoverPanel = StringName.op_Implicit("_hoverPanel");

		public static readonly StringName _hoverHeadingLabel = StringName.op_Implicit("_hoverHeadingLabel");

		public static readonly StringName _hoverSummaryLabel = StringName.op_Implicit("_hoverSummaryLabel");

		public static readonly StringName _hoverPreviewRow = StringName.op_Implicit("_hoverPreviewRow");

		public static readonly StringName _hoverHintLabel = StringName.op_Implicit("_hoverHintLabel");

		public static readonly StringName _detailPanel = StringName.op_Implicit("_detailPanel");

		public static readonly StringName _detailHeadingLabel = StringName.op_Implicit("_detailHeadingLabel");

		public static readonly StringName _detailScreenLabel = StringName.op_Implicit("_detailScreenLabel");

		public static readonly StringName _detailDescriptionLabel = StringName.op_Implicit("_detailDescriptionLabel");

		public static readonly StringName _detailOptionsList = StringName.op_Implicit("_detailOptionsList");

		public static readonly StringName _detailCloseButton = StringName.op_Implicit("_detailCloseButton");

		public static readonly StringName _languageToken = StringName.op_Implicit("_languageToken");

		public static readonly StringName _hoverPlayerId = StringName.op_Implicit("_hoverPlayerId");

		public static readonly StringName _detailPlayerId = StringName.op_Implicit("_detailPlayerId");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly Vector2 HoverPanelAnchorOffset = new Vector2(-14f, 0f);

	private static readonly Vector2 DetailPanelAnchorOffset = new Vector2(-8f, 0f);

	private const int FontSizeBump = 1;

	private readonly Dictionary<ulong, AnchorBinding> _anchors = new Dictionary<ulong, AnchorBinding>();

	private readonly Dictionary<string, Texture2D?> _textureCache = new Dictionary<string, Texture2D>();

	private PartyObserverSettings _settings = new PartyObserverSettings();

	private Control? _root;

	private PanelContainer? _hoverPanel;

	private Label? _hoverHeadingLabel;

	private Label? _hoverSummaryLabel;

	private HBoxContainer? _hoverPreviewRow;

	private Label? _hoverHintLabel;

	private PanelContainer? _detailPanel;

	private Label? _detailHeadingLabel;

	private Label? _detailScreenLabel;

	private Label? _detailDescriptionLabel;

	private VBoxContainer? _detailOptionsList;

	private Button? _detailCloseButton;

	private string _languageToken = string.Empty;

	private ulong _hoverPlayerId;

	private ulong _detailPlayerId;

	private static int BumpFont(int size)
	{
		return size + 1;
	}

	internal void Initialize(PartyObserverSettings settings)
	{
		_settings = settings;
		_settings.Normalize();
		ApplyPanelOpacity();
	}

	internal void RegisterPlayerState(NMultiplayerPlayerState playerState)
	{
		INetGameService netService = RunManager.Instance.NetService;
		ulong? obj = ((netService != null) ? new ulong?(netService.NetId) : ((ulong?)null));
		if (playerState.Player.NetId != obj)
		{
			UnregisterPlayerState(playerState);
			ulong playerId = playerState.Player.NetId;
			AnchorBinding anchorBinding = new AnchorBinding
			{
				State = playerState,
				MouseEntered = delegate
				{
					OnAnchorMouseEntered(playerId);
				},
				MouseExited = delegate
				{
					OnAnchorMouseExited(playerId);
				}
			};
			((Control)playerState.Hitbox).MouseEntered += anchorBinding.MouseEntered;
			((Control)playerState.Hitbox).MouseExited += anchorBinding.MouseExited;
			_anchors[playerId] = anchorBinding;
			if (_hoverPlayerId == playerId)
			{
				RefreshHoverCard();
			}
			if (_detailPlayerId == playerId)
			{
				RefreshDetailPanel();
			}
		}
	}

	internal void UnregisterPlayerState(NMultiplayerPlayerState playerState)
	{
		ulong netId = playerState.Player.NetId;
		if (!_anchors.Remove(netId, out AnchorBinding value))
		{
			return;
		}
		if (GodotObject.IsInstanceValid((GodotObject)(object)value.State) && value.State.Hitbox != null)
		{
			((Control)value.State.Hitbox).MouseEntered -= value.MouseEntered;
			((Control)value.State.Hitbox).MouseExited -= value.MouseExited;
		}
		if (_hoverPlayerId == netId)
		{
			_hoverPlayerId = 0uL;
			if (_hoverPanel != null)
			{
				((CanvasItem)_hoverPanel).Visible = false;
			}
		}
		if (_detailPlayerId == netId)
		{
			HideAllPanels();
		}
	}

	public override void _Ready()
	{
		((CanvasLayer)this).Layer = 131;
		CreateRoot();
		CreateHoverPanel();
		CreateDetailPanel();
		ApplyPanelOpacity();
		RefreshLocalizedChrome();
		PartyObserverRegistry.SnapshotChanged += OnSnapshotChanged;
		if (RunManager.Instance.InputSynchronizer != null)
		{
			RunManager.Instance.InputSynchronizer.ScreenChanged += OnScreenChanged;
		}
		((Node)this).SetProcess(true);
	}

	public override void _ExitTree()
	{
		PartyObserverRegistry.SnapshotChanged -= OnSnapshotChanged;
		if (RunManager.Instance.InputSynchronizer != null)
		{
			RunManager.Instance.InputSynchronizer.ScreenChanged -= OnScreenChanged;
		}
		foreach (AnchorBinding item in _anchors.Values.ToList())
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)item.State) && item.State.Hitbox != null)
			{
				((Control)item.State.Hitbox).MouseEntered -= item.MouseEntered;
				((Control)item.State.Hitbox).MouseExited -= item.MouseExited;
			}
		}
		_anchors.Clear();
	}

	public override void _Process(double delta)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		RefreshLocalizedChrome();
		ulong num = ((_detailPlayerId != 0L) ? _detailPlayerId : _hoverPlayerId);
		if (num == 0)
		{
			return;
		}
		NMultiplayerPlayerState playerState = GetPlayerState(num);
		if (playerState == null)
		{
			HideAllPanels();
			return;
		}
		PanelContainer? hoverPanel = _hoverPanel;
		if (hoverPanel != null && ((CanvasItem)hoverPanel).Visible)
		{
			PositionPanel((Control)(object)_hoverPanel, (Control)(object)playerState, HoverPanelAnchorOffset);
		}
		PanelContainer? detailPanel = _detailPanel;
		if (detailPanel != null && ((CanvasItem)detailPanel).Visible)
		{
			PositionPanel((Control)(object)_detailPanel, (Control)(object)playerState, DetailPanelAnchorOffset);
		}
		PanelContainer? hoverPanel2 = _hoverPanel;
		if (hoverPanel2 == null || !((CanvasItem)hoverPanel2).Visible)
		{
			PanelContainer? detailPanel2 = _detailPanel;
			if (detailPanel2 == null || !((CanvasItem)detailPanel2).Visible)
			{
				return;
			}
		}
		if (!ShouldKeepPanelClusterVisible((Control)(object)playerState))
		{
			HideAllPanels();
		}
	}

	public override void _Input(InputEvent @event)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I8
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		InputEventMouseButton val = (InputEventMouseButton)(object)((@event is InputEventMouseButton) ? @event : null);
		if (val == null || (long)val.ButtonIndex != 1 || !val.Pressed)
		{
			return;
		}
		PanelContainer? hoverPanel = _hoverPanel;
		if (hoverPanel == null || !((CanvasItem)hoverPanel).Visible)
		{
			PanelContainer? detailPanel = _detailPanel;
			if (detailPanel == null || !((CanvasItem)detailPanel).Visible)
			{
				return;
			}
		}
		Vector2 position = ((InputEventMouse)val).Position;
		if (!IsPointInsideVisiblePanel((Control?)(object)_hoverPanel, position) && !IsPointInsideVisiblePanel((Control?)(object)_detailPanel, position))
		{
			HideAllPanels();
		}
	}

	private void CreateRoot()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		_root = new Control
		{
			Name = StringName.op_Implicit("PartyObserverRoot"),
			MouseFilter = (MouseFilterEnum)2
		};
		_root.SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Node)this).AddChild((Node)(object)_root, false, (InternalMode)0);
	}

	private void CreateHoverPanel()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Expected O, but got Unknown
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Expected O, but got Unknown
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Expected O, but got Unknown
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Expected O, but got Unknown
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		if (_root != null)
		{
			_hoverPanel = new PanelContainer
			{
				Name = StringName.op_Implicit("PartyObserverHoverCard"),
				Visible = false,
				MouseFilter = (MouseFilterEnum)0,
				CustomMinimumSize = new Vector2(248f, 0f)
			};
			((Control)_hoverPanel).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)CreateCompactPanelStyle());
			((Control)_hoverPanel).GuiInput += new GuiInputEventHandler(OnHoverPanelGuiInput);
			((Node)_root).AddChild((Node)(object)_hoverPanel, false, (InternalMode)0);
			MarginContainer val = new MarginContainer();
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_left"), 12);
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_top"), 10);
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 12);
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_bottom"), 10);
			((Node)_hoverPanel).AddChild((Node)(object)val, false, (InternalMode)0);
			VBoxContainer val2 = new VBoxContainer();
			((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
			((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
			_hoverHeadingLabel = new Label
			{
				MouseFilter = (MouseFilterEnum)2,
				AutowrapMode = (AutowrapMode)3
			};
			((Control)_hoverHeadingLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(13));
			((Control)_hoverHeadingLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), Colors.White);
			((Node)val2).AddChild((Node)(object)_hoverHeadingLabel, false, (InternalMode)0);
			_hoverSummaryLabel = new Label
			{
				MouseFilter = (MouseFilterEnum)2,
				AutowrapMode = (AutowrapMode)3
			};
			((Control)_hoverSummaryLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(11));
			((Control)_hoverSummaryLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color("D7E4F0"));
			((Node)val2).AddChild((Node)(object)_hoverSummaryLabel, false, (InternalMode)0);
			_hoverPreviewRow = new HBoxContainer
			{
				MouseFilter = (MouseFilterEnum)2
			};
			((Control)_hoverPreviewRow).AddThemeConstantOverride(StringName.op_Implicit("separation"), 6);
			((Node)val2).AddChild((Node)(object)_hoverPreviewRow, false, (InternalMode)0);
			_hoverHintLabel = new Label
			{
				Text = PartyObserverText.HoverHint(),
				MouseFilter = (MouseFilterEnum)2
			};
			((Control)_hoverHintLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(10));
			((Control)_hoverHintLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color("8DB1D4"));
			((Node)val2).AddChild((Node)(object)_hoverHintLabel, false, (InternalMode)0);
		}
	}

	private void CreateDetailPanel()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Expected O, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Expected O, but got Unknown
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Expected O, but got Unknown
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Expected O, but got Unknown
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Expected O, but got Unknown
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Expected O, but got Unknown
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Expected O, but got Unknown
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		if (_root != null)
		{
			_detailPanel = new PanelContainer
			{
				Name = StringName.op_Implicit("PartyObserverDetailPanel"),
				Visible = false,
				MouseFilter = (MouseFilterEnum)0,
				CustomMinimumSize = new Vector2(428f, 0f)
			};
			((Control)_detailPanel).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)CreateDetailPanelStyle());
			((Node)_root).AddChild((Node)(object)_detailPanel, false, (InternalMode)0);
			MarginContainer val = new MarginContainer();
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_left"), 14);
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_top"), 14);
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 14);
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_bottom"), 14);
			((Node)_detailPanel).AddChild((Node)(object)val, false, (InternalMode)0);
			VBoxContainer val2 = new VBoxContainer();
			((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
			((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
			HBoxContainer val3 = new HBoxContainer();
			((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
			((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
			_detailHeadingLabel = new Label
			{
				MouseFilter = (MouseFilterEnum)2,
				SizeFlagsHorizontal = (SizeFlags)3,
				AutowrapMode = (AutowrapMode)3
			};
			((Control)_detailHeadingLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(14));
			((Control)_detailHeadingLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), Colors.White);
			((Node)val3).AddChild((Node)(object)_detailHeadingLabel, false, (InternalMode)0);
			Button val4 = new Button
			{
				Text = PartyObserverText.Close(),
				Flat = true,
				FocusMode = (FocusModeEnum)0,
				MouseFilter = (MouseFilterEnum)0
			};
			((BaseButton)val4).Pressed += HideDetailPanel;
			((Control)val4).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(10));
			((Control)val4).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color("9FC5E9"));
			((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
			_detailCloseButton = val4;
			_detailScreenLabel = new Label
			{
				MouseFilter = (MouseFilterEnum)2
			};
			((Control)_detailScreenLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(11));
			((Control)_detailScreenLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color("9FC0DD"));
			((Node)val2).AddChild((Node)(object)_detailScreenLabel, false, (InternalMode)0);
			_detailDescriptionLabel = new Label
			{
				MouseFilter = (MouseFilterEnum)2,
				AutowrapMode = (AutowrapMode)3
			};
			((Control)_detailDescriptionLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(11));
			((Control)_detailDescriptionLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color("D8E6F2"));
			((Node)val2).AddChild((Node)(object)_detailDescriptionLabel, false, (InternalMode)0);
			ScrollContainer val5 = new ScrollContainer
			{
				MouseFilter = (MouseFilterEnum)0,
				CustomMinimumSize = new Vector2(0f, 280f),
				HorizontalScrollMode = (ScrollMode)0
			};
			((Node)val2).AddChild((Node)(object)val5, false, (InternalMode)0);
			_detailOptionsList = new VBoxContainer
			{
				MouseFilter = (MouseFilterEnum)1
			};
			((Control)_detailOptionsList).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
			((Control)_detailOptionsList).CustomMinimumSize = new Vector2(392f, 0f);
			((Node)val5).AddChild((Node)(object)_detailOptionsList, false, (InternalMode)0);
		}
	}

	private void ApplyPanelOpacity()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		if (_hoverPanel != null)
		{
			((CanvasItem)_hoverPanel).SelfModulate = new Color(1f, 1f, 1f, _settings.PanelOpacity);
		}
		if (_detailPanel != null)
		{
			((CanvasItem)_detailPanel).SelfModulate = new Color(1f, 1f, 1f, _settings.PanelOpacity);
		}
	}

	private void OnAnchorMouseEntered(ulong playerId)
	{
		_hoverPlayerId = playerId;
		if (_detailPlayerId != 0L && _detailPlayerId != playerId)
		{
			_detailPlayerId = 0uL;
			if (_detailPanel != null)
			{
				((CanvasItem)_detailPanel).Visible = false;
			}
		}
		RefreshHoverCard();
		if (_hoverPanel != null)
		{
			((CanvasItem)_hoverPanel).Visible = _detailPlayerId != playerId;
		}
	}

	private void OnAnchorMouseExited(ulong playerId)
	{
	}

	private void OnHoverPanelGuiInput(InputEvent @event)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I8
		InputEventMouseButton val = (InputEventMouseButton)(object)((@event is InputEventMouseButton) ? @event : null);
		if (val != null && (long)val.ButtonIndex == 1 && val.Pressed && _hoverPlayerId != 0)
		{
			ShowDetailPanel(_hoverPlayerId);
			((Node)this).GetViewport().SetInputAsHandled();
		}
	}

	private void ShowDetailPanel(ulong playerId)
	{
		_hoverPlayerId = playerId;
		_detailPlayerId = playerId;
		RefreshDetailPanel();
		if (_detailPanel != null)
		{
			((CanvasItem)_detailPanel).Visible = true;
		}
		if (_hoverPanel != null && _hoverPlayerId == playerId)
		{
			((CanvasItem)_hoverPanel).Visible = false;
		}
	}

	private void HideDetailPanel()
	{
		_detailPlayerId = 0uL;
		if (_detailPanel != null)
		{
			((CanvasItem)_detailPanel).Visible = false;
		}
		NMultiplayerPlayerState playerState = GetPlayerState(_hoverPlayerId);
		if (_hoverPanel != null && playerState != null)
		{
			((CanvasItem)_hoverPanel).Visible = ShouldKeepPanelClusterVisible((Control)(object)playerState);
		}
	}

	private void RefreshHoverCard()
	{
		NMultiplayerPlayerState playerState = GetPlayerState(_hoverPlayerId);
		if (playerState != null && _hoverHeadingLabel != null && _hoverSummaryLabel != null)
		{
			Player player = playerState.Player;
			PartyObserverChoiceSnapshot snapshot = PartyObserverRegistry.GetSnapshot(player.NetId);
			_hoverHeadingLabel.Text = BuildPlayerHeading(player);
			_hoverSummaryLabel.Text = BuildHoverSummary(player.NetId, snapshot);
			PopulateHoverPreview(snapshot);
		}
	}

	private void RefreshDetailPanel()
	{
		if (_detailHeadingLabel == null || _detailScreenLabel == null || _detailDescriptionLabel == null || _detailOptionsList == null)
		{
			return;
		}
		NMultiplayerPlayerState playerState = GetPlayerState(_detailPlayerId);
		if (playerState == null)
		{
			HideDetailPanel();
			return;
		}
		Player player = playerState.Player;
		PartyObserverChoiceSnapshot snapshot = PartyObserverRegistry.GetSnapshot(player.NetId);
		_detailHeadingLabel.Text = BuildPlayerHeading(player);
		_detailScreenLabel.Text = PartyObserverText.FormatCurrentScreen(GetPlayerScreenLabel(player.NetId));
		if (snapshot == null)
		{
			_detailDescriptionLabel.Text = PartyObserverText.NoSnapshot();
			PopulateOptionCards(null);
			return;
		}
		List<string> list = new List<string>();
		string snapshotSummaryTitle = GetSnapshotSummaryTitle(snapshot);
		if (!string.IsNullOrWhiteSpace(snapshotSummaryTitle))
		{
			list.Add(snapshotSummaryTitle);
		}
		string snapshotSummaryDescription = GetSnapshotSummaryDescription(snapshot);
		if (!string.IsNullOrWhiteSpace(snapshotSummaryDescription))
		{
			list.Add(snapshotSummaryDescription);
		}
		_detailDescriptionLabel.Text = ((list.Count == 0) ? PartyObserverText.NoExtraDetails() : string.Join("\n", list));
		PopulateOptionCards(snapshot);
	}

	private void PopulateHoverPreview(PartyObserverChoiceSnapshot? snapshot)
	{
		if (_hoverPreviewRow == null)
		{
			return;
		}
		foreach (Node child in ((Node)_hoverPreviewRow).GetChildren(false))
		{
			child.QueueFree();
		}
		if (snapshot == null)
		{
			((CanvasItem)_hoverPreviewRow).Visible = false;
			return;
		}
		foreach (PartyObserverChoiceOption item in snapshot.Options.Take(3))
		{
			((Node)_hoverPreviewRow).AddChild((Node)(object)CreatePreviewChip(item), false, (InternalMode)0);
		}
		((CanvasItem)_hoverPreviewRow).Visible = ((Node)_hoverPreviewRow).GetChildCount(false) > 0;
	}

	private void PopulateOptionCards(PartyObserverChoiceSnapshot? snapshot)
	{
		if (_detailOptionsList == null)
		{
			return;
		}
		foreach (Node child in ((Node)_detailOptionsList).GetChildren(false))
		{
			child.QueueFree();
		}
		if (snapshot == null || snapshot.Options.Count == 0)
		{
			((Node)_detailOptionsList).AddChild((Node)(object)CreateEmptyStateLabel((snapshot == null) ? PartyObserverText.NoSyncedOptions() : PartyObserverText.SnapshotHasNoVisibleOptions()), false, (InternalMode)0);
			return;
		}
		foreach (PartyObserverChoiceOption option in snapshot.Options)
		{
			((Node)_detailOptionsList).AddChild((Node)(object)CreateOptionCard(option), false, (InternalMode)0);
		}
	}

	private Control CreatePreviewChip(PartyObserverChoiceOption option)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = LoadTexture(option.ImagePath);
		if (val != null)
		{
			TextureRect val2 = new TextureRect
			{
				Texture = val,
				CustomMinimumSize = GetPreviewImageSize(option),
				MouseFilter = (MouseFilterEnum)2,
				ExpandMode = (ExpandModeEnum)1,
				StretchMode = (StretchModeEnum)5
			};
			PanelContainer val3 = new PanelContainer
			{
				MouseFilter = (MouseFilterEnum)2
			};
			((Control)val3).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)CreatePreviewFrameStyle());
			((Node)val3).AddChild((Node)(object)val2, false, (InternalMode)0);
			return (Control)(object)val3;
		}
		Label val4 = new Label
		{
			Text = (string.IsNullOrWhiteSpace(option.Tag) ? "?" : PartyObserverText.LocalizeTag(option.Tag)),
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val4).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(10));
		((Control)val4).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color("CBE1F4"));
		return (Control)(object)val4;
	}

	private Control CreateOptionCard(PartyObserverChoiceOption option)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Expected O, but got Unknown
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Expected O, but got Unknown
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Expected O, but got Unknown
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Expected O, but got Unknown
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		PanelContainer val = new PanelContainer
		{
			MouseFilter = (MouseFilterEnum)2,
			SelfModulate = (Color)(option.IsDisabled ? new Color(1f, 1f, 1f, 0.65f) : Colors.White)
		};
		((Control)val).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)CreateOptionStyle(option));
		MarginContainer val2 = new MarginContainer
		{
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_left"), 10);
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_top"), 10);
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 10);
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_bottom"), 10);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		HBoxContainer val3 = new HBoxContainer
		{
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
		((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
		Texture2D val4 = LoadTexture(option.ImagePath);
		if (val4 != null)
		{
			TextureRect val5 = new TextureRect
			{
				Texture = val4,
				CustomMinimumSize = GetOptionImageSize(option),
				MouseFilter = (MouseFilterEnum)2,
				ExpandMode = (ExpandModeEnum)1,
				StretchMode = (StretchModeEnum)5
			};
			((Node)val3).AddChild((Node)(object)val5, false, (InternalMode)0);
		}
		VBoxContainer val6 = new VBoxContainer
		{
			MouseFilter = (MouseFilterEnum)2,
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val6).AddThemeConstantOverride(StringName.op_Implicit("separation"), 4);
		((Node)val3).AddChild((Node)(object)val6, false, (InternalMode)0);
		Label val7 = new Label
		{
			Text = (string.IsNullOrWhiteSpace(option.Tag) ? option.Title : (PartyObserverText.LocalizeTag(option.Tag) + " - " + option.Title)),
			AutowrapMode = (AutowrapMode)3,
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val7).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(12));
		((Control)val7).AddThemeColorOverride(StringName.op_Implicit("font_color"), Colors.White);
		((Node)val6).AddChild((Node)(object)val7, false, (InternalMode)0);
		if (!string.IsNullOrWhiteSpace(option.Subtitle))
		{
			Label val8 = new Label
			{
				Text = option.Subtitle,
				AutowrapMode = (AutowrapMode)3,
				MouseFilter = (MouseFilterEnum)2
			};
			((Control)val8).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(10));
			((Control)val8).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color("9BC0DD"));
			((Node)val6).AddChild((Node)(object)val8, false, (InternalMode)0);
		}
		if (!string.IsNullOrWhiteSpace(option.Description))
		{
			MegaRichTextLabel val9 = new MegaRichTextLabel
			{
				BbcodeEnabled = true,
				FitContent = true,
				ScrollActive = false,
				AutoSizeEnabled = false,
				AutowrapMode = (AutowrapMode)3,
				MouseFilter = (MouseFilterEnum)2,
				SizeFlagsHorizontal = (SizeFlags)3
			};
			StringName[] allFontSizes = RichTextLabel.allFontSizes;
			foreach (StringName val10 in allFontSizes)
			{
				((Control)val9).AddThemeFontSizeOverride(val10, BumpFont(10));
			}
			((Control)val9).AddThemeConstantOverride(RichTextLabel.lineSpacing, 1);
			((Control)val9).AddThemeColorOverride(RichTextLabel.defaultColor, new Color("D6E4F0"));
			((Node)val6).AddChild((Node)(object)val9, false, (InternalMode)0);
			val9.Text = option.Description;
		}
		return (Control)(object)val;
	}

	private Label CreateEmptyStateLabel(string text)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		Label val = new Label
		{
			Text = text,
			AutowrapMode = (AutowrapMode)3,
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), BumpFont(11));
		((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color("B8CBDE"));
		return val;
	}

	private string BuildHoverSummary(ulong playerId, PartyObserverChoiceSnapshot? snapshot)
	{
		List<string> list = new List<string> { PartyObserverText.FormatCurrentScreen(GetPlayerScreenLabel(playerId)) };
		if (snapshot == null)
		{
			list.Add(PartyObserverText.NoSyncedChoiceDetails());
			return string.Join("\n", list);
		}
		string snapshotSummaryTitle = GetSnapshotSummaryTitle(snapshot);
		if (!string.IsNullOrWhiteSpace(snapshotSummaryTitle))
		{
			list.Add(snapshotSummaryTitle);
		}
		list.Add((snapshot.Options.Count > 0) ? PartyObserverText.FormatSyncedOptionsAvailable(snapshot.Options.Count) : PartyObserverText.NoVisibleOptionsInSnapshot());
		return string.Join("\n", list);
	}

	private string BuildPlayerHeading(Player player)
	{
		IPlayerCollection val = (IPlayerCollection)(object)RunManager.Instance.DebugOnlyGetState();
		if (val != null)
		{
			int playerSlotIndex = val.GetPlayerSlotIndex(player);
			if (playerSlotIndex >= 0)
			{
				return $"P{playerSlotIndex + 1} {player.Character.Title.GetRawText()}";
			}
		}
		return player.Character.Title.GetRawText();
	}

	private string GetPlayerScreenLabel(ulong playerId)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		PartyObserverChoiceSnapshot snapshot = PartyObserverRegistry.GetSnapshot(playerId);
		if (snapshot != null && snapshot.Kind != PartyObserverChoiceSnapshotKind.None)
		{
			return snapshot.Kind.GetDisplayName();
		}
		if (!string.IsNullOrWhiteSpace(snapshot?.ScreenLabel))
		{
			return snapshot.ScreenLabel;
		}
		PeerInputSynchronizer inputSynchronizer = RunManager.Instance.InputSynchronizer;
		return ((inputSynchronizer != null) ? inputSynchronizer.GetScreenType(playerId).GetDisplayName() : null) ?? PartyObserverText.Unknown();
	}

	private NMultiplayerPlayerState? GetPlayerState(ulong playerId)
	{
		if (playerId == 0L || !_anchors.TryGetValue(playerId, out AnchorBinding value))
		{
			return null;
		}
		if (!GodotObject.IsInstanceValid((GodotObject)(object)value.State) || !((Node)value.State).IsInsideTree())
		{
			_anchors.Remove(playerId);
			return null;
		}
		return value.State;
	}

	private Texture2D? LoadTexture(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		if (_textureCache.TryGetValue(path, out Texture2D value))
		{
			return value;
		}
		Texture2D val = null;
		if (ResourceLoader.Exists(path, ""))
		{
			val = ResourceLoader.Load<Texture2D>(path, (string)null, (CacheMode)1);
		}
		_textureCache[path] = val;
		return val;
	}

	private void RefreshLocalizedChrome()
	{
		string text = PartyObserverText.CurrentLanguageToken();
		if (!(_languageToken == text))
		{
			_languageToken = text;
			if (_hoverHintLabel != null)
			{
				_hoverHintLabel.Text = PartyObserverText.HoverHint();
			}
			if (_detailCloseButton != null)
			{
				_detailCloseButton.Text = PartyObserverText.Close();
			}
			if (_hoverPlayerId != 0)
			{
				RefreshHoverCard();
			}
			if (_detailPlayerId != 0)
			{
				RefreshDetailPanel();
			}
		}
	}

	private void HideAllPanels()
	{
		_hoverPlayerId = 0uL;
		_detailPlayerId = 0uL;
		if (_hoverPanel != null)
		{
			((CanvasItem)_hoverPanel).Visible = false;
		}
		if (_detailPanel != null)
		{
			((CanvasItem)_detailPanel).Visible = false;
		}
	}

	private void PositionPanel(Control panel, Control anchor, Vector2 offset)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		Rect2 visibleRect = ((Node)this).GetViewport().GetVisibleRect();
		Vector2 size = ((Rect2)(ref visibleRect)).Size;
		Vector2 val = anchor.GlobalPosition + new Vector2(anchor.Size.X + offset.X, offset.Y);
		float num = val.X;
		float y = val.Y;
		if (num + panel.Size.X > size.X - 8f)
		{
			num = anchor.GlobalPosition.X - panel.Size.X - 12f;
		}
		num = Mathf.Clamp(num, 8f, Math.Max(8f, size.X - panel.Size.X - 8f));
		y = Mathf.Clamp(y, 8f, Math.Max(8f, size.Y - panel.Size.Y - 8f));
		panel.GlobalPosition = new Vector2(num, y);
	}

	private bool ShouldKeepPanelClusterVisible(Control anchor)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		Vector2 mousePosition = ((Node)this).GetViewport().GetMousePosition();
		Rect2 val = ExpandRect(anchor.GetGlobalRect(), 28f);
		PanelContainer? hoverPanel = _hoverPanel;
		if (hoverPanel != null && ((CanvasItem)hoverPanel).Visible)
		{
			val = ((Rect2)(ref val)).Merge(ExpandRect(((Control)_hoverPanel).GetGlobalRect(), 36f));
		}
		PanelContainer? detailPanel = _detailPanel;
		if (detailPanel != null && ((CanvasItem)detailPanel).Visible)
		{
			val = ((Rect2)(ref val)).Merge(ExpandRect(((Control)_detailPanel).GetGlobalRect(), 40f));
		}
		return ((Rect2)(ref val)).HasPoint(mousePosition);
	}

	private static Rect2 ExpandRect(Rect2 rect, float padding)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(padding, padding);
		return new Rect2(((Rect2)(ref rect)).Position - val, ((Rect2)(ref rect)).Size + val * 2f);
	}

	private static bool IsPointInsideVisiblePanel(Control? panel, Vector2 point)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		int result;
		if (panel != null && ((CanvasItem)panel).Visible)
		{
			Rect2 globalRect = panel.GetGlobalRect();
			result = (((Rect2)(ref globalRect)).HasPoint(point) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	private static string GetSnapshotSummaryTitle(PartyObserverChoiceSnapshot snapshot)
	{
		PartyObserverChoiceSnapshotKind kind = snapshot.Kind;
		if (1 == 0)
		{
		}
		string result = kind switch
		{
			PartyObserverChoiceSnapshotKind.Rewards => PartyObserverText.ReviewingRewards(), 
			PartyObserverChoiceSnapshotKind.RelicSelection => PartyObserverText.ChoosingRelic(), 
			PartyObserverChoiceSnapshotKind.CardRewardSelection => PartyObserverText.ChoosingCard(), 
			_ => snapshot.Title, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetSnapshotSummaryDescription(PartyObserverChoiceSnapshot snapshot)
	{
		PartyObserverChoiceSnapshotKind kind = snapshot.Kind;
		if (1 == 0)
		{
		}
		string result = kind switch
		{
			PartyObserverChoiceSnapshotKind.Rewards => PartyObserverText.FormatRewardsCount(snapshot.Options.Count), 
			PartyObserverChoiceSnapshotKind.RelicSelection => PartyObserverText.FormatRelicOptionsCount(snapshot.Options.Count), 
			PartyObserverChoiceSnapshotKind.CardRewardSelection => PartyObserverText.FormatCardOptionsCount(snapshot.Options.Count), 
			PartyObserverChoiceSnapshotKind.EventChoices => PartyObserverText.FormatEventOptionsCount(snapshot.Options.Count), 
			_ => snapshot.Description, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static Vector2 GetPreviewImageSize(PartyObserverChoiceOption option)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		return PartyObserverText.IsCardTag(option.Tag) ? new Vector2(34f, 46f) : new Vector2(36f, 36f);
	}

	private static Vector2 GetOptionImageSize(PartyObserverChoiceOption option)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		return PartyObserverText.IsCardTag(option.Tag) ? new Vector2(78f, 104f) : new Vector2(56f, 56f);
	}

	private void OnSnapshotChanged(ulong playerId)
	{
		if (_hoverPlayerId == playerId)
		{
			RefreshHoverCard();
		}
		if (_detailPlayerId == playerId)
		{
			RefreshDetailPanel();
		}
	}

	private void OnScreenChanged(ulong playerId, NetScreenType _)
	{
		if (_hoverPlayerId == playerId)
		{
			RefreshHoverCard();
		}
		if (_detailPlayerId == playerId)
		{
			RefreshDetailPanel();
		}
	}

	private static StyleBoxFlat CreateCompactPanelStyle()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		return new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.09f, 0.14f, 0.97f),
			BorderColor = new Color(0.34f, 0.49f, 0.63f, 0.82f),
			BorderWidthTop = 1,
			BorderWidthBottom = 1,
			BorderWidthLeft = 1,
			BorderWidthRight = 1,
			CornerRadiusTopLeft = 12,
			CornerRadiusTopRight = 12,
			CornerRadiusBottomLeft = 12,
			CornerRadiusBottomRight = 12,
			ShadowColor = new Color(0f, 0f, 0f, 0.24f),
			ShadowSize = 5
		};
	}

	private static StyleBoxFlat CreateDetailPanelStyle()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		return new StyleBoxFlat
		{
			BgColor = new Color(0.05f, 0.08f, 0.12f, 0.98f),
			BorderColor = new Color(0.36f, 0.5f, 0.64f, 0.88f),
			BorderWidthTop = 1,
			BorderWidthBottom = 1,
			BorderWidthLeft = 1,
			BorderWidthRight = 1,
			CornerRadiusTopLeft = 14,
			CornerRadiusTopRight = 14,
			CornerRadiusBottomLeft = 14,
			CornerRadiusBottomRight = 14,
			ShadowColor = new Color(0f, 0f, 0f, 0.28f),
			ShadowSize = 7
		};
	}

	private static StyleBoxFlat CreatePreviewFrameStyle()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected O, but got Unknown
		return new StyleBoxFlat
		{
			BgColor = new Color(0.11f, 0.15f, 0.21f, 0.92f),
			BorderColor = new Color(0.33f, 0.45f, 0.58f, 0.7f),
			BorderWidthTop = 1,
			BorderWidthBottom = 1,
			BorderWidthLeft = 1,
			BorderWidthRight = 1,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6
		};
	}

	private static StyleBoxFlat CreateOptionStyle(PartyObserverChoiceOption option)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		Color borderColor = (option.IsDisabled ? new Color("A78A8A") : (PartyObserverText.IsRelicTag(option.Tag) ? new Color("E2C26F") : (PartyObserverText.IsCardTag(option.Tag) ? new Color("6CA7D8") : (PartyObserverText.IsProceedTag(option.Tag) ? new Color("B7F0A1") : new Color("6A87A6")))));
		return new StyleBoxFlat
		{
			BgColor = new Color(0.11f, 0.14f, 0.2f, 0.95f),
			BorderColor = borderColor,
			BorderWidthTop = 1,
			BorderWidthBottom = 1,
			BorderWidthLeft = 1,
			BorderWidthRight = 1,
			CornerRadiusTopLeft = 10,
			CornerRadiusTopRight = 10,
			CornerRadiusBottomLeft = 10,
			CornerRadiusBottomRight = 10
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected O, but got Unknown
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Expected O, but got Unknown
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Expected O, but got Unknown
		//IL_03ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		//IL_0481: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_050d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0518: Expected O, but got Unknown
		//IL_0513: Unknown result type (might be due to invalid IL or missing references)
		//IL_0536: Unknown result type (might be due to invalid IL or missing references)
		//IL_0542: Unknown result type (might be due to invalid IL or missing references)
		//IL_0569: Unknown result type (might be due to invalid IL or missing references)
		//IL_058c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0598: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cf: Expected O, but got Unknown
		//IL_05ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0625: Unknown result type (might be due to invalid IL or missing references)
		//IL_0630: Expected O, but got Unknown
		//IL_062b: Unknown result type (might be due to invalid IL or missing references)
		//IL_064e: Unknown result type (might be due to invalid IL or missing references)
		//IL_065a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0681: Unknown result type (might be due to invalid IL or missing references)
		//IL_068a: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0709: Unknown result type (might be due to invalid IL or missing references)
		//IL_0714: Expected O, but got Unknown
		//IL_070f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0736: Unknown result type (might be due to invalid IL or missing references)
		//IL_0741: Expected O, but got Unknown
		//IL_073c: Unknown result type (might be due to invalid IL or missing references)
		//IL_075e: Unknown result type (might be due to invalid IL or missing references)
		//IL_076a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0791: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c4: Expected O, but got Unknown
		//IL_07bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_07cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0816: Unknown result type (might be due to invalid IL or missing references)
		//IL_0838: Unknown result type (might be due to invalid IL or missing references)
		//IL_0844: Unknown result type (might be due to invalid IL or missing references)
		//IL_086b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0894: Unknown result type (might be due to invalid IL or missing references)
		//IL_089f: Expected O, but got Unknown
		//IL_089a: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0912: Unknown result type (might be due to invalid IL or missing references)
		//IL_091e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0945: Unknown result type (might be due to invalid IL or missing references)
		//IL_0968: Unknown result type (might be due to invalid IL or missing references)
		//IL_098a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0996: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_09cd: Expected O, but got Unknown
		//IL_09c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_09fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a09: Expected O, but got Unknown
		//IL_0a04: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a0e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a3a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a45: Expected O, but got Unknown
		//IL_0a40: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4a: Unknown result type (might be due to invalid IL or missing references)
		List<MethodInfo> list = new List<MethodInfo>(33);
		list.Add(new MethodInfo(MethodName.BumpFont, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, StringName.op_Implicit("size"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.RegisterPlayerState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, StringName.op_Implicit("playerState"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.UnregisterPlayerState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, StringName.op_Implicit("playerState"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.CreateRoot, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.CreateHoverPanel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.CreateDetailPanel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.ApplyPanelOpacity, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.OnAnchorMouseEntered, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, StringName.op_Implicit("playerId"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.OnAnchorMouseExited, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, StringName.op_Implicit("playerId"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.OnHoverPanelGuiInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.ShowDetailPanel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, StringName.op_Implicit("playerId"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.HideDetailPanel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.RefreshHoverCard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.RefreshDetailPanel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.CreateEmptyStateLabel, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Label"), false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.GetPlayerScreenLabel, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, StringName.op_Implicit("playerId"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.GetPlayerState, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, StringName.op_Implicit("playerId"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.LoadTexture, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Texture2D"), false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)4, StringName.op_Implicit("path"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.RefreshLocalizedChrome, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.HideAllPanels, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.PositionPanel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, StringName.op_Implicit("panel"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
			new PropertyInfo((Type)24, StringName.op_Implicit("anchor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
			new PropertyInfo((Type)5, StringName.op_Implicit("offset"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.ShouldKeepPanelClusterVisible, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, StringName.op_Implicit("anchor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.ExpandRect, new PropertyInfo((Type)7, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
		{
			new PropertyInfo((Type)7, StringName.op_Implicit("rect"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
			new PropertyInfo((Type)3, StringName.op_Implicit("padding"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.IsPointInsideVisiblePanel, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, StringName.op_Implicit("panel"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
			new PropertyInfo((Type)5, StringName.op_Implicit("point"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.OnSnapshotChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, StringName.op_Implicit("playerId"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.OnScreenChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, StringName.op_Implicit("playerId"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
			new PropertyInfo((Type)2, StringName.op_Implicit("_"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
		}, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.CreateCompactPanelStyle, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("StyleBoxFlat"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.CreateDetailPanelStyle, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("StyleBoxFlat"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null));
		list.Add(new MethodInfo(MethodName.CreatePreviewFrameStyle, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("StyleBoxFlat"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null));
		return list;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0400: Unknown result type (might be due to invalid IL or missing references)
		//IL_0436: Unknown result type (might be due to invalid IL or missing references)
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0481: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0510: Unknown result type (might be due to invalid IL or missing references)
		//IL_0515: Unknown result type (might be due to invalid IL or missing references)
		//IL_055a: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0594: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_061b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0627: Unknown result type (might be due to invalid IL or missing references)
		//IL_066d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0672: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06be: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_070a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0719: Unknown result type (might be due to invalid IL or missing references)
		//IL_071e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0760: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0821: Unknown result type (might be due to invalid IL or missing references)
		//IL_0826: Unknown result type (might be due to invalid IL or missing references)
		//IL_086a: Unknown result type (might be due to invalid IL or missing references)
		//IL_085a: Unknown result type (might be due to invalid IL or missing references)
		//IL_085f: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.BumpFont && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			int num = BumpFont(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<int>(ref num);
			return true;
		}
		if ((ref method) == MethodName.RegisterPlayerState && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			RegisterPlayerState(VariantUtils.ConvertTo<NMultiplayerPlayerState>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UnregisterPlayerState && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UnregisterPlayerState(VariantUtils.ConvertTo<NMultiplayerPlayerState>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateRoot && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			CreateRoot();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateHoverPanel && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			CreateHoverPanel();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateDetailPanel && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			CreateDetailPanel();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ApplyPanelOpacity && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ApplyPanelOpacity();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnAnchorMouseEntered && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnAnchorMouseEntered(VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnAnchorMouseExited && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnAnchorMouseExited(VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnHoverPanelGuiInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnHoverPanelGuiInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ShowDetailPanel && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ShowDetailPanel(VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.HideDetailPanel && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			HideDetailPanel();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshHoverCard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshHoverCard();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshDetailPanel && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshDetailPanel();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateEmptyStateLabel && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Label val = CreateEmptyStateLabel(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Label>(ref val);
			return true;
		}
		if ((ref method) == MethodName.GetPlayerScreenLabel && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string playerScreenLabel = GetPlayerScreenLabel(VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref playerScreenLabel);
			return true;
		}
		if ((ref method) == MethodName.GetPlayerState && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			NMultiplayerPlayerState playerState = GetPlayerState(VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<NMultiplayerPlayerState>(ref playerState);
			return true;
		}
		if ((ref method) == MethodName.LoadTexture && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Texture2D val2 = LoadTexture(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Texture2D>(ref val2);
			return true;
		}
		if ((ref method) == MethodName.RefreshLocalizedChrome && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshLocalizedChrome();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.HideAllPanels && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			HideAllPanels();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.PositionPanel && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			PositionPanel(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ShouldKeepPanelClusterVisible && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = ShouldKeepPanelClusterVisible(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.ExpandRect && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			Rect2 val3 = ExpandRect(VariantUtils.ConvertTo<Rect2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<Rect2>(ref val3);
			return true;
		}
		if ((ref method) == MethodName.IsPointInsideVisiblePanel && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			bool flag2 = IsPointInsideVisiblePanel(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<bool>(ref flag2);
			return true;
		}
		if ((ref method) == MethodName.OnSnapshotChanged && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnSnapshotChanged(VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnScreenChanged && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			OnScreenChanged(VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<NetScreenType>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateCompactPanelStyle && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StyleBoxFlat val4 = CreateCompactPanelStyle();
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(ref val4);
			return true;
		}
		if ((ref method) == MethodName.CreateDetailPanelStyle && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StyleBoxFlat val5 = CreateDetailPanelStyle();
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(ref val5);
			return true;
		}
		if ((ref method) == MethodName.CreatePreviewFrameStyle && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StyleBoxFlat val6 = CreatePreviewFrameStyle();
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(ref val6);
			return true;
		}
		return ((CanvasLayer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.BumpFont && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			int num = BumpFont(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<int>(ref num);
			return true;
		}
		if ((ref method) == MethodName.ExpandRect && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			Rect2 val = ExpandRect(VariantUtils.ConvertTo<Rect2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<Rect2>(ref val);
			return true;
		}
		if ((ref method) == MethodName.IsPointInsideVisiblePanel && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			bool flag = IsPointInsideVisiblePanel(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.CreateCompactPanelStyle && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StyleBoxFlat val2 = CreateCompactPanelStyle();
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(ref val2);
			return true;
		}
		if ((ref method) == MethodName.CreateDetailPanelStyle && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StyleBoxFlat val3 = CreateDetailPanelStyle();
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(ref val3);
			return true;
		}
		if ((ref method) == MethodName.CreatePreviewFrameStyle && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StyleBoxFlat val4 = CreatePreviewFrameStyle();
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(ref val4);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.BumpFont)
		{
			return true;
		}
		if ((ref method) == MethodName.RegisterPlayerState)
		{
			return true;
		}
		if ((ref method) == MethodName.UnregisterPlayerState)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName._Input)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateRoot)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateHoverPanel)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateDetailPanel)
		{
			return true;
		}
		if ((ref method) == MethodName.ApplyPanelOpacity)
		{
			return true;
		}
		if ((ref method) == MethodName.OnAnchorMouseEntered)
		{
			return true;
		}
		if ((ref method) == MethodName.OnAnchorMouseExited)
		{
			return true;
		}
		if ((ref method) == MethodName.OnHoverPanelGuiInput)
		{
			return true;
		}
		if ((ref method) == MethodName.ShowDetailPanel)
		{
			return true;
		}
		if ((ref method) == MethodName.HideDetailPanel)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshHoverCard)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshDetailPanel)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateEmptyStateLabel)
		{
			return true;
		}
		if ((ref method) == MethodName.GetPlayerScreenLabel)
		{
			return true;
		}
		if ((ref method) == MethodName.GetPlayerState)
		{
			return true;
		}
		if ((ref method) == MethodName.LoadTexture)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshLocalizedChrome)
		{
			return true;
		}
		if ((ref method) == MethodName.HideAllPanels)
		{
			return true;
		}
		if ((ref method) == MethodName.PositionPanel)
		{
			return true;
		}
		if ((ref method) == MethodName.ShouldKeepPanelClusterVisible)
		{
			return true;
		}
		if ((ref method) == MethodName.ExpandRect)
		{
			return true;
		}
		if ((ref method) == MethodName.IsPointInsideVisiblePanel)
		{
			return true;
		}
		if ((ref method) == MethodName.OnSnapshotChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.OnScreenChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateCompactPanelStyle)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateDetailPanelStyle)
		{
			return true;
		}
		if ((ref method) == MethodName.CreatePreviewFrameStyle)
		{
			return true;
		}
		return ((CanvasLayer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._root)
		{
			_root = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hoverPanel)
		{
			_hoverPanel = VariantUtils.ConvertTo<PanelContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hoverHeadingLabel)
		{
			_hoverHeadingLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hoverSummaryLabel)
		{
			_hoverSummaryLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hoverPreviewRow)
		{
			_hoverPreviewRow = VariantUtils.ConvertTo<HBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hoverHintLabel)
		{
			_hoverHintLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._detailPanel)
		{
			_detailPanel = VariantUtils.ConvertTo<PanelContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._detailHeadingLabel)
		{
			_detailHeadingLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._detailScreenLabel)
		{
			_detailScreenLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._detailDescriptionLabel)
		{
			_detailDescriptionLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._detailOptionsList)
		{
			_detailOptionsList = VariantUtils.ConvertTo<VBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._detailCloseButton)
		{
			_detailCloseButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._languageToken)
		{
			_languageToken = VariantUtils.ConvertTo<string>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hoverPlayerId)
		{
			_hoverPlayerId = VariantUtils.ConvertTo<ulong>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._detailPlayerId)
		{
			_detailPlayerId = VariantUtils.ConvertTo<ulong>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._root)
		{
			value = VariantUtils.CreateFrom<Control>(ref _root);
			return true;
		}
		if ((ref name) == PropertyName._hoverPanel)
		{
			value = VariantUtils.CreateFrom<PanelContainer>(ref _hoverPanel);
			return true;
		}
		if ((ref name) == PropertyName._hoverHeadingLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _hoverHeadingLabel);
			return true;
		}
		if ((ref name) == PropertyName._hoverSummaryLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _hoverSummaryLabel);
			return true;
		}
		if ((ref name) == PropertyName._hoverPreviewRow)
		{
			value = VariantUtils.CreateFrom<HBoxContainer>(ref _hoverPreviewRow);
			return true;
		}
		if ((ref name) == PropertyName._hoverHintLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _hoverHintLabel);
			return true;
		}
		if ((ref name) == PropertyName._detailPanel)
		{
			value = VariantUtils.CreateFrom<PanelContainer>(ref _detailPanel);
			return true;
		}
		if ((ref name) == PropertyName._detailHeadingLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _detailHeadingLabel);
			return true;
		}
		if ((ref name) == PropertyName._detailScreenLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _detailScreenLabel);
			return true;
		}
		if ((ref name) == PropertyName._detailDescriptionLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _detailDescriptionLabel);
			return true;
		}
		if ((ref name) == PropertyName._detailOptionsList)
		{
			value = VariantUtils.CreateFrom<VBoxContainer>(ref _detailOptionsList);
			return true;
		}
		if ((ref name) == PropertyName._detailCloseButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref _detailCloseButton);
			return true;
		}
		if ((ref name) == PropertyName._languageToken)
		{
			value = VariantUtils.CreateFrom<string>(ref _languageToken);
			return true;
		}
		if ((ref name) == PropertyName._hoverPlayerId)
		{
			value = VariantUtils.CreateFrom<ulong>(ref _hoverPlayerId);
			return true;
		}
		if ((ref name) == PropertyName._detailPlayerId)
		{
			value = VariantUtils.CreateFrom<ulong>(ref _detailPlayerId);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		List<PropertyInfo> list = new List<PropertyInfo>();
		list.Add(new PropertyInfo((Type)24, PropertyName._root, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._hoverPanel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._hoverHeadingLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._hoverSummaryLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._hoverPreviewRow, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._hoverHintLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._detailPanel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._detailHeadingLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._detailScreenLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._detailDescriptionLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._detailOptionsList, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName._detailCloseButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)4, PropertyName._languageToken, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)2, PropertyName._hoverPlayerId, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)2, PropertyName._detailPlayerId, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		return list;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._root, Variant.From<Control>(ref _root));
		info.AddProperty(PropertyName._hoverPanel, Variant.From<PanelContainer>(ref _hoverPanel));
		info.AddProperty(PropertyName._hoverHeadingLabel, Variant.From<Label>(ref _hoverHeadingLabel));
		info.AddProperty(PropertyName._hoverSummaryLabel, Variant.From<Label>(ref _hoverSummaryLabel));
		info.AddProperty(PropertyName._hoverPreviewRow, Variant.From<HBoxContainer>(ref _hoverPreviewRow));
		info.AddProperty(PropertyName._hoverHintLabel, Variant.From<Label>(ref _hoverHintLabel));
		info.AddProperty(PropertyName._detailPanel, Variant.From<PanelContainer>(ref _detailPanel));
		info.AddProperty(PropertyName._detailHeadingLabel, Variant.From<Label>(ref _detailHeadingLabel));
		info.AddProperty(PropertyName._detailScreenLabel, Variant.From<Label>(ref _detailScreenLabel));
		info.AddProperty(PropertyName._detailDescriptionLabel, Variant.From<Label>(ref _detailDescriptionLabel));
		info.AddProperty(PropertyName._detailOptionsList, Variant.From<VBoxContainer>(ref _detailOptionsList));
		info.AddProperty(PropertyName._detailCloseButton, Variant.From<Button>(ref _detailCloseButton));
		info.AddProperty(PropertyName._languageToken, Variant.From<string>(ref _languageToken));
		info.AddProperty(PropertyName._hoverPlayerId, Variant.From<ulong>(ref _hoverPlayerId));
		info.AddProperty(PropertyName._detailPlayerId, Variant.From<ulong>(ref _detailPlayerId));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._root, ref val))
		{
			_root = ((Variant)(ref val)).As<Control>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._hoverPanel, ref val2))
		{
			_hoverPanel = ((Variant)(ref val2)).As<PanelContainer>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._hoverHeadingLabel, ref val3))
		{
			_hoverHeadingLabel = ((Variant)(ref val3)).As<Label>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._hoverSummaryLabel, ref val4))
		{
			_hoverSummaryLabel = ((Variant)(ref val4)).As<Label>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._hoverPreviewRow, ref val5))
		{
			_hoverPreviewRow = ((Variant)(ref val5)).As<HBoxContainer>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._hoverHintLabel, ref val6))
		{
			_hoverHintLabel = ((Variant)(ref val6)).As<Label>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._detailPanel, ref val7))
		{
			_detailPanel = ((Variant)(ref val7)).As<PanelContainer>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._detailHeadingLabel, ref val8))
		{
			_detailHeadingLabel = ((Variant)(ref val8)).As<Label>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._detailScreenLabel, ref val9))
		{
			_detailScreenLabel = ((Variant)(ref val9)).As<Label>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._detailDescriptionLabel, ref val10))
		{
			_detailDescriptionLabel = ((Variant)(ref val10)).As<Label>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._detailOptionsList, ref val11))
		{
			_detailOptionsList = ((Variant)(ref val11)).As<VBoxContainer>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._detailCloseButton, ref val12))
		{
			_detailCloseButton = ((Variant)(ref val12)).As<Button>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._languageToken, ref val13))
		{
			_languageToken = ((Variant)(ref val13)).As<string>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName._hoverPlayerId, ref val14))
		{
			_hoverPlayerId = ((Variant)(ref val14)).As<ulong>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName._detailPlayerId, ref val15))
		{
			_detailPlayerId = ((Variant)(ref val15)).As<ulong>();
		}
	}
}
