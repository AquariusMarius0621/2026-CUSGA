using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TowerDefenseHudState 是 HUD 每次刷新时需要看到的“最小状态快照”。
///
/// 它的设计目标不是取代 `TowerDefenseGame` 的完整状态，
/// 而是刻意只把 HUD 真正在意的数据摘出来：
/// - 资源 / 基地 / 波次
/// - 当前选中了什么塔
/// - 当前是否正在拖拽部署
///
/// 这样可以让 HUD 刷新层依赖“结果态”，
/// 而不是直接伸手去读总控里一大堆实现细节字段。
/// </summary>
public readonly struct TowerDefenseHudState
{
    public TowerDefenseHudState(
        int currentEnergy,
        int currentBaseHealth,
        int currentWave,
        int totalWaves,
        TowerType selectedTowerType,
        bool isPlacementDragActive,
        TowerType dragTowerType)
    {
        CurrentEnergy = currentEnergy;
        CurrentBaseHealth = currentBaseHealth;
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
        SelectedTowerType = selectedTowerType;
        IsPlacementDragActive = isPlacementDragActive;
        DragTowerType = dragTowerType;
    }

    public int CurrentEnergy { get; }

    public int CurrentBaseHealth { get; }

    public int CurrentWave { get; }

    public int TotalWaves { get; }

    public TowerType SelectedTowerType { get; }

    public bool IsPlacementDragActive { get; }

    public TowerType DragTowerType { get; }
}

/// <summary>
/// TowerDragPreviewState 表示拖拽提示面板真正关心的三个信息：
/// - 当前拖的是哪种塔
/// - 当前落点是否有效
/// - 如果无效，原因是什么
///
/// 它和上面的 HUD 状态分开，是因为拖拽提示只在一小段交互链路里会被频繁刷新，
/// 没必要把所有 HUD 字段都一股脑带过来。
/// </summary>
public readonly struct TowerDragPreviewState
{
    public TowerDragPreviewState(TowerType towerType, bool isValid, string invalidReason)
    {
        TowerType = towerType;
        IsValid = isValid;
        InvalidReason = invalidReason ?? string.Empty;
    }

    public TowerType TowerType { get; }

    public bool IsValid { get; }

    public string InvalidReason { get; }
}

/// <summary>
/// TowerDefenseHudPresenter 负责两类事情：
/// 1. 通过名称约定查找 HUD 相关对象和组件
/// 2. 把总控给出的状态快照渲染成最终界面
///
/// 它故意不负责：
/// - 资源结算
/// - 建造判定
/// - 拖拽状态机
/// - 胜负规则
///
/// 换句话说，它是“表现层适配器”，不是“玩法规则层”。
/// 这正好符合当前项目想做的第一刀轻量拆分：
/// 先把显示层从总控中抽出来，再继续考虑更深的 Placement / Economy 分拆。
/// </summary>
public sealed class TowerDefenseHudPresenter
{
    private const string TopBarName = "TopBar";
    private const string BottomBarName = "BottomBar";
    private const string EnergyCardName = "EnergyCard";
    private const string BaseCardName = "BaseCard";
    private const string WaveCardName = "WaveCard";
    private const string SelectionCardName = "SelectionCard";
    private const string StatusStripName = "StatusStrip";
    private const string DeployHeaderTextName = "DeployHeaderText";
    private const string RelayIconBadgeName = "RelayIconBadge";
    private const string DefenseIconBadgeName = "DefenseIconBadge";
    private const string StageCodeTextName = "StageCodeText";
    private const string SpawnTagTextName = "SpawnTagText";
    private const string BaseTagTextName = "BaseTagText";
    private const string MapCornerTopLeftName = "MapCornerTL";
    private const string MapCornerBottomRightName = "MapCornerBR";
    private const string MapEdgeTopName = "MapEdgeTop";
    private const string MapEdgeBottomName = "MapEdgeBottom";
    private readonly string _energyTextName;
    private readonly string _baseHealthTextName;
    private readonly string _waveTextName;
    private readonly string _selectionTextName;
    private readonly string _statusTextName;
    private readonly string _relayTowerButtonName;
    private readonly string _defenseTowerButtonName;
    private readonly string _clearSelectionButtonName;
    private readonly string _gameOverPanelName;
    private readonly string _gameOverTitleName;
    private readonly string _gameOverHintName;
    private readonly string _dragPreviewPanelName;
    private readonly string _dragPreviewLabelName;

    private TMP_Text _energyText;
    private TMP_Text _baseHealthText;
    private TMP_Text _waveText;
    private TMP_Text _selectionText;
    private TMP_Text _statusText;
    private TMP_Text _gameOverTitle;
    private TMP_Text _gameOverHint;
    private TMP_Text _relayTowerButtonText;
    private TMP_Text _defenseTowerButtonText;
    private TMP_Text _clearSelectionButtonText;
    private TMP_Text _dragPreviewLabel;

