using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// `TowerType` 描述当前原型里玩家可部署的建筑类型。
/// `None` 表示未选择，`Relay` 表示发电机，`Defense` 表示防御塔。
/// 这个枚举会贯穿部署卡、拖拽预览、放置校验、扣费和真正建塔等整条链路。
/// </summary>
public enum TowerType
{
    None,
    Relay,
    Defense
}

/// <summary>
/// `TowerDefenseGame` 是当前塔防原型的整局总协调器。
/// 它负责把“运行状态、放置规则、放置交互、建塔执行、HUD 表现”这些子模块装配成一条完整主链。
/// 需要注意的是：资源/基地/波次状态、放置交互和真正建塔执行都已经继续下沉到独立组件里，
/// 所以这个类越来越像一个编排层，而不是继续把所有细节都塞进一个上帝脚本。
/// </summary>
public class TowerDefenseGame : MonoBehaviour
{
    /// <summary>
    /// 当前场景中的总控单例。部署卡、旧版 BuildPad 兼容桥和部分运行时对象会通过它拿到统一入口。
    /// </summary>
    public static TowerDefenseGame Instance { get; private set; }

    [Header("Core Rules")]
    [SerializeField] private int startingEnergy = 80;
    [SerializeField] private int startingBaseHealth = 10;
    [SerializeField] private int relayTowerCost = 30;
    [SerializeField] private int defenseTowerCost = 45;

    [Header("Placement Rules")]
    [SerializeField] private float relayPlacementRadius = 0.52f;
    [SerializeField] private float defensePlacementRadius = 0.58f;

    [Header("Placement Expansion")]
    [SerializeField] private float relayExpansionSquareSize = 4.5f;
    [SerializeField] private float defenseExpansionSquareSize = 4.5f;
    [SerializeField] private Vector2 initialPlacementSquareCenter = new Vector2(-6.5f, -2.25f);
    [SerializeField] private float initialPlacementSquareSize = 3f;

    [Header("Placement Preview")]
    [SerializeField] private Color validPreviewColor = new Color(0.26f, 0.95f, 0.78f, 0.72f);
    [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.32f, 0.38f, 0.72f);
    [SerializeField] private string placementRingResourcePath = "UI/placement-ring";

    [Header("Placement Overlay")]
    [SerializeField] private float placementAreaOverlayPixelsPerUnit = 20f;
    [SerializeField] private Color placementAreaOverlayFillColor = new Color(0.18f, 0.82f, 0.86f, 0.16f);
    [SerializeField] private Color placementAreaOverlayEdgeColor = new Color(0.72f, 1f, 0.97f, 0.52f);
    [SerializeField] private int placementAreaOverlaySortingOrder = 12;

    [Header("Starter Zone Marker")]
    [SerializeField] private Color starterZoneMarkerFillColor = new Color(0.22f, 0.82f, 0.88f, 0.22f);
    [SerializeField] private Color starterZoneMarkerEdgeColor = new Color(0.9f, 1f, 0.98f, 1f);
    [SerializeField] private int starterZoneMarkerSortingOrder = 10;

    [Header("Scene References (Preferred)")]

    /// <summary>
    /// 这一组是玩法主链路优先使用的显式场景引用。
    /// 包括主相机、塔原型、运行时根节点和 `BuildZone`。如果这些引用已经在 Inspector 里配好，
    /// 运行时就不应该再依赖对象名查找；名字字段只保留给过渡期兜底或运行时容器命名。
    /// </summary>
    [SerializeField] private Camera mainCameraReference;
    [SerializeField] private GameObject relayTowerPrototypeReference;
    [SerializeField] private GameObject defenseTowerPrototypeReference;
    [SerializeField] private Transform placedTowerRootReference;
    [SerializeField] private Transform placementPreviewRootReference;
    [SerializeField] private BuildZone buildZoneReference;

    [Header("Scene Object Names")]
    [SerializeField] private string placedTowerRootName = "PlacedTowers";
    [SerializeField] private string placementPreviewRootName = "PlacementPreviewRoot";
    [SerializeField] private string buildZoneName = "BuildZone";

    [Header("HUD References (Preferred)")]

    /// <summary>
    /// 这一组是玩法 HUD 的显式场景引用。
    /// 当前策略是优先直接拖 Inspector，引导项目逐步摆脱按名字查找 UI 对象的旧做法。
    /// </summary>
    [SerializeField] private TMP_Text energyTextReference;
    [SerializeField] private TMP_Text baseHealthTextReference;
    [SerializeField] private TMP_Text waveTextReference;
    [SerializeField] private TMP_Text selectionTextReference;

    [SerializeField] private Button relayTowerButtonReference;
    [SerializeField] private Button defenseTowerButtonReference;
    [SerializeField] private Button clearSelectionButtonReference;
    [SerializeField] private GameObject gameOverPanelReference;
    [SerializeField] private TMP_Text gameOverTitleReference;
    [SerializeField] private TMP_Text gameOverHintReference;
    [SerializeField] private GameObject dragPreviewPanelReference;
    [SerializeField] private TMP_Text dragPreviewLabelReference;

