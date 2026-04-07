using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 鐢ㄦ灇涓捐〃杈锯€滃綋鍓嶅噯澶囬儴缃插摢绉嶅鈥濄€?///
/// 鐜板湪杩欎唤鍘熷瀷鏃㈡敮鎸侊細
/// - 鐐瑰嚮 / 蹇嵎閿€変腑濉旓紝鍐嶇偣鍑诲湴鍥鹃儴缃?/// - 鐩存帴浠?UI 閮ㄧ讲鍗℃嫋鍒板湴鍥鹃噷鏀剧疆
///
/// 鏃犺鍏ュ彛鏄摢涓€绉嶏紝鏈€缁堥兘浼氭敹鏉熷埌鍚屼竴涓?TowerType銆?/// 杩欒鈥滅晫闈氦浜掑眰鈥濆拰鈥滃缓閫犺鍒欏眰鈥濅箣闂磋兘鍏变韩鍚屼竴濂椾笟鍔＄姸鎬併€?/// </summary>
public enum TowerType
{
    None,
    Relay,
    Defense
}

/// <summary>
/// TowerDefenseGame 鏄綋鍓嶅闃插師鍨嬬殑鎬绘帶鑴氭湰銆?///
/// 杩欎竴鐗堢浉杈冧簬涓婁竴涓噷绋嬬锛屾湁涓€涓緢鍏抽敭鐨勭帺娉曞崌绾э細
/// 寤洪€犻€昏緫浠庘€滅偣鍑诲浐瀹氬浣嶁€濆彉鎴愪簡鈥滀粠閮ㄧ讲鍗℃嫋鎷藉埌鍦板浘鑷敱鏀剧疆鈥濄€?///
/// 鍥犳瀹冪幇鍦ㄩ櫎浜嗙户缁壙鎷呰祫婧愩€佸熀鍦拌閲忋€佹尝娆°€丠UD 绛夎亴璐ｄ箣澶栵紝
/// 杩樻柊澧炰簡鍑犻」寰堥噸瑕佺殑宸ヤ綔锛?/// 1. 绠＄悊褰撳墠閫変腑鐨勫绫诲瀷锛屼互鍙婂綋鍓嶆槸鍚﹀浜庨儴缃叉嫋鎷戒腑銆?/// 2. 缁存姢鍦板浘涓殑鎷栨嫿棰勮濉斻€佸湴闈㈡姇褰卞湀鍜屽厜鏍囨彁绀洪潰鏉裤€?/// 3. 缁熶竴鍒ゆ柇鏌愪釜涓栫晫鍧愭爣鑳戒笉鑳藉缓閫犮€?/// 4. 澶勭悊 BuildZone銆丳lacementBlocker銆佸凡鏈夊纰版挒杩欎笁绫绘斁缃害鏉熴€?/// 5. 璁?UI 閮ㄧ讲鍗°€佸揩鎹烽敭閮ㄧ讲銆佸吋瀹规棫 BuildPad 鐨勫叆鍙ｉ兘鏀跺彛鍒板悓涓€濂楄鍒欓噷銆?///
/// 杩欎粛鐒跺睘浜庘€滃師鍨嬫湡鎬绘帶闆嗕腑鏀跺彛鈥濈殑鍋氭硶锛?/// 浣嗙浉姣旀渶鍒濈増鏈紝绯荤粺杈圭晫宸茬粡鏇存竻鏅颁簡锛?/// - BuildZone 璐熻矗澶ц寖鍥磋鍙?/// - PlacementBlocker 璐熻矗绂佸缓灞€閮ㄥ尯
/// - TowerShopCard 璐熻矗 UI 鎷栨嫿杈撳叆
/// - TowerDefenseGame 璐熻矗鎶婅繖浜涜緭鍏ュ拰瑙勫垯鏁村悎鎴愬畬鏁撮棴鐜?///
/// 杩欑娓愯繘寮忓崌绾ф瘮涓€涓婃潵鍋氬ぇ鑰屽叏鏋舵瀯鏇撮€傚悎鏁欏锛?/// 鍥犱负浣犺兘鏄庢樉鐪嬭锛?/// 涓€涓師鍨嬫槸濡備綍鍦ㄤ笉鎺ㄧ炕宸叉湁绯荤粺鐨勫墠鎻愪笅锛岄€愭杩涘寲鍑烘洿鍚堢悊鐨勭粨鏋勭殑銆?/// </summary>
public class TowerDefenseGame : MonoBehaviour
{
    /// <summary>
    /// 鍦烘櫙涓殑鍞竴鎬绘帶瀹炰緥銆?    /// </summary>
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

    [Header("Scene Object Names")]
    [SerializeField] private string mainCameraName = "Main Camera";
    [SerializeField] private string relayTowerPrototypeName = "RelayTowerPrototype";
    [SerializeField] private string defenseTowerPrototypeName = "DefenseTowerPrototype";
    [SerializeField] private string placedTowerRootName = "PlacedTowers";
    [SerializeField] private string placementPreviewRootName = "PlacementPreviewRoot";
    [SerializeField] private string buildZoneName = "BuildZone";

    [Header("UI Object Names")]
    [SerializeField] private string energyTextName = "EnergyText";
    [SerializeField] private string baseHealthTextName = "BaseHealthText";
    [SerializeField] private string waveTextName = "WaveText";
    [SerializeField] private string selectionTextName = "SelectionText";
    [SerializeField] private string statusTextName = "StatusText";
    [SerializeField] private string relayTowerButtonName = "RelayTowerButton";
    [SerializeField] private string defenseTowerButtonName = "DefenseTowerButton";
    [SerializeField] private string clearSelectionButtonName = "ClearSelectionButton";
    [SerializeField] private string gameOverPanelName = "GameOverPanel";
    [SerializeField] private string gameOverTitleName = "GameOverTitle";
    [SerializeField] private string gameOverHintName = "GameOverHint";
    [SerializeField] private string dragPreviewPanelName = "DragPreviewPanel";
    [SerializeField] private string dragPreviewLabelName = "DragPreviewLabel";

    private int _currentEnergy;
    private int _currentBaseHealth;
    private int _currentWave;
    private int _totalWaves;
    private bool _isGameOver;
    private TowerType _selectedTowerType = TowerType.None;
    private bool _isPlacementDragActive;
    private TowerType _dragTowerType = TowerType.None;
    private Vector3 _previewWorldPosition;
    private bool _previewPositionIsValid;
    private string _previewInvalidReason = string.Empty;

    private GameObject _relayTowerPrototype;
    private GameObject _defenseTowerPrototype;
    private Camera _mainCamera;
    private BuildZone _buildZone;
    private Transform _placedTowerRoot;
    private Transform _placementPreviewRoot;

    /// <summary>
    /// 用于放置判定的非分配物理重叠缓存。
    ///
    /// 覆盖层会反复重放真实放置判定，
    /// 如果每次都让物理查询返回一个新数组，就很容易把 GC 压力抬高。
    /// 这里固定复用一块缓存区，优先消灭这类短时分配。
    /// </summary>
    private readonly Collider2D[] _placementValidationOverlapBuffer = new Collider2D[64];
    private GameObject _placementPreviewInstance;
    private TowerType _placementPreviewTowerType = TowerType.None;
    private SpriteRenderer _placementPreviewSpriteRenderer;
    private SpriteRenderer _placementPreviewRingRenderer;
    private PlacementAreaOverlayRenderer _placementAreaOverlayRenderer;

    /// <summary>
    /// 当前可放置区域覆盖层缓存对应的布局版本号。
    ///
    /// 只要场上已建塔布局没变化，这份覆盖层结果就仍然有效，
    /// 没必要每次开始拖拽都重新把整张合法区域重算一遍。
    /// </summary>
    private int _placementAreaOverlayRevision;

    /// <summary>
    /// 当前已经预热完成的覆盖层版本号。
    ///
    /// 它和 `_placementAreaOverlayPreparedTowerType` 组合起来，
    /// 用来判断这次拖拽能不能直接吃缓存。
    /// </summary>
    private int _placementAreaOverlayPreparedRevision = -1;

    /// <summary>
    /// 当前缓存好的覆盖层对应的是哪种塔。
    ///
    /// 因为不同塔的占地半径不同，最终合法区域也会不同，
    /// 所以缓存键不能只看布局版本，还要看塔类型。
    /// </summary>
    private TowerType _placementAreaOverlayPreparedTowerType = TowerType.None;

    /// <summary>
    /// `_towerCatalog` 缂佺喍绔寸€涙ɑ鏂侀崥鍕潚婵夋梻娈戦棃娆愨偓浣哥暰娑斿鈧?    ///
    /// 鏉╂瑦鐗遍崑姘簰閸氬函绱濋幀缁樺付娑撳秹娓剁憰浣烘埛缂侇厽濡搁垾婊嗗瀭閻?/ 閸楃姴婀撮崡濠傜窞 / HUD 鐏炴洜銇氶崥宥佲偓?    /// 閸掑棙鏆庨崘娆忔躬婢舵矮閲?switch 闁插矉绱濋崥搴ㄦ桨鐟曚胶鎴风紒顓炲婵夋梻琚崹瀣娑旂喐娲挎總鑺ュ⒖鐏炴洏鈧?    /// </summary>
    private TowerCatalog _towerCatalog;

    /// <summary>
    /// `_hudPresenter` 娑撴捇妫拹鐔荤煑 HUD 閺屻儲澹樻稉搴ｆ櫕闂堛垹鍩涢弬鑸偓?    ///
    /// 鏉╂瑨顔€ `TowerDefenseGame` 閸欘垯浜掗柅鎰劄閸ョ偛鍩岄垾婊嗩潐閸掓瑧绱幒鎺曗偓鍛偓婵堟畱鐟欐帟澹婇敍?    /// 閼板奔绗夐弰顖氭倱閺冭泛鍚嬫禒鏄忣潐閸掓瑥鐪伴崪宀冦€冮悳鏉跨湴缂佸棜濡銉ュ范閵?    /// </summary>
    private TowerDefenseHudPresenter _hudPresenter;

