using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// `TowerDefenseHudState` 是 HUD 每次刷新时真正需要的最小状态快照。
///
/// 这里刻意不把整个 `TowerDefenseGame` 直接暴露给 HUD，
/// 而是只传表现层真正关心的结果：
/// - 当前资源。
/// - 当前基地血量。
/// - 当前波次。
/// - 当前选中了什么建筑。
/// - 当前是否正在拖拽部署。
///
/// 这样做的核心好处是：
/// HUD 只依赖“结果”，不依赖总控内部的实现细节。
/// 以后即使玩法代码继续拆分，HUD 也更容易保持稳定。
/// </summary>
public readonly struct TowerDefenseHudState
{
    public TowerDefenseHudState(
        int currentScrap,
        int currentBaseHealth,
        int currentWave,
        int totalWaves,
        TowerType selectedTowerType,
        bool isPlacementDragActive,
        TowerType dragTowerType,
        PlacedStructureHudState placedStructureState,
        string transientNotice)
    {
        CurrentScrap = currentScrap;
        CurrentBaseHealth = currentBaseHealth;
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
        SelectedTowerType = selectedTowerType;
        IsPlacementDragActive = isPlacementDragActive;
        DragTowerType = dragTowerType;
        PlacedStructureState = placedStructureState;
        TransientNotice = transientNotice ?? string.Empty;
    }

    public int CurrentScrap { get; }

    public int CurrentBaseHealth { get; }

    public int CurrentWave { get; }

    public int TotalWaves { get; }

    public TowerType SelectedTowerType { get; }

    public bool IsPlacementDragActive { get; }

    public TowerType DragTowerType { get; }

    public PlacedStructureHudState PlacedStructureState { get; }

    public string TransientNotice { get; }
}

public readonly struct PlacedStructureHudState
{
    public PlacedStructureHudState(bool hasSelection, string title, string details)
    {
        HasSelection = hasSelection;
        Title = title ?? string.Empty;
        Details = details ?? string.Empty;
    }

    public bool HasSelection { get; }
    public string Title { get; }
    public string Details { get; }
}

/// <summary>
/// `TowerDragPreviewState` 是拖拽提示面板需要看到的局部状态。
///
/// 它只关心三件事：
/// - 现在拖的是哪种建筑。
/// - 当前鼠标落点是否合法。
/// - 如果不合法，失败原因是什么。
///
/// 这份状态之所以单独拆出来，
/// 是因为拖拽提示刷新频率很高，没必要每次都整包带上完整 HUD 状态。
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
/// `TowerDefenseHudPresenter` 负责把玩法层结果写进当前场景里的 HUD。
///
/// 这个类现在遵循“场景主导布局、脚本主导动态内容”的边界：
/// - 场景决定卡片放哪、面板长什么样、字体排版怎么摆。
/// - Presenter 负责把动态数字、动态说明和拖拽提示填进去。
///
/// 这样做以后，HUD 在 Scene / Inspector 里更容易直接调整，
/// 也不会一进 Play 就又被脚本整套摆回去。
/// </summary>
public sealed class TowerDefenseHudPresenter
{
    private TMP_Text _scrapText;
    private TMP_Text _baseHealthText;
    private TMP_Text _waveText;
    private TMP_Text _selectionText;

    private TMP_Text _gameOverTitle;
    private TMP_Text _gameOverHint;
    private TMP_Text _relayTowerButtonText;
    private TMP_Text _defenseTowerButtonText;
    private TMP_Text _slowFieldTowerButtonText;
    private TMP_Text _bombardTowerButtonText;
    private TMP_Text _clearSelectionButtonText;
    private TMP_Text _dragPreviewLabel;

    private Button _relayTowerButton;
    private Button _defenseTowerButton;
    private Button _slowFieldTowerButton;
    private Button _bombardTowerButton;
    private Button _clearSelectionButton;
    private GameObject _gameOverPanel;
    private GameObject _dragPreviewPanel;