    /// <summary>
    /// `_sessionState` 负责保存这一局的资源、基地、波次和结算状态。
    /// 它是当前总控最核心的一份“局内运行状态源”。
    /// </summary>
    private TowerDefenseSessionState _sessionState;

    private GameObject _relayTowerPrototype;
    private GameObject _defenseTowerPrototype;
    private Camera _mainCamera;
    private BuildZone _buildZone;
    private Transform _placedTowerRoot;
    private Transform _placementPreviewRoot;
    private TowerPlacementRules _placementRules;

    /// <summary>
    /// `_placementVisualController` 负责放置阶段的可视化反馈。
    /// 它会统一管理预览塔、合法区域覆盖层和首塔起手区标记，让 `TowerDefenseGame` 只保留调度职责。
    /// </summary>
    private TowerPlacementVisualController _placementVisualController;

    /// <summary>
    /// `_placementInteractionController` 负责“玩家怎样进入放置流程、怎样更新流程、怎样结束流程”。
    /// 这一轮把交互状态从总控里迁出去后，
    /// `TowerDefenseGame` 更明确地退回到“整局编排 + 真正建塔 + HUD 刷新入口”的职责边界。
    /// </summary>
    private TowerPlacementInteractionController _placementInteractionController;

    /// <summary>
    /// `_placementBuildExecutor` 负责真正建塔这一段执行链。
    /// 也就是：最终校验、实例化塔、兼容旧 BuildPad、补碰撞体、扣费和放置成功后的收尾刷新。
    /// 这样总控就不用再同时承担“整局状态管理”和“建塔流水线细节”两种职责。
    /// </summary>
    private TowerPlacementBuildExecutor _placementBuildExecutor;

    /// <summary>
    /// `_towerCatalog` 提供塔的静态定义，例如显示名、造价、占地半径和扩张方格边长。
    /// 总控通过它读配置，而不是把这些常量散落在很多 `switch` 里。
    /// </summary>
    private TowerCatalog _towerCatalog;

    /// <summary>
    /// `_hudPresenter` 是 HUD 表现层适配器。
    /// 它只负责把当前状态刷到界面上，并同步拖拽提示、按钮可用性与结算面板。
    /// 这样做的目的，是把“状态计算”和“界面呈现”分开，减少总控脚本继续膨胀。
    /// </summary>
    private TowerDefenseHudPresenter _hudPresenter;

    /// <summary>
    /// 对外暴露只读的结算状态，方便 HUD、敌人和其他运行时对象判断当前是否已经 Game Over。
    /// </summary>
    public bool IsGameOver => _sessionState != null && _sessionState.IsGameOver;

    /// <summary>
    /// `Awake()` 负责建立单例、锁定基础运行参数，并把场景引用与协作模块先装配起来。
    /// 之所以把这些初始化尽量前置，是为了避免部署卡、刷怪器或 HUD 在 `Start()` 前访问到半初始化状态。
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
        Application.runInBackground = true;

