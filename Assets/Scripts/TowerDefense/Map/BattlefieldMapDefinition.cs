using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// `BattlefieldMapDefinition` 是阶段 A 引入的地图总配置入口。
///
/// 它当前先解决一个很基础但很重要的问题：
/// 把“这张地图有哪些出怪口、有哪些防御点、允许放多少个继电器”
/// 从旧原型里的隐式约定，收口成一个显式场景组件。
///
/// 这样后续做供电系统、地图校验和多出怪口刷怪时，
/// 就能围绕同一个地图对象继续长，而不是各自去猜场景结构。
/// </summary>
public sealed class BattlefieldMapDefinition : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private BuildZone buildZoneReference;
    [SerializeField] private EnemySpawnGate[] spawnGates = new EnemySpawnGate[0];
    [SerializeField] private DefensePointFlag[] defensePoints = new DefensePointFlag[0];

    [Header("Gameplay Limits")]
    [SerializeField] private int relayLimit = 4;

    public BuildZone BuildZone => buildZoneReference;
    public int RelayLimit => Mathf.Max(0, relayLimit);
    public int SpawnGateCount => CollectValidSpawnGates(null);
    public int DefensePointCount => CollectValidDefensePoints(null);

    /// <summary>
    /// 当前地图的可读摘要。
    /// 主要用于启动日志和场景契约检查，帮助我们快速知道这一关的骨架到底有没有接好。
    /// </summary>
    public string BuildDebugSummary()
    {
        return $"BuildZone={(buildZoneReference != null ? buildZoneReference.name : "None")}, SpawnGates={SpawnGateCount}, DefensePoints={DefensePointCount}, RelayLimit={RelayLimit}";
    }

    public string BuildAuthoringSummary()
    {
        return BuildDebugSummary();
    }

    /// <summary>
    /// 把这张地图当前最明显的配置缺口打印成警告。
    ///
    /// 阶段 A 的目标不是立刻让所有规则完整运行，
    /// 但至少要让“地图骨架哪里没接好”变得可见，避免后面排查时还要先猜场景是否缺对象。
    /// </summary>
    public void LogConfigurationWarnings(Object context)
    {
        if (buildZoneReference == null)
        {
            Debug.LogWarning("BattlefieldMapDefinition is missing BuildZone reference.", context);
        }

        if (SpawnGateCount == 0)
        {
            Debug.LogWarning("BattlefieldMapDefinition has no valid EnemySpawnGate configured.", context);
        }

        if (DefensePointCount == 0)
        {
            Debug.LogWarning("BattlefieldMapDefinition has no DefensePointFlag configured.", context);
        }
    }

    public bool HasAnyValidSpawnGate()
    {
        return SpawnGateCount > 0;
    }

    public bool TryGetSpawnGateBySequence(int sequenceIndex, out EnemySpawnGate spawnGate)
    {
        List<EnemySpawnGate> validSpawnGates = new List<EnemySpawnGate>();
        CollectValidSpawnGates(validSpawnGates);

        if (validSpawnGates.Count == 0)
        {
            spawnGate = null;
            return false;
        }

        int normalizedIndex = Mathf.Abs(sequenceIndex) % validSpawnGates.Count;
        spawnGate = validSpawnGates[normalizedIndex];
        return spawnGate != null;
    }

    public bool TryGetPrimaryDefensePoint(out DefensePointFlag defensePoint)
    {
        List<DefensePointFlag> validDefensePoints = new List<DefensePointFlag>();
        CollectValidDefensePoints(validDefensePoints);

        if (validDefensePoints.Count == 0)
        {
            defensePoint = null;
            return false;
        }

        defensePoint = validDefensePoints[0];
        return true;
    }

    private void OnValidate()
    {
        relayLimit = Mathf.Max(0, relayLimit);
    }

    public bool CollectSceneReferences()
    {
        return false;
    }

    private int CollectValidSpawnGates(List<EnemySpawnGate> output)
    {
        int count = 0;
        if (spawnGates == null)
        {
            return 0;
        }

        for (int i = 0; i < spawnGates.Length; i++)
        {
            EnemySpawnGate spawnGate = spawnGates[i];
            if (spawnGate == null || !spawnGate.IsConfigured)
            {
                continue;
            }

            count++;
            output?.Add(spawnGate);
        }

        return count;
    }

    private int CollectValidDefensePoints(List<DefensePointFlag> output)
    {
        int count = 0;
        if (defensePoints == null)
        {
            return 0;
        }

        HashSet<DefensePointFlag> deduplicatedPoints = new HashSet<DefensePointFlag>();
        for (int i = 0; i < defensePoints.Length; i++)
        {
            DefensePointFlag defensePoint = defensePoints[i];
            if (defensePoint == null || !deduplicatedPoints.Add(defensePoint))
            {
                continue;
            }

            count++;
            output?.Add(defensePoint);
        }

        return count;
    }
}
