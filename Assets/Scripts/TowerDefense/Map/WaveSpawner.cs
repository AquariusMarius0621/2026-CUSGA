using System.Collections.Generic;
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
        public int enemyScrapReward;
    }

    [Header("Wave Timing")]
    [SerializeField] private float initialDelay = 1.5f;
    [SerializeField] private float delayBetweenWaves = 4f;

    [Header("Route Preview")]
    [Tooltip("敌人路线预告会在正式出怪前多少秒开始显示。只有第一波，或后续路线相对上一波发生变化时，这个时间窗口才会生效。")]
    [Min(0f)]
    [SerializeField] private float routePreviewLeadTime = 2f;

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
    private bool _routePreviewVisible;
    private bool _hasStartedAnyWave;
    private string _lastStartedWaveRouteSignature = string.Empty;
    private readonly List<EnemyPath> _allRoutePreviewPaths = new List<EnemyPath>();
    private readonly List<EnemyPath> _activeRoutePreviewPaths = new List<EnemyPath>();

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
        CacheRoutePreviewPaths();

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
        UpdateRoutePreviewVisibility(force: true);
    }

    private void Update()
    {
        if (TowerDefenseGame.Instance != null && TowerDefenseGame.Instance.IsGameOver)
        {
            UpdateRoutePreviewVisibility(force: true, overrideVisible: false);
            return;
        }

        if (_currentWaveIndex >= waves.Length)
        {
            UpdateRoutePreviewVisibility(force: true, overrideVisible: false);

            if (!_levelClearMessageShown && Enemy.ActiveEnemyCount == 0 && TowerDefenseGame.Instance != null)
            {
                TowerDefenseGame.Instance.SetStatusMessage("Test level complete! The base survived.");
                _levelClearMessageShown = true;
            }

            return;
        }

        _spawnTimer -= Time.deltaTime;
        UpdateRoutePreviewVisibility(force: false);

        if (_spawnTimer > 0f)
        {
            return;
        }

        WaveDefinition wave = waves[_currentWaveIndex];

        if (_spawnedInCurrentWave == 0 && TowerDefenseGame.Instance != null)
        {
            TowerDefenseGame.Instance.SetWaveProgress(_currentWaveIndex + 1, waves.Length);
            MarkCurrentWaveRouteAsStarted();

            if (_waitingForFirstWave)
            {
                TowerDefenseGame.Instance.SetStatusMessage($"Wave {_currentWaveIndex + 1} incoming. Hold the line!");
                _waitingForFirstWave = false;
            }
            else
            {
                TowerDefenseGame.Instance.SetStatusMessage($"Wave {_currentWaveIndex + 1} started.");
            }

            TowerDefenseGame.Instance.ShowTransientHudNotice(
                $"Wave {_currentWaveIndex + 1}: salvage potential {GetWaveScrapRewardTotal(wave)} SCRAP.",
                duration: 3.4f);
        }

        if (!SpawnEnemy(wave, _currentWaveIndex + 1, _spawnedInCurrentWave + 1))
        {
            enabled = false;
            return;
        }

        _spawnedInCurrentWave++;
        UpdateRoutePreviewVisibility(force: true);

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
            UpdateRoutePreviewVisibility(force: true);

            if (TowerDefenseGame.Instance != null)
            {
                TowerDefenseGame.Instance.SetStatusMessage($"Wave {_currentWaveIndex} cleared. Prepare for the next one.");
            }
        }
        else
        {
            _spawnTimer = 0f;
            UpdateRoutePreviewVisibility(force: true, overrideVisible: false);
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
            new WaveDefinition { enemyCount = 4, spawnInterval = 1.0f, moveSpeed = 1.8f, enemyHealth = 3, enemyScrapReward = 8 },
            new WaveDefinition { enemyCount = 6, spawnInterval = 0.85f, moveSpeed = 2.1f, enemyHealth = 4, enemyScrapReward = 11 },
            new WaveDefinition { enemyCount = 8, spawnInterval = 0.70f, moveSpeed = 2.4f, enemyHealth = 6, enemyScrapReward = 15 }
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
            enemy.Initialize(spawnPath, wave.moveSpeed, wave.enemyHealth, wave.enemyScrapReward);
        }

        return true;
    }

    private bool HasAnySpawnSource()
    {
        return (_battlefieldMap != null && _battlefieldMap.HasAnyValidSpawnGate()) || _fallbackEnemyPath != null;
    }

    private static int GetWaveScrapRewardTotal(WaveDefinition wave)
    {
        return Mathf.Max(0, wave.enemyCount) * Mathf.Max(0, wave.enemyScrapReward);
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

    /// <summary>
    /// 缓存本关所有需要参与“波前路线预告”的路径。
    /// 多出怪口地图会把所有有效路径都收进来；旧版单路径回退则只保留一条。
    /// </summary>
    private void CacheRoutePreviewPaths()
    {
        _allRoutePreviewPaths.Clear();

        if (_battlefieldMap != null && _battlefieldMap.HasAnyValidSpawnGate())
        {
            for (int i = 0; i < _battlefieldMap.SpawnGateCount; i++)
            {
                if (!_battlefieldMap.TryGetSpawnGateBySequence(i, out EnemySpawnGate spawnGate) || spawnGate == null)
                {
                    continue;
                }

                EnemyPath path = spawnGate.EnemyPath;
                if (path != null && !_allRoutePreviewPaths.Contains(path))
                {
                    _allRoutePreviewPaths.Add(path);
                }
            }
        }

        if (_allRoutePreviewPaths.Count == 0 && _fallbackEnemyPath != null)
        {
            _allRoutePreviewPaths.Add(_fallbackEnemyPath);
        }
    }

    /// <summary>
    /// 根据当前计时器状态统一更新路径预告显隐。
    /// </summary>
    private void UpdateRoutePreviewVisibility(bool force, bool? overrideVisible = null)
    {
        bool shouldShow = overrideVisible ?? ShouldShowRoutePreview();
        if (!force && _routePreviewVisible == shouldShow)
        {
            return;
        }

        BuildActiveRoutePreviewPaths(_activeRoutePreviewPaths);
        _routePreviewVisible = shouldShow;

        for (int i = 0; i < _allRoutePreviewPaths.Count; i++)
        {
            EnemyPath path = _allRoutePreviewPaths[i];
            if (path != null)
            {
                bool pathShouldShow = shouldShow && _activeRoutePreviewPaths.Contains(path);
                path.SetRuntimeReadabilityVisible(pathShouldShow);
            }
        }
    }

    private bool ShouldShowRoutePreview()
    {
        if (_currentWaveIndex >= waves.Length)
        {
            return false;
        }

        if (_spawnedInCurrentWave > 0)
        {
            return false;
        }

        if (_spawnTimer > Mathf.Max(0f, routePreviewLeadTime))
        {
            return false;
        }

        string upcomingSignature = BuildWaveRouteSignature(_currentWaveIndex, _spawnGateSequence);
        if (!_hasStartedAnyWave)
        {
            return true;
        }

        return !string.Equals(upcomingSignature, _lastStartedWaveRouteSignature, System.StringComparison.Ordinal);
    }

    private void MarkCurrentWaveRouteAsStarted()
    {
        _lastStartedWaveRouteSignature = BuildWaveRouteSignature(_currentWaveIndex, _spawnGateSequence);
        _hasStartedAnyWave = true;
    }

    /// <summary>
    /// 计算当前波次真正会走到的“预告路线集合”。
    /// 这里按唯一路径去重，因为玩家看到的是路线图层本身，而不是同一路径被刷了几只怪。
    /// </summary>
    private void BuildActiveRoutePreviewPaths(List<EnemyPath> output)
    {
        output.Clear();

        if (_currentWaveIndex >= waves.Length)
        {
            return;
        }

        if (_battlefieldMap != null && _battlefieldMap.HasAnyValidSpawnGate())
        {
            WaveDefinition wave = waves[_currentWaveIndex];
            for (int enemyIndex = 0; enemyIndex < wave.enemyCount; enemyIndex++)
            {
                int gateSequence = _spawnGateSequence + enemyIndex;
                if (!_battlefieldMap.TryGetSpawnGateBySequence(gateSequence, out EnemySpawnGate spawnGate) || spawnGate == null)
                {
                    continue;
                }

                EnemyPath path = spawnGate.EnemyPath;
                if (path != null && !output.Contains(path))
                {
                    output.Add(path);
                }
            }
        }

        if (output.Count == 0 && _fallbackEnemyPath != null)
        {
            output.Add(_fallbackEnemyPath);
        }
    }

    /// <summary>
    /// 把某一波的预告路线集合压成稳定签名，用来和上一波比较。
    /// 只有当签名不同，才说明玩家看到的路线真的发生了变化。
    /// </summary>
    private string BuildWaveRouteSignature(int waveIndex, int spawnGateSequenceAtWaveStart)
    {
        if (waveIndex < 0 || waveIndex >= waves.Length)
        {
            return string.Empty;
        }

        List<int> instanceIds = new List<int>();

        if (_battlefieldMap != null && _battlefieldMap.HasAnyValidSpawnGate())
        {
            WaveDefinition wave = waves[waveIndex];
            for (int enemyIndex = 0; enemyIndex < wave.enemyCount; enemyIndex++)
            {
                int gateSequence = spawnGateSequenceAtWaveStart + enemyIndex;
                if (!_battlefieldMap.TryGetSpawnGateBySequence(gateSequence, out EnemySpawnGate spawnGate) || spawnGate == null)
                {
                    continue;
                }

                EnemyPath path = spawnGate.EnemyPath;
                if (path == null)
                {
                    continue;
                }

                int instanceId = path.GetInstanceID();
                if (!instanceIds.Contains(instanceId))
                {
                    instanceIds.Add(instanceId);
                }
            }
        }
        else if (_fallbackEnemyPath != null)
        {
            instanceIds.Add(_fallbackEnemyPath.GetInstanceID());
        }

        instanceIds.Sort();
        return string.Join("|", instanceIds);
    }
}
