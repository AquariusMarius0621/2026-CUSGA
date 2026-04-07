using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TowerDefenseHudState 是 HUD 每次刷新时真正需要的最小状态快照。
///
/// 这里故意不把整个 `TowerDefenseGame` 直接暴露给 HUD，
/// 而是只传表现层真正关心的几项结果：
/// - 当前资源
/// - 当前基地血量
/// - 当前波次
/// - 当前选中了什么建筑
/// - 当前是否正在拖拽部署
///
/// 这样做的核心好处是：
/// HUD 只依赖“结果”，不依赖总控内部的具体实现细节，
/// 以后即使玩法代码继续拆分，HUD 也更容易保持稳定。
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
/// TowerDragPreviewState 是拖拽提示面板需要看到的局部状态。
///
/// 它只关注三件事：
/// - 现在拖的是哪种建筑
/// - 当前鼠标落点是否合法
/// - 如果不合法，失败原因是什么
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
/// TowerDefenseHudPresenter 是当前塔防玩法 HUD 的表现层适配器。
///
/// 这一版的设计目标非常明确：
/// - 场景负责布局和大部分视觉样式
/// - 脚本只负责动态内容刷新和少量必要状态同步
///
/// 也就是说，这个类现在不再承担：
/// - 运行时强制重排顶部卡片和右侧部署区
/// - 运行时大规模重写面板样式
/// - 运行时生成一堆额外装饰节点
///
/// 这样你以后打开 `SampleScene` 时，
/// 看到的 HUD 基本就是可以直接在 Scene / Inspector 里改的真实界面，
/// 不会一进 Play 就又被脚本全部摆回去。
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
    /// 由外部把已经在 Inspector 中拖好的 HUD 引用直接注入进来。
    ///
    /// 这一步是当前项目从“按名字查找场景对象”逐步迁移到“显式 Inspector 引用”的第一步：
    /// - 如果场景作者已经把引用拖好，HUDPresenter 就直接使用这些确定的对象
    /// - 如果某些引用暂时还没拖，后面 FindSceneReferences() 仍然会继续补走名字查找兜底
    ///
    /// 这样做的好处是迁移可以分阶段进行：
    /// 我们不需要一次性把所有场景都重做完，
    /// 但新补好的场景引用已经能立刻摆脱“改名就炸”的脆弱模式。
    /// </summary>
    public void BindSceneReferences(
        TMP_Text energyText,
        TMP_Text baseHealthText,
        TMP_Text waveText,
        TMP_Text selectionText,
        TMP_Text statusText,
        Button relayTowerButton,
        Button defenseTowerButton,
        Button clearSelectionButton,
        GameObject gameOverPanel,
        TMP_Text gameOverTitle,
        TMP_Text gameOverHint,
        GameObject dragPreviewPanel,
        TMP_Text dragPreviewLabel)
    {
        _energyText = energyText;
        _baseHealthText = baseHealthText;
        _waveText = waveText;
        _selectionText = selectionText;
        _statusText = statusText;
        _relayTowerButton = relayTowerButton;
        _defenseTowerButton = defenseTowerButton;
        _clearSelectionButton = clearSelectionButton;
        _gameOverPanel = gameOverPanel;
        _gameOverTitle = gameOverTitle;
        _gameOverHint = gameOverHint;
        _dragPreviewPanel = dragPreviewPanel;
        _dragPreviewLabel = dragPreviewLabel;

        _relayTowerButtonText = _relayTowerButton != null ? _relayTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _defenseTowerButtonText = _defenseTowerButton != null ? _defenseTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _clearSelectionButtonText = _clearSelectionButton != null ? _clearSelectionButton.GetComponentInChildren<TMP_Text>(true) : null;
    }

    /// <summary>
    /// 按当前项目仍在使用的对象名约定，把 HUD 相关引用找齐。
    ///
    /// 这一层仍然使用名字查找，
    /// 是因为项目整体还没完全切到 Inspector 直拖引用的装配方式。
    /// 所以：
    /// - 场景里这些对象名仍然要保持稳定
    /// - 如果你改名，就要同步改脚本默认值
    /// </summary>
    public void FindSceneReferences()
    {
        // 这一轮开始，HUD 主链不再主动按名字回捞场景对象。
        // 对当前 SampleScene 而言，核心 HUD 节点都应当已经通过外部显式绑定传进来。
        // 所以这里现在只做两件事：
        // 1. 补齐按钮内部的子文本缓存
        // 2. 对缺失引用给出明确告警，帮助场景装配尽快暴露问题
        if (_relayTowerButtonText == null && _relayTowerButton != null)
        {
            _relayTowerButtonText = _relayTowerButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (_defenseTowerButtonText == null && _defenseTowerButton != null)
        {
            _defenseTowerButtonText = _defenseTowerButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (_clearSelectionButtonText == null && _clearSelectionButton != null)
        {
            _clearSelectionButtonText = _clearSelectionButton.GetComponentInChildren<TMP_Text>(true);
        }

        WarnIfMissing(_energyText, _energyTextName);
        WarnIfMissing(_baseHealthText, _baseHealthTextName);
        WarnIfMissing(_waveText, _waveTextName);
        WarnIfMissing(_selectionText, _selectionTextName);
        WarnIfMissing(_statusText, _statusTextName);
        WarnIfMissing(_relayTowerButton, _relayTowerButtonName);
        WarnIfMissing(_defenseTowerButton, _defenseTowerButtonName);
        WarnIfMissing(_clearSelectionButton, _clearSelectionButtonName);
        WarnIfMissing(_gameOverPanel, _gameOverPanelName);
        WarnIfMissing(_gameOverTitle, _gameOverTitleName);
        WarnIfMissing(_gameOverHint, _gameOverHintName);
        WarnIfMissing(_dragPreviewPanel, _dragPreviewPanelName);
        WarnIfMissing(_dragPreviewLabel, _dragPreviewLabelName);
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
    /// 这里仍然保留少量格式控制，原因是部署卡文案本身是由玩法数据驱动的：
    /// - 展示名会变
    /// - 成本会变
    /// - 扩张方格说明会变
    ///
    /// 但这里不会再接管卡片位置、底板样式和整个右侧区布局，
    /// 那些内容现在应该主要由场景来控制。
    /// </summary>
    public void ConfigureCardLabels(TowerCatalog towerCatalog)
    {
        ConfigureTowerCardLabel(_relayTowerButtonText, towerCatalog.GetDefinition(TowerType.Relay));
        ConfigureTowerCardLabel(_defenseTowerButtonText, towerCatalog.GetDefinition(TowerType.Defense));

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
    /// 所以你在场景里调好的布局，会被保留下来；
    /// 脚本只负责把当前游戏状态填进对应文本里。
    /// </summary>
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

        UpdateButtonInteractableState(canAffordTower);
    }

    /// <summary>
    /// 更新底部状态条消息。
    ///
    /// 注意这里不改消息内容本身，
    /// 因为“什么时候说什么”属于玩法层决策；
    /// HUD 只是把总控给出的结果显示出来。
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
    /// 这里仍然保留“跟着鼠标走”的行为，
    /// 因为它本来就属于交互期动态反馈，
    /// 而不是应该由场景固定摆死的界面。
    ///
    /// 同时这里会根据当前塔型和合法性结果，
    /// 实时刷新提示文案，帮助玩家理解：
    /// - 当前拖的是发电机还是炮塔
    /// - 这次落点为什么能放 / 不能放
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
    /// 显示 Game Over 面板并填入文案。
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
    /// 单独控制 Game Over 面板显隐。
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
    /// 但它不会再去改按钮位置、父物体布局或整个右侧区域结构。
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
    /// 在“场景主导 HUD”的模式下，只维护按钮的可交互状态。
    ///
    /// 也就是说：
    /// - 场景负责这些按钮长什么样
    /// - 脚本只负责告诉它们当前能不能点
    ///
    /// 如果你想修改不可购买时的颜色、选中时的高亮、按下时的过渡，
    /// 现在更推荐直接去 Button 的 Transition / ColorBlock 里改。
    /// </summary>
    private void UpdateButtonInteractableState(Func<TowerType, bool> canAffordTower)
    {
        if (_relayTowerButton != null)
        {
            _relayTowerButton.interactable = canAffordTower(TowerType.Relay);
        }

        if (_defenseTowerButton != null)
        {
            _defenseTowerButton.interactable = canAffordTower(TowerType.Defense);
        }

        if (_clearSelectionButton != null)
        {
            _clearSelectionButton.interactable = true;
        }
    }

    /// <summary>
    /// 组装顶部资源卡的富文本。
    ///
    /// 这里返回的是“内容格式”，不是布局格式：
    /// 卡片放哪、字块多大、外边距多少，应该主要由场景控制；
    /// 但每张卡文字里标签和数值的层级关系，仍然适合由代码统一生成。
    /// </summary>
    private static string BuildMetricText(string label, string value, string accentHex)
    {
        return
            $"<size=18><color=#8EA8BE>{label}</color></size>\n" +
            $"<size=56><color=#{accentHex}>{value}</color></size>";
    }
}