        _sessionState = new TowerDefenseSessionState(startingEnergy, startingBaseHealth);
        InitializeArchitectureModules();
    }

    /// <summary>
    /// 释放放置可视化控制器，并在对象销毁时安全清理单例引用。
    /// 这样可以避免场景重载或脚本重编译后残留旧实例状态。
    /// </summary>
    private void OnDestroy()
    {
        _placementVisualController?.Dispose();
        _placementVisualController = null;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// `Start()` 负责完成进入关卡后的首轮就绪工作。
    /// 包括补齐场景引用、建立运行时根节点、配置 HUD、隐藏初始面板、刷新首塔标记，
    /// 以及执行一次起手区自检，确保第一时间就能发现“首塔放不下”到底是交互问题还是规则问题。
    /// </summary>
    private void Start()
    {
        FindSceneReferences();
        EnsureRuntimeRoots();
        RefreshPlacementRuleContext();
        InitializePlacementVisuals();
        _hudPresenter.ConfigureCardLabels(_towerCatalog);

        SetStatusMessage("Place your first structure in the starter zone. Drag a Generator or Turret into a highlighted legal area. Hotkeys: 1 / 2.");
        RefreshHud();

        _hudPresenter.SetGameOverVisible(false);
        _hudPresenter.SetDragPreviewVisible(false);
        HidePlacementAreaOverlay();
        RefreshStarterZoneMarker();
        RunStarterPlacementSanityCheck();
    }

    /// <summary>
    /// `Update()` 只负责处理输入入口。
    /// 这里刻意保持轻量，把热键和快速点击放置拆开处理，避免在每帧主循环里塞入过多业务细节。
    /// </summary>
    private void Update()
    {
        HandleHotkeys();
        HandleQuickPlacementInput();
    }

    /// <summary>
    /// 旧版 `BuildPad` 入口的兼容桥。
    /// 虽然当前主玩法已经改成自由拖拽部署，但这个方法还能把固定塔位请求转发到统一的真正建塔逻辑里。
    /// </summary>
    public bool TryPlaceTower(BuildPad pad)
    {
        if (pad == null)
        {
            return false;
        }

        TowerType selectedTowerType = _placementInteractionController != null
            ? _placementInteractionController.SelectedTowerType
            : TowerType.None;
        return TryPlaceTowerAt(pad.GetBuildPosition(), selectedTowerType, pad);
    }

    /// <summary>
    /// 开始一次新的拖拽部署流程。
    /// 这里会先检查塔型、资源和原型体是否有效，再进入拖拽状态并生成对应的预览反馈。
    /// </summary>
    public bool BeginPlacementDrag(TowerType towerType, Vector2 screenPosition)
    {
        return _placementInteractionController != null &&
               _placementInteractionController.BeginPlacementDrag(towerType, screenPosition);
    }

    /// <summary>
    /// 在拖拽过程中持续更新预览塔。
    /// 每一帧都会把屏幕坐标换算到世界坐标，重跑放置校验，然后同步更新预览表现和拖拽提示面板。
    /// </summary>
    public void UpdatePlacementDrag(Vector2 screenPosition)
    {
        _placementInteractionController?.UpdatePlacementDrag(screenPosition);
    }

    /// <summary>
    /// 在玩家松手时结束拖拽部署。
    /// 这里会先拿到最终鼠标位置和合法性结果，再决定是正式建塔、提示失败，还是仅仅取消本次拖拽。
    /// </summary>
    public void EndPlacementDrag(Vector2 screenPosition, bool releasedOverUserInterface)
    {
        _placementInteractionController?.EndPlacementDrag(screenPosition, releasedOverUserInterface);
    }


    /// <summary>
    /// 对外暴露的取消拖拽入口。
    /// 按钮、快捷键和其他外部调用都可以走这里，统一回收预览与临时状态。
    /// </summary>
    public void CancelPlacementDrag()
    {
        _placementInteractionController?.CancelPlacementDrag();
    }

    /// <summary>
    /// 增加电量。
    /// 这里只接受正数收入，并且在 Game Over 后不再改动局内资源。
    /// </summary>
    public void AddEnergy(int amount)
    {
        if (_sessionState == null || !_sessionState.TryAddEnergy(amount))
        {
            return;
        }

        RefreshHud();
    }

    /// <summary>
    /// 让基地承受一次伤害。
    /// 方法会扣血、刷新 HUD、推送提示，并在生命降到零时切到 Game Over。
    /// </summary>
    public void DamageBase(int amount)
    {
        if (_sessionState == null || !_sessionState.TryApplyBaseDamage(amount, out int actualDamage, out bool baseDepleted))
        {
            return;
        }

        RefreshHud();
        SetStatusMessage($"An enemy slipped through. Base lost {actualDamage} HP.");

        if (baseDepleted)
        {
            ShowGameOver();
        }
    }

    /// <summary>
    /// 同步当前波次进度，并刷新顶部 HUD 的波次显示。
    /// </summary>
    public void SetWaveProgress(int currentWave, int totalWaves)
    {
        _sessionState?.SetWaveProgress(currentWave, totalWaves);
        RefreshHud();
    }

    /// <summary>
    /// 向 HUD 层发送状态消息。
    /// 虽然当前常驻 `StatusStrip` 已移除，但保留这个入口仍然有价值，
    /// 因为它让结算、放置失败和调试提示继续有统一出口。
    /// </summary>
    public void SetStatusMessage(string message)
    {
        _hudPresenter?.SetStatusMessage(message);
    }

    /// <summary>
    /// 选中发电机，供按钮事件或快捷键直接调用。
    /// </summary>
    public void SelectRelayTower()
    {
        _placementInteractionController?.SelectRelayTower();
    }

    /// <summary>
    /// 选中防御塔，供按钮事件或快捷键直接调用。
    /// </summary>
    public void SelectDefenseTower()
    {
        _placementInteractionController?.SelectDefenseTower();
    }

    /// <summary>
    /// 清空当前部署选择。
    /// 这里会同时取消拖拽中的预览状态，避免界面显示和内部选择状态脱节。
    /// </summary>
    public void ClearSelection()
    {
        _placementInteractionController?.ClearSelection();
    }

    /// <summary>
    /// 判断当前电量是否足够支付指定塔型的造价。
    /// `None` 永远视为不可购买，这样可以避免“未选中状态”误走通过分支。
    /// </summary>
    public bool CanAffordTower(TowerType towerType)
    {
        if (towerType == TowerType.None)
        {
            return false;
        }

        return _sessionState != null && _sessionState.CanAfford(GetTowerCost(towerType));
    }

    /// <summary>
    /// 处理本场景约定的快捷键。
    /// `1 / 2` 用来快速切换发电机和防御塔，`Esc / 右键` 用来取消当前部署状态。
    /// </summary>
    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectRelayTower();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectDefenseTower();
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ClearSelection();
        }
    }

    /// <summary>
    /// 处理“已选中塔型但没有拖拽时”的快速点击放置入口。
    /// 规则是：必须已经选塔、不能处于拖拽中、不能点在会拦截玩法的 UI 上，然后才尝试按鼠标世界坐标直接落塔。
    /// </summary>
    private void HandleQuickPlacementInput()
    {
        if (IsGameOver || _placementInteractionController == null)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0) || IsPointerOverUserInterface())
        {
            return;
        }

        _placementInteractionController.TryQuickPlacementAt(GetMouseWorldPosition());
    }


    /// <summary>
    /// 统一输出放置诊断日志。
    /// 这里单独收口，是为了以后可以集中控制开关、格式和节流策略，而不是把 `Debug.Log` 散落在整条放置链路里。
    /// </summary>
    private void LogPlacementDiagnostic(string message)
    {
        Debug.Log($"[PlacementDebug] {message}", this);
    }

    /// <summary>
    /// 进入 Play 后，立刻用“起手区中心点”做一次非常轻量的放置自检。
    /// 这个方法的价值不是参与真正建塔，而是快速回答一个关键问题：
    /// 如果玩家怎么拖都放不下第一座塔，究竟是交互入口失效了，还是规则本身就把起手点判成了非法。
    /// 这里故意只测两次：
    /// 1. `Relay` 在起手区中心是否合法。
    /// 2. `Defense` 在起手区中心是否合法。
    /// 这样既能提供排查信息，又不会做整区扫描造成额外负担。
    /// </summary>
    private void RunStarterPlacementSanityCheck()
    {
        if (IsGameOver)
        {
            return;
        }

        Vector3 starterWorldPosition = new Vector3(initialPlacementSquareCenter.x, initialPlacementSquareCenter.y, 0f);
        bool relayValid = ValidatePlacementPosition(starterWorldPosition, TowerType.Relay, out string relayReason);
        bool defenseValid = ValidatePlacementPosition(starterWorldPosition, TowerType.Defense, out string defenseReason);

        LogPlacementDiagnostic($"Starter sanity check: center={starterWorldPosition} relayValid={relayValid} relayReason={relayReason} defenseValid={defenseValid} defenseReason={defenseReason}");
    }


    /// <summary>
    /// 初始化当前总控依赖的几个核心协作模块。
    /// 包括：塔静态数据目录 `TowerCatalog`、HUD 表现层 `TowerDefenseHudPresenter`、
    /// 以及放置规则入口 `TowerPlacementRules`。这样后续逻辑就能围绕这些边界清晰的对象展开。
    /// </summary>
    private void InitializeArchitectureModules()
    {
        _towerCatalog = new TowerCatalog(
            relayDefinition: new TowerDefinition(
                towerType: TowerType.Relay,
                displayName: "Relay Generator",
                buildCost: relayTowerCost,
                placementRadius: relayPlacementRadius,
                expansionSquareSize: relayExpansionSquareSize,
                cardRoleSummary: "Generator / Power Income",
                accentColor: new Color(1f, 0.55f, 0.22f, 1f)),
            defenseDefinition: new TowerDefinition(
                towerType: TowerType.Defense,
                displayName: "Defense Turret",
                buildCost: defenseTowerCost,
                placementRadius: defensePlacementRadius,
                expansionSquareSize: defenseExpansionSquareSize,
                cardRoleSummary: "Frontline Damage",
                accentColor: new Color(0.28f, 0.78f, 1f, 1f)));

        _hudPresenter = new TowerDefenseHudPresenter();
        _placementRules = new TowerPlacementRules(GetPlacementRadius, GetExpansionSquareSize);
        _placementInteractionController = new TowerPlacementInteractionController(
            isGameOverQuery: () => _sessionState != null && _sessionState.IsGameOver,
            currentEnergyQuery: () => _sessionState != null ? _sessionState.CurrentEnergy : 0,
            canAffordTower: CanAffordTower,
            getPrototype: GetPrototype,
            getTowerDisplayName: GetTowerDisplayName,
            screenToWorldPosition: ScreenToWorldPosition,
            validatePlacementPosition: ValidatePlacementPosition,
            getPlacementOverlayWorldBounds: GetPlacementOverlayWorldBounds,
            tryPlaceTowerAt: (worldPosition, towerType) => TryPlaceTowerAt(worldPosition, towerType),
            refreshHud: RefreshHud,
            setStatusMessage: SetStatusMessage,
            logPlacementDiagnostic: LogPlacementDiagnostic);
        _placementBuildExecutor = new TowerPlacementBuildExecutor(
            isGameOverQuery: () => _sessionState != null && _sessionState.IsGameOver,
            currentEnergyQuery: () => _sessionState != null ? _sessionState.CurrentEnergy : 0,
            setCurrentEnergy: value => _sessionState?.SetCurrentEnergy(value),
            getTowerCost: GetTowerCost,
            getTowerDisplayName: GetTowerDisplayName,
            getPrototype: GetPrototype,
            getPlacedTowerRoot: () => _placedTowerRoot,
            getPlacementRadius: GetPlacementRadius,
            validatePlacementPosition: ValidatePlacementPosition,
            invalidatePlacementAreaOverlayCache: InvalidatePlacementAreaOverlayCache,
            refreshHud: RefreshHud,
            setStatusMessage: SetStatusMessage,
            logPlacementDiagnostic: LogPlacementDiagnostic);
    }

    /// <summary>
    /// 初始化放置可视化控制器。
    /// 这里会把颜色、排序、资源入口以及规则查询函数一次性注入，
    /// 让可视化层只专注于“怎么显示”，而不是反向知道整局状态或自己去找场景对象。
    /// </summary>
    private void InitializePlacementVisuals()
    {
        _placementVisualController?.Dispose();

        _placementVisualController = new TowerPlacementVisualController(
            placementRingResourcePath,
            validPreviewColor,
            invalidPreviewColor,
            placementAreaOverlayPixelsPerUnit,
            placementAreaOverlayFillColor,
            placementAreaOverlayEdgeColor,
            placementAreaOverlaySortingOrder,
            starterZoneMarkerFillColor,
            starterZoneMarkerEdgeColor,
            starterZoneMarkerSortingOrder,
            GetPrototype,
            GetTowerDisplayName,
            GetPlacementRadius);

        _placementVisualController.BindPlacementPreviewRoot(_placementPreviewRoot);
        _placementInteractionController?.BindPresentation(_placementVisualController, _hudPresenter, _towerCatalog);
    }

    /// <summary>
    /// 把场景层和 Inspector 上的放置规则上下文同步给 `TowerPlacementRules`。
    ///
    /// 这一步之所以单独抽出来，是为了避免以后每次场景引用或起手区参数变化时，
    /// 又把散落的同步代码塞回 `Start / FindSceneReferences / EnsureRuntimeRoots` 这些生命周期方法里。
    /// </summary>
    private void RefreshPlacementRuleContext()
    {
        _placementRules?.BindSceneReferences(_buildZone, _placedTowerRoot);
        _placementRules?.ConfigureStarterZone(initialPlacementSquareCenter, initialPlacementSquareSize);
    }

    /// <summary>
    /// 构造玩法 HUD 使用的整局状态快照。
    /// 它把资源、基地、波次、选中塔型和拖拽状态统一收口，便于 Presenter 一次性刷新。
    /// </summary>
    private TowerDefenseHudState CreateHudState()
    {
        TowerType selectedTowerType = _placementInteractionController != null
            ? _placementInteractionController.SelectedTowerType
            : TowerType.None;
        bool isPlacementDragActive = _placementInteractionController != null &&
                                     _placementInteractionController.IsPlacementDragActive;
        TowerType dragTowerType = _placementInteractionController != null
            ? _placementInteractionController.DragTowerType
            : TowerType.None;
        int currentEnergy = _sessionState != null ? _sessionState.CurrentEnergy : 0;
        int currentBaseHealth = _sessionState != null ? _sessionState.CurrentBaseHealth : 0;
        int currentWave = _sessionState != null ? _sessionState.CurrentWave : 0;
        int totalWaves = _sessionState != null ? _sessionState.TotalWaves : 0;

        return new TowerDefenseHudState(
            currentEnergy: currentEnergy,
            currentBaseHealth: currentBaseHealth,
            currentWave: currentWave,
            totalWaves: totalWaves,
            selectedTowerType: selectedTowerType,
            isPlacementDragActive: isPlacementDragActive,
            dragTowerType: dragTowerType);
    }

    /// <summary>
    /// 刷新 HUD。
    /// 这里会先更新首塔起手区标记，再把当前整局状态打包后交给 HUD 表现层统一刷新。
    /// </summary>
    private void RefreshHud()
    {
        RefreshStarterZoneMarker();
        if (_hudPresenter == null || _towerCatalog == null)
        {
            return;
        }

        _hudPresenter.Refresh(CreateHudState(), _towerCatalog, CanAffordTower);
    }

    /// <summary>
    /// 触发 Game Over。
    /// 这里会锁定结算状态、取消当前部署、隐藏敌人血条、暂停时间，并把结算面板切到可见。
    /// </summary>
    private void ShowGameOver()
    {
        if (_sessionState != null)
        {
            _sessionState.MarkGameOver();
        }

        _placementInteractionController?.ForceCancelPlacementDrag();
        HideActiveEnemyHealthBars();
        Time.timeScale = 0f;

        _hudPresenter?.ShowGameOver(
            title: "GAME OVER",
            hint: "The base has fallen. Exit Play Mode to keep adjusting the level and deployment flow.");

        SetStatusMessage("Base integrity depleted. Operation failed.");
        RefreshHud();
    }

    /// <summary>
    /// 在结算时隐藏所有仍然存活敌人的血条。
    /// 这样可以避免 Game Over 画面出现后，场上的血条还悬浮在界面前面干扰阅读。
    /// </summary>
    private void HideActiveEnemyHealthBars()
    {
        int activeEnemyCount = Enemy.ActiveEnemyCount;
        for (int i = 0; i < activeEnemyCount; i++)
        {
            Enemy enemy = Enemy.GetActiveEnemy(i);
            if (enemy != null)
            {
                enemy.SetHealthBarVisible(false);
            }
        }
    }


    /// <summary>
    /// 对总控内部与外部兼容层保留一个统一的“真正建塔”入口。
    /// 现在具体执行细节已经下沉到 `_placementBuildExecutor`，
    /// 所以这个方法更像一个稳定门面，避免别的脚本将来直接耦合到执行器实现。
    /// </summary>
    private bool TryPlaceTowerAt(Vector3 worldPosition, TowerType towerType, BuildPad ownerPad = null)
    {
        return _placementBuildExecutor != null &&
               _placementBuildExecutor.TryPlaceTowerAt(worldPosition, towerType, ownerPad);
    }

    /// <summary>
    /// 这是总控侧的放置校验入口。
    /// 当前真正的规则判断已经下沉到 `TowerPlacementRules`，这里主要负责把忽略条件和输出消息统一转发进去。
    /// 下面保留的 `#if false` 旧实现只作为历史对照，不参与运行时判定。
    /// </summary>
    private bool ValidatePlacementPosition(Vector3 worldPosition, TowerType towerType, out string invalidReason)
    {
        if (_placementRules == null)
        {
            invalidReason = "Placement rules are not initialized.";
            return false;
        }

        return _placementRules.ValidatePlacementPosition(
            worldPosition,
            towerType,
            ShouldIgnorePlacementTransform,
            out invalidReason);
#if false
        invalidReason = string.Empty;

        if (_buildZone == null)
        {
            invalidReason = "No BuildZone is configured in this level.";
            return false;
        }

        if (!_buildZone.ContainsPoint(worldPosition))
        {
            invalidReason = "Outside the level's buildable area.";
            return false;
        }

        if (!IsWithinPlacementNetwork(worldPosition, out invalidReason))
        {
            return false;
        }

        float placementRadius = GetPlacementRadius(towerType);
        int overlapCount = Physics2D.OverlapCircleNonAlloc(worldPosition, placementRadius, _placementValidationOverlapBuffer);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlap = _placementValidationOverlapBuffer[i];
            if (overlap == null)
            {
                continue;
            }

            if (_placementVisualController != null && _placementVisualController.ContainsPreviewTransform(overlap.transform))
            {
                continue;
            }

            PlacementBlocker blocker = overlap.GetComponentInParent<PlacementBlocker>();
            if (blocker != null)
            {
                invalidReason = blocker.BlockerReason;
                return false;
            }


            // 这里再补一层边界判断，只把真正挂在 `PlacedTowers` 根节点下的正式塔实例算作已建结构。
            Transform placedTowerRoot = _placedTowerRoot != null ? _placedTowerRoot : placedTowerRootReference;
            bool belongsToPlacedTower = placedTowerRoot != null && overlap.transform.IsChildOf(placedTowerRoot);
            if (belongsToPlacedTower && (overlap.GetComponentInParent<DefenseTower>() != null || overlap.GetComponentInParent<RelayTower>() != null))
            {
                invalidReason = "Too close to another structure. Move it a little.";
                return false;
            }
        }


        return true;