    /// <summary>
    /// 娓告垙鏄惁宸茬粡缁撴潫锛屽澶栧叕寮€鍙璁块棶銆?    /// </summary>
    public bool IsGameOver => _isGameOver;

    /// <summary>
    /// 鍒濆鍖栨€绘帶鍗曚緥鍜屽紑灞€璧勬簮鐘舵€併€?    /// </summary>
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

        _currentEnergy = startingEnergy;
        _currentBaseHealth = startingBaseHealth;
        InitializeArchitectureModules();
        _placementAreaOverlayRenderer = new PlacementAreaOverlayRenderer(
            placementAreaOverlayPixelsPerUnit,
            placementAreaOverlayFillColor,
            placementAreaOverlayEdgeColor,
            placementAreaOverlaySortingOrder);
    }

    /// <summary>
    /// 释放运行时生成的覆盖图资源，并在总控销毁时清掉单例引用。
    ///
    /// 这一步很小，但它能避免：
    /// - 运行时 Texture2D / Sprite 资源残留
    /// - 旧场景退出后，静态单例还指向失效对象
    /// </summary>
    private void OnDestroy()
    {
        ReleasePlacementPreviewInstance();
        _placementAreaOverlayRenderer?.Dispose();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 鍦烘櫙鍚姩鍚庢煡鎵惧紩鐢ㄣ€佸噯澶囪繍琛屾椂鑺傜偣锛屽苟鍒锋柊鍒濆 UI銆?    ///
    /// 杩欎竴鐗堝惎鍔ㄩ樁娈甸澶栬鍋氱殑浜嬫槸锛?    /// - 纭繚 BuildZone 瀛樺湪
    /// - 鍑嗗鎷栨嫿棰勮鏍硅妭鐐?    /// - 闅愯棌鎷栨嫿鎻愮ず闈㈡澘
    /// - 鎶婃棫鎸夐挳鏂囨湰鏀归€犳垚鏇村儚閮ㄧ讲鍗＄殑鏂囨缁撴瀯
    /// </summary>
    private void Start()
    {
        FindSceneReferences();
        EnsureRuntimeRoots();
        _hudPresenter.ConfigureCardLabels(_towerCatalog);

        SetStatusMessage("Place your first structure in the starter zone. Drag a Generator or Turret into a highlighted legal area. Hotkeys: 1 / 2.");
        RefreshHud();

        _hudPresenter.SetGameOverVisible(false);
        _hudPresenter.SetDragPreviewVisible(false);
        HidePlacementAreaOverlay();
    }

    /// <summary>
    /// 鐩戝惉蹇嵎閿拰闈炴嫋鎷芥ā寮忎笅鐨勫揩閫熼儴缃茶緭鍏ャ€?    ///
    /// 鐜板湪 Update 涓昏鎵挎媴涓ょ被杈撳叆锛?    /// 1. 蹇嵎閿垏鎹㈤儴缃茬被鍨嬫垨鍙栨秷閫夋嫨銆?    /// 2. 褰撶帺瀹跺凡缁忛€氳繃鐐瑰嚮鍗＄墖鎴栭敭鐩橀€変腑浜嗗鍚庯紝鍏佽鐩存帴鐐瑰嚮鍦板浘閮ㄧ讲銆?    ///
    /// 鐪熸鐨勬嫋鎷芥祦绋嬶紝鍒欑敱 TowerShopCard 閫氳繃浜嬩欢鎺ュ彛椹卞姩銆?    /// </summary>
    private void Update()
    {
        HandleHotkeys();
        HandleQuickPlacementInput();
    }

    /// <summary>
    /// 鍏煎鏃?BuildPad 鍏ュ彛鐨勫缓閫犳帴鍙ｃ€?    ///
    /// 铏界劧褰撳墠鍏冲崱宸茬粡鍒囨崲鍒拌嚜鐢辨斁缃柟妗堬紝
    /// 浣嗕繚鐣欒繖涓叆鍙ｆ湁涓や釜濂藉锛?    /// 1. 涓嶄細璁╂棫鐨?BuildPad 鑴氭湰鐩存帴澶辨晥鎶ラ敊銆?    /// 2. 浠ュ悗濡傛灉浣犳兂鍋氭煇浜涒€滅壒娈婂浐瀹氬浣嶁€濈帺娉曪紝涔熻繕鑳界户缁鐢ㄥ畠銆?    /// </summary>
    public bool TryPlaceTower(BuildPad pad)
    {
        if (pad == null)
        {
            return false;
        }

        return TryPlaceTowerAt(pad.GetBuildPosition(), _selectedTowerType, pad);
    }

    /// <summary>
    /// 寮€濮嬩竴娆♀€滀粠閮ㄧ讲鍗℃嫋鎷藉埌鍦板浘鈥濈殑娴佺▼銆?    /// </summary>
    public bool BeginPlacementDrag(TowerType towerType, Vector2 screenPosition)
    {
        if (_isGameOver || towerType == TowerType.None)
        {
            return false;
        }

        if (!CanAffordTower(towerType))
        {
            _selectedTowerType = towerType;
            RefreshHud();
            SetStatusMessage($"Not enough energy. You currently have {_currentEnergy} EN.");
            return false;
        }

        GameObject prototype = GetPrototype(towerType);
        if (prototype == null)
        {
            SetStatusMessage("Card prototype is missing. Check the scene setup.");
            return false;
        }

        CancelPlacementDragInternal();

        _selectedTowerType = towerType;
        _dragTowerType = towerType;
        _isPlacementDragActive = true;

        EnsurePlacementPreviewInstance(towerType);
        _hudPresenter.SetDragPreviewVisible(true);
        ShowPreparedPlacementAreaOverlay(towerType);
        RefreshHud();
        UpdatePlacementDrag(screenPosition);
        SetStatusMessage("Drag the Generator or Turret into a highlighted legal area, then release to deploy.");
        return true;
    }

    /// <summary>
    /// 鍦ㄦ嫋鎷借繃绋嬩腑鍒锋柊棰勮濉斾綅缃€佸悎娉曟€у拰鎻愮ず闈㈡澘銆?    /// </summary>
    public void UpdatePlacementDrag(Vector2 screenPosition)
    {
        if (!_isPlacementDragActive)
        {
            return;
        }

        _previewWorldPosition = ScreenToWorldPosition(screenPosition);
        _previewPositionIsValid = ValidatePlacementPosition(_previewWorldPosition, _dragTowerType, out _previewInvalidReason);

        if (_placementPreviewInstance != null)
        {
            _placementPreviewInstance.transform.position = _previewWorldPosition;
        }

        UpdatePlacementPreviewVisual();
        _hudPresenter.UpdateDragPreviewPanel(screenPosition, CreateDragPreviewState(), _towerCatalog);
        RefreshHud();
    }

    /// <summary>
    /// 缁撴潫涓€娆℃嫋鎷介儴缃叉祦绋嬨€?    /// </summary>
    public void EndPlacementDrag(Vector2 screenPosition, bool releasedOverUserInterface)
    {
        if (!_isPlacementDragActive)
        {
            return;
        }

        UpdatePlacementDrag(screenPosition);

        TowerType towerType = _dragTowerType;
        Vector3 worldPosition = _previewWorldPosition;
        bool canPlace = _previewPositionIsValid && !releasedOverUserInterface;
        string invalidReason = _previewInvalidReason;

        CancelPlacementDragInternal();

        if (canPlace)
        {
            TryPlaceTowerAt(worldPosition, towerType);
            return;
        }

        if (releasedOverUserInterface)
        {
            SetStatusMessage("Deployment cancelled.");
        }
        else if (!string.IsNullOrEmpty(invalidReason))
        {
            SetStatusMessage(invalidReason);
        }
    }

    /// <summary>
    /// 鍙栨秷褰撳墠鎷栨嫿閮ㄧ讲娴佺▼銆?    /// </summary>
    public void CancelPlacementDrag()
    {
        CancelPlacementDragInternal();
        RefreshHud();
    }

    /// <summary>
    /// 缁欑帺瀹跺鍔犵數閲忋€?    /// </summary>
    public void AddEnergy(int amount)
    {
        if (_isGameOver || amount <= 0)
        {
            return;
        }

        _currentEnergy += amount;
        RefreshHud();
    }

    /// <summary>
    /// 鎵ｉ櫎鍩哄湴鐢熷懡鍊笺€?    /// </summary>
    public void DamageBase(int amount)
    {
        if (_isGameOver || amount <= 0)
        {
            return;
        }

        _currentBaseHealth = Mathf.Max(0, _currentBaseHealth - amount);
        RefreshHud();
        SetStatusMessage($"An enemy slipped through. Base lost {amount} HP.");

        if (_currentBaseHealth == 0)
        {
            ShowGameOver();
        }
    }

    /// <summary>
    /// 鏇存柊褰撳墠娉㈡淇℃伅銆?    /// </summary>
    public void SetWaveProgress(int currentWave, int totalWaves)
    {
        _currentWave = currentWave;
        _totalWaves = totalWaves;
        RefreshHud();
    }

    /// <summary>
    /// 鏇存柊搴曢儴鐘舵€佹彁绀烘枃鏈€?    ///
    /// 鐘舵€佹爮鍦ㄨ繖涓師鍨嬮噷闈炲父閲嶈锛?    /// 鍥犱负瀹冩壙鎷呬簡鈥滄暀瀛﹁В閲婂櫒鈥濈殑浣滅敤锛?    /// 褰撹鍒欒繕涓嶅鏉傛椂锛屾槑纭殑鏂囧瓧鎻愮ず鑳芥樉钁楅檷浣庤瘯鐜╅棬妲涖€?    /// </summary>
    public void SetStatusMessage(string message)
    {
        _hudPresenter?.SetStatusMessage(message);
    }

    /// <summary>
    /// 閫変腑缁х數鍣ㄥ銆?    /// </summary>
    public void SelectRelayTower()
    {
        SelectTower(TowerType.Relay);
    }

    /// <summary>
    /// 閫変腑闃插尽濉斻€?    /// </summary>
    public void SelectDefenseTower()
    {
        SelectTower(TowerType.Defense);
    }

    /// <summary>
    /// 娓呯┖褰撳墠閫夊鐘舵€侊紝骞跺悓鏃剁粨鏉熷彲鑳戒粛鍦ㄨ繘琛岀殑鎷栨嫿棰勮銆?    /// </summary>
    public void ClearSelection()
    {
        CancelPlacementDragInternal();
        SelectTower(TowerType.None);
    }

    /// <summary>
    /// 鍒ゆ柇褰撳墠鐢甸噺鏄惁瓒冲寤洪€犳寚瀹氬銆?    /// </summary>
    public bool CanAffordTower(TowerType towerType)
    {
        if (towerType == TowerType.None)
        {
            return false;
        }

        return _currentEnergy >= GetTowerCost(towerType);
    }

    /// <summary>
    /// 澶勭悊蹇嵎閿緭鍏ャ€?    /// </summary>
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
    /// 澶勭悊鈥滈潪鎷栨嫿妯″紡涓嬶紝宸茬粡閫変腑濉斿悗鐩存帴鐐瑰嚮鍦板浘閮ㄧ讲鈥濈殑蹇嵎杈撳叆銆?    /// </summary>
    private void HandleQuickPlacementInput()
    {
        if (_isGameOver || _isPlacementDragActive || _selectedTowerType == TowerType.None)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0) || IsPointerOverUserInterface())
        {
            return;
        }

        TryPlaceTowerAt(GetMouseWorldPosition(), _selectedTowerType);
    }

    /// <summary>
    /// 缁熶竴澶勭悊鈥滃綋鍓嶉€変腑濉旂姸鎬佲€濈殑鍐呴儴鍒囨崲銆?    /// </summary>
    private void SelectTower(TowerType towerType)
    {
        if (_isGameOver)
        {
            return;
        }

        _selectedTowerType = towerType;

        if (towerType == TowerType.None)
        {
            SetStatusMessage("Deployment selection cleared.");
        }
        else
        {
            SetStatusMessage($"Selected: {GetTowerDisplayName(towerType)}. Drag the card to preview exact legal areas.");
            PrewarmPlacementAreaOverlay(towerType);
        }

        RefreshHud();
    }

    /// <summary>
    /// 鎶婂綋鍓嶈繍琛岀姸鎬佸悓姝ュ埌 HUD 鏂囨湰銆?    /// </summary>
    /// <summary>
    /// 閸掓繂顫愰崠鏍箹濞嗏剝濯堕崙鐑樻降閻ㄥ嫪琚辨稉顏勭唨绾偓濡€虫健閿?    /// - `TowerCatalog`閿涙碍澹欐潪钘夘敊閻ㄥ嫰娼ら幀浣哥暰娑?    /// - `TowerDefenseHudPresenter`閿涙碍澹欐潪?HUD 閺屻儲澹樻稉搴″煕閺?    ///
    /// 鏉╂瑩鍣锋禒宥囧姧閻㈣鲸鈧粯甯剁紒鐔剁閸掓稑缂撶€瑰啩婊戦敍?    /// 閺勵垰娲滄稉鍝勭秼閸撳秹銆嶉惄顔荤贩閺冄傜箽閹镐讲鈧粌甯崹瀣埂闂嗗棔鑵戦幀缁樺付閳ユ繄娈戦幀璁崇秼缁涙牜鏆愰敍?    /// 閹存垳婊戞潻娆庣濮濄儱浠涢惃鍕Ц閸戝繗鈧讣绱濇稉宥嗘Ц瑜拌绨冲鏇炲弳閺傛壆娈戦崥顖氬З濡楀棙鐏﹂妴?    /// </summary>
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

        _hudPresenter = new TowerDefenseHudPresenter(
            energyTextName: energyTextName,
            baseHealthTextName: baseHealthTextName,
            waveTextName: waveTextName,
            selectionTextName: selectionTextName,
            statusTextName: statusTextName,
            relayTowerButtonName: relayTowerButtonName,
            defenseTowerButtonName: defenseTowerButtonName,
            clearSelectionButtonName: clearSelectionButtonName,
            gameOverPanelName: gameOverPanelName,
            gameOverTitleName: gameOverTitleName,
            gameOverHintName: gameOverHintName,
            dragPreviewPanelName: dragPreviewPanelName,
            dragPreviewLabelName: dragPreviewLabelName);
    }

    /// <summary>
    /// 閹跺﹥鈧粯甯堕柌宀€娈戦崗鎶芥暛鏉╂劘顢戦弮鍓佸Ц閹礁甯囬幋鎰娑擃亣浜ら柌?HUD 韫囶偆鍙庨妴?    ///
    /// 鏉╂瑦鐗?HUD 鐏炲倻婀呴崚鎵畱閺勵垪鈧粍鏆ｉ悶鍡椼偨閻ㄥ嫮绮ㄩ弸婧锯偓婵撶礉
    /// 閼板奔绗夐弰顖滄纯閹恒儰绶风挧鏍ㄢ偓缁樺付闁插瞼娈戦崚鍡樻殠鐎涙顔岄妴?    /// </summary>
    private TowerDefenseHudState CreateHudState()
    {
        return new TowerDefenseHudState(
            currentEnergy: _currentEnergy,
            currentBaseHealth: _currentBaseHealth,
            currentWave: _currentWave,
            totalWaves: _totalWaves,
            selectedTowerType: _selectedTowerType,
            isPlacementDragActive: _isPlacementDragActive,
            dragTowerType: _dragTowerType);
    }

    /// <summary>
    /// 閹跺﹥瀚嬮幏鑺ュ絹缁€娲桨閺夎法婀″锝呭彠韫囧啰娈戦弫鐗堝祦閺€璺哄經閹存劒绔存稉顏嗗缁斿濮搁幀浣碘偓?    ///
    /// 鏉╂瑦鐗辨禒銉ユ倵婵″倹鐏夐幋鎴滄粦缂佈呯敾閹?`Placement` 娴犲孩鈧粯甯堕柌灞惧閸戝搫骞撻敍?    /// 鏉╂瑥娼￠幒銉ュ經娑旂喕鍏橀惄绋款嚠楠炶櫕绮﹂崷鐗堢川鏉╂稏鈧?    /// </summary>
    private TowerDragPreviewState CreateDragPreviewState()
    {
        return new TowerDragPreviewState(
            towerType: _dragTowerType,
            isValid: _previewPositionIsValid,
            invalidReason: _previewInvalidReason);
    }

    /// <summary>
    /// 閹跺﹤缍嬮崜宥堢箥鐞涘瞼濮搁幀浣告倱濮濄儱鍩?HUD閵?    /// </summary>
    private void RefreshHud()
    {
        if (_hudPresenter == null || _towerCatalog == null)
        {
            return;
        }

        _hudPresenter.Refresh(CreateHudState(), _towerCatalog, CanAffordTower);
    }

    /// <summary>
    /// 閺勫墽銇?Game Over閵?    ///
    /// 閻滄澘婀悰銊у箛鐏炲倻绮忛懞鍌氬嚒缂佸繋姘︾紒?HUD presenter閿?    /// 閸ョ姵顒濇潻娆撳櫡娑撴槒顩︽穱婵堟殌閳ユ粏绻橀崗?Game Over 閻樿埖鈧焦妞傜憴鍕灟鐏炲倿娓剁憰浣镐粵娴犫偓娑斿牃鈧繐绱?    /// - 闁夸礁鐣惧〒鍛婂灆缂佹挻娼悩鑸碘偓?    /// - 閸欐牗绉烽幏鏍ㄥ娑擃厾娈戦柈銊ц濞翠胶鈻?    /// - 閺嗗倸浠犻弮鍫曟？
    /// - 閹恒劑鈧礁銇戠拹銉﹀絹缁€铏圭舶 HUD
    /// </summary>
    private void ShowGameOver()
    {
        _isGameOver = true;
        CancelPlacementDragInternal();
        HideActiveEnemyHealthBars();
        Time.timeScale = 0f;

        _hudPresenter?.ShowGameOver(
            title: "GAME OVER",
            hint: "The base has fallen. Exit Play Mode to keep adjusting the level and deployment flow.");

        SetStatusMessage("Base integrity depleted. Operation failed.");
        RefreshHud();
    }

    /// <summary>
    /// 閻喐顒滈幍褑顢戞稉鈧▎鈥崇紦闁姰鈧?    ///
    /// 鏉╂瑩鍣烽弰顖涘閺堝缂撻柅鐘插弳閸欙絾娓剁紒鍫熺湽閸氬牏娈戦崷鐗堟煙閿?    /// - 閸︽澘娴樿箛顐︹偓鐔哄仯閸戝鍎寸純?    /// - 闁劎璁查崡鈩冨珛閹峰€熸儰閻?    /// - 閸忕厧顔愰弮?BuildPad 閻ㄥ嫮鍋ｉ崙璇茬紦闁?    ///
    /// 娑旂喎姘ㄩ弰顖濐嚛閿涘本妫ょ拋铏瑰负鐎硅埖妲搁幀搴濈疄閸欐垼鎹ｅ娲偓鐘侯嚞濮瑰倻娈戦敍?    /// 闁棄绻€妞ゅ鈧俺绻冩潻娆撳櫡缂佺喍绔撮崑姘崇カ濠ф劗绮ㄧ粻妤€鎷伴崥鍫熺《閹冨灲鐎规哎鈧?    /// </summary>
    /// <summary>
    /// 在进入 Game Over 结算界面时，主动隐藏当前场上敌人的血条。
    ///
    /// 这一步不是为了影响战斗逻辑，而是为了清理结算界面的视觉噪音。
    /// 当前项目的敌人血条是世界空间里的绿色条形精灵，
    /// 如果不主动收掉，它们会继续停在画面里，看起来就像 Game Over 面板内部残留的绿色装饰块。
    ///
    /// 这里不销毁敌人本体，只隐藏血条：
    /// - 这样不会额外改变关卡失败当帧的战场状态
    /// - 也能把“视觉清理”和“玩法结算”两类职责分开
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


    private bool TryPlaceTowerAt(Vector3 worldPosition, TowerType towerType, BuildPad ownerPad = null)
    {
        if (_isGameOver || towerType == TowerType.None)
        {
            return false;
        }

        if (ownerPad != null && ownerPad.IsOccupied)
        {
            SetStatusMessage("This legacy build pad is already occupied.");
            return false;
        }

        int cost = GetTowerCost(towerType);
        if (_currentEnergy < cost)
        {
            SetStatusMessage($"Not enough energy. You currently have {_currentEnergy} EN.");
            return false;
        }

        GameObject prototype = GetPrototype(towerType);
        if (prototype == null)
        {
            SetStatusMessage("Tower prototype is missing. Check the scene setup.");
            return false;
        }

        if (!ValidatePlacementPosition(worldPosition, towerType, out string invalidReason))
        {
            SetStatusMessage(invalidReason);
            return false;
        }

        GameObject tower = Instantiate(prototype, worldPosition, Quaternion.identity, _placedTowerRoot);
        tower.name = ownerPad != null
            ? $"{GetTowerDisplayName(towerType)}_{ownerPad.name}"
            : $"{GetTowerDisplayName(towerType)}_{_placedTowerRoot.childCount:00}";
        tower.SetActive(true);

        EnsureTowerPlacementCollider(tower, towerType);

        if (ownerPad != null)
        {
            ownerPad.SetOccupant(tower);

            PlacedTower placedTower = tower.GetComponent<PlacedTower>();
            if (placedTower == null)
            {
                placedTower = tower.AddComponent<PlacedTower>();
            }

            placedTower.Initialize(ownerPad, towerType);
        }

        _currentEnergy -= cost;
        InvalidatePlacementAreaOverlayCache();
        SetStatusMessage($"Deployed {GetTowerDisplayName(towerType)} for {cost} EN.");
        RefreshHud();
        return true;
    }

    /// <summary>
    /// 鍒ゆ柇鏌愪釜鍧愭爣鏄惁鍏佽寤洪€犮€?    ///
    /// 杩欐槸鑷敱鏀剧疆绯荤粺閲屾渶鏍稿績鐨勮鍒欏嚱鏁颁箣涓€銆?    /// 鐩墠瀹冩寜浠ヤ笅椤哄簭杩涜鍒ゅ畾锛?    /// 1. 鏄惁浣嶄簬 BuildZone 澶ц寖鍥村唴銆?    /// 2. 鏄惁鍘嬪埌浠讳綍 PlacementBlocker銆?    /// 3. 鏄惁绂诲凡瀛樺湪鐨勫澶繎銆?    ///
    /// 杩欑鈥滃厛绮楄繃婊わ紝鍐嶇粏杩囨护鈥濈殑鍐欐硶鏈変袱涓ソ澶勶細
    /// - 闃呰璺緞娓呮櫚
    /// - 浠ュ悗鏂板瑙勫垯鏃讹紝涔熷鏄撶户缁線涓嬭拷鍔?    /// </summary>
    private bool ValidatePlacementPosition(Vector3 worldPosition, TowerType towerType, out string invalidReason)
    {
        invalidReason = string.Empty;

        if (_buildZone == null)
        {
            invalidReason = "No BuildZone is configured in this level.";
            return false;
        }

        // 第一层：仍然必须位于关卡许可的大建造区里。
        // 这条规则保留下来，是为了防止部署网络一路扩到地图设计边界之外。
        if (!_buildZone.ContainsPoint(worldPosition))
        {
            invalidReason = "Outside the level's buildable area.";
            return false;
        }

        // 第二层：必须接在当前部署网络上。
        // 没有塔时只能用初始小区域起手；有塔后就只能沿着已建塔的方形范围继续扩张。
        if (!IsWithinPlacementNetwork(worldPosition, out invalidReason))
        {
            return false;
        }

        // 第三层：即使在部署网络内，也不能压到路径、出生点、基地等禁建区。
        float placementRadius = GetPlacementRadius(towerType);
        // 第三层：即使在部署网络内，也不能压到路径、出生点、基地等禁建区。
        // 这里改成 NonAlloc 查询，是因为这段逻辑不仅用于真正落塔，
        // 还会被覆盖层可视化重复采样很多次。
        // 如果继续用 OverlapCircleAll，就会不停创建新数组，拖拽和放塔后很容易出现 GC 卡顿。
        int overlapCount = Physics2D.OverlapCircleNonAlloc(worldPosition, placementRadius, _placementValidationOverlapBuffer);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlap = _placementValidationOverlapBuffer[i];
            if (overlap == null)
            {
                continue;
            }

            if (_placementPreviewInstance != null && overlap.transform.IsChildOf(_placementPreviewInstance.transform))
            {
                continue;
            }

            PlacementBlocker blocker = overlap.GetComponentInParent<PlacementBlocker>();
            if (blocker != null)
            {
                invalidReason = blocker.BlockerReason;
                return false;
            }

            // 第四层：最后再做塔与塔之间的占地冲突检查。
            // 这样玩家先看到“归不归当前部署网络管”，再看到“这里会不会和别的塔打架”。
            if (overlap.GetComponentInParent<DefenseTower>() != null || overlap.GetComponentInParent<RelayTower>() != null)
            {
                invalidReason = "Too close to another structure. Move it a little.";
                return false;
            }
        }


        return true;
    }

    /// <summary>
    /// 缁欐寮忔斁涓嬬殑濉旇ˉ涓婁竴涓敤浜庡崰鍦版娴嬬殑 CircleCollider2D銆?    ///
    /// 鏁屼汉绉诲姩鐩墠骞朵笉渚濊禆鐗╃悊绯荤粺锛?    /// 鎵€浠ヨ繖閲屾妸纰版挒鍣ㄨ涓?Trigger锛屽氨鑳戒笓蹇冭瀹冩湇鍔′簬鈥滃崰浣嶅垽瀹氣€濓紝
    /// 鑰屼笉浼氬紩鍏ラ澶栫殑鐗╃悊鍓綔鐢ㄣ€?    /// </summary>
    private void EnsureTowerPlacementCollider(GameObject tower, TowerType towerType)
    {
        if (tower == null)
        {
            return;
        }

        CircleCollider2D circleCollider = tower.GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = tower.AddComponent<CircleCollider2D>();
        }

        circleCollider.isTrigger = true;
        circleCollider.radius = GetPlacementRadius(towerType);
    }

    /// <summary>
    /// 鏍规嵁濉旂被鍨嬭繑鍥炲叾鍗犲湴鍗婂緞銆?    /// </summary>
    /// <summary>
    /// 閺嶈宓佹繅鏃傝閸ㄥ绻戦崶鐐插従閸楃姴婀撮崡濠傜窞閵?    ///
    /// 閻滄澘婀棃娆愨偓浣规殶閹诡喗鏁奸悽?`TowerCatalog` 閹绘劒绶甸敍?    /// 閹粯甯舵稉宥呭晙閼奉亜绻佺紒瀛樺Б婢舵矮鍞ら獮瀹狀攽 switch閵?    /// </summary>
    private float GetPlacementRadius(TowerType towerType)
    {
        TowerDefinition definition = _towerCatalog != null ? _towerCatalog.GetDefinition(towerType) : null;
        return definition != null ? definition.PlacementRadius : 0.5f;
    }

    /// <summary>
    /// 根据塔类型返回它用于“部署网络扩张”的方形边长。
    ///
    /// 占地半径和扩张方格是两套不同规则：
    /// - 占地半径回答“这座塔会不会和别的塔挤在一起”
    /// - 扩张方格回答“下一座塔理论上能不能接着往外放”
    ///
    /// 把它们拆开后，后续调平衡时就能分别调：
    /// 你可以让某种塔占地不大，但能把战线扩出去很远；
    /// 也可以让某种塔本体很大，但扩张能力一般。
    /// </summary>
    private float GetExpansionSquareSize(TowerType towerType)
    {
        TowerDefinition definition = _towerCatalog != null ? _towerCatalog.GetDefinition(towerType) : null;
        return definition != null ? definition.ExpansionSquareSize : 4.5f;
    }

    /// <summary>
    /// 判定当前落点是否位于“部署网络”允许的区域里。
    ///
    /// 这次新规则的核心就收口在这里：
    /// - 如果场上还没有塔，只能在初始小方区里放第一座
    /// - 只要已经有塔存在，后续落点就必须位于任意一座已建塔提供的方形扩张区内
    ///
    /// 注意这里判断的是“新塔中心点”是否落入允许区，
    /// 而不是要求整座塔的占地圆完整包在方格里。
    /// 这样更贴近拖拽放置时的玩家直觉。
    /// </summary>
    private bool IsWithinPlacementNetwork(Vector3 worldPosition, out string invalidReason)
    {
        invalidReason = string.Empty;

        if (_placedTowerRoot == null || _placedTowerRoot.childCount == 0)
        {
            if (IsInsideSquare(worldPosition, initialPlacementSquareCenter, initialPlacementSquareSize))
            {
                return true;
            }

            invalidReason = "Your first structure must be placed in the starter zone.";
            return false;
        }

        for (int i = 0; i < _placedTowerRoot.childCount; i++)
        {
            Transform placedTower = _placedTowerRoot.GetChild(i);
            if (placedTower == null || !placedTower.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!TryGetPlacedTowerType(placedTower, out TowerType placedTowerType))
            {
                continue;
            }

            if (IsInsideSquare(worldPosition, placedTower.position, GetExpansionSquareSize(placedTowerType)))
            {
                return true;
            }
        }

        invalidReason = "New structures must connect to an existing expansion square.";
        return false;
    }

    /// <summary>
    /// 用一个很轻量的方式识别“这座已建塔属于哪种塔类型”。
    ///
    /// 这里没有再引入新的注册表或额外资产，
    /// 是因为当前原型只有两种塔，直接看组件已经足够稳定，
    /// 也能避免为了一个简单规则把系统复杂度再抬高一层。
    /// </summary>
    private bool TryGetPlacedTowerType(Transform placedTower, out TowerType towerType)
    {
        if (placedTower == null)
        {
            towerType = TowerType.None;
            return false;
        }

        if (placedTower.GetComponent<RelayTower>() != null)
        {
            towerType = TowerType.Relay;
            return true;
        }

        if (placedTower.GetComponent<DefenseTower>() != null)
        {
            towerType = TowerType.Defense;
            return true;
        }

        towerType = TowerType.None;
        return false;
    }

    /// <summary>
    /// 判断某个世界坐标点是否落在以指定中心为基准的方形区域内。
    ///
    /// 这里使用的是轴对齐正方形判定：
    /// 只要 X 和 Y 到中心的距离都不超过半边长，就视为处于该区域中。
    /// 这种写法比额外创建检测碰撞体更轻，也更适合当前原型期规则验证。
    /// </summary>
    private bool IsInsideSquare(Vector3 worldPosition, Vector2 squareCenter, float squareSize)
    {
        float halfSize = Mathf.Max(0f, squareSize) * 0.5f;
        return Mathf.Abs(worldPosition.x - squareCenter.x) <= halfSize
            && Mathf.Abs(worldPosition.y - squareCenter.y) <= halfSize;
    }

    /// <summary>
    /// 返回当前这类塔最值得采样的覆盖层边界。
    ///
    /// 之前覆盖层默认总是扫整块 BuildZone，
    /// 这在规则上当然没错，但在性能上非常浪费：
    /// 大量明明不可能合法的空白区域，也会被重复做精确采样。
    ///
    /// 现在我们把采样范围收敛到“当前部署网络本身的外接包围盒”，
    /// 也就是：
    /// - 首塔阶段：只看初始部署区
    /// - 已有塔后：只看所有扩张方格并集的包围范围
    /// - 最后再和 BuildZone 求交集
    ///
    /// 这样不会改变任何真实合法性规则，
    /// 但能显著减少覆盖层需要评估的像素数量。
    /// </summary>
    private Bounds GetPlacementOverlayWorldBounds(TowerType towerType)
    {
        if (_buildZone == null)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Bounds buildBounds = _buildZone.WorldBounds;

        if (_placedTowerRoot == null || _placedTowerRoot.childCount == 0)
        {
            Bounds initialBounds = CreateSquareBounds(initialPlacementSquareCenter, initialPlacementSquareSize);
            return IntersectBounds(buildBounds, initialBounds);
        }

        if (!TryBuildPlacementNetworkBounds(out Bounds networkBounds))
        {
            return buildBounds;
        }

        return IntersectBounds(buildBounds, networkBounds);
    }

    /// <summary>
    /// 根据场上已建塔提供的扩张方格，构建当前部署网络的总包围盒。
    ///
    /// 注意这里并不是求“最终精确合法区域”，
    /// 而只是求一个足够小、但又能完整包住所有潜在合法区的采样边界。
    /// 真正是否能放，仍然继续由 `ValidatePlacementPosition` 逐点裁决。
    /// </summary>
    private bool TryBuildPlacementNetworkBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasAnyBounds = false;

        if (_placedTowerRoot == null)
        {
            return false;
        }

        for (int i = 0; i < _placedTowerRoot.childCount; i++)
        {
            Transform placedTower = _placedTowerRoot.GetChild(i);
            if (placedTower == null || !placedTower.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!TryGetPlacedTowerType(placedTower, out TowerType placedTowerType))
            {
                continue;
            }

            Bounds squareBounds = CreateSquareBounds(placedTower.position, GetExpansionSquareSize(placedTowerType));
            if (!hasAnyBounds)
            {
                bounds = squareBounds;
                hasAnyBounds = true;
            }
            else
            {
                bounds.Encapsulate(squareBounds.min);
                bounds.Encapsulate(squareBounds.max);
            }
        }

        return hasAnyBounds;
    }

    /// <summary>
    /// 以中心点和边长构建一个世界空间方形 Bounds。
    /// </summary>
    private static Bounds CreateSquareBounds(Vector2 center, float size)
    {
        return new Bounds(new Vector3(center.x, center.y, 0f), new Vector3(size, size, 0f));
    }

    /// <summary>
    /// 求两个世界包围盒的交集。
    ///
    /// 如果两者没有重叠，就返回一个零尺寸 Bounds，
    /// 上层覆盖层逻辑会自动把它当成“没有可显示区域”。
    /// </summary>
    private static Bounds IntersectBounds(Bounds a, Bounds b)
    {
        Vector3 min = Vector3.Max(a.min, b.min);
        Vector3 max = Vector3.Min(a.max, b.max);

        if (max.x <= min.x || max.y <= min.y)
        {
            return new Bounds(a.center, Vector3.zero);
        }

        Bounds intersection = new Bounds();
        intersection.SetMinMax(min, max);
        return intersection;
    }


    /// <summary>
    /// 娴犲骸缍嬮崜宥夌炊閺嶅洤鐫嗛獮鏇炴綏閺嶅洩顓哥粻妤佸灛閸﹁桨鑵戦惃鍕瑯閻ｅ苯娼楅弽鍥モ偓?    /// </summary>
    /// <summary>
    /// 根据当前拖拽的塔类型，重新生成一张“精确合法区域”覆盖图。
    ///
    /// 这里故意不复制第二套几何规则，
    /// 而是直接重放真实的 `ValidatePlacementPosition` 判定。
    /// 这样能保证：
    /// 玩家在地图上看到的青色高亮区域，和真正能放下去的中心点区域保持一致。
    /// </summary>

    /// <summary>
    /// 提前预热某种塔当前版本下的合法区域覆盖层。
    ///
    /// 这一步的目标不是立刻显示覆盖图，
    /// 而是把最重的整图重建尽量前移到“悬停卡片 / 点击选塔”阶段。
    /// 这样等真正开始拖拽时，通常只需要把缓存重新显示出来。
    /// </summary>
    public void PrewarmPlacementAreaOverlay(TowerType towerType)
    {
        if (_isGameOver || towerType == TowerType.None)
        {
            return;
        }

        if (_placementAreaOverlayRenderer == null || _buildZone == null || _placementPreviewRoot == null)
        {
            return;
        }

        if (IsPlacementAreaOverlayPreparedFor(towerType))
        {
            return;
        }

        _placementAreaOverlayRenderer.Show(
            _placementPreviewRoot,
            GetPlacementOverlayWorldBounds(towerType),
            worldPosition => ValidatePlacementPosition(worldPosition, towerType, out _));
        _placementAreaOverlayRenderer.Hide();

        _placementAreaOverlayPreparedRevision = _placementAreaOverlayRevision;
        _placementAreaOverlayPreparedTowerType = towerType;
    }

    /// <summary>
    /// 判断当前缓存里，是否已经有“这类塔 + 当前布局版本”对应的覆盖层结果。
    /// </summary>
    private bool IsPlacementAreaOverlayPreparedFor(TowerType towerType)
    {
        return _placementAreaOverlayPreparedRevision == _placementAreaOverlayRevision
            && _placementAreaOverlayPreparedTowerType == towerType;
    }

    /// <summary>
    /// 当场上已建塔发生变化时，让旧的覆盖层缓存失效。
    ///
    /// 这能避免玩家看到一张已经过期的合法区域图。
    /// </summary>
    private void InvalidatePlacementAreaOverlayCache()
    {
        _placementAreaOverlayRevision++;
        _placementAreaOverlayPreparedRevision = -1;
        _placementAreaOverlayPreparedTowerType = TowerType.None;
    }

    /// <summary>
    /// 在拖拽真正开始时显示覆盖层。
    ///
    /// 如果缓存已经准备好，就只做一次重新显示；
    /// 如果缓存还没好，才退回到现场重建，保证正确性优先。
    /// </summary>
    private void ShowPreparedPlacementAreaOverlay(TowerType towerType)
    {
        if (_placementAreaOverlayRenderer == null || _buildZone == null || _placementPreviewRoot == null)
        {
            return;
        }

        if (IsPlacementAreaOverlayPreparedFor(towerType))
        {
            _placementAreaOverlayRenderer.ShowPrepared(_placementPreviewRoot, GetPlacementOverlayWorldBounds(towerType));
            return;
        }

        RefreshPlacementAreaOverlay(towerType);
        _placementAreaOverlayPreparedRevision = _placementAreaOverlayRevision;
        _placementAreaOverlayPreparedTowerType = towerType;
    }

    private void RefreshPlacementAreaOverlay(TowerType towerType)
    {
        if (_placementAreaOverlayRenderer == null || !_isPlacementDragActive || towerType == TowerType.None || _buildZone == null || _placementPreviewRoot == null)
        {
            HidePlacementAreaOverlay();
            return;
        }

        _placementAreaOverlayRenderer.Show(
            _placementPreviewRoot,
            GetPlacementOverlayWorldBounds(towerType),
            worldPosition => ValidatePlacementPosition(worldPosition, towerType, out _));
    }

    /// <summary>
    /// 隐藏当前合法区域覆盖图。
    ///
    /// 之所以把这个动作单独抽出来，
    /// 是为了让“拖拽结束 / 取消 / GameOver”这些出口都能走同一条清理路径。
    /// </summary>
    private void HidePlacementAreaOverlay()
    {
        _placementAreaOverlayRenderer?.Hide();
    }

    private Vector3 GetMouseWorldPosition()
    {
        return ScreenToWorldPosition(Input.mousePosition);
    }

    /// <summary>
    /// 鎶婁换鎰忓睆骞曞潗鏍囪浆鎹㈡垚鎴樺満涓殑 2D 涓栫晫鍧愭爣銆?    ///
    /// 鐢变簬褰撳墠鏄浜ょ浉鏈轰刊瑙?2D 鍦烘櫙锛?    /// 鎵€浠ヨ繖閲屾渶缁堜細鎶?Z 鍥哄畾鍥?0锛岀‘淇濆濮嬬粓钀藉湪鍚屼竴骞抽潰涓娿€?    /// </summary>
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
    /// 纭繚褰撳墠鎷栨嫿濉旂被鍨嬪搴旂殑棰勮瀵硅薄瀛樺湪銆?    ///
    /// 杩欓噷鐨勫疄鐜版槸鍏嬮殕姝ｅ紡濉斿師鍨嬶紝
    /// 浣嗘妸浼氱湡姝ｅ弬涓庢垬鏂楃殑琛屼负鑴氭湰绂佺敤鎺夛紝璁╁畠鍙綔涓鸿瑙夐瑙堝瓨鍦ㄣ€?    /// 杩欑鍋氭硶姣旀墜鍐欏彟涓€濂椻€滃亣棰勮妯″瀷鈥濇洿鐪佹垚鏈紝涔熸洿涓嶅鏄撳瑙傚涓嶄笂銆?    /// </summary>
    private void EnsurePlacementPreviewInstance(TowerType towerType)
    {
        if (_placementPreviewInstance != null && _placementPreviewTowerType == towerType)
        {
            _placementPreviewInstance.SetActive(true);
            return;
        }

        ReleasePlacementPreviewInstance();

        GameObject prototype = GetPrototype(towerType);
        if (prototype == null)
        {
            return;
        }

        _placementPreviewInstance = Instantiate(prototype, Vector3.zero, Quaternion.identity, _placementPreviewRoot);
        _placementPreviewTowerType = towerType;
        _placementPreviewInstance.name = $"{GetTowerDisplayName(towerType)}_Preview";
        _placementPreviewInstance.SetActive(true);

        DefenseTower defenseTower = _placementPreviewInstance.GetComponent<DefenseTower>();
        if (defenseTower != null)
        {
            defenseTower.enabled = false;
        }

        RelayTower relayTower = _placementPreviewInstance.GetComponent<RelayTower>();
        if (relayTower != null)
        {
            relayTower.enabled = false;
        }

        Collider2D[] previewColliders = _placementPreviewInstance.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < previewColliders.Length; i++)
        {
            previewColliders[i].enabled = false;
        }

        _placementPreviewSpriteRenderer = _placementPreviewInstance.GetComponent<SpriteRenderer>();
        if (_placementPreviewSpriteRenderer != null)
        {
            _placementPreviewSpriteRenderer.sortingOrder = 15;
        }

        // Add a ground ring so free placement shows the actual occupied area more clearly.
        // This keeps the preview readable without requiring the player to guess from the tower sprite alone.
        Sprite ringSprite = Resources.Load<Sprite>(placementRingResourcePath);
        if (ringSprite != null)
        {
            GameObject placementRing = new GameObject("PlacementRing");
            placementRing.transform.SetParent(_placementPreviewInstance.transform, false);
            placementRing.transform.localPosition = Vector3.zero;
            placementRing.transform.localScale = Vector3.one * (GetPlacementRadius(towerType) * 2.35f);

            _placementPreviewRingRenderer = placementRing.AddComponent<SpriteRenderer>();
            _placementPreviewRingRenderer.sprite = ringSprite;
            _placementPreviewRingRenderer.sortingOrder = 14;
        }
    }

    private void DestroyPlacementPreview()
    {
        if (_placementPreviewInstance != null)
        {
            _placementPreviewInstance.SetActive(false);
        }
    }

    /// <summary>
    /// 真正释放当前预览塔对象。
    ///
    /// 平时结束拖拽时我们只隐藏预览塔，尽量复用同塔型实例，
    /// 以减少频繁 Instantiate / Destroy 带来的延迟 GC 抖动。
    ///
    /// 只有在切换到另一种塔型，或者总控自身销毁时，
    /// 才需要真的把旧预览对象释放掉。
    /// </summary>
    private void ReleasePlacementPreviewInstance()
    {
        if (_placementPreviewInstance != null)
        {
            Destroy(_placementPreviewInstance);
        }

        _placementPreviewInstance = null;
        _placementPreviewTowerType = TowerType.None;
        _placementPreviewSpriteRenderer = null;
        _placementPreviewRingRenderer = null;
    }

    /// <summary>
    /// 鏍规嵁褰撳墠棰勮鍚堟硶鎬у埛鏂伴瑙堝棰滆壊銆?    /// </summary>
    private void UpdatePlacementPreviewVisual()
    {
        if (_placementPreviewSpriteRenderer == null)
        {
            return;
        }

        Color previewColor = _previewPositionIsValid ? validPreviewColor : invalidPreviewColor;
        _placementPreviewSpriteRenderer.color = previewColor;

        if (_placementPreviewRingRenderer != null)
        {
            Color ringColor = previewColor;
            ringColor.a = _previewPositionIsValid ? 0.9f : 0.82f;
            _placementPreviewRingRenderer.color = ringColor;
        }
    }

    /// <summary>
    /// 鏇存柊璺熼殢榧犳爣鐨勬嫋鎷芥彁绀洪潰鏉裤€?    ///
    /// 杩欎釜闈㈡澘鎵挎媴鐨勬槸鈥滅簿鐐艰鏄庘€濈殑鑱岃矗锛?    /// 瀹冧細鍛婅瘔鐜╁褰撳墠鎷栫潃鍝紶鍗°€侀渶瑕佸灏戠數閲忥紝浠ュ強姝ゅ埢涓轰粈涔堣兘鏀?/ 涓嶈兘鏀俱€?    /// </summary>
    /// <summary>
    /// 閸愬懘鍎撮悧鍫㈡畱閹锋牗瀚块崣鏍ㄧХ闁槒绶妴?    ///
    /// 鐎瑰啫褰х拹鐔荤煑濞撳懐鎮婇幏鏍ㄥ鏉╂劘顢戦弮鍓佸Ц閹焦婀伴煬顐礉
    /// 娑撳秳瀵岄崝銊︽暭閸斻劉鈧粌缍嬮崜宥夆偓澶夎厬閻ㄥ嫬顢欑猾璇茬€烽垾婵勨偓?    /// 鏉╂瑦鐗辩拫鍐暏閺傜懓姘ㄩ崣顖欎簰閺嶈宓侀崷鐑樻珯閸愬啿鐣鹃敍?    /// 閺勵垰褰ч崣鏍ㄧХ閹锋牗瀚块敍宀冪箷閺勵垵绻涢崥宀勨偓澶夎厬閻樿埖鈧椒绔寸挧閿嬬閹哄鈧?    /// </summary>
    private void CancelPlacementDragInternal()
    {
        _isPlacementDragActive = false;
        _dragTowerType = TowerType.None;
        _previewPositionIsValid = false;
        _previewInvalidReason = string.Empty;
        _hudPresenter.SetDragPreviewVisible(false);
        HidePlacementAreaOverlay();
        DestroyPlacementPreview();
    }

    /// <summary>
    /// 鍒ゆ柇褰撳墠榧犳爣鏄惁鎮仠鍦?UI 涓娿€?    ///
    /// 杩欐槸鈥滃湴鍥句氦浜掆€濅笌鈥滅晫闈氦浜掆€濅箣闂存渶鍩虹鐨勪竴灞傞殧绂讳繚鎶ゃ€?    /// </summary>
    private bool IsPointerOverUserInterface()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 鏍规嵁濉旂被鍨嬭繑鍥炲缓閫犳垚鏈€?    /// </summary>
    /// <summary>
    /// 閺嶈宓佹繅鏃傝閸ㄥ绻戦崶鐐茬紦闁姵鍨氶張顑锯偓?    /// </summary>
    private int GetTowerCost(TowerType towerType)
    {
        TowerDefinition definition = _towerCatalog != null ? _towerCatalog.GetDefinition(towerType) : null;
        return definition != null ? definition.BuildCost : 0;
    }

    /// <summary>
    /// 鏉╂柨娲栭弴鎾偓鍌氭値閺勫墽銇氱紒娆戝负鐎瑰墎娈戞繅鏂挎倳缁夎埇鈧?    ///
    /// 瑜版挸澧犻柌鍥╂暏閻ｃ儱浜搁垾婊冨叡閸涙﹢鍎寸純鑼矒缁旑垪鈧繈顥撻弽鑲╂畱閻厼鎮曢敍?    /// 閺勵垯璐熸禍鍡氼唨閸楋紕澧栭崪宀€濮搁幀浣圭埉閻鎹ｉ弶銉︽纯閸嶅繋绔存總妤€鐣弫瀛樻惙娴ｆ粎鏅棃顫偓?    /// </summary>
    /// <summary>
    /// 鏉╂柨娲栭弴鎾偓鍌氭値閺勫墽銇氱紒娆戝负鐎瑰墎娈戞繅鏂挎倳缁夎埇鈧?    ///
    /// 瑜版挸澧犻柌鍥╂暏閻ｃ儱浜搁垾婊冨叡閸涙﹢鍎寸純鑼矒缁旑垪鈧繈顥撻弽鑲╂畱閻厼鎮曢敍?    /// 閺勵垯璐熸禍鍡氼唨閸楋紕澧栭崪宀€濮搁幀浣圭埉閻鎹ｉ弶銉︽纯閸嶅繋绔存總妤€鐣弫瀛樻惙娴ｆ粎鏅棃顫偓?    /// </summary>
    private string GetTowerDisplayName(TowerType towerType)
    {
        TowerDefinition definition = _towerCatalog != null ? _towerCatalog.GetDefinition(towerType) : null;
        return definition != null ? definition.DisplayName : "None";
    }

    /// <summary>
    /// 閺嶈宓佹繅鏃傝閸ㄥ绻戦崶鐐差嚠鎼存柨甯崹瀣嚠鐠灺扳偓?    /// </summary>
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
    /// 鎸夊悕瀛楁煡鎵惧綋鍓嶅師鍨嬫墍闇€鐨勫満鏅璞′笌缁勪欢銆?    ///
    /// 杩欏椤圭洰鐩墠浠嶇劧閲囩敤鈥滈€氳繃鍛藉悕绾﹀畾鏌ユ壘瀵硅薄鈥濈殑鍘熷瀷鏈熺瓥鐣ワ紝
    /// 鐩殑鏄繚鎸佹惌寤洪€熷害鍜屽彲璇绘€с€?    /// 绛夊悗缁郴缁熷彉澶氫箣鍚庯紝鍐嶉€愭寰€鏄惧紡鎷栧紩鐢ㄦ垨閰嶇疆璧勪骇婕旇繘涔熶笉杩熴€?    /// </summary>
    /// <summary>
    /// 閹稿鎮曠€涙鐓￠幍鎯х秼閸撳秴甯崹瀣闂団偓閻ㄥ嫬婧€閺咁垰顕挒鈥茬瑢缂佸嫪娆㈤妴?    ///
    /// 瑜版挸澧犻崷鐑樻珯鐟佸懘鍘ゆ禒宥囧姧娴ｈ法鏁ら垾婊冩倳缁夋壆瀹崇€?+ SceneObjectFinder閳ユ繆绻栭弶陇鐭剧痪鍖＄礉
    /// 閸ョ姳璐熸潻娆愵偧閺嬭埖鐎銉ょ稊閸欘亝鍏傞幏鍡氫捍鐠愶綇绱濇稉宥嗗厒閸氬本妞傞弴瀛樺床鐟佸懘鍘ら弬鐟扮础閵?    /// </summary>
    private void FindSceneReferences()
    {
        _mainCamera = SceneObjectFinder.FindComponent<Camera>(mainCameraName);
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        _relayTowerPrototype = SceneObjectFinder.FindGameObject(relayTowerPrototypeName);
        _defenseTowerPrototype = SceneObjectFinder.FindGameObject(defenseTowerPrototypeName);

        _hudPresenter?.FindSceneReferences();
        _buildZone = EnsureBuildZoneExists();
    }

    /// <summary>
    /// 绾喕绻氭潻鎰攽閺冨墎鏁ら崚鎵畱閺嶇濡悙鐟扮摠閸︺劊鈧?    /// </summary>
    private void EnsureRuntimeRoots()
    {
        _placedTowerRoot = SceneObjectFinder.FindOrCreateTransform(placedTowerRootName);
        _placementPreviewRoot = SceneObjectFinder.FindOrCreateTransform(placementPreviewRootName);
    }

    /// <summary>
    /// 绾喕绻?BuildZone 鐎涙ê婀妴?    ///
    /// 濮濓絽鐖堕幆鍛枌娑撳鐣犳惔鏃囶嚉閺勵垰婧€閺咁垶鍣烽弰搴ｂ€橀幗鍡樻杹婵傜晫娈戠€电钖勯敍?    /// 鏉╂瑩鍣锋０婵嗩樆閹绘劒绶垫稉鈧稉顏囩箥鐞涘本妞傞崗婊冪俺閸掓稑缂撻敍灞炬Ц娑撹桨绨￠柆鍨帳娴ｇ姴婀崢鐔风€烽梼鑸殿唽閸ョ姳璐熷蹇旀啘鐎电钖勯懓宀€娲块幒銉ュ幢濮濈粯绁︾粙瀣ㄢ偓?    /// </summary>
    private BuildZone EnsureBuildZoneExists()
    {
        BuildZone buildZone = SceneObjectFinder.FindComponent<BuildZone>(buildZoneName);
        if (buildZone != null)
        {
            return buildZone;
        }

        GameObject buildZoneObject = new GameObject(buildZoneName);
        buildZoneObject.transform.position = new Vector3(0f, 0.25f, 0f);

        BoxCollider2D boxCollider = buildZoneObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(18f, 10.5f);

        return buildZoneObject.AddComponent<BuildZone>();
    }
}