    /// <summary>
    /// 由外部把已经在 Inspector 里拖好的 HUD 引用直接注入进来。
    ///
    /// 这是项目从“按名字查找场景对象”逐步迁移到“显式 Inspector 引用”的关键步骤：
    /// - 如果场景作者已经把引用拖好，Presenter 就直接使用这些确定对象。
    /// - 如果某些引用暂时还没拖，后续仍然可以给出缺项告警。
    ///
    /// 这样做的好处是迁移可以分阶段进行，
    /// 我们不需要一次性把所有场景重做完，
    /// 但新补好的场景已经能立刻摆脱“改名就炸”的脆弱模式。
    /// </summary>
    public void BindSceneReferences(
        TMP_Text scrapText,
        TMP_Text baseHealthText,
        TMP_Text waveText,
        TMP_Text selectionText,
        Button relayTowerButton,
        Button defenseTowerButton,
        Button slowFieldTowerButton,
        Button bombardTowerButton,
        Button clearSelectionButton,
        GameObject gameOverPanel,
        TMP_Text gameOverTitle,
        TMP_Text gameOverHint,
        GameObject dragPreviewPanel,
        TMP_Text dragPreviewLabel)
    {
        _scrapText = scrapText;
        _baseHealthText = baseHealthText;
        _waveText = waveText;
        _selectionText = selectionText;
        _relayTowerButton = relayTowerButton;
        _defenseTowerButton = defenseTowerButton;
        _slowFieldTowerButton = slowFieldTowerButton;
        _bombardTowerButton = bombardTowerButton;
        _clearSelectionButton = clearSelectionButton;
        _gameOverPanel = gameOverPanel;
        _gameOverTitle = gameOverTitle;
        _gameOverHint = gameOverHint;
        _dragPreviewPanel = dragPreviewPanel;
        _dragPreviewLabel = dragPreviewLabel;

        EnsureDragPreviewDoesNotBlockRaycasts();

        _relayTowerButtonText = _relayTowerButton != null ? _relayTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _defenseTowerButtonText = _defenseTowerButton != null ? _defenseTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _slowFieldTowerButtonText = _slowFieldTowerButton != null ? _slowFieldTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _bombardTowerButtonText = _bombardTowerButton != null ? _bombardTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _clearSelectionButtonText = _clearSelectionButton != null ? _clearSelectionButton.GetComponentInChildren<TMP_Text>(true) : null;
    }