#endif
    }

    /// <summary>
    /// 放置规则层本身不应该知道“预览对象”是谁。
    ///
    /// 这里由总控提供一个非常窄的忽略入口：
    /// - 如果当前重叠对象属于预览塔，就忽略
    /// - 其他对象仍然全部交给规则层判断
    ///
    /// 这样既保住了解耦，也不会丢掉之前修首塔误判时建立的那层边界。
    /// </summary>
    private bool ShouldIgnorePlacementTransform(Transform candidate)
    {
        return _placementVisualController != null && _placementVisualController.ContainsPreviewTransform(candidate);
    }

    /// <summary>
    /// 读取塔型对应的占地半径。
    /// </summary>
    private float GetPlacementRadius(TowerType towerType)
    {
        TowerDefinition definition = _towerCatalog != null ? _towerCatalog.GetDefinition(towerType) : null;
        return definition != null ? definition.PlacementRadius : 0.5f;
    }

    /// <summary>
    /// 读取塔型对应的扩张方格边长。
    /// </summary>
    private float GetExpansionSquareSize(TowerType towerType)
    {
        TowerDefinition definition = _towerCatalog != null ? _towerCatalog.GetDefinition(towerType) : null;
        return definition != null ? definition.ExpansionSquareSize : 4.5f;
    }

    /// <summary>
    /// 计算合法区域覆盖层需要扫描的世界边界。
    /// 这一步很重要，因为它决定覆盖层只扫描和当前部署网络相关的区域，
    /// 而不是每次都把整张 `BuildZone` 全量采样一遍。
    /// </summary>
    private Bounds GetPlacementOverlayWorldBounds(TowerType towerType)
    {
        if (_placementRules == null)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        return _placementRules.GetPlacementOverlayWorldBounds(towerType);
    }

    /// <summary>
    /// 预热指定塔型的合法区域覆盖层。
    /// 常见调用时机是悬停部署卡或刚切换选中塔型时，
    /// 目的是把代价提前摊掉，减少真正开始拖拽那一瞬间的卡顿感。
    /// </summary>
    public void PrewarmPlacementAreaOverlay(TowerType towerType)
    {
        _placementInteractionController?.PrewarmPlacementAreaOverlay(towerType);
    }

    /// <summary>
    /// 标记合法区域覆盖层缓存失效。
    /// 当场上的塔布局变化后，旧缓存就不再可信，下一次需要重新生成。
    /// </summary>
    private void InvalidatePlacementAreaOverlayCache()
    {
        _placementVisualController?.InvalidatePlacementAreaOverlayCache();
    }

    /// <summary>
    /// 隐藏合法区域覆盖层。
    /// </summary>
    private void HidePlacementAreaOverlay()
    {
        _placementVisualController?.HidePlacementAreaOverlay();
    }

    /// <summary>
    /// 同步首塔起手区标记的显隐。
    /// 每次 HUD 刷新或放置状态变化时都会走这里，保证“首塔前显示、首塔后隐藏”的规则稳定成立。
    /// </summary>
    private void RefreshStarterZoneMarker()
    {
        Bounds starterBounds = _placementRules != null
            ? _placementRules.GetStarterZoneBounds()
            : TowerPlacementRules.CreateSquareBounds(initialPlacementSquareCenter, initialPlacementSquareSize);
        _placementVisualController?.RefreshStarterZoneMarker(!IsGameOver && ShouldShowStarterZoneMarker(), starterBounds);
    }

    /// <summary>
    /// 判断当前是否应该显示首塔起手区标记。
    /// 只有在还没放下任何塔、并且没有进入结算时，这块提示区域才应该出现。
    /// </summary>
    private bool ShouldShowStarterZoneMarker()
    {
        if (_placementRules != null)
        {
            return _placementRules.ShouldShowStarterZoneMarker();
        }

        Transform placedTowerRoot = _placedTowerRoot != null ? _placedTowerRoot : placedTowerRootReference;
        return placedTowerRoot == null || placedTowerRoot.childCount == 0;
    }

    /// <summary>
    /// 常规 Scene 视图 Gizmo 入口。
    /// 当前主要用它在不进 Play 的情况下，把首塔起手区直接画在编辑器里。
    /// </summary>
    private void OnDrawGizmos()
    {
        if (Application.isPlaying || !ShouldShowStarterZoneMarker())
        {
            return;
        }

        DrawStarterZoneGizmo();
    }

    /// <summary>
    /// 选中对象时也绘制起手区 Gizmo，方便调整时更容易看清边界。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying || !ShouldShowStarterZoneMarker())
        {
            return;
        }

        DrawStarterZoneGizmo();
    }

    /// <summary>
    /// 在 Scene 视图里画出首塔起手区的方形 Gizmo。
    /// </summary>
    private void DrawStarterZoneGizmo()
    {
        Vector3 center = new Vector3(initialPlacementSquareCenter.x, initialPlacementSquareCenter.y, 0f);
        Vector3 size = new Vector3(initialPlacementSquareSize, initialPlacementSquareSize, 0.01f);

        Color fillColor = starterZoneMarkerFillColor;
        fillColor.a = Mathf.Max(fillColor.a, 0.3f);
        Gizmos.color = fillColor;
        Gizmos.DrawCube(center, size);

        Color edgeColor = starterZoneMarkerEdgeColor;
        edgeColor.a = 1f;
        Gizmos.color = edgeColor;
        Gizmos.DrawWireCube(center, size);
        Gizmos.DrawWireCube(center, size * 1.04f);
    }


    private Vector3 GetMouseWorldPosition()
    {
        return ScreenToWorldPosition(Input.mousePosition);
    }

    /// <summary>
    /// 把屏幕坐标转换到玩法所在的世界平面。
    /// </summary>
    private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null)
        {
            return Vector3.zero;
        }

        Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(_mainCamera.transform.position.z));
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPoint);
        worldPosition.z = 0f;
        return worldPosition;
    }

    /// <summary>
    /// 判断当前鼠标是否压在“会拦截玩法”的 UI 上。
    /// 这层过滤是为了解决装饰性 UI 误伤拖拽和快速放置的问题。
    /// </summary>
    private bool IsPointerOverUserInterface()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        System.Collections.Generic.List<RaycastResult> raycastResults = new System.Collections.Generic.List<RaycastResult>(8);
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject target = raycastResults[i].gameObject;
            if (IsGameplayBlockingUserInterface(target))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断某个 UI 对象是否真的应该阻挡玩法输入。
    /// 这里刻意只把部署卡和 `Selectable` 系交互控件视为阻挡，避免装饰文本、边框或标签误判成 UI 遮挡。
    /// </summary>
    private static bool IsGameplayBlockingUserInterface(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.GetComponentInParent<TowerShopCard>() != null)
        {
            return true;
        }

        return target.GetComponentInParent<Selectable>() != null;
    }

    /// <summary>
    /// 读取塔的建造成本。
    /// 如果目录还没准备好，就返回 `0`，避免空引用把整条购买链路打断。
    /// </summary>
    private int GetTowerCost(TowerType towerType)
    {
        TowerDefinition definition = _towerCatalog != null ? _towerCatalog.GetDefinition(towerType) : null;
        return definition != null ? definition.BuildCost : 0;
    }

    /// <summary>
    /// 读取塔的显示名称。
    /// 如果目录还没准备好，就返回一个安全的占位文本。
    /// </summary>
    private string GetTowerDisplayName(TowerType towerType)
    {
        TowerDefinition definition = _towerCatalog != null ? _towerCatalog.GetDefinition(towerType) : null;
        return definition != null ? definition.DisplayName : "None";
    }

    /// <summary>
    /// 根据塔型拿到对应的原型体。
    /// </summary>
    private GameObject GetPrototype(TowerType towerType)
    {
        switch (towerType)
        {
            case TowerType.Relay:
                return _relayTowerPrototype;
            case TowerType.Defense:
                return _defenseTowerPrototype;
            default:
                return null;
        }
    }

    /// <summary>
    /// 把场景里的显式引用读进总控运行时字段。
    /// 这里同时会把 HUD 引用转交给 Presenter，并在关键引用缺失时给出清晰警告，方便在 Inspector 里补线。
    /// </summary>
    private void FindSceneReferences()
    {
        // 这里优先采用 Inspector 已经拖好的显式引用，再把它们同步交给 HUD Presenter。
        _relayTowerPrototype = relayTowerPrototypeReference;
        _defenseTowerPrototype = defenseTowerPrototypeReference;

        _hudPresenter?.BindSceneReferences(
            energyText: energyTextReference,
            baseHealthText: baseHealthTextReference,
            waveText: waveTextReference,
            selectionText: selectionTextReference,
            relayTowerButton: relayTowerButtonReference,
            defenseTowerButton: defenseTowerButtonReference,
            clearSelectionButton: clearSelectionButtonReference,
            gameOverPanel: gameOverPanelReference,
            gameOverTitle: gameOverTitleReference,
            gameOverHint: gameOverHintReference,
            dragPreviewPanel: dragPreviewPanelReference,
            dragPreviewLabel: dragPreviewLabelReference);

        _hudPresenter?.FindSceneReferences();
        _buildZone = EnsureBuildZoneExists();

        if (_mainCamera == null)
        {
            Debug.LogWarning("TowerDefenseGame is missing Main Camera reference. Camera.main fallback also failed.");
        }

        if (_relayTowerPrototype == null || _defenseTowerPrototype == null)
        {
            Debug.LogWarning("TowerDefenseGame is missing one or more tower prototype references. Check the scene wiring.");
        }
    }

    private void EnsureRuntimeRoots()
    {
        _placedTowerRoot = EnsureRuntimeRoot(placedTowerRootReference, placedTowerRootName);
        _placementPreviewRoot = EnsureRuntimeRoot(placementPreviewRootReference, placementPreviewRootName);

        placedTowerRootReference = _placedTowerRoot;
        placementPreviewRootReference = _placementPreviewRoot;
        RefreshPlacementRuleContext();
        _placementVisualController?.BindPlacementPreviewRoot(_placementPreviewRoot);
    }

    /// <summary>
    /// 确保某个运行时根节点一定存在。
    /// 如果场景里已经显式拖好了引用，就直接复用；否则按约定名称新建一个父节点，给运行时塔和预览对象提供稳定挂点。
    /// </summary>
    private static Transform EnsureRuntimeRoot(Transform existingReference, string objectName)
    {
        if (existingReference != null)
        {
            return existingReference;
        }

        GameObject runtimeRoot = new GameObject(objectName);
        return runtimeRoot.transform;
    }

    private BuildZone EnsureBuildZoneExists()
    {
        if (buildZoneReference != null)
        {
            return buildZoneReference;
        }

        Debug.LogWarning("TowerDefenseGame is missing BuildZone reference. Creating a temporary runtime BuildZone fallback.");

        GameObject buildZoneObject = new GameObject(buildZoneName);
        buildZoneObject.transform.position = new Vector3(0f, 0.25f, 0f);

        BoxCollider2D boxCollider = buildZoneObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(18f, 10.5f);

        buildZoneReference = buildZoneObject.AddComponent<BuildZone>();
        return buildZoneReference;
    }
}