    private Button _relayTowerButton;
    private Button _defenseTowerButton;
    private Button _clearSelectionButton;
    private GameObject _gameOverPanel;
    private GameObject _dragPreviewPanel;
    private TMP_Text _deployHeaderText;
    private TMP_Text _stageCodeText;
    private TMP_Text _spawnTagText;
    private TMP_Text _baseTagText;
    private Image _topBarImage;
    private Image _bottomBarImage;
    private Image _energyCardImage;
    private Image _baseCardImage;
    private Image _waveCardImage;
    private Image _selectionCardImage;
    private Image _statusStripImage;
    private Image _relayIconBadgeImage;
    private Image _defenseIconBadgeImage;
    private Image _mapCornerTopLeftImage;
    private Image _mapCornerBottomRightImage;
    private Image _mapEdgeTopImage;
    private Image _mapEdgeBottomImage;
    private Image _dragPreviewPanelImage;
    private Image _gameOverPanelImage;

    public TowerDefenseHudPresenter(
        string energyTextName,
        string baseHealthTextName,
        string waveTextName,
        string selectionTextName,
        string statusTextName,
        string relayTowerButtonName,
        string defenseTowerButtonName,
        string clearSelectionButtonName,
        string gameOverPanelName,
        string gameOverTitleName,
        string gameOverHintName,
        string dragPreviewPanelName,
        string dragPreviewLabelName)
    {
        _energyTextName = energyTextName;
        _baseHealthTextName = baseHealthTextName;
        _waveTextName = waveTextName;
        _selectionTextName = selectionTextName;
        _statusTextName = statusTextName;
        _relayTowerButtonName = relayTowerButtonName;
        _defenseTowerButtonName = defenseTowerButtonName;
        _clearSelectionButtonName = clearSelectionButtonName;
        _gameOverPanelName = gameOverPanelName;
        _gameOverTitleName = gameOverTitleName;
        _gameOverHintName = gameOverHintName;
        _dragPreviewPanelName = dragPreviewPanelName;
        _dragPreviewLabelName = dragPreviewLabelName;
    }

    /// <summary>
    /// 按当前原型的“对象名约定”查找 HUD 所需引用。
    ///
    /// 这里没有立刻改成 Inspector 拖引用，
    /// 是因为这个项目当前明确还处在原型优先阶段，
    /// 我们这次的目标是“先拆职责”，不是“同时替换整套场景装配方式”。
    /// </summary>
    public void FindSceneReferences()
    {
        _energyText = SceneObjectFinder.FindComponent<TMP_Text>(_energyTextName);
        _baseHealthText = SceneObjectFinder.FindComponent<TMP_Text>(_baseHealthTextName);
        _waveText = SceneObjectFinder.FindComponent<TMP_Text>(_waveTextName);
        _selectionText = SceneObjectFinder.FindComponent<TMP_Text>(_selectionTextName);
        _statusText = SceneObjectFinder.FindComponent<TMP_Text>(_statusTextName);
        _gameOverTitle = SceneObjectFinder.FindComponent<TMP_Text>(_gameOverTitleName);
        _gameOverHint = SceneObjectFinder.FindComponent<TMP_Text>(_gameOverHintName);
        _dragPreviewLabel = SceneObjectFinder.FindComponent<TMP_Text>(_dragPreviewLabelName);

        _relayTowerButton = SceneObjectFinder.FindComponent<Button>(_relayTowerButtonName);
        _defenseTowerButton = SceneObjectFinder.FindComponent<Button>(_defenseTowerButtonName);
        _clearSelectionButton = SceneObjectFinder.FindComponent<Button>(_clearSelectionButtonName);
        _gameOverPanel = SceneObjectFinder.FindGameObject(_gameOverPanelName);
        _dragPreviewPanel = SceneObjectFinder.FindGameObject(_dragPreviewPanelName);

        _relayTowerButtonText = _relayTowerButton != null ? _relayTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _defenseTowerButtonText = _defenseTowerButton != null ? _defenseTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _clearSelectionButtonText = _clearSelectionButton != null ? _clearSelectionButton.GetComponentInChildren<TMP_Text>(true) : null;

        CacheThemeReferences();
    }

    /// <summary>
    /// 根据塔定义统一配置部署卡文案。
    ///
    /// 这样文案来源就从“总控里零散写死的字符串”
    /// 收束成了“塔定义 -> HUD 渲染”这条更清晰的链路。
    /// </summary>
    public void ConfigureCardLabels(TowerCatalog towerCatalog)
    {
        ConfigureTowerCardLabel(_relayTowerButtonText, towerCatalog.GetDefinition(TowerType.Relay));
        ConfigureTowerCardLabel(_defenseTowerButtonText, towerCatalog.GetDefinition(TowerType.Defense));

        if (_clearSelectionButtonText != null)
        {
            _clearSelectionButtonText.text = "CANCEL DEPLOY\n<size=20><color=#A4B2C0>Esc / RMB</color></size>";
            _clearSelectionButtonText.alignment = TextAlignmentOptions.Center;
            _clearSelectionButtonText.margin = new Vector4(24f, 14f, 24f, 14f);
        }

        ApplyRuntimeTheme(towerCatalog);
    }

