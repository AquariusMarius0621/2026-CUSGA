using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// TowerShopCard 閻犳劗鍠曢惌妤呭箮?UI 濞戞挸锕﹀▓鎴炵▔閳ь剙顕ｉ悩璇插姤缂傚啯褰冨畷閬嶆晬鐏炶棄缍侀柟瀛樺姉濠€鈥愁潰閿濆懎璁查柣鎰嚀閸ゎ噣濡存担绋胯闁归攱鐗楃€氬潡鎯冮崟顐ょ处闂侇偆濮撮崣鍡涘矗閿濆啠鍋?///
/// 闁革负鍔岀紞瀣礈瀹ュ牏绠归柣妤€鐗嗛、娆撴⒓閹绘帒鏂ч柛銊ヮ儔閸ｇ兘鏁嶅畝鈧敮铏光偓瑙勬构缁楀矂鍨惧鍐处闂侇偆濮烽柈瀵哥磼閻旀祴鍋撳┑鍫熺暠濞戞捁顔婂锔界閹烘垵寮抽柛娆欑到濮樸劑寮伴鐐插姤缂傚啯褰冨畷閬嶆晬?/// 闁圭鍋撳ù鐘劥缁绘牕顕ｉ悩鎻掑耿闁煎嘲鍟块惃顖滄啺娴ｇ懓濮悺鎺戝帠鐞氳京绮斿澶嬪閻㈩垱鐡曢崵婊堟倿閸撲焦鐣遍柟鍨С缂嶆梹锛愰崟顒佸焸闁?/// 1. 闁绘劗鎳撻崵顔锯偓鐟板枦缁辨繄鎮伴妸褋浠涢柍銉︾矋閸ㄦ粓鎮抽弶鎸庤含闁诡垱濞婇崕瀵哥磾閼煎墎绠圭紒澶婄Т椤㈡瑩鍨惧┑鍕ㄥ亾?/// 2. 闁烩晛鐡ㄧ敮鎾箯閺嵮呮殜闁挎稑鐭侀妴鍐矆鐞涒檧鍋撳鍕亯闁绘粍婢樺﹢顏嗕焊鏉堫偒娲ｉ柟璺猴龚缁绘牜绮斿鍜佹晩闁归攱鐗曢崺宀勫捶閺夋寧绂堝☉鎾筹攻閺備焦绋夌€ｎ亜绠甸柍銉︾缚閳?///
/// 濞戞挸锕ｇ粩瀛樼▔椤忓棗顣奸柡鍫墮瑜把呮啺閸℃瑦纾板ù婊冩鐎氬骞忛崐鐔虹唴鐎垫澘瀚哥槐婵嗏柦閳╁啯绠掗悷鏇炴濞插﹪鎮欓悷鏉挎瘖闂侇偄顦、娆戞崉椤栨氨绐為柨?/// 闁绘壕鏅涢宥嗙▔閳ь剟寮敂鐣屾⒕闁哄牆顦崳顖滄兜椤旇棄鐝涢悹褔鏀卞鐢告晬鐏炶姤鐨戝ù鍏间亢椤曘倖绂掗妷銈堢闁轰焦娼欑槐鍫曞础閳╁啯笑闁秆冪箳濞堟垿濡存担鍦⒕闁哄牆顦鑺ユ償閺冩挾绀?
/// 閺夆晜鐟ょ弧鍐及椤栨氨绉奸柛鎾崇Х閻︻垶鎮抽埡鈧紞瀣殽瀹€鍕闁哄牃鍋撻悗纭咁潐濡叉鏌呴悩鍐茬亣闁搞儳澧楅崕婊堟儍閸曨厼浠☉鏂款儎缁旀挳濡?///
/// 閺夆晜鐟ょ粩瀛樻姜椤旂厧瀚夐悶娑栧劙缁ㄢ剝绋夐埀顒備沪閸屾せ鍋撳鍐х俺闁告柣鍔嶉弲銉╁灳濠靛﹣鎹嶉悹鎰剁秶缁?
/// - 闁诡噮鍓欐禒鐘诲籍鐠哄搫骞㈤柣妤€娲ｇ槐鐗堟姜鐠囪弓绨抽柛娑氬帶閹盯寮ㄩ幆褋浜?
/// - 闂傚牏鍋炵€氬骞忛悾灞叫﹂柟顑挎缁楀懏绌卞┑鍥х槷閺夌偛顭烽崳娲嚍閸屾凹娈ч柟?/// - 闁活亞鍠愰婊堝箯閺嶎剚宕抽柡澶堝劜濡炲倿宕氶崶褎绀€闁哄嫬娴烽垾姗€鎯冮崟銊㈠亾濠婂棭娼堕柟鑸垫崄閹癸綁鍨惧┑鍡楀唨濡?///
/// 閻庣懓鍟╃划娑㈡倿閺堢數鐟濋悹鎰枙閻鏁?/// - 闁革附婢樺ù姗€鎷冮悾灞戒化闁哄嫷鍨伴幆渚€宕ラ崼鐔恒€?
/// - 闁汇垻鏁婚崳鐑樺緞閻斿墎鐟濆?/// - 闁活亞鍠庨悿鍕箙閺傛鍤犻悹鐑囩磿閺佹捇骞?/// - 閻犱警鍨扮欢鐐哄礌妤﹀灝鍘村☉鎾崇Х閸忔﹢寮?///
/// 閺夆晜鐟ょ花娲礂閵娾晛鍘寸紓浣堝懐鏁惧ù婧垮€楃划?TowerDefenseGame 缂備胶鍠嶇粩瀵告啑娴ｇ鏋€闁?/// 閺夆晜鐟﹂悧閬嶆嚄閹存帞绠介柟?UI 閺夊牊鎸搁崣鍡欎沪閸屾碍瀚查柣婧炬櫆绾墎鎲撮崟顐㈢仧閻忕偛鍊风粻锝夋⒒鐎靛憡鐣遍弶鍫濇贡閺咁偄銆掗崨顔界彴闁?/// </summary>
public class TowerShopCard : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IInitializePotentialDragHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Card Identity")]

    /// <summary>
    /// 閺夆晜鐟ョ槐鍫曟焾閵娧嗩唹闁告せ鈧剚鍤犻幖瀛樻⒒濞堟垶绻呴弮鍌濐潶闁搞劌顑冮埀?    ///
    /// 閺夆晜鐟ょ粩瀛樻姜椤旇姤鏆悗鐟拌嫰閹鏁嶇仦钘夊耿闁绘娲╅棅鈺傜閽樺绠戝銈堝吹閺?Inspector 闁哄嫭鍎崇槐锟犳煀瀹ュ洨鏋傞柨?    /// 濞戞挸绉撮崯鈧柛蹇庢祰椤斿繘鎳樺顓熸嫳闁哄秷顫夊畵浣衡偓鐢殿攰閽栧嫰宕ュ鍛川闁稿鏀辩敮褰掑棘椤撴壕鍋?    ///
    /// 閺夆晜鐟﹂悧閬嶅磻濮樿鲸鐣卞┑鍌濇椤︹晠寮伴銈囩獥
    /// - 闁衡偓閻熸壆婀寸紒鐙欏啯瀚查柡鈧悷閭﹀殸閻犵偐鈧櫕鍊抽柡鍐啇缁辨繃绋夊鍕獥闁硅泛锕崕瀵哥磾閹绘帒骞㈤棅顒夊亗閸炪倖绋夐埀顒傛導闁垮鏆☉鏂剧┒閳?    /// - 闁革妇鍎ゅ▍娆戞啑閸涙潙甯抽梺鎸庣懆椤曘倖瀵煎顓熺函闁哄啠鏅滃В姘舵鐠囇呯闁兼澘濂旂粭澶愬及椤栨繍娼堕梻鍛姇缁憋繝宕堕悙琛″亾閳ь剟鏌呴弰蹇曞竼闁硅　鏅濆ú濠囧Υ?    /// </summary>
    [SerializeField] private TowerType towerType = TowerType.None;

    /// <summary>
    /// 濮捬呭У閻栵綁骞冮鈧禒鐘诲捶閵娿儱骞㈤柣妤€娲ｇ粭鍌炲籍鐠佸湱绀夐幖瀛樻礋閸庢挳鎮╅懜纰樺亾娴ｅ湱鍩夐柡鍕⒔閵囨岸鎯冮崟顒€绲圭紒鈧ウ娆惧殧闁?    ///
    /// 閺夆晜鐟﹀顖炲箵閹邦喓浠涘☉鎾崇Т閻ɑ绂嶆惔銏㈠闊洤鍟抽～澶愬礆濞嗘劖鎷遍棅顒夊亾缁?
    /// 濞达絽妫楅悾鐘绘嚄閼恒儲鈻旈柦浣诡殜濡鹃攱鎷呮惔锝庡剳濞戞挴鍋撴繛鍠°倗妲搁柣婧炬櫆濡炲倿鎯冮崟顓熷€為悷娆欑秮濡剙危濞戞牑鍋?    /// </summary>
    [SerializeField] private string hoverHint = "Drag the card to preview exact legal areas. Your first structure starts in the starter zone.";

    [Header("Drag Feedback")]

    /// <summary>
    /// 闁归攱鐗楃€氭寧娼婚崶鈹炬煠濞戞搩鍘煎畷閬嶆偋閸ャ劍鎷卞ù锝嗘尵濞堟垿鏌呰箛鏃€顫栭幖杈捐礋閳?    ///
    /// 闁伙絻鍎辨禍鏇㈡⒔瀹ュ嫮绉甸梺顐㈢箲濡叉垶鎯旈敂鑲╃闁告瑯鍨禍鎺旀媼閳哄啫璐熼悗纭呭煐閸斿懘鎯岄妷銉ョ厒闁炽儲绮屽畷閬嶆偋閸パ冨殥缂備礁绻楅～锕傚箮閹捐宕抽柡澶堝劙缁繝鍨惧┑鍕ㄥ亾?    /// </summary>
    [SerializeField] private float draggingAlpha = 0.82f;

    /// <summary>
    /// 闁归攱鐗楃€氭寧娼婚崶鈹炬煠濞戞搩鍘煎畷閬嶆偋閸モ晜鐣辩紓鍌楁櫆閺備線宕愬鍥ц姵闁?    ///
    /// 閺夌偠顕ф禍鏇犵磽閳哄倹鏉归柡鍫濐槸婵亝绂嶆惔鈥崇厬闂侇偆濮崇粩瀵哥矓瀹ヤ讲鍋撳鍕樆闂佺瓔鍠涢～锕傚箵閹邦垱宕抽柍銉︾箘濞堟垹鎲撮敂钘夊Τ闁告瑥绉归々顓㈠Υ?    /// </summary>
    [SerializeField] private float draggingScaleMultiplier = 0.98f;

    [Header("Idle Motion")]

    /// <summary>
    /// 濮捬呭У閻栵綁骞冮鈧禒鐘诲籍鐠哄搫骞㈤柣妤€娲﹂弳锝嗘媴閹捐埖鐣遍弶鐐额嚙娴滄洟寮ㄩ幆褋浜ｉ梺鎻掔箞閳?    ///
    /// 閺夆晜鐟ら柌婊堟煂韫囧海鐟濋悗瑙勭矎缁诲啯寰勮缁辨繈宕ラ敃鈧崹顖涘濮樻剚鍞ㄩ柛妤嬬磿婢ф牠宕撹箛鎾磋含闁硅埖鐗曟慨鈺呮晬?    /// 闁烩晩鍠楅悥锝夊及椤栨粎鑸堕柣婧炬櫅椤斿秵绋夐埀顒傜矓瀹ヤ讲鍋撳鍡欑鐎殿喚濮村畷鍗灻洪懡銈嗙祷闁靛棔绀佽ぐ鍙夌閵堝嫮闉嶉柍銉︾箘濞堟垿骞囬悢娲绘綍闁?    /// </summary>
    [SerializeField] private float hoverScaleMultiplier = 1.035f;

    /// <summary>
    /// 闁告绱曟晶鏍箖椤掆偓娴犵娀宕ㄩ悡搴㈠劎闁汇劌瀚伴。鍫曟偝閸ャ儮鍋?    ///
    /// 闁轰焦婢橀埀顒冨缁夌儤顨囧鍫㈢闁告稓鍘ч幆娑氭惥婵犲倹褰ラ柨?    /// 鐟滅増鎸告晶鐘崇┍濠靛洤鐦柛锔哄妺缁斿瓨绋夐鍛Х閺夊牆鍟穱顖炲椽瀹€鈧▓鎴︽嚍閸屾凹娈у☉鎾愁煭缁辨繈鏌嗛崹顔煎赋闁哥姍鍐惧晭濠㈡儼妗ㄧ€靛矂濡?    /// </summary>
    [SerializeField] private float hoverPulseSpeed = 4.2f;

    /// <summary>
    /// 闁告绱曟晶鏍箖椤掆偓娴犵娀宕ㄩ悡搴㈠劎闁汇劌瀚亸鐔肩嵁閸涱偀鍋?    ///
    /// 閺夆晜鐟╅崳鐑芥偨閵娿儳鍙戦悘蹇撶箳濞堟垿鐛崨顓烆唺闁挎稑鏈Σ鍛婄▔鏉炴壆鍟婂ǎ鍥ㄧ箖鐎垫棃鍨惧鍛勘闁煎嘲顕▓鎴澝洪弰蹇曗攬闁规壆鍟块埀顒佺箰缁?
    /// 闁兼澘濂旂粭澶愬及椤栨繍鍞?UI 闁活亜顑堥幑锝夊级閵夈儱鍓肩€点倕顦悳顖氼嚕绾懐鍎查柟绋款樀閹告娊濡?    /// </summary>
    [SerializeField] private float hoverPulseAmplitude = 0.02f;

    /// <summary>
    /// 閻?CanvasGroup 闁汇劌瀚槐锔锯偓娑櫭槐鈺呮偨閵婏絺鍋?    /// </summary>
    private CanvasGroup _canvasGroup;

    /// <summary>
    /// 闁告绱曟晶鏍礆濠靛棭娼楃紓鍌楁櫆閺備線鏁嶅畝鈧弫銈嗙鎼淬垹鐝涢柟椋庢櫕缁劑寮堕悢绋跨仐闁诡噮鍓欐禒鐘电磼閹惧瓨灏嗛柛姘娴狀喗寰勫鍐ｅ亾?    /// </summary>
    private Vector3 _originalScale;

    /// <summary>
    /// 鐟滅増鎸告晶鐘诲及椤栨碍鍎婂璺哄缁剟寮垫径瀣珡闁归攱鐗楃€氭寧绋夐婧惧亾?    /// </summary>
    private bool _isDragging;

    /// <summary>
    /// 记录“Unity 已经进入拖拽手势，但我们还没真正启动放置拖拽链”的过渡状态。
    ///
    /// 这次修复的核心就是把“Unity 开始认定是拖拽”和“我们正式创建放置预览塔”
    /// 拆成两个阶段：
    /// 1. `OnBeginDrag` 只记录候选状态
    /// 2. 第一次 `OnDrag` 到来时，再用最新鼠标位置正式启动放置拖拽
    ///
    /// 这样可以避免第一次只是轻微点按/抖动时，就在地图中央提前实例化一个不跟手的预览塔。
    /// </summary>
    private bool _isAwaitingPlacementDragStart;

    /// <summary>
    /// 濮捬呭У閻栵綀銇愰幘鍐差枀闁哄嫷鍨伴幆渚€骞冮鈧禒鐘诲捶閵娿劎绠圭€殿喚濮村畷杈ㄧ▔婵炲簱鍋?    ///
    /// 閺夆晜鐟ら柌婊堝冀閸ヮ亶鍞堕悹浣叉櫆閸ㄦ粍绂掗鈧ぐ鍙夌閵夈儲韬?Update 闂佹彃鑻ぐ褏鈧潧婀卞﹢鈥愁潰閿濆牜娼堕柛蹇氭珪閺佺偤鎯冮崟顐㈠耿闁绘娲﹂幐閬嶅绩閹规劒姘﹂梺鎻掔箰閹崇娀宕ョ粙鍨楅柡浣哥墑閳?    /// </summary>
    private bool _isPointerOver;

    /// <summary>
    /// 闁告帗绻傞～鎰板礌閺嵮冨耿闁绘娲ㄥ▓鎴﹀矗瀹ュ娲〒姘箚缁傚棝鏁嶇仦鍊熷珯閻忓繋绮欓崳娲嚊椤忓嫬袟閻炴稏鍎电紞鍫熺箙閺冨倽顫﹂柛銊ヮ儍閳?    /// </summary>
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        _originalScale = transform.localScale;
    }

    /// <summary>
    /// 婵絽绻愰幎姘跺即鐎涙ɑ鐓€闁告绱曟晶鏍儍閸曨喕姘﹂梺鎻掔箰閹崇娀宕ョ粙鍨楅柣銏＄湽閳?    ///
    /// 閺夆晜鐟╅崳鐑藉极閸涱喖澹堝☉鎾崇Ф閺?Animator闁?    /// 闁哄嫷鍨板ú婊勭▔閸濆嫮绉奸柛鎾崇Т瑜把囧及椤栨艾鏂ч柛銊ヮ儐濠€鈩冪▔閳ь剙顕ｉ悩鑼彂閺夌偠宕靛▓?UI 闁告绱曟晶鏍嚍閸屾凹娈ч柨?    /// 闁活潿鍔嬮崬顒勬儘娴ｇ儤绾柟鎭掑劜鐢爼宕氶懜鍨函閻庣顫夊Σ妤冩嫚濮瑰洠鍋撴担瑙勬毉闁告粌鏈弳鈧悗娑宠礋閳?    /// </summary>
    private void Update()
    {
        if (_isDragging)
        {
            return;
        }

        if (!_isPointerOver)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _originalScale, Time.unscaledDeltaTime * 14f);
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * hoverPulseSpeed) * hoverPulseAmplitude;
        Vector3 targetScale = _originalScale * hoverScaleMultiplier * pulse;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 12f);
    }

    /// <summary>
    /// 鐟滅増鎹囩槐鍫曞冀閸ャ劌鐦诲☉鎾愁儎缁茬偓娼诲Ο鑽ゆ⒕闁活亞鍠愰婊勬交濞戞ê寮抽柟閿嬬墬鐎氬潡姊奸崼婵冨亾閻撳骸顤呴柨娑樺鐎靛矂宕濋妸銉ュ綘闂傚偆鍙冪划顖滄媼閵堝棗鐝涢柟铚傜矙濡插洭宕愮粭琛″亾?    /// </summary>
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // 这里明确保留 Unity 自带的拖拽阈值。
        //
        // 之前把阈值强行关掉后，鼠标一次很轻的点击抖动就可能被判成“开始拖拽”，
        // 于是玩家第一次只是想点一下部署卡，也会提前生成预览塔，
        // 看起来就像“卡片一点击，地图中间先冒出一个不能动的放置示意画面”。
        //
        // 保留阈值后的交互边界会更符合直觉：
        // - 轻点：走 OnPointerClick，做“选中该塔型”
        // - 真正拖动：跨过阈值后才进入 OnBeginDrag，开始跟手拖拽
        //
        // 这样可以把“点击选择”和“拖拽放置”重新分开，避免第一次点击就误触发拖拽链。
        eventData.useDragThreshold = true;
    }

    /// <summary>
    /// 闁绘劗鎳撻崵顕€宕￠敍鍕暬闁哄啳顔愮槐婵嬪礆閸ャ劌搴婄憸鐗堟尭婢х娀鏌呮径澶庡幀闁汇劌瀚、娆戠尵鐠囪尙鈧兘濡?    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging || TowerDefenseGame.Instance == null || !HasConfiguredTowerType())
        {
            return;
        }

        switch (towerType)
        {
            case TowerType.Relay:
                TowerDefenseGame.Instance.SelectRelayTower();
                break;
            case TowerType.Defense:
                TowerDefenseGame.Instance.SelectDefenseTower();
                break;
        }
    }

    /// <summary>
    /// 濮捬呭У閻栵絾娼诲☉妯哄汲闁告绱曟晶鏍籍鐠佸湱绀夌€殿喒鍋撳┑顔碱儐閹搁亶寮ㄩ幑鎰唉鐎甸偊鍠栭幊鐘诲触缁嬪灝鍐€濡絽鐗勯埀?    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;

        if (_isDragging || TowerDefenseGame.Instance == null || !HasConfiguredTowerType())
        {
            return;
        }

        // 闁诡噮鍓欐禒鐘诲础閿涘嫬顣婚梺顐ｈ壘閻栬埖瀵煎顓燂紞濞存粌娴峰﹢鈥愁潰閿濆懐纾诲┑顔碱儐鐎氬骞忛弬銈囩
        // 闁圭鍋撳ù鐘劥缁绘牠鏌屽畝鍕┾偓搴ㄥ箥鐎ｎ収鍤炴慨鐟板€风粩鏉戔枎闄囬々顐︽儎閺嵮呮勾濡澘瀚崕褰掓晬瀹€鍐ㄥ幋闁硅泛锕ュ〒鍫曟煂瀹ュ洦鐣遍梺鎻掔Т缂傛捇骞嬮幇顓熸嫳閻忓繋绮欓崳娲礈瀹ュ泦鈺呮晬?        // 闁告垵绻愰惃顖炴偝閳轰緡鍟€闁革负鍔夐埀顒佺矊閸ㄤ即骞庨幘瑙勫闯闁告绱曟晶鏍焽閿濆嫮顏卞☉鎾愁儌閳ь剚绻冮崝鍛村矗濡も偓閸╁矂鎯冮崟顐㈠耿濡炪倛锟ラ埀?        TowerDefenseGame.Instance.PrewarmPlacementAreaOverlay(towerType);
        TowerDefenseGame.Instance.SetStatusMessage(hoverHint);
    }

    /// <summary>
    /// 濮捬呭У閻栵絿绮嬬拠鑼；闁告绱曟晶鏍籍鐠佸湱绀夌紓浣规尰濞碱偊骞冮鈧禒鐘绘偐閼哥鍋撴担姝屽珯妤犵偠娅曠划锕傚炊閻愭彃鐓傞柛鎺撶箓椤劗浜搁崫鍕靛殶闁?    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
    }

    /// <summary>
    /// 鐟滅増鎸剧敮铏光偓纭呮硾缁辨垶鎱ㄧ€ｎ偄鐝涢柟宄扳偓鐔虹鐎殿喚濮村畷閬嶅籍鐠佸湱绀夐柛姘灦閳ь剛绮敮鍫曟偨鐎圭媭鍤為弶鈺傜☉閸欏棝鏌堥妸褑顔夐柟閿嬬墬鐎氬潡鎮╅懜纰樺亾娴ｇ鍋?    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || TowerDefenseGame.Instance == null || !HasConfiguredTowerType())
        {
            return;
        }

        // 不在这里立刻启动真正的放置拖拽。
        //
        // `OnBeginDrag` 只说明 Unity 认定这次输入已经跨过拖拽阈值，
        // 但这时鼠标位置、UI 射线状态和玩家真实意图都还处在一个“刚开始切换”的边缘帧。
        // 之前在这里马上调用 `BeginPlacementDrag()`，就会出现：
        // - 第一次点卡片时预览塔先被创建
        // - 如果后续没有稳定进入持续拖拽，预览塔就停在默认位置不动
        //
        // 所以这里先只记一个“等待真正开始放置拖拽”的标志，
        // 把正式启动延后到第一次 `OnDrag`。
        _isAwaitingPlacementDragStart = true;
    }

    /// <summary>
    /// 闁归攱鐗楃€氭寧娼婚崶鈹炬煠濞戞搩鍙忕槐婵嬪箰娴ｈ櫣鏁鹃柟璺猴工缂嶅宕滃澶岀倞闁哄秴娲ｇ紞鍛磾椤斿吋鍊辨慨婵勫劤缁即骞€缂佹ê浠橀柕?    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (TowerDefenseGame.Instance == null)
        {
            return;
        }

        if (!_isDragging)
        {
            if (!_isAwaitingPlacementDragStart || eventData.button != PointerEventData.InputButton.Left || !HasConfiguredTowerType())
            {
                return;
            }

            // 直到真正收到第一帧 Drag，我们才正式创建预览塔并切换卡片视觉。
            //
            // 这样第一次真实拖卡时仍然能立即跟手，
            // 但第一次只是点击或轻微误触时，不会再在地图中央留下一个“冻结”的预览塔。
            if (!TowerDefenseGame.Instance.BeginPlacementDrag(towerType, eventData.position))
            {
                _isAwaitingPlacementDragStart = false;
                return;
            }

            _isDragging = true;
            _isAwaitingPlacementDragStart = false;
            _canvasGroup.alpha = draggingAlpha;
            _canvasGroup.blocksRaycasts = false;
            transform.localScale = _originalScale * draggingScaleMultiplier;
        }

        TowerDefenseGame.Instance.UpdatePlacementDrag(eventData.position);
    }

    /// <summary>
    /// 鐟滅増鎸剧敮铏光偓纭呭煐濠㈡顕ｉ埀顒佄楅悩宕囧灱闁哄啳顔愮槐婵堢磼閹惧瓨灏嗛柡鍫墯椤愬ジ鏌堥妸褑顔夐柟閿嬬墬鐎氬潡濡?    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        _isAwaitingPlacementDragStart = false;

        if (!_isDragging)
        {
            RestoreVisualState();
            return;
        }

        _isDragging = false;
        RestoreVisualState();

        if (TowerDefenseGame.Instance == null)
        {
            return;
        }

        bool releasedOverUserInterface = IsReleasedOverUserInterface(eventData);
        TowerDefenseGame.Instance.EndPlacementDrag(eventData.position, releasedOverUserInterface);
    }

    /// <summary>
    /// 闁告帇鍊栭弻鍥ㄦ交濞嗘劦鍋ч柟閿嬬墬鐎氬潡鏌屾繝鍐╂澒闁哄啳顔愮槐婵囄楅悩宕囧灱鐟滅増鎸告晶鐘典焊閸曨厼娈犻柡鍕靛灠閹線鎯囬悢鐑樼暠闁告ê顑呭﹢顏堝蓟閹邦亪鍤?UI 闁稿繐鍟扮粈灞剧▔婵炲簱鍋?    ///
    /// 閺夆晜鐟╅崳鐑藉礆缂佹ê澹堥柛娆樹簽濠€?`pointerCurrentRaycast`闁挎稑濂旂粭澶愬礃瀹ュ懎妫橀柤?`pointerEnter` 闁?`hovered` 闁汇劌瀚濠氬矗閼碱剙笑闁诡兛闄嶉埀?    /// 闁告鍠庡ú婊堝及椤栨稑鐝涢柟鐤Г濞奸潧鈹冮幇顓熸嫳闁哄鍎卞銊╁及椤栨瑧顏辩€?UI 闁告绱曟晶鏍晬鐏炵偓锛嬮柣妯垮煐閳ь兛绀佺欢銏⑩偓纭咁潐濡叉娼诲Ο鑽ゆ殭闁伙絾鐟у鍐灳濠娾偓缁狅綁宕滃鍛闁革负鍔嶇€垫粓鏌﹂鑽ょ憪闁炽儲绻勫▓鎴炵┍閳╁啩绱栭柨?    /// 閻庝絻澹堥崵褔鎮抽埡渚囧晙闁哄嫬瀛╁Σ鎴﹀箮婵犲偆鏁婇柟閿嬬墪閸╁本绂嶉崱妤佸嬀闁搞儵绠栭崳鐑芥晬瀹€鍕珵闁衡偓閻愵剚顦ч柛妤冪節缁盯鎮為幆閭︽蕉閻犲浂鍨伴崹鑺ョ▔鐞涒檧鍋撳鍫濇珵闁衡偓閹勮含 UI 濞戞挸艌閳ь剚绺块埀?    ///
    /// 闁绘粍婢樺﹢顏堝箣閹存粍绮﹂柛娆樹簽濞村绌遍檱缁绘牗绋夐埀顒傛暜瑜旂槐鍫曞冀閸ワ妇鐟撻柡鍫氬亾闁哄倹澹嗗▓鎴犱焊閸曨厼娈犵紓浣规尰閻忓鏁?    /// - 濠碘€冲€归悘澶庛亹閹惧啿顤呴悘蹇撳閸ゅ酣宕ㄩ幋鎺曞幀闁汇劌瀚Σ?Canvas 濞达絾鎸鹃柈鎾煂瀹€鈧▓鎴犫偓鐢殿攰閽栧嫰鏁嶇仦鑺ョ殤缂佺姵顨婇崳鎾绩閹勮含 UI 濞?    /// - 濠碘€冲€归悘澶庛亹閹惧啿顤呴悘蹇撳閸ゅ骸鈻介埄鍐╃畳闁告稒鍨濋懙?UI闁挎稑鑻銊╁礂娴ｇ瓔鍟呯紓浣堝懐鏁鹃悹褎婢樺﹢鎾炊閻愵剚鏉圭紓?    /// </summary>
    private static bool IsReleasedOverUserInterface(PointerEventData eventData)
    {
        if (eventData == null || EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = eventData.position
        };

        System.Collections.Generic.List<RaycastResult> raycastResults = new System.Collections.Generic.List<RaycastResult>(8);
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        GameObject ignoredDragObject = eventData.pointerDrag != null ? eventData.pointerDrag.gameObject : null;
        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject currentRaycastObject = raycastResults[i].gameObject;
            if (IsGameplayBlockingUserInterface(currentRaycastObject, ignoredDragObject))
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// 闁告帇鍊栭弻鍥亹閹惧啿顤呴柛娑欏灊閼垫垿鎯?UI 閻庣數顢婇挅鍕晬鐏炵偓笑闁告熬绠戦惈妯荤鎼存ǚ鍋撳鍐畨閻犲洢鍎靛Ο鍡楊潰閵忕姵鍕鹃柛銉у亾閺備焦绻呴弬琛″亾濠靛牊鐣卞ù婧垮€撶花浼村箳瑜屽▎銏ゅΥ?    ///
    /// 閺夆晜鐟╅崳鐑藉礆缂佹ê澹堝☉鎾崇У婵℃悂骞嶉埀顒勫嫉?Canvas 閻庢稒鍔楁晶鎸庢媴閹捐鍘寸紒鐘愁殙缁绘﹢宕㈡导娆戠
    /// 闁兼澘鏈Σ鎼佸矗椤忓浂鍚囬柨?    /// 1. 闁稿繑婀圭划顒勬焾閵娧嗩唹闁告銈呮闂?    /// 2. Unity UI 闁?Selectable 濞达絾鎸鹃柈鎾晬閸︽敆tton / Toggle / Slider 缂佹稑顧€缁?
    ///
    /// 閺夆晜鐟﹂悧閬嶅捶閺夋寧绂堝☉鎾筹功濞堟垹鎲楅崨娣仒闁哄秴娲ㄩ椋庝焊閸欐鐟濆ù鍏艰壘閸熲偓閻犲浂鍨┑鈧柟閿嬬墬鐎氬潡鏌屾繝鍐╂澒闁?    /// </summary>
    private static bool IsGameplayBlockingUserInterface(GameObject currentRaycastObject, GameObject ignoredDragObject)
    {
        if (currentRaycastObject == null)
        {
            return false;
        }

        if (ignoredDragObject != null && currentRaycastObject.transform.IsChildOf(ignoredDragObject.transform))
        {
            return false;
        }

        if (currentRaycastObject.GetComponentInParent<TowerShopCard>() != null)
        {
            return true;
        }

        return currentRaycastObject.GetComponentInParent<Selectable>() != null;
    }

    /// <summary>
    /// 鐟滅増鎸搁顔炬寬闄囬～锔剧矉娴ｇ儤鏆忛柡鍐啇缁辨繂顕ｉ崫鍕厬闁诡厹鍨归ˇ鏌ュ础閿涘嫬顣诲鑸电墳椤洭濡?    /// </summary>
    private void OnDisable()
    {
        _isDragging = false;
        _isAwaitingPlacementDragStart = false;
        _isPointerOver = false;
        RestoreVisualState();
    }

    /// <summary>
    /// 闁哄嫬娴烽垾妯何涢埀顒勫蓟閵夈劎绠圭€殿喚濞€閸庡绱旈幓鎺戝耿闁哄嫷鍨伴幆浣割啅閼碱剛鐥呴柛?Inspector 闂佹彃鐭傞崢銈囩磾椤旂鍋?towerType闁?    ///
    /// 闁哄唲鍛暭闁哄牜鍏涚拹鐔哥閸℃绂堥柣顏冩缁ㄣ劑鏁嶇仦鑲╃獥闁革负鍔忕换鏍煂鐏炴垝绱ㄩ柛瀣敱閻楁挳骞戦纰卞殸閻犵偐鈧櫕鍊抽柟鎭掑妽閺屽洭鍨惧鍡欑闁哄嫷鍨拌ぐ鍌炴偨閸偅绨氶柛妞斻倗绠烽柡鍕靛灣閸嬫牗绻呴弬鍨耿闁炽儲绺块埀?    /// 閺夆晜鐟ч～鎺楀礃濞嗘劗銆婇柧蹇曟櫕閸斞囧储閻旈鈧兘寮甸悢鑽ょ崜鐎电増顨呴幓鈺呮晬鐏炶偐绋诲ù鍏肩婵″摜鈧數顢婇挅鍕触瀹ュ懎缍侀柟瀛樺姍濞堬綁鎸婅箛搴ｈ穿閻犙勭壄缁?
    /// 濞寸姰鍎遍幃妤呭矗椤忓浂娲ｅù锝囧С鐠愮喐绂嶉崱妯绘闁荤偛妫楅惇鎵棯瑜庨弫鍏肩▔閳ь剚绋夌€ｎ亝鍊抽悗娑欘殣缁辨繃绂嶉妶鍕瀺闂婎剦鍋傞崬銈囦焊閸楃偛璁查柤瀹犲Г閸婃捇骞冮崟顐㈢秮闁瑰搫顦埀?    ///
    /// 闁绘粍婢樺﹢顏堝箣閹存粍绮﹂柟璺猴工瀹曢亶鎮ч崶顏堢叐濞寸姾濮ら弫褰掑炊閻愭彃鐓傞柡鍕劤缁扁剝鎯旇箛鎾崇仚闁告牗鐗曢悺褍鈻撻悽绋挎闁?    /// - 闁革妇鍎ゅ▍娆撴煂瀹€鍐惧殙闂佹澘绉崇划鍫熺▕閸繍鏁婇柛銊ヮ儜缁辨繄浜告潏鈺傜函闁规亽鍎卞﹢?Inspector 闁哄嫬娴烽垾姗€鏌婂鍛仺闁?    /// - 濠碘€冲€归悘澶庣疀濡湱鍟婇梺鏉跨▌缁辨繄浜告潏銊︻潠缁绢収鍠楁慨銈夊礄閸濆嫭鍟為悹鈧敂鑲╃闁兼澘濂旂粭澶愬及椤栨粍鍩涚紓渚囧幘鐎典粙濡?    /// </summary>
    private bool HasConfiguredTowerType()
    {
        if (towerType != TowerType.None)
        {
            return true;
        }

        Debug.LogWarning("TowerShopCard is missing an explicit towerType assignment. Configure the card in the Inspector.", this);
        return false;
    }


    /// <summary>
    /// 闁诡厹鍨归ˇ鏌ュ箯閺嶃劌顏奸柛鎾崇Ф濞堟垿宕￠敍鍕暬閻熸瑥妫滈～搴ㄦ偐閼哥鍋撴担纰樺亾?    /// </summary>
    private void RestoreVisualState()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        transform.localScale = _originalScale;
    }
}
