using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyPath 用来保存敌人需要经过的一整条路线。
///
/// 这里采用的是一种非常适合原型和关卡编辑的做法：
/// - 场景里放一个父对象 EnemyPath
/// - 在它下面摆若干子节点作为路径点
/// - 敌人按这些子节点的顺序依次前进
///
/// 这种设计把“路径数据”放在场景层面管理，而不是硬编码在脚本里。
/// 好处很明显：
/// - 调整路线时不需要改代码
/// - 关卡设计可以直接在 Scene 里拖点位
/// - 敌人逻辑与地图路线配置天然解耦
///
/// 对于塔防原型来说，这是一种非常直观、维护成本也很低的方案。
/// </summary>
public class EnemyPath : MonoBehaviour
{
    /// <summary>
    /// 当前路径上缓存的所有路径点。
    ///
    /// 它们来自该对象的直接子节点，顺序与 Hierarchy 中的排列顺序一致。
    /// 敌人会按照这个列表依次移动。
    /// </summary>
    private readonly List<Transform> _waypoints = new List<Transform>();

    /// <summary>
    /// 当前路径点数量。
    /// </summary>
    public int WaypointCount => _waypoints.Count;

    /// <summary>
    /// 在运行时初始化路径点缓存。
    /// </summary>
    private void Awake()
    {
        CacheWaypoints();
    }

    /// <summary>
    /// 当 Inspector 中的值或子物体结构变化时，在编辑器中重新整理路径点缓存。
    ///
    /// 这样你在场景里拖动、增删或重排路径点后，
    /// 路径数据能尽量保持与当前层级结构一致。
    /// </summary>
    private void OnValidate()
    {
        CacheWaypoints();
    }

    /// <summary>
    /// 获取敌人的出生位置。
    ///
    /// 默认取第一个路径点的位置作为出生点；
    /// 如果当前没有任何路径点，则退回到 EnemyPath 对象自身的位置。
    /// 这种回退策略能在配置不完整时提供一个可预期结果。
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        if (_waypoints.Count == 0)
        {
            return transform.position;
        }

        return _waypoints[0].position;
    }

    /// <summary>
    /// 获取指定索引路径点的世界坐标。
    /// </summary>
    public Vector3 GetWaypointPosition(int index)
    {
        return _waypoints[index].position;
    }

    /// <summary>
    /// 重新扫描当前对象的子节点，并用它们刷新路径点列表。
    ///
    /// 这里不做复杂筛选，而是把所有直接子节点都视为路径点。
    /// 好处是规则简单、使用成本低；代价是你需要自己保证该父对象下不要混入无关节点。
    /// 对当前原型规模来说，这是一个很合理的取舍。
    /// </summary>
    private void CacheWaypoints()
    {
        _waypoints.Clear();

        foreach (Transform child in transform)
        {
            _waypoints.Add(child);
        }
    }

    /// <summary>
    /// 在 Scene 视图中绘制路线辅助图形。
    ///
    /// Gizmos 是 Unity 编辑器里非常常见的调试与可视化工具。
    /// 这里我们用它来：
    /// - 在每个路径点位置画一个球体
    /// - 在相邻路径点之间画一条连线
    ///
    /// 这样你不用进入 Play 模式，也能立刻检查路线顺序和整体走向是否正确。
    /// 对调试关卡数据特别有帮助。
    /// </summary>
    private void OnDrawGizmos()
    {
        CacheWaypoints();
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 1f);

        for (int i = 0; i < _waypoints.Count; i++)
        {
            Transform waypoint = _waypoints[i];
            if (waypoint == null)
            {
                continue;
            }

            Gizmos.DrawSphere(waypoint.position, 0.12f);

            if (i < _waypoints.Count - 1 && _waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoint.position, _waypoints[i + 1].position);
            }
        }
    }
}
