using System.Collections;
using UnityEngine;

/// <summary>
/// DefenseTower 是当前原型中的基础攻击塔。
///
/// 它的核心玩法职责很明确：
/// 1. 周期性扫描周围敌人。
/// 2. 从扫描结果中找出最近的有效目标。
/// 3. 对目标立即造成一次伤害。
/// 4. 通过短暂闪白反馈告诉玩家“刚刚发生了一次攻击”。
///
/// 这里采用的是“瞬间命中”方案，也就是常见的 hitscan 思路：
/// - 不生成子弹对象
/// - 不模拟弹道飞行
/// - 不处理碰撞命中延迟
///
/// 这种实现非常适合原型阶段，因为它能把注意力集中在塔防核心循环上：
/// “发现敌人 -> 输出伤害 -> 敌人死亡或继续前进”
/// 等基础玩法稳定后，再逐步扩展为抛射物、特效、暴击或元素伤害都很自然。
/// </summary>
/// <remarks>
/// 该脚本依赖 SpriteRenderer，用于显示塔本体颜色以及攻击瞬间的闪烁反馈。
/// </remarks>
[RequireComponent(typeof(SpriteRenderer))]
public class DefenseTower : MonoBehaviour
{
    [Header("Combat")]

    /// <summary>
    /// 塔的攻击半径。
    ///
    /// 只有进入这个半径内的敌人才会被视为候选目标。
    /// 当前使用的是世界坐标下的距离判断，因此它直接决定了塔的实际覆盖范围。
    /// </summary>
    [SerializeField] private float attackRange = 2.8f;

    /// <summary>
    /// 两次攻击之间的时间间隔，单位为秒。
    ///
    /// 数值越小，塔攻击得越频繁；数值越大，塔的节奏越慢。
    /// 在这个脚本里，它通过计时器累计实现，而不是协程等待。
    /// </summary>
    [SerializeField] private float attackInterval = 0.65f;

    /// <summary>
    /// 每次命中时对目标造成的伤害值。
    ///
    /// 当前原型只使用整数血量和固定伤害，没有暴击、护甲穿透或伤害浮动，
    /// 是为了先让“伤害结算链路”保持足够简单。
    /// </summary>
    [SerializeField] private int damagePerShot = 1;

    [Header("Visuals")]

    /// <summary>
    /// 塔在正常待机状态下显示的主体颜色。
    /// </summary>
    [SerializeField] private Color towerColor = new Color(0.2f, 0.55f, 1f, 1f);

    /// <summary>
    /// 塔完成一次攻击后瞬间闪烁时使用的颜色。
    ///
    /// 这种极短的颜色变化虽然简单，但对原型验证非常有帮助，
    /// 因为它能让你不借助复杂特效也看清楚塔是否真的在开火。
    /// </summary>
    [SerializeField] private Color flashColor = Color.white;

    /// <summary>
    /// 对本体 SpriteRenderer 的缓存引用。
    /// </summary>
    private SpriteRenderer _spriteRenderer;

    /// <summary>
    /// 攻击计时器。
    ///
    /// 每帧把 Time.deltaTime 累加到这里，
    /// 当累计时间达到 attackInterval 时，就执行一次攻击尝试。
    /// </summary>
    private float _attackTimer;

    /// <summary>
    /// 初始化渲染引用并设置塔的默认外观。
    /// </summary>
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.color = towerColor;
    }

    /// <summary>
    /// 每帧执行塔的攻击逻辑。
    ///
    /// 运行流程可以概括为：
    /// 1. 游戏是否已经结束，如果结束则停止攻击。
    /// 2. 继续累积攻击计时器。
    /// 3. 如果还没到攻击间隔，就继续等待。
    /// 4. 到点后查找最近敌人。
    /// 5. 如果找到目标，就立即造成伤害并播放一次闪烁反馈。
    ///
    /// 这种“Update + 计时器”的写法在教学和原型中很常见，
    /// 因为所有状态都集中在一个地方，调试时也很直观。
    /// </summary>
    private void Update()
    {
        if (TowerDefenseGame.Instance == null || TowerDefenseGame.Instance.IsGameOver)
        {
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer < attackInterval)
        {
            return;
        }

        // 到达攻击阈值后，不是直接清零，而是减去一个间隔值。
        // 这样能在帧率不稳定时保留一部分剩余时间，
        // 让长期节奏比“清零重算”更平滑一些。
        _attackTimer -= attackInterval;

        Enemy target = FindClosestTarget();
        if (target == null)
        {
            return;
        }

        target.TakeDamage(damagePerShot);
        StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// 在所有激活中的敌人里，找出攻击范围内距离自己最近的目标。
    ///
    /// 当前策略非常直接，就是“最近优先”。
    /// 它不是唯一策略，但对原型验证特别实用，因为行为结果容易理解：
    /// 谁离塔最近，谁就更容易先被打。
    ///
    /// 这里使用距离平方而不是真实距离，
    /// 是因为比较大小规律时没有必要开平方，能稍微减少计算量。
    /// 在敌人数量变多时，这种写法会更划算。
    /// </summary>
    /// <returns>
    /// 返回找到的最佳敌人；如果范围内没有可攻击目标，则返回 null。
    /// </returns>
    private Enemy FindClosestTarget()
    {
        float maxDistanceSqr = attackRange * attackRange;
        float closestDistanceSqr = float.MaxValue;
        Enemy bestTarget = null;

        for (int i = 0; i < Enemy.ActiveEnemyCount; i++)
        {
            Enemy candidate = Enemy.GetActiveEnemy(i);
            if (candidate == null)
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr > maxDistanceSqr || distanceSqr >= closestDistanceSqr)
            {
                continue;
            }

            closestDistanceSqr = distanceSqr;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    /// <summary>
    /// 攻击命中时播放一个极短的闪白效果。
    ///
    /// 这里的实现很轻量：
    /// - 先把颜色改成 flashColor
    /// - 等待一帧
    /// - 再恢复成塔的默认颜色
    ///
    /// 这种一帧级别的反馈足够便宜，也足够清晰，
    /// 特别适合没有正式特效资源时的玩法验证阶段。
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        _spriteRenderer.color = flashColor;
        yield return null;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = towerColor;
        }
    }
}