/// <summary>
/// PlacementAreaOverlayRenderer 璐熻矗鎶娾€滃綋鍓嶅绫诲瀷鐪熸鍏佽钀戒腑蹇冪偣鐨勪綅缃€濈粯鍒舵垚涓€寮犱笘鐣岀┖闂磋鐩栧浘銆?///
/// 涓轰粈涔堣繖閲屼笉鍐嶆墜鍐欎竴濂楀崟鐙殑鍑犱綍鍙鍖栬鍒欙紵
/// 鍥犱负褰撳墠鐪熷疄鏀剧疆鍒ゅ畾宸茬粡鍚屾椂渚濊禆锛?/// - BuildZone
/// - 鍒濆閮ㄧ讲鍖?/ 宸插缓濉旈儴缃茬綉缁?/// - PlacementBlocker
/// - 宸叉湁濉斿崰鍦扮鎾?///
/// 濡傛灉瑕嗙洊灞傝嚜宸卞啀澶嶅埗涓€濂楄鍒欙紝
/// 寰堝鏄撳嚭鐜扳€滅敾鍑烘潵鑳芥斁锛屼絾鐪熸鐐逛笅鍘诲嵈涓嶈兘鏀锯€濈殑淇℃伅鍒嗗弶銆?///
/// 鎵€浠ヨ繖閲岄噰鐢ㄤ竴涓洿绋崇殑鍋氭硶锛?/// 鐩存帴鎸変竴瀹氬垎杈ㄧ巼閲嶆斁鐪熷疄鍒ゅ畾鍑芥暟锛?/// 鎶婃瘡涓噰鏍风偣鏄笉鏄悎娉曪紝鐑樻垚涓€寮犲甫杈圭紭楂樹寒鐨勮创鍥俱€?///
/// 杩欑鏂规涓嶆槸鏈€鏁板鍖栫殑瑙ｆ瀽瑁佸壀锛?/// 浣嗗畠鏈€澶х殑浠峰€兼槸锛?/// 鍙鍖栧拰鐪熷疄鐜╂硶姘歌繙鏉ヨ嚜鍚屼竴濂楀垽鏂€昏緫銆?/// 瀵瑰綋鍓嶅師鍨嬫湡椤圭洰鏉ヨ锛岃繖鏄潪甯稿垝绠楃殑鎶樹腑銆?/// </summary>
public sealed class PlacementAreaOverlayRenderer : IDisposable
{
    /// <summary>
    /// 在基础像素密度之上，再额外给覆盖层一点分辨率加成。
    ///
    /// 当前可放置区域覆盖层的边界粗糙，核心原因之一就是纹理分辨率偏低：
    /// 同样一段世界空间边界，被投到纹理里时像素太少，就会天然出现明显台阶感。
    ///
    /// 这里额外乘一个温和的倍率，而不是直接把外部序列化字段调得特别夸张，
    /// 是为了把“更平滑”作为这套覆盖层的默认品质，同时又不需要人每次手动调场景参数。
    /// </summary>
    private const float OverlayResolutionScale = 1.1f;

