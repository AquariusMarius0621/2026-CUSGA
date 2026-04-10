using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TowerDefenseHudState 鏄?HUD 姣忔鍒锋柊鏃剁湡姝ｉ渶瑕佺殑鏈€灏忕姸鎬佸揩鐓с€?///
/// 杩欓噷鏁呮剰涓嶆妸鏁翠釜 `TowerDefenseGame` 鐩存帴鏆撮湶缁?HUD锛?/// 鑰屾槸鍙紶琛ㄧ幇灞傜湡姝ｅ叧蹇冪殑鍑犻」缁撴灉锛?/// - 褰撳墠璧勬簮
/// - 褰撳墠鍩哄湴琛€閲?/// - 褰撳墠娉㈡
/// - 褰撳墠閫変腑浜嗕粈涔堝缓绛?/// - 褰撳墠鏄惁姝ｅ湪鎷栨嫿閮ㄧ讲
///
/// 杩欐牱鍋氱殑鏍稿績濂藉鏄細
/// HUD 鍙緷璧栤€滅粨鏋溾€濓紝涓嶄緷璧栨€绘帶鍐呴儴鐨勫叿浣撳疄鐜扮粏鑺傦紝
/// 浠ュ悗鍗充娇鐜╂硶浠ｇ爜缁х画鎷嗗垎锛孒UD 涔熸洿瀹规槗淇濇寔绋冲畾銆?/// </summary>
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
/// TowerDragPreviewState 鏄嫋鎷芥彁绀洪潰鏉块渶瑕佺湅鍒扮殑灞€閮ㄧ姸鎬併€?///
/// 瀹冨彧鍏虫敞涓変欢浜嬶細
/// - 鐜板湪鎷栫殑鏄摢绉嶅缓绛?/// - 褰撳墠榧犳爣钀界偣鏄惁鍚堟硶
/// - 濡傛灉涓嶅悎娉曪紝澶辫触鍘熷洜鏄粈涔?///
/// 杩欎唤鐘舵€佷箣鎵€浠ュ崟鐙媶鍑烘潵锛?/// 鏄洜涓烘嫋鎷芥彁绀哄埛鏂伴鐜囧緢楂橈紝娌″繀瑕佹瘡娆￠兘鏁村寘甯︿笂瀹屾暣 HUD 鐘舵€併€?/// </summary>
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

/// 涓嶄細涓€杩?Play 灏卞張琚剼鏈叏閮ㄦ憜鍥炲幓銆?/// </summary>
public sealed class TowerDefenseHudPresenter
{

    private TMP_Text _energyText;
    private TMP_Text _baseHealthText;
    private TMP_Text _waveText;
    private TMP_Text _selectionText;

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


    /// <summary>
    /// 鐢卞閮ㄦ妸宸茬粡鍦?Inspector 涓嫋濂界殑 HUD 寮曠敤鐩存帴娉ㄥ叆杩涙潵銆?    ///
    /// 杩欎竴姝ユ槸褰撳墠椤圭洰浠庘€滄寜鍚嶅瓧鏌ユ壘鍦烘櫙瀵硅薄鈥濋€愭杩佺Щ鍒扳€滄樉寮?Inspector 寮曠敤鈥濈殑绗竴姝ワ細
    /// - 濡傛灉鍦烘櫙浣滆€呭凡缁忔妸寮曠敤鎷栧ソ锛孒UDPresenter 灏辩洿鎺ヤ娇鐢ㄨ繖浜涚‘瀹氱殑瀵硅薄
    /// - 濡傛灉鏌愪簺寮曠敤鏆傛椂杩樻病鎷栵紝鍚庨潰 FindSceneReferences() 浠嶇劧浼氱户缁ˉ璧板悕瀛楁煡鎵惧厹搴?    ///
    /// 杩欐牱鍋氱殑濂藉鏄縼绉诲彲浠ュ垎闃舵杩涜锛?    /// 鎴戜滑涓嶉渶瑕佷竴娆℃€ф妸鎵€鏈夊満鏅兘閲嶅仛瀹岋紝
    /// 浣嗘柊琛ュソ鐨勫満鏅紩鐢ㄥ凡缁忚兘绔嬪埢鎽嗚劚鈥滄敼鍚嶅氨鐐糕€濈殑鑴嗗急妯″紡銆?    /// </summary>
    public void BindSceneReferences(
        TMP_Text energyText,
        TMP_Text baseHealthText,
        TMP_Text waveText,
        TMP_Text selectionText,
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
        _relayTowerButton = relayTowerButton;
        _defenseTowerButton = defenseTowerButton;
        _clearSelectionButton = clearSelectionButton;
        _gameOverPanel = gameOverPanel;
        _gameOverTitle = gameOverTitle;
        _gameOverHint = gameOverHint;
        _dragPreviewPanel = dragPreviewPanel;
        _dragPreviewLabel = dragPreviewLabel;

        EnsureDragPreviewDoesNotBlockRaycasts();

        _relayTowerButtonText = _relayTowerButton != null ? _relayTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _defenseTowerButtonText = _defenseTowerButton != null ? _defenseTowerButton.GetComponentInChildren<TMP_Text>(true) : null;
        _clearSelectionButtonText = _clearSelectionButton != null ? _clearSelectionButton.GetComponentInChildren<TMP_Text>(true) : null;
    }

