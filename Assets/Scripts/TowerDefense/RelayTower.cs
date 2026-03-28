using UnityEngine;

/// <summary>
/// RelayTower 是一种不直接输出伤害的资源型建筑。
///
/// 它的定位更接近“经济塔”或“能源塔”：
/// - 不负责拦截敌人
/// - 不负责攻击目标
/// - 只负责按固定节奏为玩家补充可用电量
///
/// 这类单位在塔防里通常承担“成长”和“投资”角色。
/// 玩家如果愿意先花资源建造它，就能在后续波次里获得更高的资源回报，
/// 从而有机会铺出更大的防线。
///
/// 当前实现保持得非常轻量：
/// 只要游戏还没结束，它就会按照设定间隔周期性给总控增加资源。
/// 这样足以先验证“资源生产型塔”是否能给玩法带来有意义的策略分支。
/// </summary>
/// <remarks>
/// 需要 SpriteRenderer 来显示塔本体的颜色。
/// </remarks>
[RequireComponent(typeof(SpriteRenderer))]
public class RelayTower : MonoBehaviour
{
    [Header("Energy Output")]

    /// <summary>
    /// 每次脉冲产能时为玩家增加的电量。
    ///
    /// 这个值直接决定了继电器塔的经济价值，
    /// 也会影响玩家更倾向于前期投资还是即时防守。
    /// </summary>
    [SerializeField] private int energyPerPulse = 12;

    /// <summary>
    /// 两次产能脉冲之间的时间间隔，单位为秒。
    ///
    /// 数值越小，资源产出越频繁；数值越大，收益回收周期越长。
    /// </summary>
    [SerializeField] private float pulseInterval = 3f;

    [Header("Visuals")]

    /// <summary>
    /// 继电器塔在场景中显示的主体颜色。
    ///
    /// 使用偏亮的黄颜色，有助于把它与蓝色系攻击塔区分开，
    /// 让玩家一眼看出它是不同功能的建筑。
    /// </summary>
    [SerializeField] private Color towerColor = new Color(1f, 0.85f, 0.2f, 1f);

    /// <summary>
    /// 对本体 SpriteRenderer 的缓存引用。
    /// </summary>
    private SpriteRenderer _spriteRenderer;

    /// <summary>
    /// 资源产出计时器。
    ///
    /// 每帧累积经过的时间，达到 pulseInterval 后触发一次产能。
    /// </summary>
    private float _pulseTimer;

    /// <summary>
    /// 初始化外观显示。
    /// </summary>
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.color = towerColor;
    }

    /// <summary>
    /// 每帧推进继电器塔的产能逻辑。
    ///
    /// 流程很简单：
    /// 1. 如果总控不存在或游戏已经结束，停止运行。
    /// 2. 继续累积脉冲计时器。
    /// 3. 当累计时间达到阈值时，向总控发放一次电量。
    ///
    /// 这是一种很经典的“Update + 计时器”写法，
    /// 对教学、调试和原型验证都很友好。
    /// </summary>
    private void Update()
    {
        if (TowerDefenseGame.Instance == null || TowerDefenseGame.Instance.IsGameOver)
        {
            return;
        }

        // 这是“计时器式 Update”的典型结构：
        // 每帧先累加时间，再在达到阈值时触发一次离散事件。
        _pulseTimer += Time.deltaTime;

        if (_pulseTimer < pulseInterval)
        {
            return;
        }

        // 这里减去一个周期而不是直接清零，
        // 是为了让计时误差不会在长时间运行中不断放大。
        _pulseTimer -= pulseInterval;
        TowerDefenseGame.Instance.AddEnergy(energyPerPulse);
    }
}
