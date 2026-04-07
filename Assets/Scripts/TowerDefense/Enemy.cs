using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy 表示关卡中沿固定路径前进的敌人单位。
///
/// 这个脚本负责敌人的三个核心系统：
/// 1. 沿着 EnemyPath 提供的路径点逐段移动。
/// 2. 维护自身生命值，并响应防御塔造成的伤害。
/// 3. 把当前生命值同步到头顶血条，提供清晰的战斗反馈。
///
/// 当前原型刻意不实现敌人的攻击、技能、特殊状态或复杂 AI，
/// 因为这阶段更重要的是先把最基础的塔防闭环跑通：
/// “出生 -> 沿路前进 -> 被塔攻击 -> 死亡，或成功抵达基地”
///
/// 这种从最小可玩闭环开始的做法，能让系统更容易定位问题，
/// 也更适合一边搭建一边观察玩法节奏。
/// </summary>
/// <remarks>
/// 该脚本要求对象上存在 SpriteRenderer，用于敌人主体的显示。
/// 头顶血条则通过子节点上的 SpriteRenderer 完成。
/// </remarks>
[RequireComponent(typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour
{
    /// <summary>
    /// 当前场景中所有激活敌人的静态列表。
    ///
    /// 这样做的主要目的是给防御塔提供一个稳定的“扫描池”，
    /// 让塔能够快速遍历当前所有活跃敌人，而不需要每次都用 Find 类接口全场搜索。
    ///
    /// 对于原型规模的塔防项目，这是一种简单、直观且足够高效的管理方式。
    /// </summary>
    private static readonly List<Enemy> ActiveEnemies = new List<Enemy>();

    [Header("Movement")]

    /// <summary>
    /// 敌人与当前目标路径点之间的“到达判定距离”。
    ///
    /// 因为浮点移动很少能刚好精确落在某个点上，
    /// 所以通常都会设一个很小的容差范围，
    /// 只要进入这个范围，就认为已经到达该路径点，可以切换到下一个点。
    /// </summary>
    [SerializeField] private float reachWaypointDistance = 0.05f;

    [Header("Visuals")]

    /// <summary>
    /// 敌人主体显示颜色。
    /// </summary>
    [SerializeField] private Color bodyColor = new Color(0.9f, 0.25f, 0.25f, 1f);

    /// <summary>
    /// 血条填充部分的颜色。
    ///
    /// 这部分会随着剩余生命值缩放宽度，是玩家判断集火效果的重要反馈。
    /// </summary>
    [SerializeField] private Color healthBarFillColor = new Color(0.2f, 0.9f, 0.35f, 1f);

    /// <summary>
    /// 血条背景部分的颜色，用于衬托血量变化。
    /// </summary>
    [SerializeField] private Color healthBarBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("Health Bar References")]

    /// <summary>
    /// 敌人血条根节点的显式引用。
    ///
    /// 现在我们不再靠 	ransform.Find("HealthBarRoot") 这种名字查找去摸索层级，
    /// 而是要求预制体或场景原型把血条根明确接到这里。
    /// 这样后续即使你重命名子节点，血条系统也不会因为名字变化而悄悄失效。
    /// </summary>
    [SerializeField] private Transform healthBarRootReference;

    /// <summary>
    /// 血条填充条的显式引用。
    ///
    /// 有了这个引用后，运行时只需要改它的缩放和位置，
    /// 不再需要一层层按名字去树里找 Fill 子节点。
    /// </summary>
    [SerializeField] private Transform healthBarFillReference;

    /// <summary>
    /// 填充条 SpriteRenderer 的显式引用。
    ///
    /// 这样颜色主题应用也能直接命中目标，
    /// 不必再从填充节点向下或向旁边做任何名字推断。
    /// </summary>
    [SerializeField] private SpriteRenderer healthBarFillRendererReference;

    /// <summary>
    /// 血条背景 SpriteRenderer 的显式引用。
    ///
    /// 这条引用和 Fill 一起，组成了完整的血条显式装配链。
    /// </summary>
    [SerializeField] private SpriteRenderer healthBarBackgroundRendererReference;

    /// <summary>
    /// 敌人主体的 SpriteRenderer 缓存。
    /// </summary>
    private SpriteRenderer _spriteRenderer;

    /// <summary>
    /// 血条根节点。
    ///
    /// 这里单独缓存整个根节点，而不是只记住 Fill，
    /// 是因为后续除了更新血量比例，还需要在某些全局状态下“一键隐藏整条血条”。
    ///
    /// 最典型的场景就是 Game Over：
    /// 玩家已经进入结算界面时，继续显示敌人头顶的绿色血条只会污染 UI，
    /// 所以直接隐藏整个 HealthBarRoot 会比逐个关子节点更稳、更清楚。
    /// </summary>
    private Transform _healthBarRoot;

    /// <summary>
    /// 血条填充节点的 Transform。
    ///
    /// 我们需要它来同时修改血条的局部缩放和局部位置，
    /// 从而实现“从左向右缩短”的视觉效果。
    /// </summary>
    private Transform _healthBarFill;

    /// <summary>
    /// 血条填充部分的渲染器。
    /// </summary>
    private SpriteRenderer _healthBarFillRenderer;

    /// <summary>
    /// 血条背景部分的渲染器。
    /// </summary>
    private SpriteRenderer _healthBarBackgroundRenderer;

    /// <summary>
    /// 当前敌人正在遵循的路径对象。
    /// </summary>
    private EnemyPath _path;

    /// <summary>
    /// 当前敌人的移动速度。
    ///
    /// 这个值由刷怪器在生成时注入，
    /// 因此同一个敌人预制体可以在不同波次表现出不同强度。
    /// </summary>
    private float _moveSpeed;

    /// <summary>
    /// 敌人的最大生命值。
    /// </summary>
    private int _maxHealth;

    /// <summary>
    /// 敌人的当前生命值。
    /// </summary>
    private int _currentHealth;

    /// <summary>
    /// 当前要前往的目标路径点索引。
    ///
    /// 初始化时从 1 开始，是因为 0 号点会作为出生位置使用，
    /// 敌人生成后已经站在那个点附近了，接下来应该前往下一个点。
    /// </summary>
    private int _targetWaypointIndex;

    /// <summary>
    /// 是否已经成功抵达基地。
    ///
    /// 这个标记主要用于防止重复触发扣血逻辑。
    /// </summary>
    private bool _hasReachedBase;

    /// <summary>
    /// 当前激活敌人的数量。
    /// </summary>
    public static int ActiveEnemyCount => ActiveEnemies.Count;

    /// <summary>
    /// 按索引获取某个激活中的敌人。
    ///
    /// 防御塔会通过这个接口遍历敌人列表，进行目标筛选。
    /// </summary>
    public static Enemy GetActiveEnemy(int index)
    {
        return ActiveEnemies[index];
    }

    /// <summary>
    /// 控制敌人头顶血条的整体显隐。
    ///
    /// 这里专门由 Enemy 自己对外提供入口，
    /// 是为了把“血条挂在哪里、由哪些子节点组成”继续封装在敌人内部。
    ///
    /// 这样总控层只需要表达业务意图：
    /// “现在进入 Game Over 了，把场上敌人的血条都收起来”，
    /// 而不需要自己深入到场景树里找 HealthBarRoot/HealthBarFill 这些实现细节。
    /// </summary>
    public void SetHealthBarVisible(bool visible)
    {
        CacheReferences();

        if (_healthBarRoot != null)
        {
            _healthBarRoot.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// 在编辑器里尽量补齐“显式血条引用链”的轻量派生项。
    ///
    /// 这里不再按名字去找血条子节点，
    /// 但如果填充节点引用已经明确拖好了，而对应的 SpriteRenderer 还没接，
    /// 我们就直接从这个显式引用上拿组件，减少重复手工操作。
    /// </summary>
    private void OnValidate()
    {
        if (healthBarFillReference != null && healthBarFillRendererReference == null)
        {
            healthBarFillRendererReference = healthBarFillReference.GetComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// 初始化组件引用并应用默认外观。
    /// </summary>
    private void Awake()
    {
        CacheReferences();
        ApplyVisualTheme();
        SetHealthBarVisible(true);
    }

    /// <summary>
    /// 当对象进入激活状态时，把自己注册到全局激活敌人列表。
    ///
    /// 这样塔在扫描目标时，才能看见这个敌人。
    /// </summary>
    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
        {
            ActiveEnemies.Add(this);
        }
    }

    /// <summary>
    /// 当对象被禁用或销毁时，把自己从全局激活敌人列表移除。
    ///
    /// 这样可以避免塔继续扫描到已经死亡或离场的对象。
    /// </summary>
    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    /// <summary>
    /// 在敌人生成后，为其注入当前波次需要的运行参数。
    ///
    /// 这样同一个敌人模板就能在不同波次复用，
    /// 只需改变移动速度和生命值，就能做出明显的难度变化。
    ///
    /// 初始化流程包括：
    /// 1. 缓存相关引用并刷新配色。
    /// 2. 记录路径、速度和生命值。
    /// 3. 把敌人放到出生点。
    /// 4. 重置行进索引与基地抵达标记。
    /// 5. 刷新一次血条显示。
    /// </summary>
    public void Initialize(EnemyPath path, float moveSpeed, int maxHealth)
    {
        CacheReferences();
        ApplyVisualTheme();

        _path = path;
        _moveSpeed = moveSpeed;
        _maxHealth = Mathf.Max(1, maxHealth);
        _currentHealth = _maxHealth;
        _targetWaypointIndex = 1;
        _hasReachedBase = false;

        if (_path != null)
        {
            transform.position = _path.GetSpawnPosition();
        }

        UpdateHealthBar();
    }

    /// <summary>
    /// 每帧推动敌人的前进逻辑。
    ///
    /// 大致流程是：
    /// 1. 如果游戏结束，就停止更新。
    /// 2. 如果路径无效、没有路径点，或已经到达基地，也停止更新。
    /// 3. 如果当前目标索引越界，说明路线走完，触发抵达基地逻辑。
    /// 4. 否则朝当前目标路径点移动。
    /// 5. 当靠近到足够距离时，切换到下一个路径点。
    /// </summary>
    private void Update()
    {
        if (TowerDefenseGame.Instance != null && TowerDefenseGame.Instance.IsGameOver)
        {
            return;
        }

        if (_path == null || _path.WaypointCount == 0 || _hasReachedBase)
        {
            return;
        }

        if (_targetWaypointIndex >= _path.WaypointCount)
        {
            ReachBase();
            return;
        }

        Vector3 targetPosition = _path.GetWaypointPosition(_targetWaypointIndex);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) <= reachWaypointDistance)
        {
            _targetWaypointIndex++;

            if (_targetWaypointIndex >= _path.WaypointCount)
            {
                ReachBase();
            }
        }
    }

    /// <summary>
    /// 让敌人承受一次伤害。
    ///
    /// 这里会先过滤无效输入或已经死亡的情况，
    /// 然后扣除生命值，刷新血条，并在生命值归零时触发死亡。
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || _currentHealth <= 0)
        {
            return;
        }

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        UpdateHealthBar();

        if (_currentHealth == 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 处理敌人成功抵达基地的结果。
    ///
    /// 这会对基地造成固定伤害，并销毁当前敌人对象。
    /// 使用 _hasReachedBase 标记可以防止该逻辑被重复触发。
    /// </summary>
    private void ReachBase()
    {
        if (_hasReachedBase)
        {
            return;
        }

        _hasReachedBase = true;
        TowerDefenseGame.Instance?.DamageBase(1);
        Destroy(gameObject);
    }

    /// <summary>
    /// 处理敌人死亡。
    ///
    /// 当前版本只做最基础的销毁，
    /// 以后可以很自然地在这里扩展掉落、死亡动画、积分或音效逻辑。
    /// </summary>
    private void Die()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// 根据当前生命比例刷新头顶血条。
    ///
    /// 血条由两部分组成：
    /// - Background：固定显示的背景底板
    /// - Fill：表示剩余血量的填充条
    ///
    /// 填充条既要缩放 X 轴，也要同步调整局部位置。
    /// 因为默认缩放是以中心点为轴心进行的，
    /// 如果只改缩放不改位置，血条会从中间同时向两边收缩，
    /// 看起来就不像常见的“从左往右掉血”。
    /// </summary>
    private void UpdateHealthBar()
    {
        if (_healthBarFill == null)
        {
            return;
        }

        float healthRatio = _maxHealth <= 0 ? 0f : (float)_currentHealth / _maxHealth;

        Vector3 fillScale = _healthBarFill.localScale;
        fillScale.x = healthRatio;
        _healthBarFill.localScale = fillScale;

        // 通过同步修正局部位置，让血条视觉上保持左侧固定，
        // 从而更符合玩家对生命值减少的直觉认知。
        Vector3 fillPosition = _healthBarFill.localPosition;
        fillPosition.x = (healthRatio - 1f) * 0.5f;
        _healthBarFill.localPosition = fillPosition;
    }

    /// <summary>
    /// 缓存敌人本体和血条相关节点引用。
    ///
    /// 这一轮改完后，血条链路优先走显式序列化引用：
    /// - 原型或预制体把 Root / Fill / Background 直接拖进来
    /// - 运行时实例直接继承这些引用
    ///
    /// 这样我们既保留了“只在第一次缓存时做轻量整理”的结构，
    /// 又把按名字找子节点的隐藏依赖彻底移出了主流程。
    /// </summary>
    private void CacheReferences()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_healthBarRoot == null)
        {
            _healthBarRoot = healthBarRootReference;
        }

        if (_healthBarFill == null)
        {
            _healthBarFill = healthBarFillReference;
        }

        if (_healthBarFillRenderer == null)
        {
            _healthBarFillRenderer = healthBarFillRendererReference;
            if (_healthBarFillRenderer == null && _healthBarFill != null)
            {
                _healthBarFillRenderer = _healthBarFill.GetComponent<SpriteRenderer>();
            }
        }

        if (_healthBarBackgroundRenderer == null)
        {
            _healthBarBackgroundRenderer = healthBarBackgroundRendererReference;
        }
    }

    /// <summary>
    /// 把 Inspector 中配置的颜色应用到敌人本体和血条显示上。
    ///
    /// 这样美术风格可以通过 Inspector 调整，而不是硬编码在逻辑流程里。
    /// </summary>
    private void ApplyVisualTheme()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = bodyColor;
        }

        if (_healthBarFillRenderer != null)
        {
            _healthBarFillRenderer.color = healthBarFillColor;
        }

        if (_healthBarBackgroundRenderer != null)
        {
            _healthBarBackgroundRenderer.color = healthBarBackgroundColor;
        }
    }
}