    /// <summary>
    /// 单个最终像素内部用于抗锯齿的子采样网格边长。
    ///
    /// 例如取值 3 时，表示每个最终像素会再细分成 3 x 3 个子采样点。
    /// 这样我们就不再只知道“这个像素是否合法”，
    /// 还能知道“这个像素内部有多少比例实际落在合法区域里”。
    ///
    /// 边界平滑感的关键，就来自这种“覆盖率”信息。
    /// </summary>
    private const int EdgeSupersampleGridSize = 2;

    private readonly float _pixelsPerUnit;
    private readonly Color _fillColor;
    private readonly Color _edgeColor;
    private readonly int _sortingOrder;

    private GameObject _overlayObject;
    private SpriteRenderer _spriteRenderer;
    private Texture2D _overlayTexture;
    private Sprite _overlaySprite;
    private bool[] _legalMaskBuffer;
    private Color[] _pixelBuffer;
    private int _bufferWidth;
    private int _bufferHeight;

    public PlacementAreaOverlayRenderer(float pixelsPerUnit, Color fillColor, Color edgeColor, int sortingOrder)
    {
        _pixelsPerUnit = Mathf.Max(4f, pixelsPerUnit);
        _fillColor = fillColor;
        _edgeColor = edgeColor;
        _sortingOrder = sortingOrder;
    }

