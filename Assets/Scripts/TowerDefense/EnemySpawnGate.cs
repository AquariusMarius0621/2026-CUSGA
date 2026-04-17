using UnityEngine;

/// <summary>
/// `EnemySpawnGate` 表示地图上的一个出怪口旗帜。
///
/// 新玩法要求“地图中需要有数量大于一的出怪口通向特定的防御点”，
/// 所以这里不再把刷怪入口只当成 `WaveSpawner` 里的一条路径引用，
/// 而是把它提升成一个独立的场景对象。
///
/// 当前这个脚本先承担三件事：
/// 1. 标记这里是一个出怪口
/// 2. 显式关联它使用的 `EnemyPath`
/// 3. 显式关联它通向的 `DefensePointFlag`
/// </summary>
public sealed class EnemySpawnGate : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string gateId = "Gate01";
    [SerializeField] private string displayName = "Spawn Gate";

    [Header("Route")]
    [SerializeField] private EnemyPath enemyPathReference;
    [SerializeField] private DefensePointFlag targetDefensePointReference;

    [Header("Scene Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.4f, 0.22f, 1f);
    [SerializeField] private float gizmoRadius = 0.28f;

    public string GateId => string.IsNullOrWhiteSpace(gateId) ? name : gateId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? GateId : displayName;
    public bool IsConfigured => enemyPathReference != null;
    public EnemyPath EnemyPath => enemyPathReference;
    public DefensePointFlag TargetDefensePoint => targetDefensePointReference;

    /// <summary>
    /// 返回敌人的出生位置。
    /// 优先使用路径首点；如果暂时没配路径，就回退到出怪口自身坐标，方便场景调试。
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return enemyPathReference != null ? enemyPathReference.GetSpawnPosition() : transform.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius * 1.6f);

        if (targetDefensePointReference != null)
        {
            Gizmos.DrawLine(transform.position, targetDefensePointReference.WorldPosition);
        }
    }
}