    /// <summary>
    /// 刷新常驻 HUD。
    ///
    /// 这里接收的是已经整理好的状态快照和塔目录，
    /// 因此 HUD 不需要知道总控内部字段是怎么组织的。
    /// </summary>
    /// <summary>
    /// 缂撳瓨涓€鎵归€夐厤鍙敤浜庤瑙夊崌绾х殑 HUD 寮曠敤銆?
    ///
    /// 瀹冧滑涓嶄竴瀹氱洿鎺ュ弬涓庢瘡娆℃暟鍊煎埛鏂帮紝
    /// 浣嗗湪杩欐鎴樻湳缁堢椋庢牸鍗囩骇閲岋紝闇€瑕佽闆嗕腑绠＄悊銆?
    /// </summary>
    private void CacheThemeReferences()
    {
        _topBarImage = SceneObjectFinder.FindComponent<Image>(TopBarName);
        _bottomBarImage = SceneObjectFinder.FindComponent<Image>(BottomBarName);
        _energyCardImage = SceneObjectFinder.FindComponent<Image>(EnergyCardName);
        _baseCardImage = SceneObjectFinder.FindComponent<Image>(BaseCardName);
        _waveCardImage = SceneObjectFinder.FindComponent<Image>(WaveCardName);
        _selectionCardImage = SceneObjectFinder.FindComponent<Image>(SelectionCardName);
        _statusStripImage = SceneObjectFinder.FindComponent<Image>(StatusStripName);
        _relayIconBadgeImage = SceneObjectFinder.FindComponent<Image>(RelayIconBadgeName);
        _defenseIconBadgeImage = SceneObjectFinder.FindComponent<Image>(DefenseIconBadgeName);
        _mapCornerTopLeftImage = SceneObjectFinder.FindComponent<Image>(MapCornerTopLeftName);
        _mapCornerBottomRightImage = SceneObjectFinder.FindComponent<Image>(MapCornerBottomRightName);
        _mapEdgeTopImage = SceneObjectFinder.FindComponent<Image>(MapEdgeTopName);
        _mapEdgeBottomImage = SceneObjectFinder.FindComponent<Image>(MapEdgeBottomName);
        _dragPreviewPanelImage = SceneObjectFinder.FindComponent<Image>(_dragPreviewPanelName);
        _gameOverPanelImage = SceneObjectFinder.FindComponent<Image>(_gameOverPanelName);

        _deployHeaderText = SceneObjectFinder.FindComponent<TMP_Text>(DeployHeaderTextName);
        _stageCodeText = SceneObjectFinder.FindComponent<TMP_Text>(StageCodeTextName);
        _spawnTagText = SceneObjectFinder.FindComponent<TMP_Text>(SpawnTagTextName);
        _baseTagText = SceneObjectFinder.FindComponent<TMP_Text>(BaseTagTextName);
    }

