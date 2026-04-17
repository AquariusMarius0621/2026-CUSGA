using UnityEngine;

/// <summary>
/// `DefensePointFlag` 表示地图中的一个防御点旗帜。
///
/// 当前版本通常还是单防御点共享生命值，
/// 但文档已经明确要求后续地图必须允许扩展到多个防御点，
/// 所以这里先把“防御点”做成显式场景对象。
///
/// 这样后面无论是做多终点地图，还是让不同路线指向不同终点，
/// 都不需要再把地图结构从头改一遍。
/// </summary>
public sealed class DefensePointFlag : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string pointId = "Core";
    [SerializeField] private string displayName = "Defense Point";

    [Header("Scene Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(0.15f, 0.9f, 1f, 1f);
    [SerializeField] private float gizmoRadius = 0.35f;

    public string PointId => string.IsNullOrWhiteSpace(pointId) ? name : pointId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? PointId : displayName;
    public Vector3 WorldPosition => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius * 1.6f);
    }
}