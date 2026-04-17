using UnityEngine;

/// <summary>
/// `WaveSpawner` 负责按波次把敌人送进战场。
///
/// 旧原型默认只有“一条路径 + 一个出怪口”的思路，
/// 这和新玩法文档里“地图中需要有多个出怪口”的要求已经不一致了。
///
/// 所以这一轮不直接重做整套刷怪系统，
/// 而是先做一层兼容迁移：
/// - 优先读取新的 `BattlefieldMapDefinition`
/// - 如果地图里已经配置了多个 `EnemySpawnGate`，就按轮询顺序把敌人分配到不同出怪口
/// - 如果新地图结构还没接好，仍然允许回退到旧的单路径引用
/// </summary>
public sealed class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    private struct WaveDefinition
    {
        public int enemyCount;
        public float spawnInterval;
        public float moveSpeed;
        public int enemyHealth;
    }

    [Header("Wave Timing")]
    [SerializeField] private float initialDelay = 1.5f;
    [SerializeField] private float delayBetweenWaves = 4f;

    [Header("Map References")]
    [SerializeField] private BattlefieldMapDefinition battlefieldMapReference;

    [Header("Scene References (Fallback)")]
    [SerializeField] private EnemyPath enemyPathReference;
    [SerializeField] private GameObject enemyPrototypeReference;
    [SerializeField] private Transform enemyRootReference;

    [Header("Scene Object Names")]
    [SerializeField] private string enemyRootName = "EnemiesRoot";

    [Header("Wave Content")]
    [SerializeField] private WaveDefinition[] waves;

    private BattlefieldMapDefinition _battlefieldMap;
    private EnemyPath _fallbackEnemyPath;
    private GameObject _enemyPrototype;
    private Transform _enemyRoot;
    private int _currentWaveIndex;
    private int _spawnedInCurrentWave;
    private float _spawnTimer;
    private bool _waitingForFirstWave;
    private bool _levelClearMessageShown;
    private int _spawnGateSequence;

    private void Start()
    {
        EnsureWaveData();

        // 阶段 A 里优先尝试显式地图定义；如果 Inspector 暂时没接，
        // 就用按类型查找做一次过渡兜底，帮助当前样例场景平稳迁移。
        _battlefieldMap = battlefieldMapReference != null
            ? battlefieldMapReference
            : Object.FindFirstObjectByType<BattlefieldMapDefinition>();
        battlefieldMapReference = _battlefieldMap;

        _fallbackEnemyPath = enemyPathReference;
        _enemyPrototype = enemyPrototypeReference;
        _enemyRoot = enemyRootReference != null ? enemyRootReference : new GameObject(enemyRootName).transform;
        enemyRootReference = _enemyRoot;

        if (_battlefieldMap != null)
        {
            _battlefieldMap.LogConfigurationWarnings(this);
            Debug.Log($"[WaveSpawner] Stage-A map summary: {_battlefieldMap.BuildDebugSummary()}", this);
        }

        if (_enemyPrototype == null || !HasAnySpawnSource())
        {
            Debug.LogWarning("WaveSpawner is missing a valid spawn source or enemy prototype. Check BattlefieldMapDefinition / EnemyPath wiring.", this);
            enabled = false;
            return;
        }

        _spawnTimer = initialDelay;
        _waitingForFirstWave = true;
    }

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

        if (!SpawnEnemy(wave, _currentWaveIndex + 1, _spawnedInCurrentWave + 1))
        {
            enabled = false;
            return;
        }

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

    private bool SpawnEnemy(WaveDefinition wave, int waveNumber, int enemyNumber)
    {
        EnemyPath spawnPath = ResolveSpawnPath(out EnemySpawnGate spawnGate);
        if (spawnPath == null)
        {
            Debug.LogWarning("WaveSpawner could not resolve a valid EnemyPath for the next spawn.", this);
            return false;
        }

        GameObject enemyObject = Instantiate(_enemyPrototype, spawnPath.GetSpawnPosition(), Quaternion.identity, _enemyRoot);
        enemyObject.name = spawnGate != null
            ? $"Enemy_{spawnGate.GateId}_W{waveNumber}_{enemyNumber}"
            : $"Enemy_W{waveNumber}_{enemyNumber}";
        enemyObject.SetActive(true);

        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(spawnPath, wave.moveSpeed, wave.enemyHealth);
        }

        return true;
    }

    private bool HasAnySpawnSource()
    {
        return (_battlefieldMap != null && _battlefieldMap.HasAnyValidSpawnGate()) || _fallbackEnemyPath != null;
    }

    private EnemyPath ResolveSpawnPath(out EnemySpawnGate spawnGate)
    {
        spawnGate = null;

        if (_battlefieldMap != null && _battlefieldMap.TryGetSpawnGateBySequence(_spawnGateSequence, out spawnGate))
        {
            _spawnGateSequence++;
            return spawnGate != null ? spawnGate.EnemyPath : null;
        }

        return _fallbackEnemyPath;
    }
}