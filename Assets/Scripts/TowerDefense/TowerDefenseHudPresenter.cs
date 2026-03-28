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
            _clearSelectionButtonText.text = "CANCEL DEPLOY\n<size=22><color=#A4B2C0>Esc / RMB</color></size>";
            _clearSelectionButtonText.alignment = TextAlignmentOptions.Center;
            _clearSelectionButtonText.margin = new Vector4(20f, 16f, 20f, 16f);
        }
    }

    /// <summary>
    /// 刷新常驻 HUD。
    ///
    /// 这里接收的是已经整理好的状态快照和塔目录，
    /// 因此 HUD 不需要知道总控内部字段是怎么组织的。
    /// </summary>
    public void Refresh(TowerDefenseHudState state, TowerCatalog towerCatalog, Func<TowerType, bool> canAffordTower)
    {
        if (_energyText != null)
        {
            _energyText.text = $"ENERGY\n<size=54>{state.CurrentEnergy}</size>";
        }

        if (_baseHealthText != null)
        {
            _baseHealthText.text = $"BASE CORE\n<size=54>{state.CurrentBaseHealth}</size>";
        }

        if (_waveText != null)
        {
            string waveDisplay = state.TotalWaves > 0 ? $"{state.CurrentWave}/{state.TotalWaves}" : "0/0";
            _waveText.text = $"WAVE\n<size=54>{waveDisplay}</size>";
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
                screenPosition + new Vector2(118f, -72f),
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

        string stateLine = previewState.IsValid
            ? "<color=#FFAF64>READY TO DEPLOY</color>"
            : $"<color=#FF6271>{previewState.InvalidReason}</color>";

        _dragPreviewLabel.text =
            $"{definition.DisplayName.ToUpperInvariant()}\n" +
            $"<size=24>{definition.BuildCost} EN</size>\n" +
            $"<size=20>{stateLine}</size>";
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
        label.margin = new Vector4(28f, 18f, 24f, 18f);
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
                return
                    $"DEPLOYING\n<size=30>{draggingDefinition.DisplayName}</size>\n" +
                    $"<size=24>{draggingDefinition.BuildCost} EN / Release to deploy</size>";
            }
        }

        if (state.SelectedTowerType != TowerType.None)
        {
            TowerDefinition selectedDefinition = towerCatalog.GetDefinition(state.SelectedTowerType);
            if (selectedDefinition != null)
            {
                return
                    $"SELECTED\n<size=30>{selectedDefinition.DisplayName}</size>\n" +
                    "<size=24>Click map or drag card to deploy</size>";
            }
        }

        return
            "OPERATION\n<size=28>Drag a tower card onto the battlefield</size>\n" +
            "<size=22>1 Relay / 2 Defense / Esc Cancel</size>";
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
            state.SelectedTowerType == TowerType.Relay,
            relayDefinition != null && canAffordTower(TowerType.Relay),
            relayDefinition != null ? relayDefinition.AccentColor : Color.white);

        UpdateShopButtonVisual(
            _defenseTowerButton,
            _defenseTowerButtonText,
            state.SelectedTowerType == TowerType.Defense,
            defenseDefinition != null && canAffordTower(TowerType.Defense),
            defenseDefinition != null ? defenseDefinition.AccentColor : Color.white);

        UpdateShopButtonVisual(
            _clearSelectionButton,
            _clearSelectionButtonText,
            state.SelectedTowerType == TowerType.None && !state.IsPlacementDragActive,
            true,
            new Color(0.65f, 0.72f, 0.82f, 1f));
    }

    /// <summary>
    /// 刷新单张按钮 / 卡片的背景与文字颜色。
    /// </summary>
    private void UpdateShopButtonVisual(Button button, TMP_Text label, bool isSelected, bool isAvailable, Color accentColor)
    {
        if (button != null && button.targetGraphic != null)
        {
            Color idleColor = isAvailable ? new Color(0.08f, 0.11f, 0.16f, 0.92f) : new Color(0.08f, 0.09f, 0.11f, 0.56f);
            Color selectedColor = Color.Lerp(idleColor, accentColor, 0.76f);
            button.targetGraphic.color = isSelected ? selectedColor : idleColor;
        }

        if (label != null)
        {
            label.color = !isAvailable
                ? new Color(0.55f, 0.60f, 0.68f, 1f)
                : (isSelected ? Color.white : new Color(0.93f, 0.96f, 1f, 1f));
        }
    }
}
