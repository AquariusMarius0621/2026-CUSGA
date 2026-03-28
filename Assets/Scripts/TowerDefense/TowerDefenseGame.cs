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

    [Header("Placement Preview")]
    [SerializeField] private Color validPreviewColor = new Color(0.26f, 0.95f, 0.78f, 0.72f);
    [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.32f, 0.38f, 0.72f);
    [SerializeField] private string placementRingResourcePath = "UI/placement-ring";

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
    private GameObject _placementPreviewInstance;
    private SpriteRenderer _placementPreviewSpriteRenderer;
    private SpriteRenderer _placementPreviewRingRenderer;

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

        SetStatusMessage("拖拽右侧部署卡到地图中的非路径区域进行放置。快捷键 1 / 2 可快速选塔。");
        RefreshHud();

        _hudPresenter.SetGameOverVisible(false);
        _hudPresenter.SetDragPreviewVisible(false);
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
            SetStatusMessage($"电量不足，当前只有 {_currentEnergy} 点。");
            return false;
        }

        GameObject prototype = GetPrototype(towerType);
        if (prototype == null)
        {
            SetStatusMessage("部署卡对应的塔原型缺失，请检查场景配置。");
            return false;
        }

        CancelPlacementDragInternal();

        _selectedTowerType = towerType;
        _dragTowerType = towerType;
        _isPlacementDragActive = true;

        EnsurePlacementPreviewInstance(towerType);
        _hudPresenter.SetDragPreviewVisible(true);
        RefreshHud();
        UpdatePlacementDrag(screenPosition);
        SetStatusMessage("拖到地图空地区域后松手即可部署。");
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
            SetStatusMessage("已取消本次部署。");
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
        SetStatusMessage($"怪物突破防线，基地损失了 {amount} 点耐久。");

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
            SetStatusMessage("已取消部署选择。");
        }
        else
        {
            SetStatusMessage($"当前选中：{GetTowerDisplayName(towerType)}。点击地图或直接拖拽部署卡进行放置。");
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
                displayName: "Relay Node",
                buildCost: relayTowerCost,
                placementRadius: relayPlacementRadius,
                cardRoleSummary: "Power Income / Support",
                accentColor: new Color(1f, 0.55f, 0.22f, 1f)),
            defenseDefinition: new TowerDefinition(
                towerType: TowerType.Defense,
                displayName: "Defense Turret",
                buildCost: defenseTowerCost,
                placementRadius: defensePlacementRadius,
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
        Time.timeScale = 0f;

        _hudPresenter?.ShowGameOver(
            title: "GAME OVER",
            hint: "基地已被突破。停止 Play 模式后，可以继续调整关卡与部署逻辑。");

        SetStatusMessage("基地耐久归零，行动失败。");
        RefreshHud();
    }

    /// <summary>
    /// 閻喐顒滈幍褑顢戞稉鈧▎鈥崇紦闁姰鈧?    ///
    /// 鏉╂瑩鍣烽弰顖涘閺堝缂撻柅鐘插弳閸欙絾娓剁紒鍫熺湽閸氬牏娈戦崷鐗堟煙閿?    /// - 閸︽澘娴樿箛顐︹偓鐔哄仯閸戝鍎寸純?    /// - 闁劎璁查崡鈩冨珛閹峰€熸儰閻?    /// - 閸忕厧顔愰弮?BuildPad 閻ㄥ嫮鍋ｉ崙璇茬紦闁?    ///
    /// 娑旂喎姘ㄩ弰顖濐嚛閿涘本妫ょ拋铏瑰负鐎硅埖妲搁幀搴濈疄閸欐垼鎹ｅ娲偓鐘侯嚞濮瑰倻娈戦敍?    /// 闁棄绻€妞ゅ鈧俺绻冩潻娆撳櫡缂佺喍绔撮崑姘崇カ濠ф劗绮ㄧ粻妤€鎷伴崥鍫熺《閹冨灲鐎规哎鈧?    /// </summary>
    private bool TryPlaceTowerAt(Vector3 worldPosition, TowerType towerType, BuildPad ownerPad = null)
    {
        if (_isGameOver || towerType == TowerType.None)
        {
            return false;
        }

        if (ownerPad != null && ownerPad.IsOccupied)
        {
            SetStatusMessage("这个旧塔位已经被占用了。");
            return false;
        }

        int cost = GetTowerCost(towerType);
        if (_currentEnergy < cost)
        {
            SetStatusMessage($"电量不足，当前只有 {_currentEnergy} 点。");
            return false;
        }

        GameObject prototype = GetPrototype(towerType);
        if (prototype == null)
        {
            SetStatusMessage("塔的原型对象没有准备好，请检查场景配置。");
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
        SetStatusMessage($"已部署 {GetTowerDisplayName(towerType)}，消耗 {cost} 点电量。");
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
            invalidReason = "当前关卡没有配置 BuildZone，暂时无法部署。";
            return false;
        }

        if (!_buildZone.ContainsPoint(worldPosition))
        {
            invalidReason = "超出了当前关卡允许建造的范围。";
            return false;
        }

        float placementRadius = GetPlacementRadius(towerType);
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(worldPosition, placementRadius);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];
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

            if (overlap.GetComponentInParent<DefenseTower>() != null || overlap.GetComponentInParent<RelayTower>() != null)
            {
                invalidReason = "这个位置离其他塔太近了，请稍微挪开一些。";
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
    /// 娴犲骸缍嬮崜宥夌炊閺嶅洤鐫嗛獮鏇炴綏閺嶅洩顓哥粻妤佸灛閸﹁桨鑵戦惃鍕瑯閻ｅ苯娼楅弽鍥モ偓?    /// </summary>
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
        DestroyPlacementPreview();

        GameObject prototype = GetPrototype(towerType);
        if (prototype == null)
        {
            return;
        }

        _placementPreviewInstance = Instantiate(prototype, Vector3.zero, Quaternion.identity, _placementPreviewRoot);
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
    /// <summary>
    /// 閿€姣佸綋鍓嶆嫋鎷介瑙堝璞°€?    ///
    /// 鍗曠嫭鎶芥垚鏂规硶涔嬪悗锛?    /// Begin / Cancel / GameOver 閮藉彲浠ュ鐢ㄥ悓涓€濂楁竻鐞嗘祦绋嬨€?    /// </summary>
    private void DestroyPlacementPreview()
    {
        if (_placementPreviewInstance != null)
        {
            Destroy(_placementPreviewInstance);
        }

        _placementPreviewInstance = null;
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

