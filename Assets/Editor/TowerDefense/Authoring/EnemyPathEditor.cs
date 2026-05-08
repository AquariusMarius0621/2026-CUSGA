using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowerDefense.Editor
{
    /// <summary>
    /// `EnemyPath` 的自定义检查器。
    ///
    /// 这份 Inspector 现在承担三件面向作者的事：
    /// 1. 明确显示当前路径点根节点和路径点数量。
    /// 2. 提供显式入口去创建 / 接管 `Waypoints` 根节点。
    /// 3. 作为路径制作工具和 Scene 视图快捷操作的桥接入口。
    ///
    /// 这样作者在 Unity 里既能用传统 Inspector 工作流，也能快速切换到更强的路径制作台。
    /// </summary>
    [CustomEditor(typeof(EnemyPath))]
    public sealed class EnemyPathEditor : UnityEditor.Editor
    {
        private SerializedProperty _waypointRootReferenceProperty; // 中文：路径点根节点引用 Property

        private void OnEnable()
        {
            _waypointRootReferenceProperty = serializedObject.FindProperty("waypointRootReference");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EnemyPath enemyPath = (EnemyPath)target;

            string waypointRootName = enemyPath.WaypointRoot != null ? enemyPath.WaypointRoot.name : "(Direct Children)";
            bool proceduralOverlay = serializedObject.FindProperty("proceduralReadabilityOverlay")?.boolValue ?? true;
            string readabilityMode = proceduralOverlay ? "程序化覆盖层" : "仅使用作者根节点";
            EditorGUILayout.HelpBox($"路径点数量：{enemyPath.WaypointCount}\n路径点根节点：{waypointRootName}\n可读性模式：{readabilityMode}", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("指定 / 创建 Waypoints 根节点"))
            {
                AssignOrCreateWaypointRoot(enemyPath);
                serializedObject.Update();
            }

            if (GUILayout.Button("打开路径制作工具"))
            {
                EnemyPathAuthoringTool.OpenWindow(enemyPath);
            }

            if (GUILayout.Button("刷新路径表现"))
            {
                enemyPath.EditorRefreshAuthoringState();
                EditorUtility.SetDirty(enemyPath);
                EditorSceneManager.MarkSceneDirty(enemyPath.gameObject.scene);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);

            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            EnemyPath enemyPath = (EnemyPath)target;
            if (enemyPath == null)
            {
                return;
            }

            List<Transform> waypoints = EnemyPathAuthoringUtility.GetWaypointChildren(enemyPath);

            Handles.color = new Color(0.98f, 0.84f, 0.32f, 1f);
            for (int index = 0; index < waypoints.Count; index++)
            {
                Transform waypoint = waypoints[index];
                if (waypoint == null)
                {
                    continue;
                }

                Handles.Label(waypoint.position + Vector3.up * 0.28f, $"#{index + 1:D2}");
            }

            Transform activeWaypoint = Selection.activeTransform != null && waypoints.Contains(Selection.activeTransform)
                ? Selection.activeTransform
                : null;
            List<Transform> selectedExistingWaypoints = EnemyPathAuthoringUtility.GetSelectedExistingWaypoints(enemyPath);
            List<Transform> selectedNewWaypoints = EnemyPathAuthoringUtility.GetSelectedNewWaypointCandidates(enemyPath);
            Transform secondarySelectedWaypoint = selectedExistingWaypoints.Count == 2
                ? selectedExistingWaypoints.Find(point => point != activeWaypoint)
                : null;

            if (activeWaypoint == null && selectedNewWaypoints.Count == 0 && selectedExistingWaypoints.Count != 2)
            {
                return;
            }

            Vector2 guiAnchor = activeWaypoint != null
                ? HandleUtility.WorldToGUIPoint(activeWaypoint.position)
                : new Vector2(48f, 72f);

            Handles.BeginGUI();
            Rect panelRect = new Rect(guiAnchor.x + 18f, guiAnchor.y - 6f, 248f, 132f);
            GUILayout.BeginArea(panelRect, "Path Quick Fix", "Window");

            using (new EditorGUI.DisabledScope(activeWaypoint == null || secondarySelectedWaypoint == null))
            {
                if (GUILayout.Button("把激活点插到另一个点后面"))
                {
                    if (EnemyPathAuthoringUtility.TryMoveExistingWaypointAfter(enemyPath, activeWaypoint, secondarySelectedWaypoint, true))
                    {
                        enemyPath.EditorRefreshAuthoringState();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(selectedExistingWaypoints.Count != 2))
            {
                if (GUILayout.Button("交换这两个点"))
                {
                    if (EnemyPathAuthoringUtility.TrySwapSelectedWaypoints(enemyPath, true))
                    {
                        enemyPath.EditorRefreshAuthoringState();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(activeWaypoint == null || selectedNewWaypoints.Count == 0))
            {
                if (GUILayout.Button("已选点插到当前点后面"))
                {
                    EnemyPathAuthoringTool.OpenWindow(enemyPath);
                    if (EnemyPathAuthoringUtility.TryInsertSelectedCandidatesAfter(
                        enemyPath,
                        activeWaypoint,
                        EnemyPathPointOrderMode.HierarchyOrder,
                        activeWaypoint,
                        true))
                    {
                        enemyPath.EditorRefreshAuthoringState();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(selectedNewWaypoints.Count == 0))
            {
                if (GUILayout.Button("已选点插到末尾"))
                {
                    EnemyPathAuthoringTool.OpenWindow(enemyPath);
                    if (EnemyPathAuthoringUtility.TryAppendSelectedCandidatesToEnd(
                        enemyPath,
                        EnemyPathPointOrderMode.HierarchyOrder,
                        activeWaypoint,
                        true))
                    {
                        enemyPath.EditorRefreshAuthoringState();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(activeWaypoint == null))
            {
                if (GUILayout.Button("当前点移到末尾"))
                {
                    if (EnemyPathAuthoringUtility.TryMoveWaypointToEnd(enemyPath, activeWaypoint, true))
                    {
                        enemyPath.EditorRefreshAuthoringState();
                    }
                }
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void AssignOrCreateWaypointRoot(EnemyPath enemyPath)
        {
            Transform existingRoot = EnemyPathAuthoringUtility.EnsureWaypointRoot(enemyPath);
            if (existingRoot == null)
            {
                return;
            }

            for (int childIndex = enemyPath.transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = enemyPath.transform.GetChild(childIndex);
                if (child == null || child == existingRoot || child.name == "__PathReadability")
                {
                    continue;
                }

                Undo.SetTransformParent(child, existingRoot, "把路径点移入 Waypoints 根节点");
            }

            _waypointRootReferenceProperty.objectReferenceValue = existingRoot;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            enemyPath.EditorRefreshAuthoringState();
            EnemyPathAuthoringUtility.MarkSceneDirty(enemyPath);
        }
    }
}
