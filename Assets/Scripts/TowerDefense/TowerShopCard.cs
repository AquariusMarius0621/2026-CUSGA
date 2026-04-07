using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// TowerShopCard 负责把 UI 上的一张部署卡，变成真正可点击、可拖拽的建造入口。
///
/// 在当前这版塔防原型里，玩家与“建造系统”的主交互入口就是部署卡，
/// 所以这张卡至少要满足两种非常自然的操作预期：
/// 1. 点击它，表示“我现在想部署这种塔”。
/// 2. 直接拖它，表示“我现在就要把这种塔拖到地图上放下去”。
///
/// 上一个版本只覆盖了拖拽路径，没有覆盖点击选塔路径，
/// 玩家一旦没有准确拖起来，就会误以为整张卡是坏的、没有反应，
/// 这也是当前试玩体验里最容易造成困惑的点之一。
///
/// 这一轮又补了一层“微动效”职责：
/// - 悬停时卡片会轻微呼吸放大
/// - 非拖拽状态下保持轻量节奏感
/// - 真正拖起来时切回明确的“被抓起”反馈
///
/// 它仍然不负责：
/// - 地图落点是否合法
/// - 电量够不够
/// - 真实塔对象生成
/// - 路径区能不能放
///
/// 这些全都继续交给 TowerDefenseGame 统一裁决。
/// 这样能保持 UI 输入层和玩法规则层之间的边界清晰。
/// </summary>
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
    /// 这张部署卡对应的塔类型。
    ///
    /// 这一轮改完后，卡片身份必须由 Inspector 显式配置，
    /// 不再允许脚本根据对象名偷偷推断。
    ///
    /// 这样做的好处是：
    /// - 改层级和改对象名时，不会把部署卡身份一起改乱。
    /// - 场景装配错误会更早暴露，而不是被隐式回退逻辑掩盖。
    /// </summary>
    [SerializeField] private TowerType towerType = TowerType.None;

    /// <summary>
    /// 鼠标悬停在卡片上时，底部状态栏显示的提示语。
    ///
    /// 这条提示不属于核心规则本身，
    /// 但它能显著降低第一次试玩时的理解门槛。
    /// </summary>
    [SerializeField] private string hoverHint = "Drag the card to preview exact legal areas. Your first structure starts in the starter zone.";

    [Header("Drag Feedback")]

    /// <summary>
    /// 拖拽过程中卡片本体的透明度。
    ///
    /// 略微降低透明度，可以让玩家感知到“卡片已经被抓起来了”。
    /// </summary>
    [SerializeField] private float draggingAlpha = 0.82f;

    /// <summary>
    /// 拖拽过程中卡片的缩放倍率。
    ///
    /// 轻微缩放有助于制造一种“按钮被提起”的触感反馈。
    /// </summary>
    [SerializeField] private float draggingScaleMultiplier = 0.98f;

    [Header("Idle Motion")]

    /// <summary>
    /// 鼠标悬停时卡片整体的轻微放大量。
    ///
    /// 这个量不宜过大，否则会让卡片像在抖动；
    /// 目标是给玩家一种“这张卡活着、可交互”的感觉。
    /// </summary>
    [SerializeField] private float hoverScaleMultiplier = 1.035f;

    /// <summary>
    /// 卡片悬停呼吸的频率。
    ///
    /// 数值越高，呼吸越快；
    /// 当前保持在一个比较温和的节奏上，避免喧宾夺主。
    /// </summary>
    [SerializeField] private float hoverPulseSpeed = 4.2f;

    /// <summary>
    /// 卡片悬停呼吸的振幅。
    ///
    /// 这里用很小的幅度，是为了保持“精致的活跃感”，
    /// 而不是让 UI 看起来像廉价弹跳按钮。
    /// </summary>
    [SerializeField] private float hoverPulseAmplitude = 0.02f;

    /// <summary>
    /// 对 CanvasGroup 的缓存引用。
    /// </summary>
    private CanvasGroup _canvasGroup;

    /// <summary>
    /// 卡片初始缩放，用于拖拽结束或悬停结束后恢复。
    /// </summary>
    private Vector3 _originalScale;

    /// <summary>
    /// 当前是否处于有效拖拽中。
    /// </summary>
    private bool _isDragging;

    /// <summary>
    /// 鼠标当前是否悬停在这张卡上。
    ///
    /// 这个标记让我们可以在 Update 里只对真正被关注的卡片播放轻量呼吸动效。
    /// </summary>
    private bool _isPointerOver;

    /// <summary>
    /// 初始化卡片的反馈依赖，并尽量自动补齐塔类型。
    /// </summary>
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
    /// 每帧更新卡片的轻量呼吸动画。
    ///
    /// 这里故意不用 Animator，
    /// 是因为当前只是原型期一张很轻的 UI 卡片节奏，
    /// 用代码直接控制更容易读、改和教学。
    /// </summary>
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
    /// 当鼠标按下但还没真正进入拖拽阈值前，主动关闭默认拖拽阈值。
    /// </summary>
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    /// <summary>
    /// 点击卡片时，切换当前选中的塔类型。
    /// </summary>
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
    /// 鼠标进入卡片时，开始播放轻微呼吸反馈。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;

        if (_isDragging || TowerDefenseGame.Instance == null || !HasConfiguredTowerType())
        {
            return;
        }

        // 悬停卡片通常会早于真正开始拖拽，
        // 所以这里顺手请求一次覆盖层预热，能把最重的重建成本尽量前移，
        // 减少玩家在“刚抓起卡片那一下”感受到的卡顿。
        TowerDefenseGame.Instance.PrewarmPlacementAreaOverlay(towerType);
        TowerDefenseGame.Instance.SetStatusMessage(hoverHint);
    }

    /// <summary>
    /// 鼠标离开卡片时，结束悬停状态并平滑回到初始尺寸。
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
    }

    /// <summary>
    /// 当玩家开始拖拽这张卡时，向总控申请进入部署拖拽状态。
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || TowerDefenseGame.Instance == null || !HasConfiguredTowerType())
        {
            return;
        }

        if (!TowerDefenseGame.Instance.BeginPlacementDrag(towerType, eventData.position))
        {
            return;
        }

        _isDragging = true;
        _canvasGroup.alpha = draggingAlpha;
        _canvasGroup.blocksRaycasts = false;
        transform.localScale = _originalScale * draggingScaleMultiplier;
    }

    /// <summary>
    /// 拖拽过程中，持续把当前鼠标位置同步给总控。
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || TowerDefenseGame.Instance == null)
        {
            return;
        }

        TowerDefenseGame.Instance.UpdatePlacementDrag(eventData.position);
    }

    /// <summary>
    /// 当玩家松开鼠标时，结束本次部署拖拽。
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        RestoreVisualState();

        if (TowerDefenseGame.Instance == null)
        {
            return;
        }

        bool releasedOverUserInterface = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(eventData.pointerId);
        TowerDefenseGame.Instance.EndPlacementDrag(eventData.position, releasedOverUserInterface);
    }

    /// <summary>
    /// 当对象被禁用时，强制恢复卡片外观。
    /// </summary>
    private void OnDisable()
    {
        _isDragging = false;
        _isPointerOver = false;
        RestoreVisualState();
    }

    /// <summary>
    /// 明确检查这张部署卡是否已经在 Inspector 里配置好 towerType。
    ///
    /// 旧版本为了图省事，会在这里偷偷根据对象名推断“这是发电机卡还是炮塔卡”。
    /// 这种写法虽然原型期跑得快，但会把对象名变成隐藏依赖，
    /// 以后只要你为了整理层级改一下名字，交互身份就可能悄悄变掉。
    ///
    /// 现在我们把卡片身份收回到显式序列化字段里：
    /// - 场景里该配什么塔型，就直接在 Inspector 明确配好。
    /// - 如果忘了配，就明确报出告警，而不是继续猜。
    /// </summary>
    private bool HasConfiguredTowerType()
    {
        if (towerType != TowerType.None)
        {
            return true;
        }

        Debug.LogWarning("TowerShopCard 缺少 towerType 显式配置。请在 Inspector 中为这张部署卡指定塔类型。", this);
        return false;
    }

    /// <summary>
    /// 恢复拖拽前的卡片视觉状态。
    /// </summary>
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