    /// <summary>
    /// 对当前 HUD 引用做一次补齐与告警检查。
    ///
    /// 现在它不再主动按名字回捞整套 HUD，
    /// 这里只负责：
    /// - 补按钮内部文字缓存。
    /// - 纠正拖拽提示面板的射线设置。
    /// - 对缺失引用输出明确告警。
    /// </summary>
    public void FindSceneReferences()
    {
        if (_relayTowerButtonText == null && _relayTowerButton != null)
        {
            _relayTowerButtonText = _relayTowerButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (_defenseTowerButtonText == null && _defenseTowerButton != null)
        {
            _defenseTowerButtonText = _defenseTowerButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (_slowFieldTowerButtonText == null && _slowFieldTowerButton != null)
        {
            _slowFieldTowerButtonText = _slowFieldTowerButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (_bombardTowerButtonText == null && _bombardTowerButton != null)
        {
            _bombardTowerButtonText = _bombardTowerButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (_clearSelectionButtonText == null && _clearSelectionButton != null)
        {
            _clearSelectionButtonText = _clearSelectionButton.GetComponentInChildren<TMP_Text>(true);
        }

        EnsureDragPreviewDoesNotBlockRaycasts();

        WarnIfMissing(_scrapText, "ScrapText");
        WarnIfMissing(_baseHealthText, "BaseHealthText");
        WarnIfMissing(_waveText, "WaveText");
        WarnIfMissing(_selectionText, "SelectionText");
        WarnIfMissing(_relayTowerButton, "RelayTowerButton");
        WarnIfMissing(_defenseTowerButton, "DefenseTowerButton");
        WarnIfMissing(_slowFieldTowerButton, "SlowFieldTowerButton");
        WarnIfMissing(_bombardTowerButton, "BombardTowerButton");
        WarnIfMissing(_clearSelectionButton, "ClearSelectionButton");
        WarnIfMissing(_gameOverPanel, "GameOverPanel");
        WarnIfMissing(_gameOverTitle, "GameOverTitle");
        WarnIfMissing(_gameOverHint, "GameOverHint");
        WarnIfMissing(_dragPreviewPanel, "DragPreviewPanel");
        WarnIfMissing(_dragPreviewLabel, "DragPreviewLabel");
    }

    /// <summary>
    /// 拖拽提示面板只是跟随鼠标的视觉说明，不应该拦截任何鼠标释放事件。
    ///
    /// 否则玩家把塔拖到地图上时，鼠标下方其实压着这个提示面板本身，
    /// `EventSystem` 就会误以为“这次释放仍然发生在 UI 上”，
    /// 从而把一次本来合法的放塔当成取消操作。
    ///
    /// 这里同时把：
    /// - 面板背景 `Graphic` 的 `RaycastTarget` 关掉。
    /// - 文本本身的 `RaycastTarget` 关掉。
    /// - 整个面板挂一个 `CanvasGroup` 并关闭 `blocksRaycasts`。
    ///
    /// 这样就算场景里谁又手滑把某个 UI 组件的勾重新点上了，
    /// 运行时也会在 Presenter 绑定阶段把它纠正回“纯提示、不拦鼠标”的状态。
    /// </summary>
    private void EnsureDragPreviewDoesNotBlockRaycasts()
    {
        if (_dragPreviewPanel != null)
        {
            Graphic panelGraphic = _dragPreviewPanel.GetComponent<Graphic>();
            if (panelGraphic != null)
            {
                panelGraphic.raycastTarget = false;
            }

            CanvasGroup canvasGroup = _dragPreviewPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = _dragPreviewPanel.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (_dragPreviewLabel != null)
        {
            _dragPreviewLabel.raycastTarget = false;
        }
    }

    private static void WarnIfMissing(UnityEngine.Object reference, string expectedName)
    {
        if (reference == null)
        {
            Debug.LogWarning($"TowerDefenseHudPresenter is missing HUD reference: {expectedName}. Check the scene wiring.");
        }
    }

    /// <summary>
    /// 根据塔目录统一配置两张部署卡的静态文案。
    ///
    /// 这里仍然保留少量格式控制，因为部署卡文案本身是由玩法数据驱动的：
    /// - 展示名会变。
    /// - 成本会变。
    /// - 扩张方格说明会变。
    ///
    /// 但这里不会再接管卡片位置、底板样式和整个右侧区布局，
    /// 那些内容现在应该主要由场景来控制。
    /// </summary>
    public void ConfigureCardLabels(TowerCatalog towerCatalog)
    {
        ConfigureTowerCardLabel(_relayTowerButtonText, towerCatalog.GetDefinition(TowerType.Relay));
        ConfigureTowerCardLabel(_defenseTowerButtonText, towerCatalog.GetDefinition(TowerType.SingleTarget));
        ConfigureTowerCardLabel(_slowFieldTowerButtonText, towerCatalog.GetDefinition(TowerType.SlowField));
        ConfigureTowerCardLabel(_bombardTowerButtonText, towerCatalog.GetDefinition(TowerType.Bombard));

        if (_clearSelectionButtonText != null)
        {
            _clearSelectionButtonText.text = "CANCEL DEPLOY\n<size=20><color=#A4B2C0>Esc / RMB</color></size>";
            _clearSelectionButtonText.alignment = TextAlignmentOptions.Center;
        }
    }

    /// <summary>
    /// 刷新常驻 HUD。
    ///
    /// 这里更新的是“值”和“状态”，不是“版式骨架”。
    /// 所以你在场景里调好的布局会被保留下来；
    /// 脚本只负责把当前游戏状态填进对应文本里。
    /// </summary>
    public void Refresh(TowerDefenseHudState state, TowerCatalog towerCatalog, Func<TowerType, bool> canAffordTower)
    {
        if (_scrapText != null)
        {
            _scrapText.text = BuildMetricText("SCRAP STOCK", state.CurrentScrap.ToString(), "FFB567");
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

        UpdateButtonInteractableState(canAffordTower);
    }

    /// <summary>
    /// 接收玩法层发来的状态消息。
    ///
    /// 现在项目已经移除了常驻 `StatusStrip`，
    /// 所以这里保留接口但不再显示固定状态栏。
    /// 这样可以避免调用链断裂，同时把表现权交给别的提示 UI。
    /// </summary>
    public void SetStatusMessage(string message)
    {
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
    /// 这里仍然保留“跟着鼠标走”的行为，
    /// 因为它本来就属于交互期动态反馈，而不是应该由场景固定摆死的界面。
    ///
    /// 同时这里会根据当前塔型和合法性结果，
    /// 实时刷新提示文案，帮助玩家理解：
    /// - 当前拖的是发电机还是炮塔。
    /// - 这次落点为什么能放或不能放。
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
            $"<size=20><color=#{accentHex}>{definition.BuildCostLabel}</color>  <color=#88A5BC>GRID {definition.ExpansionSquareSize:0.0}</color></size>\n" +
            "<size=18><color=#87A5BD>Cyan sectors show exact legal drop zones</color></size>\n" +
            $"<size=18>{stateLine}</size>";
    }

    /// <summary>
    /// 显示 `Game Over` 面板并填入文案。
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
    /// 单独控制 `Game Over` 面板显隐。
    /// </summary>
    public void SetGameOverVisible(bool visible)
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(visible);
        }
    }

    /// <summary>
    /// 配置单张部署卡的文案。
    ///
    /// 这里保留少量文本排版控制，
    /// 只是为了确保多行卡片文案在当前卡片里能稳定读清楚。
    /// 但它不会再去改按钮位置、父物体布局或整个右侧区结构。
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
    /// 组装“当前选中 / 当前拖拽”的说明文案。
    ///
    /// 这部分仍然由脚本生成，
    /// 因为它本质上就是当前状态的动态摘要，而不是固定装饰性文本。
    /// </summary>
    private string BuildSelectionText(TowerDefenseHudState state, TowerCatalog towerCatalog)
    {
        if (state.IsPlacementDragActive)
        {
            TowerDefinition draggingDefinition = towerCatalog.GetDefinition(state.DragTowerType);
            if (draggingDefinition != null)
            {
                string accentHex = ColorUtility.ToHtmlStringRGB(draggingDefinition.AccentColor);
                return AppendTransientNotice(
                    baseText:
                    "DEPLOY TRACE\n" +
                    $"<size=30>{draggingDefinition.DisplayName}</size>\n" +
                    $"<size=20><color=#{accentHex}>{draggingDefinition.BuildCostLabel}</color>  <color=#89A7BF>Cyan sectors = exact legal zone</color></size>",
                    transientNotice: state.TransientNotice);
            }
        }

        if (state.SelectedTowerType != TowerType.None)
        {
            TowerDefinition selectedDefinition = towerCatalog.GetDefinition(state.SelectedTowerType);
            if (selectedDefinition != null)
            {
                string accentHex = ColorUtility.ToHtmlStringRGB(selectedDefinition.AccentColor);
                string economyLine = BuildSelectionEconomyLine(state.CurrentScrap, selectedDefinition);
                return AppendTransientNotice(
                    baseText:
                    "TACTICAL READY\n" +
                    $"<size=30>{selectedDefinition.DisplayName}</size>\n" +
                    $"<size=20><color=#{accentHex}>{selectedDefinition.SelectionHint}</color></size>\n" +
                    $"<size=18><color=#8AA7BF>{selectedDefinition.UpgradeFocusSummary}</color></size>\n" +
                    $"<size=18>{economyLine}</size>",
                    transientNotice: state.TransientNotice);
            }
        }

        if (state.PlacedStructureState.HasSelection)
        {
            return AppendTransientNotice(
                baseText:
                "STRUCTURE LINK\n" +
                $"<size=30>{state.PlacedStructureState.Title}</size>\n" +
                $"<size=18><color=#89A7BF>{state.PlacedStructureState.Details}</color></size>",
                transientNotice: state.TransientNotice);
        }

        return AppendTransientNotice(
            baseText:
            "OPERATION LINK\n" +
            "<size=28>Click or drag a tower card to project legal sectors</size>\n" +
            "<size=20><color=#89A7BF>1 Relay / 2 Single / 3 Slow / 4 Bomb / Esc Cancel</color></size>",
            transientNotice: state.TransientNotice);
    }

    private static string AppendTransientNotice(string baseText, string transientNotice)
    {
        if (string.IsNullOrWhiteSpace(transientNotice))
        {
            return baseText;
        }

        string accentColor = transientNotice.StartsWith("+", StringComparison.Ordinal)
            ? "#7DF3B1"
            : "#FFD878";
        return $"{baseText}\n<size=18><color={accentColor}>{transientNotice}</color></size>";
    }

    private static string BuildSelectionEconomyLine(int currentScrap, TowerDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        if (definition.BuildCost <= 0)
        {
            return "<color=#7DF3B1>FREE deploy. Scrap remains unchanged.</color>";
        }

        int remainingAfterBuild = currentScrap - definition.BuildCost;
        if (remainingAfterBuild >= 0)
        {
            return $"<color=#7DF3B1>{remainingAfterBuild} SCRAP left after deploy.</color>";
        }

        return $"<color=#FF7282>Need {-remainingAfterBuild} more SCRAP to deploy.</color>";
    }

    /// <summary>
    /// 在“场景主导 HUD”的模式下，只维护按钮的可交互状态。
    ///
    /// 也就是说：
    /// - 场景负责这些按钮长什么样。
    /// - 脚本只负责告诉它们当前能不能点。
    ///
    /// 如果你想修改不可购买时的颜色、选中时的高亮、按下时的过渡，
    /// 现在更推荐直接去 Button 的 `Transition / ColorBlock` 里改。
    /// </summary>
    private void UpdateButtonInteractableState(Func<TowerType, bool> canAffordTower)
    {
        if (_relayTowerButton != null)
        {
            _relayTowerButton.interactable = canAffordTower(TowerType.Relay);
        }

        if (_defenseTowerButton != null)
        {
            _defenseTowerButton.interactable = canAffordTower(TowerType.SingleTarget);
        }

        if (_slowFieldTowerButton != null)
        {
            _slowFieldTowerButton.interactable = canAffordTower(TowerType.SlowField);
        }

        if (_bombardTowerButton != null)
        {
            _bombardTowerButton.interactable = canAffordTower(TowerType.Bombard);
        }

        if (_clearSelectionButton != null)
        {
            _clearSelectionButton.interactable = true;
        }
    }

    /// <summary>
    /// 组装顶部资源卡的富文本。
    ///
    /// 这里返回的是“内容格式”，不是布局格式。
    /// 卡片放哪、字号多大、外边距多少，应该主要由场景控制；
    /// 但每张卡内部标签和数值的层级关系，仍然适合由代码统一生成。
    /// </summary>
    private static string BuildMetricText(string label, string value, string accentHex)
    {
        return
            $"<size=18><color=#8EA8BE>{label}</color></size>\n" +
            $"<size=56><color=#{accentHex}>{value}</color></size>";
    }
}