    /// <summary>
    /// 鎸夊綋鍓嶉」鐩粛鍦ㄤ娇鐢ㄧ殑瀵硅薄鍚嶇害瀹氾紝鎶?HUD 鐩稿叧寮曠敤鎵鹃綈銆?    ///
    /// 杩欎竴灞備粛鐒朵娇鐢ㄥ悕瀛楁煡鎵撅紝
    /// 鏄洜涓洪」鐩暣浣撹繕娌″畬鍏ㄥ垏鍒?Inspector 鐩存嫋寮曠敤鐨勮閰嶆柟寮忋€?    /// 鎵€浠ワ細
    /// - 鍦烘櫙閲岃繖浜涘璞″悕浠嶇劧瑕佷繚鎸佺ǔ瀹?    /// - 濡傛灉浣犳敼鍚嶏紝灏辫鍚屾鏀硅剼鏈粯璁ゅ€?    /// </summary>
    public void FindSceneReferences()
    {
        // 杩欎竴杞紑濮嬶紝HUD 涓婚摼涓嶅啀涓诲姩鎸夊悕瀛楀洖鎹炲満鏅璞°€?        // 瀵瑰綋鍓?SampleScene 鑰岃█锛屾牳蹇?HUD 鑺傜偣閮藉簲褰撳凡缁忛€氳繃澶栭儴鏄惧紡缁戝畾浼犺繘鏉ャ€?        // 鎵€浠ヨ繖閲岀幇鍦ㄥ彧鍋氫袱浠朵簨锛?        // 1. 琛ラ綈鎸夐挳鍐呴儴鐨勫瓙鏂囨湰缂撳瓨
        // 2. 瀵圭己澶卞紩鐢ㄧ粰鍑烘槑纭憡璀︼紝甯姪鍦烘櫙瑁呴厤灏藉揩鏆撮湶闂
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

        EnsureDragPreviewDoesNotBlockRaycasts();

        WarnIfMissing(_energyText, "EnergyText");
        WarnIfMissing(_baseHealthText, "BaseHealthText");
        WarnIfMissing(_waveText, "WaveText");
        WarnIfMissing(_selectionText, "SelectionText");
        WarnIfMissing(_relayTowerButton, "RelayTowerButton");
        WarnIfMissing(_defenseTowerButton, "DefenseTowerButton");
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
    /// EventSystem 就会误以为“这次释放仍然发生在 UI 上”，
    /// 从而把一次本来合法的放塔当成取消操作。
    ///
    /// 这里同时把：
    /// - 面板背景 Graphic 的 RaycastTarget 关掉
    /// - 文本本身的 RaycastTarget 关掉
    /// - 整个面板挂一个 CanvasGroup 并关闭 blocksRaycasts
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
    /// 鏍规嵁濉旂洰褰曠粺涓€閰嶇疆涓ゅ紶閮ㄧ讲鍗＄殑闈欐€佹枃妗堛€?    ///
    /// 杩欓噷浠嶇劧淇濈暀灏戦噺鏍煎紡鎺у埗锛屽師鍥犳槸閮ㄧ讲鍗℃枃妗堟湰韬槸鐢辩帺娉曟暟鎹┍鍔ㄧ殑锛?    /// - 灞曠ず鍚嶄細鍙?    /// - 鎴愭湰浼氬彉
    /// - 鎵╁紶鏂规牸璇存槑浼氬彉
    ///
    /// 浣嗚繖閲屼笉浼氬啀鎺ョ鍗＄墖浣嶇疆銆佸簳鏉挎牱寮忓拰鏁翠釜鍙充晶鍖哄竷灞€锛?    /// 閭ｄ簺鍐呭鐜板湪搴旇涓昏鐢卞満鏅潵鎺у埗銆?    /// </summary>
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
    /// 鍒锋柊甯搁┗ HUD銆?    ///
    /// 杩欓噷鏇存柊鐨勬槸鈥滃€尖€濆拰鈥滅姸鎬佲€濓紝涓嶆槸鈥滅増寮忛鏋垛€濄€?    /// 鎵€浠ヤ綘鍦ㄥ満鏅噷璋冨ソ鐨勫竷灞€锛屼細琚繚鐣欎笅鏉ワ紱
    /// 鑴氭湰鍙礋璐ｆ妸褰撳墠娓告垙鐘舵€佸～杩涘搴旀枃鏈噷銆?    /// </summary>
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
    /// 鏇存柊搴曢儴鐘舵€佹潯娑堟伅銆?    ///
    /// 娉ㄦ剰杩欓噷涓嶆敼娑堟伅鍐呭鏈韩锛?    /// 鍥犱负鈥滀粈涔堟椂鍊欒浠€涔堚€濆睘浜庣帺娉曞眰鍐崇瓥锛?    /// HUD 鍙槸鎶婃€绘帶缁欏嚭鐨勭粨鏋滄樉绀哄嚭鏉ャ€?    /// </summary>
    public void SetStatusMessage(string message)
    {
    }

    /// <summary>
    /// 鎺у埗鎷栨嫿鎻愮ず闈㈡澘鏄鹃殣銆?    /// </summary>
    public void SetDragPreviewVisible(bool visible)
    {
        if (_dragPreviewPanel != null)
        {
            _dragPreviewPanel.SetActive(visible);
        }
    }

    /// <summary>
    /// 鏇存柊璺熼殢榧犳爣鐨勬嫋鎷芥彁绀洪潰鏉裤€?    ///
    /// 杩欓噷浠嶇劧淇濈暀鈥滆窡鐫€榧犳爣璧扳€濈殑琛屼负锛?    /// 鍥犱负瀹冩湰鏉ュ氨灞炰簬浜や簰鏈熷姩鎬佸弽棣堬紝
    /// 鑰屼笉鏄簲璇ョ敱鍦烘櫙鍥哄畾鎽嗘鐨勭晫闈€?    ///
    /// 鍚屾椂杩欓噷浼氭牴鎹綋鍓嶅鍨嬪拰鍚堟硶鎬х粨鏋滐紝
    /// 瀹炴椂鍒锋柊鎻愮ず鏂囨锛屽府鍔╃帺瀹剁悊瑙ｏ細
    /// - 褰撳墠鎷栫殑鏄彂鐢垫満杩樻槸鐐
    /// - 杩欐钀界偣涓轰粈涔堣兘鏀?/ 涓嶈兘鏀?    /// </summary>
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
    /// 鏄剧ず Game Over 闈㈡澘骞跺～鍏ユ枃妗堛€?    /// </summary>
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
    /// 鍗曠嫭鎺у埗 Game Over 闈㈡澘鏄鹃殣銆?    /// </summary>
    public void SetGameOverVisible(bool visible)
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(visible);
        }
    }

    /// <summary>
    /// 閰嶇疆鍗曞紶閮ㄧ讲鍗＄殑鏂囨銆?    ///
    /// 杩欓噷淇濈暀灏戦噺鏂囨湰鎺掔増鎺у埗锛?    /// 鍙槸涓轰簡纭繚澶氳鍗＄墖鏂囨鍦ㄥ綋鍓嶅崱鐗囬噷鑳界ǔ瀹氳娓呮銆?    /// 浣嗗畠涓嶄細鍐嶅幓鏀规寜閽綅缃€佺埗鐗╀綋甯冨眬鎴栨暣涓彸渚у尯鍩熺粨鏋勩€?    /// </summary>
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
    /// 缁勮鈥滃綋鍓嶉€変腑 / 褰撳墠鎷栨嫿鈥濈殑璇存槑鏂囨銆?    ///
    /// 杩欓儴鍒嗕粛鐒剁敱鑴氭湰鐢熸垚锛?    /// 鍥犱负瀹冩湰璐ㄤ笂灏辨槸褰撳墠鐘舵€佺殑鍔ㄦ€佹憳瑕侊紝鑰屼笉鏄浐瀹氳楗版€ф枃鏈€?    /// </summary>
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
    /// 鍦ㄢ€滃満鏅富瀵?HUD鈥濈殑妯″紡涓嬶紝鍙淮鎶ゆ寜閽殑鍙氦浜掔姸鎬併€?    ///
    /// 涔熷氨鏄锛?    /// - 鍦烘櫙璐熻矗杩欎簺鎸夐挳闀夸粈涔堟牱
    /// - 鑴氭湰鍙礋璐ｅ憡璇夊畠浠綋鍓嶈兘涓嶈兘鐐?    ///
    /// 濡傛灉浣犳兂淇敼涓嶅彲璐拱鏃剁殑棰滆壊銆侀€変腑鏃剁殑楂樹寒銆佹寜涓嬫椂鐨勮繃娓★紝
    /// 鐜板湪鏇存帹鑽愮洿鎺ュ幓 Button 鐨?Transition / ColorBlock 閲屾敼銆?    /// </summary>
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
    /// 缁勮椤堕儴璧勬簮鍗＄殑瀵屾枃鏈€?    ///
    /// 杩欓噷杩斿洖鐨勬槸鈥滃唴瀹规牸寮忊€濓紝涓嶆槸甯冨眬鏍煎紡锛?    /// 鍗＄墖鏀惧摢銆佸瓧鍧楀澶с€佸杈硅窛澶氬皯锛屽簲璇ヤ富瑕佺敱鍦烘櫙鎺у埗锛?    /// 浣嗘瘡寮犲崱鏂囧瓧閲屾爣绛惧拰鏁板€肩殑灞傜骇鍏崇郴锛屼粛鐒堕€傚悎鐢变唬鐮佺粺涓€鐢熸垚銆?    /// </summary>
    private static string BuildMetricText(string label, string value, string accentHex)
    {
        return
            $"<size=18><color=#8EA8BE>{label}</color></size>\n" +
            $"<size=56><color=#{accentHex}>{value}</color></size>";
    }
}