    /// <summary>
    /// 鎶?HUD 鍗囩骇鎴愭洿瀹屾暣鐨勨€滄垬鏈粓绔€濋鏍笺€?
    ///
    /// 杩欓噷閲嶇偣涓嶆槸澶ф壒鍒涘缓鏂?UI锛岃€屾槸灏介噺澶嶇敤鐜版湁瀵硅薄锛屼粠锛?
    /// - 闈㈡澘搴曡壊灞傛
    /// - 缁嗘弿杈瑰拰闃村奖
    /// - 寮鸿皟鑹叉潯
    /// - 鏇存湁鑺傚鐨勬帓鐗?
    /// 杩欏嚑鏉℃€荤粨鏉ュ仛鎰熺煡鍗囩骇銆?
    /// </summary>
    private void ApplyRuntimeTheme(TowerCatalog towerCatalog)
    {
        TowerDefinition relayDefinition = towerCatalog != null ? towerCatalog.GetDefinition(TowerType.Relay) : null;
        TowerDefinition defenseDefinition = towerCatalog != null ? towerCatalog.GetDefinition(TowerType.Defense) : null;

        StyleSurface(_topBarImage, new Color(0.03f, 0.05f, 0.08f, 0.88f), new Color(0.32f, 0.58f, 0.66f, 0.22f), new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, -8f));
        StyleSurface(_bottomBarImage, new Color(0.03f, 0.05f, 0.08f, 0.94f), new Color(1f, 0.54f, 0.25f, 0.22f), new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -10f));
        StyleSurface(_statusStripImage, new Color(0.04f, 0.07f, 0.1f, 0.96f), new Color(0.54f, 0.82f, 0.96f, 0.2f), new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -6f));
        StyleSurface(_dragPreviewPanelImage, new Color(0.02f, 0.06f, 0.09f, 0.98f), new Color(0.39f, 0.92f, 0.86f, 0.22f), new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -8f));
        StyleSurface(_gameOverPanelImage, new Color(0.03f, 0.05f, 0.08f, 0.97f), new Color(1f, 0.43f, 0.38f, 0.22f), new Color(0f, 0f, 0f, 0.34f), new Vector2(0f, -10f));

        StyleCardSurface(_energyCardImage, new Color(1f, 0.62f, 0.29f, 1f));
        StyleCardSurface(_baseCardImage, new Color(0.44f, 0.92f, 1f, 1f));
        StyleCardSurface(_waveCardImage, new Color(1f, 0.85f, 0.44f, 1f));
        StyleCardSurface(_selectionCardImage, new Color(0.45f, 0.95f, 0.84f, 1f));

        StyleBadge(_relayIconBadgeImage, relayDefinition != null ? relayDefinition.AccentColor : new Color(1f, 0.62f, 0.29f, 1f));
        StyleBadge(_defenseIconBadgeImage, defenseDefinition != null ? defenseDefinition.AccentColor : new Color(0.3f, 0.82f, 1f, 1f));

        StyleMapFrame(_mapCornerTopLeftImage, new Color(1f, 0.74f, 0.31f, 0.24f));
        StyleMapFrame(_mapCornerBottomRightImage, new Color(0.39f, 0.94f, 0.89f, 0.24f));
        StyleMapFrame(_mapEdgeTopImage, new Color(0.86f, 0.61f, 0.29f, 0.18f));
        StyleMapFrame(_mapEdgeBottomImage, new Color(0.31f, 0.82f, 0.95f, 0.18f));

        StyleMetricText(_energyText);
        StyleMetricText(_baseHealthText);
        StyleMetricText(_waveText);
        StyleSelectionText(_selectionText);
        StyleStatusText(_statusText);
        StylePreviewText(_dragPreviewLabel);
        StyleGameOverText(_gameOverTitle, _gameOverHint);
        StyleMapLabel(_stageCodeText, new Color(0.99f, 0.81f, 0.42f, 1f));
        StyleMapLabel(_spawnTagText, new Color(0.98f, 0.73f, 0.4f, 1f));
        StyleMapLabel(_baseTagText, new Color(0.48f, 0.92f, 0.98f, 1f));

        if (_deployHeaderText != null)
        {
            _deployHeaderText.text = "DEPLOY OPTIONS\n<size=18><color=#7F9DB5>Generator or Turret / Drag to scan legal sectors</color></size>";
            _deployHeaderText.alignment = TextAlignmentOptions.Center;
            _deployHeaderText.fontStyle = FontStyles.Bold;
            _deployHeaderText.characterSpacing = 3f;
            _deployHeaderText.lineSpacing = -14f;
            _deployHeaderText.margin = new Vector4(0f, 6f, 0f, 0f);
            _deployHeaderText.color = new Color(0.95f, 0.98f, 1f, 1f);
        }

        if (_dragPreviewPanel != null)
        {
            RectTransform previewRect = _dragPreviewPanel.GetComponent<RectTransform>();
            if (previewRect != null)
            {
                previewRect.sizeDelta = new Vector2(316f, 154f);
            }
        }

        ApplyRuntimeLayout();
    }

    public void Refresh(TowerDefenseHudState state, TowerCatalog towerCatalog, Func<TowerType, bool> canAffordTower)
    {
        if (_energyText != null)
        {
            _energyText.text = BuildMetricText("ENERGY GRID", state.CurrentEnergy.ToString(), "FFB567");
        }

        if (_baseHealthText != null)
        {
            _baseHealthText.text = BuildMetricText("BASE CORE", state.CurrentBaseHealth.ToString(), "72E8FF");
        }

        if (_waveText != null)
        {
            string waveDisplay = state.TotalWaves > 0 ? $"{state.CurrentWave}/{state.TotalWaves}" : "0/0";
            _waveText.text = BuildMetricText("WAVE CLOCK", waveDisplay, "FFD878");
        }

        if (_selectionText != null)
        {
            _selectionText.text = BuildSelectionText(state, towerCatalog);
        }

        UpdateButtonVisuals(state, towerCatalog, canAffordTower);
    }

    /// <summary>
    /// 刷新底部状态栏消息。
    ///
    /// 这个接口被总控直接调用，
    /// 因为“何时显示什么消息”仍然属于规则层决定，
    /// HUD 这里只负责把最终文本画出来。
    /// </summary>
    public void SetStatusMessage(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
        }
    }

    /// <summary>
    /// 控制拖拽提示面板显隐。
    /// </summary>
    public void SetDragPreviewVisible(bool visible)
    {
        if (_dragPreviewPanel != null)
        {
            _dragPreviewPanel.SetActive(visible);
        }
    }

    /// <summary>
    /// 更新跟随鼠标的拖拽提示面板。
    ///
    /// 注意：这个方法只渲染“已经得出的判定结果”，
    /// 不负责自己去做任何建造合法性计算。
    /// </summary>
    public void UpdateDragPreviewPanel(Vector2 screenPosition, TowerDragPreviewState previewState, TowerCatalog towerCatalog)
    {
        if (_dragPreviewPanel == null || _dragPreviewLabel == null)
        {
            return;
        }

        RectTransform parentRect = _dragPreviewPanel.transform.parent as RectTransform;
        RectTransform panelRect = _dragPreviewPanel.GetComponent<RectTransform>();
        if (parentRect != null && panelRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition + new Vector2(142f, -92f),
                null,
                out Vector2 localPoint);
            panelRect.anchoredPosition = localPoint;
        }

        TowerDefinition definition = towerCatalog.GetDefinition(previewState.TowerType);
        if (definition == null)
        {
            _dragPreviewLabel.text = string.Empty;
            return;
        }

        string accentHex = ColorUtility.ToHtmlStringRGB(definition.AccentColor);
        string stateLine = previewState.IsValid
            ? "<color=#78F3DA>DROP POINT CONFIRMED</color>"
            : $"<color=#FF7282>{previewState.InvalidReason}</color>";

        _dragPreviewLabel.text =
            "<size=20><color=#97B2C8>DEPLOY TRACE</color></size>\n" +
            $"<size=34>{definition.DisplayName.ToUpperInvariant()}</size>\n" +
            $"<size=20><color=#{accentHex}>{definition.BuildCost} EN</color>  <color=#88A5BC>GRID {definition.ExpansionSquareSize:0.0}</color></size>\n" +
            "<size=18><color=#87A5BD>Cyan sectors show exact legal drop zones</color></size>\n" +
            $"<size=18>{stateLine}</size>";
    }

    /// <summary>
    /// 显示 Game Over 面板并填充文案。
    /// </summary>
    public void ShowGameOver(string title, string hint)
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(true);
        }

        if (_gameOverTitle != null)
        {
            _gameOverTitle.text = title;
        }

        if (_gameOverHint != null)
        {
            _gameOverHint.text = hint;
        }
    }

    /// <summary>
    /// 单独控制 Game Over 面板显隐，
    /// 方便启动时先统一关掉。
    /// </summary>
    public void SetGameOverVisible(bool visible)
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(visible);
        }
    }

    /// <summary>
    /// 配置单张塔部署卡的文本样式。
    /// </summary>
    private void ConfigureTowerCardLabel(TMP_Text label, TowerDefinition definition)
    {
        if (label == null || definition == null)
        {
            return;
        }

        label.text = definition.BuildCardLabelMarkup();
        label.alignment = TextAlignmentOptions.Left;
        label.margin = new Vector4(108f, 18f, 24f, 18f);
        label.enableWordWrapping = false;
        label.characterSpacing = 1.2f;
        label.lineSpacing = -10f;
        label.color = new Color(0.96f, 0.98f, 1f, 1f);
    }

    /// <summary>
    /// 组装 HUD 中“当前选中 / 当前拖拽”的说明文案。
    /// </summary>
    private string BuildSelectionText(TowerDefenseHudState state, TowerCatalog towerCatalog)
    {
        if (state.IsPlacementDragActive)
        {
            TowerDefinition draggingDefinition = towerCatalog.GetDefinition(state.DragTowerType);
            if (draggingDefinition != null)
            {
                string accentHex = ColorUtility.ToHtmlStringRGB(draggingDefinition.AccentColor);
                return
                    "DEPLOY TRACE\n" +
                    $"<size=30>{draggingDefinition.DisplayName}</size>\n" +
                    $"<size=20><color=#{accentHex}>{draggingDefinition.BuildCost} EN</color>  <color=#89A7BF>Cyan sectors = exact legal zone</color></size>";
            }
        }

        if (state.SelectedTowerType != TowerType.None)
        {
            TowerDefinition selectedDefinition = towerCatalog.GetDefinition(state.SelectedTowerType);
            if (selectedDefinition != null)
            {
                return
                    "TACTICAL READY\n" +
                    $"<size=30>{selectedDefinition.DisplayName}</size>\n" +
                    "<size=20><color=#8AA7BF>Drag the card to scan exact legal sectors</color></size>";
            }
        }

        return
            "OPERATION LINK\n" +
            "<size=28>Drag a tower card to project legal sectors</size>\n" +
            "<size=20><color=#89A7BF>1 Relay / 2 Defense / Esc Cancel</color></size>";
    }

    /// <summary>
    /// 根据状态和塔目录刷新三张按钮 / 卡片的视觉状态。
    /// </summary>
    private void UpdateButtonVisuals(TowerDefenseHudState state, TowerCatalog towerCatalog, Func<TowerType, bool> canAffordTower)
    {
        TowerDefinition relayDefinition = towerCatalog.GetDefinition(TowerType.Relay);
        TowerDefinition defenseDefinition = towerCatalog.GetDefinition(TowerType.Defense);

        UpdateShopButtonVisual(
            _relayTowerButton,
            _relayTowerButtonText,
            _relayIconBadgeImage,
            state.SelectedTowerType == TowerType.Relay,
            relayDefinition != null && canAffordTower(TowerType.Relay),
            relayDefinition != null ? relayDefinition.AccentColor : Color.white);

        UpdateShopButtonVisual(
            _defenseTowerButton,
            _defenseTowerButtonText,
            _defenseIconBadgeImage,
            state.SelectedTowerType == TowerType.Defense,
            defenseDefinition != null && canAffordTower(TowerType.Defense),
            defenseDefinition != null ? defenseDefinition.AccentColor : Color.white);

        UpdateShopButtonVisual(
            _clearSelectionButton,
            _clearSelectionButtonText,
            null,
            state.SelectedTowerType == TowerType.None && !state.IsPlacementDragActive,
            true,
            new Color(0.7f, 0.77f, 0.85f, 1f));
    }

    /// <summary>
    /// 刷新单张按钮 / 卡片的背景与文字颜色。
    /// </summary>
    private void UpdateShopButtonVisual(Button button, TMP_Text label, Image badgeImage, bool isSelected, bool isAvailable, Color accentColor)
    {
        Image backgroundImage = button != null ? button.targetGraphic as Image : null;
        if (backgroundImage != null)
        {
            Color idleColor = isAvailable ? new Color(0.06f, 0.1f, 0.14f, 0.94f) : new Color(0.05f, 0.07f, 0.09f, 0.62f);
            Color selectedColor = Color.Lerp(idleColor, accentColor, 0.78f);
            backgroundImage.color = isSelected ? selectedColor : idleColor;

            Outline outline = EnsureEffect<Outline>(backgroundImage.gameObject);
            outline.effectDistance = isSelected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            outline.effectColor = !isAvailable
                ? new Color(0.18f, 0.24f, 0.29f, 0.45f)
                : (isSelected ? WithAlpha(Color.Lerp(accentColor, Color.white, 0.28f), 0.78f) : new Color(0.23f, 0.31f, 0.39f, 0.42f));
            outline.useGraphicAlpha = true;

            Shadow shadow = EnsureEffect<Shadow>(backgroundImage.gameObject);
            shadow.effectDistance = new Vector2(0f, -8f);
            shadow.effectColor = isSelected ? WithAlpha(accentColor, 0.28f) : new Color(0f, 0f, 0f, 0.24f);
            shadow.useGraphicAlpha = true;
        }

        RectTransform buttonRect = button != null ? button.transform as RectTransform : null;
        Image topAccent = EnsureAccentStrip(buttonRect, "ThemeTopAccent", 4f, true);
        if (topAccent != null)
        {
            topAccent.color = !isAvailable
                ? new Color(0.22f, 0.28f, 0.33f, 0.6f)
                : (isSelected ? WithAlpha(accentColor, 0.95f) : WithAlpha(accentColor, 0.38f));
        }

        Image sideAccent = EnsureAccentStrip(buttonRect, "ThemeLeftAccent", 6f, false);
        if (sideAccent != null)
        {
            sideAccent.color = !isAvailable
                ? new Color(0.22f, 0.28f, 0.33f, 0.6f)
                : (isSelected ? WithAlpha(accentColor, 0.92f) : WithAlpha(accentColor, 0.46f));
        }

        UpdateBadgeVisual(badgeImage, isSelected, isAvailable, accentColor);

        if (label != null)
        {
            label.color = !isAvailable
                ? new Color(0.57f, 0.62f, 0.69f, 1f)
                : (isSelected ? Color.white : new Color(0.94f, 0.97f, 1f, 1f));
        }
    }

    /// <summary>
    /// 缁熶竴閰嶇疆鏅€氶潰鏉跨殑搴曡壊銆佹弿杈瑰拰闃村奖銆?
    ///
    /// 杩欓噷鐨勭洰鏍囦笉鏄仛澶嶆潅鐗规晥锛屽彧鏄敤寰堝皯鐨勬垚鏈?抒鎶?鍘熷瀷骞抽潰鍧椻€濇彁鍗囨垚鏇存湁灞傛鐨勬垬鏈潰鏉裤€?
    /// </summary>
    /// <summary>
    /// 让卡片左侧的图标徽记区也同步成当前选择状态。
    ///
    /// 这样玩家不只会看到卡片底板变化，
    /// 也能从图标底色和轮廓上更快读出当前焦点。
    /// </summary>
    private void UpdateBadgeVisual(Image badgeImage, bool isSelected, bool isAvailable, Color accentColor)
    {
        if (badgeImage == null)
        {
            return;
        }

        Color baseColor = isAvailable
            ? new Color(0.08f, 0.12f, 0.16f, 0.92f)
            : new Color(0.06f, 0.08f, 0.1f, 0.62f);
        badgeImage.color = isSelected ? Color.Lerp(baseColor, accentColor, 0.42f) : baseColor;

        Outline outline = EnsureEffect<Outline>(badgeImage.gameObject);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.effectColor = isSelected
            ? WithAlpha(Color.Lerp(accentColor, Color.white, 0.25f), 0.72f)
            : new Color(0.22f, 0.3f, 0.38f, 0.42f);
        outline.useGraphicAlpha = true;
    }

    private void StyleSurface(Image image, Color fillColor, Color outlineColor, Color shadowColor, Vector2 shadowOffset)
    {
        if (image == null)
        {
            return;
        }

        image.color = fillColor;

        Outline outline = EnsureEffect<Outline>(image.gameObject);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.effectColor = outlineColor;
        outline.useGraphicAlpha = true;

        Shadow shadow = EnsureEffect<Shadow>(image.gameObject);
        shadow.effectDistance = shadowOffset;
        shadow.effectColor = shadowColor;
        shadow.useGraphicAlpha = true;
    }

    /// <summary>
    /// 缁欓《閮ㄤ俊鎭崱鍔犱笂寮鸿皟鏉″拰鏇存竻鏅扮殑闈㈡澘灞傛銆?
    /// </summary>
    private void StyleCardSurface(Image image, Color accentColor)
    {
        if (image == null)
        {
            return;
        }

        StyleSurface(image, new Color(0.07f, 0.1f, 0.14f, 0.94f), WithAlpha(accentColor, 0.32f), new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -6f));

        Image topAccent = EnsureAccentStrip(image.rectTransform, "ThemeTopAccent", 4f, true);
        if (topAccent != null)
        {
            topAccent.color = WithAlpha(accentColor, 0.94f);
        }
    }

    /// <summary>
    /// 缁欏崱鐗囧乏渚х殑鍥炬爣寰界鍖烘坊鍔犳洿瀹屾暣鐨勫簳鏉挎劅銆?
    /// </summary>
    private void StyleBadge(Image image, Color accentColor)
    {
        if (image == null)
        {
            return;
        }

        StyleSurface(image, Color.Lerp(new Color(0.07f, 0.1f, 0.14f, 0.96f), accentColor, 0.22f), WithAlpha(accentColor, 0.42f), new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -4f));
    }

    /// <summary>
    /// 鍦板浘杈规涓嶆敼缁撴瀯锛屽彧閫氳繃棰滆壊鍗囩骇鍏剁粓绔劅銆?
    /// </summary>
    private void StyleMapFrame(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }

        image.color = color;
    }

    /// <summary>
    /// 閰嶇疆鏁板€煎崱鏂囨湰銆?
    /// </summary>
    private void StyleMetricText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 2.2f;
        text.lineSpacing = -18f;
        text.enableWordWrapping = false;
        text.margin = new Vector4(18f, 14f, 18f, 18f);
        text.color = new Color(0.96f, 0.98f, 1f, 1f);
    }

    /// <summary>
    /// 閰嶇疆涓ぎ閫夋嫨淇℃伅鍗＄殑鏂囨湰鏍峰紡銆?
    /// </summary>
    private void StyleSelectionText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 1.5f;
        text.lineSpacing = -12f;
        text.margin = new Vector4(22f, 16f, 20f, 16f);
        text.color = new Color(0.96f, 0.99f, 1f, 1f);
    }

    /// <summary>
    /// 閰嶇疆搴曢儴鐘舵€佹爮鏂囨湰銆?
    /// </summary>
    private void StyleStatusText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 0.8f;
        text.lineSpacing = -6f;
        text.margin = new Vector4(18f, 10f, 18f, 10f);
        text.color = new Color(0.95f, 0.98f, 1f, 1f);
    }

    /// <summary>
    /// 閰嶇疆鎷栨嫿鎻愮ず闈㈡澘鏂囨湰銆?
    /// </summary>
    private void StylePreviewText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 1.1f;
        text.lineSpacing = -10f;
        text.margin = new Vector4(18f, 16f, 18f, 16f);
        text.color = new Color(0.96f, 0.99f, 1f, 1f);
    }

    /// <summary>
    /// 閰嶇疆 Game Over 闈㈡澘鏂囨湰銆?
    /// </summary>
    private void StyleGameOverText(TMP_Text title, TMP_Text hint)
    {
        if (title != null)
        {
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 8f;
            title.lineSpacing = -10f;
            title.color = new Color(1f, 0.88f, 0.8f, 1f);
        }

        if (hint != null)
        {
            hint.fontStyle = FontStyles.Bold;
            hint.characterSpacing = 1f;
            hint.lineSpacing = -6f;
            hint.color = new Color(0.89f, 0.94f, 1f, 1f);
        }
    }

    /// <summary>
    /// 閰嶇疆鍦板浘瑙掓爣鏂囨湰銆?
    /// </summary>
    private void StyleMapLabel(TMP_Text text, Color color)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 3f;
        text.color = color;
    }

    /// <summary>
    /// 鐢熸垚鎴栧鐢ㄤ竴鏍归暱鏉＄姸 UI 瑁呴グ銆?
    /// </summary>
    private Image EnsureAccentStrip(RectTransform parent, string objectName, float thickness, bool horizontal)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(objectName);
        GameObject accentObject = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        Image accentImage = accentObject.GetComponent<Image>();

        if (existing == null)
        {
            accentRect.SetParent(parent, false);
            accentRect.SetAsFirstSibling();
        }

        accentImage.raycastTarget = false;

        if (horizontal)
        {
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.anchoredPosition = new Vector2(0f, -2f);
            accentRect.sizeDelta = new Vector2(0f, thickness);
        }
        else
        {
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(thickness, 0f);
        }

        return accentImage;
    }

    /// <summary>
    /// 淇濊瘉瀵硅薄韬笂瀛樺湪鎸囧畾鐨?UI 鐗规晥缁勪欢銆?
    /// </summary>
    private static T EnsureEffect<T>(GameObject target) where T : BaseMeshEffect
    {
        if (target == null)
        {
            return null;
        }

        T effect = target.GetComponent<T>();
        if (effect == null)
        {
            effect = target.AddComponent<T>();
        }

        return effect;
    }

    /// <summary>
    /// 缁勮椤堕儴鏁板€煎崱鐨勫瘜鏂囨湰銆?
    /// </summary>
    /// <summary>
    /// 把顶部 HUD 和右侧部署区摆回清楚、稳定、不重叠的运行时布局。
    ///
    /// 当前场景里不少节点的初始锚点都比较接近，
    /// 之前更多依赖搭场景时的人为摆放或隐式布局关系。
    ///
    /// 这次既然 HUD 已经变成运行时主题驱动，
    /// 那布局也一起在这里明确收口，避免出现：
    /// - 顶部卡片互相压住
    /// - 右侧两张部署卡叠在一起
    /// - 取消按钮和卡片抢空间
    /// </summary>
    private void ApplyRuntimeLayout()
    {
        LayoutTopBar();
        LayoutDeployPanel();
    }

    /// <summary>
    /// 明确顶部四张信息卡和状态条的位置。
    ///
    /// 顶部区现在分成两排：
    /// - 上排四张卡：资源 / 基地 / 波次 / 选中状态
    /// - 下排一条状态条：承接中文战术提示
    ///
    /// 这样既能保持终端风格的密度，
    /// 也能避免所有信息都挤在同一条水平线上互相碰撞。
    /// </summary>
    private void LayoutTopBar()
    {
        SetRect(_energyCardImage != null ? _energyCardImage.rectTransform : null, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(184f, 92f));
        SetRect(_baseCardImage != null ? _baseCardImage.rectTransform : null, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(216f, -14f), new Vector2(184f, 92f));
        SetRect(_waveCardImage != null ? _waveCardImage.rectTransform : null, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(416f, -14f), new Vector2(184f, 92f));
        SetRect(_selectionCardImage != null ? _selectionCardImage.rectTransform : null, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(616f, -14f), new Vector2(548f, 92f));
        SetRect(_statusStripImage != null ? _statusStripImage.rectTransform : null, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -116f), new Vector2(1148f, 42f));
    }

    /// <summary>
    /// 明确右侧部署选择区的标题、两张部署卡和取消按钮位置。
    ///
    /// 这里重点保证两件事：
    /// - “发电机 / 炮塔”两个选项永远同时可见
    /// - 标题、卡片、取消按钮彼此之间有足够留白，不再互相覆盖
    /// </summary>
    private void LayoutDeployPanel()
    {
        SetRect(_deployHeaderText != null ? _deployHeaderText.rectTransform : null, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(300f, 44f));
        SetRect(_relayTowerButton != null ? _relayTowerButton.transform as RectTransform : null, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(300f, 122f));
        SetRect(_defenseTowerButton != null ? _defenseTowerButton.transform as RectTransform : null, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -208f), new Vector2(300f, 122f));
        SetRect(_clearSelectionButton != null ? _clearSelectionButton.transform as RectTransform : null, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(300f, 72f));
    }

    /// <summary>
    /// 统一设置某个 RectTransform 的锚点、枢轴、位置和尺寸。
    ///
    /// 单独收成这个小工具，是为了让布局代码保持“像排版表一样好读”，
    /// 而不是每个节点都重复写一大串 anchor / pivot / size 赋值。
    /// </summary>
    private static void SetRect(RectTransform rectTransform, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }

    private static string BuildMetricText(string label, string value, string accentHex)
    {
        return
            $"<size=18><color=#8EA8BE>{label}</color></size>\n" +
            $"<size=56><color=#{accentHex}>{value}</color></size>";
    }

    /// <summary>
    /// 杩斿洖涓€浠藉彧鏀归€忔槑搴︿笉鏀?RGB 鐨勯鑹插壇鏈?
    /// </summary>
    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