    /// <summary>
    /// 根据当前 BuildZone 世界范围和真实合法性判定，显示一张世界空间覆盖图。
    ///
    /// 这里仍然坚持一个非常重要的边界：
    /// “画出来哪里能放”必须继续使用真实的 validator 来决定，
    /// 我们只改可视化质量，不复制也不发明第二套规则。
    /// </summary>
    public void Show(Transform parent, Bounds worldBounds, Func<Vector3, bool> validator)
    {
        if (parent == null || validator == null || worldBounds.size.x <= Mathf.Epsilon || worldBounds.size.y <= Mathf.Epsilon)
        {
            Hide();
            return;
        }

        EnsureOverlayObject(parent);
        RebuildOverlayTexture(worldBounds, validator);

        if (_overlayObject != null)
        {
            ApplyOverlayTransform(worldBounds);
            _overlayObject.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏当前覆盖图，但暂时保留对象和纹理容器，方便下次继续复用。
    /// </summary>

    /// <summary>
    /// 显示已经准备好的覆盖层，不再重复重建纹理。
    ///
    /// 只要缓存仍然有效，这里就只是一次轻量的显隐与位置同步，
    /// 远比重新跑整张合法性采样便宜。
    /// </summary>
    public void ShowPrepared(Transform parent, Bounds worldBounds)
    {
        if (parent == null || worldBounds.size.x <= Mathf.Epsilon || worldBounds.size.y <= Mathf.Epsilon)
        {
            Hide();
            return;
        }

        if (_overlayObject == null || _spriteRenderer == null || _spriteRenderer.sprite == null)
        {
            return;
        }

        EnsureOverlayObject(parent);
        ApplyOverlayTransform(worldBounds);
        _overlayObject.SetActive(true);
    }

    public void Hide()
    {
        if (_overlayObject != null)
        {
            _overlayObject.SetActive(false);
        }
    }

    /// <summary>
    /// 彻底释放覆盖层持有的纹理与精灵资源。
    /// </summary>
    public void Dispose()
    {
        DestroyTextureResources();

        if (_overlayObject != null)
        {
            UnityEngine.Object.Destroy(_overlayObject);
            _overlayObject = null;
            _spriteRenderer = null;
        }
    }

    /// <summary>
    /// 确保世界空间覆盖层对象存在，并挂到预览根节点下。
    ///
    /// 这里继续使用 SpriteRenderer + Texture2D 的轻量方案，
    /// 是因为它对当前原型已经足够直观，而且很容易继续在像素层面做边界优化。
    /// </summary>
    private void EnsureOverlayObject(Transform parent)
    {
        if (_overlayObject == null)
        {
            _overlayObject = new GameObject("PlacementAreaOverlay");
            _overlayObject.transform.SetParent(parent, false);

            _spriteRenderer = _overlayObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sortingOrder = _sortingOrder;
            _spriteRenderer.color = Color.white;
        }
        else if (_overlayObject.transform.parent != parent)
        {
            _overlayObject.transform.SetParent(parent, false);
        }
    }

    /// <summary>
    /// 同步覆盖层对象在世界中的位置与缩放。
    ///
    /// 单独拆出这个方法，是为了让“重建时”和“直接显示缓存时”
    /// 都能复用同一套摆放逻辑。
    /// </summary>
    private void ApplyOverlayTransform(Bounds worldBounds)
    {
        if (_overlayObject == null)
        {
            return;
        }

        _overlayObject.transform.position = new Vector3(worldBounds.center.x, worldBounds.center.y, 0f);
        _overlayObject.transform.localScale = Vector3.one;
    }


    /// <summary>
    /// 重新生成可放置区域覆盖层纹理。
    ///
    /// 这一版和平滑度直接相关的改动有三层：
    /// 1. 提高基础分辨率，减少大台阶。
    /// 2. 只对真正贴边的像素再做多重采样，兼顾平滑度与运行时成本。
    /// 3. 纹理过滤从 Point 改为 Bilinear，让边界在屏幕上进一步柔和。
    ///
    /// 注意：真正的“这里能不能放”仍然只由 validator 决定。
    /// 覆盖率只服务于边界显示质量，不改变玩法判定本身。
    /// </summary>
    private void RebuildOverlayTexture(Bounds worldBounds, Func<Vector3, bool> validator)
    {
        float effectivePixelsPerUnit = _pixelsPerUnit * OverlayResolutionScale;
        int width = Mathf.Clamp(Mathf.CeilToInt(worldBounds.size.x * effectivePixelsPerUnit), 32, 1024);
        int height = Mathf.Clamp(Mathf.CeilToInt(worldBounds.size.y * effectivePixelsPerUnit), 32, 1024);

        EnsureWorkingBuffers(width, height);
        bool[] legalMask = _legalMaskBuffer;
        Color[] pixels = _pixelBuffer;

        Vector3 min = worldBounds.min;
        float stepX = worldBounds.size.x / width;
        float stepY = worldBounds.size.y / height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector3 centerPoint = new Vector3(
                    min.x + (x + 0.5f) * stepX,
                    min.y + (y + 0.5f) * stepY,
                    0f);

                legalMask[index] = validator(centerPoint);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                if (!legalMask[index])
                {
                    pixels[index] = Color.clear;
                    continue;
                }

                bool isBoundaryPixel = HasIllegalNeighbour(legalMask, width, height, x, y);
                float coverage = isBoundaryPixel ? SamplePixelCoverage(min, stepX, stepY, x, y, validator) : 1f;
                pixels[index] = BuildPixelColor(isBoundaryPixel, coverage);
            }
        }

        EnsureTextureResources(width, height, worldBounds.size.x);
        _overlayTexture.SetPixels(pixels);
        _overlayTexture.Apply(false, false);

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = _overlaySprite;
        }
    }


    private void EnsureWorkingBuffers(int width, int height)
    {
        if (_legalMaskBuffer == null || _pixelBuffer == null || _bufferWidth != width || _bufferHeight != height)
        {
            _legalMaskBuffer = new bool[width * height];
            _pixelBuffer = new Color[width * height];
            _bufferWidth = width;
            _bufferHeight = height;
        }
    }

    /// <summary>
    /// 确保覆盖层使用的 Texture2D / Sprite 资源已经就绪。
    ///
    /// 如果尺寸没变，就继续复用上一轮的纹理和 Sprite；
    /// 只有真正发生尺寸变化时，才释放旧资源并创建新资源。
    ///
    /// 由于当前 BuildZone 尺寸基本稳定，这意味着大多数重建都不再分配新的引擎对象。
    /// </summary>
    private void EnsureTextureResources(int width, int height, float worldWidth)
    {
        if (_overlayTexture != null && _overlaySprite != null && _overlayTexture.width == width && _overlayTexture.height == height)
        {
            return;
        }

        DestroyTextureResources();

        _overlayTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        _overlayTexture.wrapMode = TextureWrapMode.Clamp;
        _overlayTexture.filterMode = FilterMode.Bilinear;

        float spritePixelsPerUnit = width / worldWidth;
        _overlaySprite = Sprite.Create(
            _overlayTexture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            spritePixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
    }

    private static float SamplePixelCoverage(
        Vector3 min,
        float stepX,
        float stepY,
        int x,
        int y,
        Func<Vector3, bool> validator)
    {
        int legalSampleCount = 0;
        int totalSampleCount = EdgeSupersampleGridSize * EdgeSupersampleGridSize;

        for (int sampleY = 0; sampleY < EdgeSupersampleGridSize; sampleY++)
        {
            for (int sampleX = 0; sampleX < EdgeSupersampleGridSize; sampleX++)
            {
                float normalizedX = (sampleX + 0.5f) / EdgeSupersampleGridSize;
                float normalizedY = (sampleY + 0.5f) / EdgeSupersampleGridSize;

                Vector3 samplePoint = new Vector3(
                    min.x + (x + normalizedX) * stepX,
                    min.y + (y + normalizedY) * stepY,
                    0f);

                if (validator(samplePoint))
                {
                    legalSampleCount++;
                }
            }
        }

        return (float)legalSampleCount / totalSampleCount;
    }

    /// <summary>
    /// 根据“是否边界像素”和“覆盖率”组装最终颜色。
    ///
    /// 这里的核心思路是：
    /// - 内部像素更接近 fillColor
    /// - 边界像素更接近 edgeColor
    /// - 透明度再根据覆盖率做平滑衰减
    ///
    /// 这样玩家仍然能清楚看出边界位置，
    /// 但它不再是一圈生硬的块状描边，而会更接近柔和的区域轮廓。
    /// </summary>
    private Color BuildPixelColor(bool isBoundaryPixel, float coverage)
    {
        float boundaryBlend = isBoundaryPixel
            ? Mathf.Lerp(0.25f, 0.65f, 1f - coverage)
            : 0f;

        Color baseColor = Color.Lerp(_fillColor, _edgeColor, boundaryBlend);
        float softenedAlpha = baseColor.a * Mathf.SmoothStep(0f, 1f, coverage);
        baseColor.a = softenedAlpha;
        return baseColor;
    }

    /// <summary>
    /// 判断当前像素是否贴着非法区域。
    ///
    /// 这里只看 8 邻域，已经足够提供一圈稳定的“边界感”。
    /// 而真正让边界柔和起来的工作，则交给上面的 coverage 和颜色混合来完成。
    /// </summary>
    private static bool HasIllegalNeighbour(bool[] legalMask, int width, int height, int x, int y)
    {
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0)
                {
                    continue;
                }

                int neighbourX = x + offsetX;
                int neighbourY = y + offsetY;
                if (neighbourX < 0 || neighbourX >= width || neighbourY < 0 || neighbourY >= height)
                {
                    return true;
                }

                if (!legalMask[(neighbourY * width) + neighbourX])
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 释放当前覆盖层使用的临时纹理与 Sprite 资源。
    /// </summary>
    private void DestroyTextureResources()
    {
        if (_overlaySprite != null)
        {
            UnityEngine.Object.Destroy(_overlaySprite);
            _overlaySprite = null;
        }

        if (_overlayTexture != null)
        {
            UnityEngine.Object.Destroy(_overlayTexture);
            _overlayTexture = null;

        _legalMaskBuffer = null;
        _pixelBuffer = null;
        _bufferWidth = 0;
        _bufferHeight = 0;
        }
    }
}
