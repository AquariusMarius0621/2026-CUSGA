using UnityEngine;

/// <summary>
/// BuildZone 用来定义“这张地图允许玩家放塔的大区域”。
///
/// 在旧版本里，我们是通过若干固定 BuildPad 来限制建造位置；
/// 现在改成自由放置后，系统需要一个新的“第一层过滤器”：
/// 先判断玩家拖拽落点是否位于关卡允许建造的大范围内，
/// 再继续判断它有没有压到敌人路径、其他塔或特殊阻挡区。
///
/// 你可以把 BuildZone 理解成一张“建造许可证边界框”：
/// - 在框里：有资格继续做更细的合法性校验
/// - 在框外：直接判定为不可建造
///
/// 这种做法的好处是职责非常清楚：
/// 1. BuildZone 只负责回答“这个点有没有落在允许建造的大区域里”。
/// 2. PlacementBlocker 负责回答“这个点是不是压到了禁建区域”。
/// 3. TowerDefenseGame 负责把这些规则组合起来，形成最终可部署判断。
///
/// 相比把所有坐标范围硬编码进总控脚本，单独放一个可视化对象更利于教学和迭代：
/// - 调整范围时不必改逻辑代码
/// - Scene 里更容易理解“为什么这里能建、那里不能建”
/// - 后续如果关卡越来越多，这种做法也更容易复用
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BuildZone : MonoBehaviour
{
    [Header("Gizmo")]

    /// <summary>
    /// 在 Scene 视图里绘制建造区轮廓时使用的颜色。
    ///
    /// 这纯粹是编辑期辅助信息，
    /// 目的是让你在调关卡时更容易看清当前允许建造的边界。
    /// </summary>
    [SerializeField] private Color gizmoColor = new Color(0.25f, 0.85f, 0.95f, 0.9f);

    /// <summary>
    /// 对建造区碰撞盒的缓存引用。
    ///
    /// 由于 BoxCollider2D 就是这张“许可区域”的真实数据来源，
    /// 所以脚本会把它缓存下来，避免反复 GetComponent。
    /// </summary>
    private BoxCollider2D _boxCollider;

    /// <summary>
    /// 当前建造区在世界空间中的包围盒。
    ///
    /// 对外暴露这个只读属性，
    /// 主要是为了以后如果你想做调试显示或编辑器工具时能方便取到范围数据。
    /// </summary>
    public Bounds WorldBounds => _boxCollider != null ? _boxCollider.bounds : new Bounds(transform.position, Vector3.zero);

    /// <summary>
    /// 在运行时初始化引用，并确保碰撞盒以 Trigger 模式工作。
    ///
    /// 这里使用 Trigger 是因为我们只想拿它做“区域判定”，
    /// 而不是让它真实参与物理碰撞和阻挡。
    /// </summary>
    private void Awake()
    {
        CacheReference();
        EnsureTriggerMode();
    }

    /// <summary>
    /// 当 Inspector 参数变化时，在编辑器里同步修正组件状态。
    ///
    /// 这样可以避免你不小心把 BoxCollider2D 设成非 Trigger，
    /// 然后在 Play 模式里才发现建造判定和预期不一致。
    /// </summary>
    private void OnValidate()
    {
        CacheReference();
        EnsureTriggerMode();
    }

    /// <summary>
    /// 判断某个世界坐标点是否位于允许建造的大区域内。
    ///
    /// 这里直接调用 BoxCollider2D 的 OverlapPoint，
    /// 因为它已经很好地封装了“点是否落在当前碰撞盒范围里”的逻辑。
    /// </summary>
    public bool ContainsPoint(Vector3 worldPosition)
    {
        CacheReference();
        return _boxCollider != null && _boxCollider.OverlapPoint(worldPosition);
    }

    /// <summary>
    /// 缓存 BoxCollider2D 引用。
    ///
    /// 这里采用“如果还没缓存过，再去取一次”的写法，
    /// 兼顾了稳健性和运行效率。
    /// </summary>
    private void CacheReference()
    {
        if (_boxCollider == null)
        {
            _boxCollider = GetComponent<BoxCollider2D>();
        }
    }

    /// <summary>
    /// 强制把建造区碰撞盒设为 Trigger。
    ///
    /// 这是一个很重要的小细节：
    /// BuildZone 应该是一个“逻辑区域”，而不是“物理墙体”。
    /// 如果让它参与真实碰撞，后面很容易引入不必要的副作用。
    /// </summary>
    private void EnsureTriggerMode()
    {
        if (_boxCollider != null)
        {
            _boxCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// 在 Scene 视图里绘制建造区线框，帮助你调试关卡边界。
    ///
    /// 对自由放置系统来说，
    /// Gizmo 能显著降低“明明不能放但我不知道为什么”的编辑期困惑。
    /// </summary>
    private void OnDrawGizmos()
    {
        CacheReference();
        if (_boxCollider == null)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(_boxCollider.bounds.center, _boxCollider.bounds.size);
    }
}