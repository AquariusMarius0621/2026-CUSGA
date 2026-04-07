using UnityEngine;

/// <summary>
/// WaveSpawner 负责按波次控制敌人的生成节奏。
///
/// 从玩法角度看，它承担的是“战斗节拍器”的角色：
/// - 什么时候开第一波
/// - 一波里总共刷多少只
/// - 每只之间隔多久出现
/// - 每波结束后间隔多久再进入下一波
///
/// 这里使用的是“Update + 计时器”的方案，而不是 Coroutine 等待。
/// 这种写法在原型和教学中很有优势：
/// - 所有状态都集中在一个脚本里，阅读路径清晰
/// - 更容易打断点和观察每帧变化
/// - 更容易把刷怪流程理解成一个简单状态机
///
/// 你可以把它概括为下面这条流程：
/// 1. 等待开局延迟
/// 2. 开始当前波次
/// 3. 按设定间隔逐个刷敌人
/// 4. 本波刷完后等待下一波
/// 5. 所有波次结束后，等场上敌人被清空，再提示关卡完成
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    /// <summary>
    /// 单波次配置数据。
    ///
    /// 每个元素描述一整波敌人的核心参数：
    /// - enemyCount：这一波要刷多少个敌人
    /// - spawnInterval：相邻两个敌人之间的生成间隔
    /// - moveSpeed：这一波敌人的移动速度
    /// - enemyHealth：这一波敌人的生命值
    ///
    /// 用结构体数组来描述波次内容，能让波次数据既集中又容易在 Inspector 中调整。
    /// </summary>
    [System.Serializable]
    private struct WaveDefinition
    {
        public int enemyCount;
        public float spawnInterval;
        public float moveSpeed;
        public int enemyHealth;
    }

    [Header("Wave Timing")]

    /// <summary>
    /// 关卡开始到第一波出现前的等待时间。
    ///
    /// 留一点起始缓冲能让玩家先看清场面、选塔并做第一次部署。
    /// </summary>
    [SerializeField] private float initialDelay = 1.5f;

    /// <summary>
    /// 相邻两波之间的等待时间。
    ///
    /// 这个时间既是节奏控制，也是玩家调整防线的重要窗口。
    /// </summary>
    [SerializeField] private float delayBetweenWaves = 4f;

    [Header("Scene References (Preferred)")]

    /// <summary>
    /// 这是第一阶段迁移里给刷怪器补上的显式场景引用。
    ///
    /// 长期来看，WaveSpawner 这种核心流程脚本更适合直接看 Inspector 就知道自己依赖什么：
    /// - 敌人走哪条路
    /// - 从哪个原型生成
    /// - 生成后挂到哪个根节点下
    ///
    /// 所以这里改成“引用优先、名字兜底”，
    /// 先把最核心的隐式依赖关系显式化，再慢慢移除旧 fallback。
    /// </summary>
    [SerializeField] private EnemyPath enemyPathReference;
    [SerializeField] private GameObject enemyPrototypeReference;
    [SerializeField] private Transform enemyRootReference;

    [Header("Scene Object Names")]

    /// <summary>
    /// 路径对象名称。
    ///
    /// 刷怪器会通过它找到敌人出生点和路线数据。
    /// </summary>

    /// <summary>
    /// 敌人原型对象名称。
    ///
    /// 每次刷怪时都会克隆这个模板。
    /// </summary>

    /// <summary>
    /// 运行时敌人实例父节点名称。
    ///
    /// 把刷出的敌人统一挂到这个根节点下，方便整理层级和调试。
    /// </summary>
    [SerializeField] private string enemyRootName = "EnemiesRoot";

    [Header("Wave Content")]

    /// <summary>
    /// 当前关卡的全部波次配置。
    /// </summary>
    [SerializeField] private WaveDefinition[] waves;

    /// <summary>
    /// 敌人行进路径引用。
    /// </summary>
    private EnemyPath _enemyPath;

    /// <summary>
    /// 敌人原型对象引用。
    /// </summary>
    private GameObject _enemyPrototype;

    /// <summary>
    /// 运行时敌人父节点。
    /// </summary>
    private Transform _enemyRoot;

    /// <summary>
    /// 当前正在处理的波次索引。
    ///
    /// 这是零基索引，所以第 1 波对应值为 0。
    /// </summary>
    private int _currentWaveIndex;

    /// <summary>
    /// 当前波次已经刷出了多少只敌人。
    /// </summary>
    private int _spawnedInCurrentWave;

    /// <summary>
    /// 刷怪计时器。
    ///
    /// 在不同阶段，它可能表示：
    /// - 距离第一波开始还剩多久
    /// - 距离下一个敌人刷出还剩多久
    /// - 距离下一波开始还剩多久
    /// </summary>
    private float _spawnTimer;

    /// <summary>
    /// 是否仍处于“等待第一波开始”的状态。
    ///
    /// 主要用于在第一波到来时显示不同于后续波次的提示语。
    /// </summary>
    private bool _waitingForFirstWave;

    /// <summary>
    /// 是否已经显示过关卡完成提示。
    ///
    /// 用来避免在最后一波结束后，每帧都重复刷出同一条通关消息。
    /// </summary>
    private bool _levelClearMessageShown;

    /// <summary>
    /// 初始化波次数据和场景引用。
    ///
    /// 启动时会：
    /// 1. 如果 Inspector 没配波次，则生成一套默认测试数据。
    /// 2. 查找路径、敌人原型和敌人根节点。
    /// 3. 如果关键依赖缺失，则禁用自己并输出警告。
    /// 4. 设置第一波开始前的等待计时。
    /// </summary>
    private void Start()
    {
        EnsureWaveData();

        // 继续把刷怪主链从“按名字找对象”迁到“显式场景装配”。
        // 对当前 SampleScene 而言，EnemyPath 和 EnemyPrototype 都应当明确拖在 Inspector 上；
        // EnemiesRoot 如果没拖，则这里允许安全地新建一个运行时容器，但不再靠名字去搜旧对象。
        _enemyPath = enemyPathReference;
        _enemyPrototype = enemyPrototypeReference;
        _enemyRoot = enemyRootReference != null ? enemyRootReference : new GameObject(enemyRootName).transform;
        enemyRootReference = _enemyRoot;

        if (_enemyPath == null || _enemyPrototype == null)
        {
            Debug.LogWarning("WaveSpawner is missing EnemyPath or EnemyPrototype reference. Check the scene wiring.");
            enabled = false;
            return;
        }

        _spawnTimer = initialDelay;
        _waitingForFirstWave = true;
    }

    /// <summary>
    /// 每帧推进刷怪状态机。
    ///
    /// 主要逻辑分为三大段：
    /// 1. 如果游戏结束，就不再刷怪。
    /// 2. 如果所有波次都刷完了，就等待场上敌人清空后显示通关消息。
    /// 3. 如果还在刷怪流程中，就递减计时器，并在到点时生成敌人或切换波次。
    /// </summary>
    private void Update()
    {
        if (TowerDefenseGame.Instance != null && TowerDefenseGame.Instance.IsGameOver)
        {
            return;
        }

        if (_currentWaveIndex >= waves.Length)
        {
            if (!_levelClearMessageShown && Enemy.ActiveEnemyCount == 0 && TowerDefenseGame.Instance != null)
            {
                TowerDefenseGame.Instance.SetStatusMessage("Test level complete! The base survived.");
                _levelClearMessageShown = true;
            }

            return;
        }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f)
        {
            return;
        }

        WaveDefinition wave = waves[_currentWaveIndex];

        // 只有当这一波真正刷出第一个敌人时，才更新 HUD 波次和提示文案。
        // 这样界面显示会和玩家实际看到的战斗节奏更一致。
        if (_spawnedInCurrentWave == 0 && TowerDefenseGame.Instance != null)
        {
            TowerDefenseGame.Instance.SetWaveProgress(_currentWaveIndex + 1, waves.Length);

            if (_waitingForFirstWave)
            {
                TowerDefenseGame.Instance.SetStatusMessage($"Wave {_currentWaveIndex + 1} incoming. Hold the line!");
                _waitingForFirstWave = false;
            }
            else
            {
                TowerDefenseGame.Instance.SetStatusMessage($"Wave {_currentWaveIndex + 1} started.");
            }
        }

        SpawnEnemy(wave, _currentWaveIndex + 1, _spawnedInCurrentWave + 1);
        _spawnedInCurrentWave++;

        if (_spawnedInCurrentWave < wave.enemyCount)
        {
            _spawnTimer = wave.spawnInterval;
            return;
        }

        _currentWaveIndex++;
        _spawnedInCurrentWave = 0;

        if (_currentWaveIndex < waves.Length)
        {
            _spawnTimer = delayBetweenWaves;

            if (TowerDefenseGame.Instance != null)
            {
                TowerDefenseGame.Instance.SetStatusMessage($"Wave {_currentWaveIndex} cleared. Prepare for the next one.");
            }
        }
        else
        {
            _spawnTimer = 0f;
        }
    }

    /// <summary>
    /// 如果当前没有配置波次数据，就生成一套默认测试波次。
    ///
    /// 这样即使你忘了先在 Inspector 里填数据，原型也仍然可以直接运行起来，
    /// 非常适合快速试玩和教学演示。
    /// </summary>
    private void EnsureWaveData()
    {
        if (waves != null && waves.Length > 0)
        {
            return;
        }

        waves = new[]
        {
            new WaveDefinition { enemyCount = 4, spawnInterval = 1.0f, moveSpeed = 1.8f, enemyHealth = 3 },
            new WaveDefinition { enemyCount = 6, spawnInterval = 0.85f, moveSpeed = 2.1f, enemyHealth = 4 },
            new WaveDefinition { enemyCount = 8, spawnInterval = 0.70f, moveSpeed = 2.4f, enemyHealth = 6 }
        };
    }

    /// <summary>
    /// 生成一个敌人实例，并把当前波次参数注入到敌人脚本中。
    ///
    /// 这里会完成三件事：
    /// 1. 克隆敌人原型并放到出生点。
    /// 2. 生成便于调试识别的名字，例如 Enemy_W2_3。
    /// 3. 调用 Enemy.Initialize，把速度、生命值和路径配置传给敌人。
    /// </summary>
    private void SpawnEnemy(WaveDefinition wave, int waveNumber, int enemyNumber)
    {
        GameObject enemyObject = Instantiate(_enemyPrototype, _enemyPath.GetSpawnPosition(), Quaternion.identity, _enemyRoot);
        enemyObject.name = $"Enemy_W{waveNumber}_{enemyNumber}";
        enemyObject.SetActive(true);

        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(_enemyPath, wave.moveSpeed, wave.enemyHealth);
        }
    }
}
