using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Editor
{
    /// <summary>
    /// A dedicated authoring window for multi-gate / multi-defense-point map topology.
    ///
    /// Why this window exists:
    /// 1. Once a level has 3+ spawn gates, the default Inspector no longer gives a comfortable
    ///    overview of "which gate drives which path and which core does it attack?".
    /// 2. Later levels now use multiple defense points, so level difficulty depends on topology
    ///    relationships, not just on raw coordinates.
    /// 3. The user asked for a dedicated editor instead of continuing to wire these links by
    ///    manually clicking through scattered scene objects.
    ///
    /// This window deliberately works directly on scene-authored objects rather than creating an
    /// extra data layer. That keeps the workflow aligned with the rest of the project:
    /// Scene view and explicit scene references remain the source of truth.
    /// </summary>
    public sealed class LevelTopologyEditorWindow : EditorWindow
    {
        [SerializeField] private BattlefieldMapDefinition targetMap;
        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private bool autoAdoptActiveScene = true;
        [SerializeField] private bool sortEntriesByName = true;
        [SerializeField] private bool showPathSummaries = true;
        [SerializeField] private bool showTargetUsageMatrix = true;

        [MenuItem("Tools/Tower Defense/Authoring/Level Topology Editor")]
        public static void OpenWindow()
        {
            LevelTopologyEditorWindow window = GetWindow<LevelTopologyEditorWindow>("Level Topology");
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            TryAdoptActiveSceneMap();
        }

        private void OnHierarchyChange()
        {
            Repaint();
        }

        private void OnSelectionChange()
        {
            if (autoAdoptActiveScene)
            {
                TryAdoptActiveSceneMap();
            }

            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (targetMap == null)
            {
                EditorGUILayout.HelpBox(
                    "No BattlefieldMapDefinition is currently targeted. Open a level scene or assign the map root manually.",
                    MessageType.Info);
                return;
            }

            Scene scene = targetMap.gameObject.scene;
            if (!scene.IsValid())
            {
                EditorGUILayout.HelpBox("The selected BattlefieldMapDefinition does not belong to a valid scene.", MessageType.Warning);
                return;
            }

            List<EnemySpawnGate> spawnGates = CollectSpawnGates(scene);
            List<DefensePointFlag> defensePoints = CollectDefensePoints(scene);
            List<EnemyPath> enemyPaths = CollectEnemyPaths(scene);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawSceneSummary(scene, spawnGates, defensePoints, enemyPaths);
            DrawTopologyActions(scene, spawnGates, defensePoints);
            DrawDefensePointSection(defensePoints, spawnGates);
            DrawSpawnGateSection(spawnGates, defensePoints, enemyPaths);

            if (showPathSummaries)
            {
                DrawPathSection(enemyPaths, spawnGates);
            }

            if (showTargetUsageMatrix)
            {
                DrawTargetUsageMatrix(defensePoints, spawnGates);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Scene Ownership", EditorStyles.boldLabel);
                targetMap = (BattlefieldMapDefinition)EditorGUILayout.ObjectField("Target Map", targetMap, typeof(BattlefieldMapDefinition), true);
                autoAdoptActiveScene = EditorGUILayout.Toggle("Auto Adopt Active Scene", autoAdoptActiveScene);
                sortEntriesByName = EditorGUILayout.Toggle("Sort Entries By Name", sortEntriesByName);
                showPathSummaries = EditorGUILayout.Toggle("Show Path Summaries", showPathSummaries);
                showTargetUsageMatrix = EditorGUILayout.Toggle("Show Target Usage Matrix", showTargetUsageMatrix);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Adopt Active Scene"))
                    {
                        TryAdoptActiveSceneMap(force: true);
                    }

                    if (GUILayout.Button("Collect Scene References"))
                    {
                        CollectSceneReferencesOnTargetMap();
                    }

                    if (GUILayout.Button("Ping Map Root") && targetMap != null)
                    {
                        Selection.activeObject = targetMap.gameObject;
                        EditorGUIUtility.PingObject(targetMap.gameObject);
                    }
                }
            }
        }

        private void DrawSceneSummary(Scene scene, List<EnemySpawnGate> spawnGates, List<DefensePointFlag> defensePoints, List<EnemyPath> enemyPaths)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Topology Summary", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Scene", string.IsNullOrWhiteSpace(scene.name) ? "(Unnamed Scene)" : scene.name);
                EditorGUILayout.LabelField("Scene Path", string.IsNullOrWhiteSpace(scene.path) ? "(Unsaved)" : scene.path);
                EditorGUILayout.LabelField("Spawn Gates", spawnGates.Count.ToString());
                EditorGUILayout.LabelField("Defense Points", defensePoints.Count.ToString());
                EditorGUILayout.LabelField("Enemy Paths", enemyPaths.Count.ToString());
                EditorGUILayout.LabelField("Configured Gates", spawnGates.Count(gate => gate != null && gate.IsConfigured).ToString());
            }
        }

        private void DrawTopologyActions(Scene scene, List<EnemySpawnGate> spawnGates, List<DefensePointFlag> defensePoints)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Topology Actions", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Apply Current Gate Order To Map"))
                    {
                        ApplyMapArrays(spawnGates, defensePoints);
                    }

                    if (GUILayout.Button("Sort Gates By Name + Apply"))
                    {
                        ApplyMapArrays(
                            spawnGates.OrderBy(gate => gate.name, StringComparer.Ordinal).ToList(),
                            defensePoints);
                    }

                    if (GUILayout.Button("Sort Defense Points By Name + Apply"))
                    {
                        ApplyMapArrays(
                            spawnGates,
                            defensePoints.OrderBy(point => point.name, StringComparer.Ordinal).ToList());
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Spawn Gate"))
                    {
                        CreateSpawnGate(scene);
                    }

                    if (GUILayout.Button("Create Defense Point"))
                    {
                        CreateDefensePoint(scene);
                    }

                    if (GUILayout.Button("Create Enemy Path"))
                    {
                        CreateEnemyPath(scene);
                    }
                }
            }
        }

        private void DrawDefensePointSection(List<DefensePointFlag> defensePoints, List<EnemySpawnGate> spawnGates)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Defense Points", EditorStyles.boldLabel);

                foreach (DefensePointFlag defensePoint in SortIfNeeded(defensePoints))
                {
                    if (defensePoint == null)
                    {
                        continue;
                    }

                    int inboundGateCount = spawnGates.Count(gate => gate != null && gate.TargetDefensePoint == defensePoint);

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField("Scene Object", defensePoint, typeof(DefensePointFlag), true);
                            DrawSelectAndPingButtons(defensePoint.gameObject);
                        }

                        SerializedObject serializedDefensePoint = new SerializedObject(defensePoint);
                        DrawStringProperty(serializedDefensePoint, "pointId", "Point Id");
                        DrawStringProperty(serializedDefensePoint, "displayName", "Display Name");
                        serializedDefensePoint.ApplyModifiedPropertiesWithoutUndo();

                        EditorGUILayout.LabelField("Inbound Gates", inboundGateCount.ToString());
                        EditorGUILayout.Vector3Field("World Position", defensePoint.WorldPosition);
                    }
                }
            }
        }

        private void DrawSpawnGateSection(List<EnemySpawnGate> spawnGates, List<DefensePointFlag> defensePoints, List<EnemyPath> enemyPaths)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Spawn Gates", EditorStyles.boldLabel);

                foreach (EnemySpawnGate spawnGate in SortIfNeeded(spawnGates))
                {
                    if (spawnGate == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField("Scene Object", spawnGate, typeof(EnemySpawnGate), true);
                            DrawSelectAndPingButtons(spawnGate.gameObject);
                        }

                        SerializedObject serializedGate = new SerializedObject(spawnGate);
                        DrawStringProperty(serializedGate, "gateId", "Gate Id");
                        DrawStringProperty(serializedGate, "displayName", "Display Name");
                        DrawObjectProperty(serializedGate, "enemyPathReference", "Enemy Path", typeof(EnemyPath));
                        DrawObjectProperty(serializedGate, "targetDefensePointReference", "Target Defense Point", typeof(DefensePointFlag));
                        serializedGate.ApplyModifiedPropertiesWithoutUndo();

                        EnemyPath connectedPath = spawnGate.EnemyPath;
                        DefensePointFlag targetDefense = spawnGate.TargetDefensePoint;
                        EditorGUILayout.LabelField("Configured", spawnGate.IsConfigured ? "Yes" : "No");
                        EditorGUILayout.LabelField("Path Waypoint Count", connectedPath != null ? EnemyPathAuthoringUtility.GetWaypointChildren(connectedPath).Count.ToString() : "0");
                        EditorGUILayout.LabelField("Target Core", targetDefense != null ? targetDefense.DisplayName : "(None)");

                        if (connectedPath != null && !enemyPaths.Contains(connectedPath))
                        {
                            EditorGUILayout.HelpBox("This gate points at a path that is not part of the current scene path set.", MessageType.Warning);
                        }

                        if (targetDefense != null && !defensePoints.Contains(targetDefense))
                        {
                            EditorGUILayout.HelpBox("This gate points at a defense point that is not part of the current scene defense-point set.", MessageType.Warning);
                        }
                    }
                }
            }
        }

        private void DrawPathSection(List<EnemyPath> enemyPaths, List<EnemySpawnGate> spawnGates)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Enemy Paths", EditorStyles.boldLabel);

                foreach (EnemyPath enemyPath in SortIfNeeded(enemyPaths))
                {
                    if (enemyPath == null)
                    {
                        continue;
                    }

                    List<Transform> waypoints = EnemyPathAuthoringUtility.GetWaypointChildren(enemyPath);
                    string gateNames = string.Join(", ", spawnGates
                        .Where(gate => gate != null && gate.EnemyPath == enemyPath)
                        .Select(gate => gate.DisplayName));

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField("Scene Object", enemyPath, typeof(EnemyPath), true);
                            DrawSelectAndPingButtons(enemyPath.gameObject);
                        }

                        EditorGUILayout.LabelField("Waypoint Count", waypoints.Count.ToString());
                        EditorGUILayout.LabelField("Used By Gates", string.IsNullOrWhiteSpace(gateNames) ? "(None)" : gateNames);

                        if (waypoints.Count > 0)
                        {
                            EditorGUILayout.Vector3Field("Start", waypoints[0].position);
                            EditorGUILayout.Vector3Field("End", waypoints[waypoints.Count - 1].position);
                        }
                    }
                }
            }
        }

        private void DrawTargetUsageMatrix(List<DefensePointFlag> defensePoints, List<EnemySpawnGate> spawnGates)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Target Usage Matrix", EditorStyles.boldLabel);

                foreach (DefensePointFlag defensePoint in SortIfNeeded(defensePoints))
                {
                    if (defensePoint == null)
                    {
                        continue;
                    }

                    string gateSummary = string.Join(", ", spawnGates
                        .Where(gate => gate != null && gate.TargetDefensePoint == defensePoint)
                        .Select(gate => gate.DisplayName));

                    EditorGUILayout.LabelField(
                        defensePoint.DisplayName,
                        string.IsNullOrWhiteSpace(gateSummary) ? "(No gates currently target this point)" : gateSummary);
                }
            }
        }

        private void CollectSceneReferencesOnTargetMap()
        {
            if (targetMap == null)
            {
                return;
            }

            Undo.RecordObject(targetMap, "Collect Map Scene References");
            targetMap.CollectSceneReferences();
            EditorUtility.SetDirty(targetMap);
            EditorSceneManager.MarkSceneDirty(targetMap.gameObject.scene);
        }

        private void ApplyMapArrays(IReadOnlyList<EnemySpawnGate> spawnGates, IReadOnlyList<DefensePointFlag> defensePoints)
        {
            if (targetMap == null)
            {
                return;
            }

            SerializedObject serializedMap = new SerializedObject(targetMap);
            SerializedProperty spawnGatesProperty = serializedMap.FindProperty("spawnGates");
            SerializedProperty defensePointsProperty = serializedMap.FindProperty("defensePoints");

            if (spawnGatesProperty != null)
            {
                spawnGatesProperty.arraySize = spawnGates.Count;
                for (int index = 0; index < spawnGates.Count; index++)
                {
                    spawnGatesProperty.GetArrayElementAtIndex(index).objectReferenceValue = spawnGates[index];
                }
            }

            if (defensePointsProperty != null)
            {
                defensePointsProperty.arraySize = defensePoints.Count;
                for (int index = 0; index < defensePoints.Count; index++)
                {
                    defensePointsProperty.GetArrayElementAtIndex(index).objectReferenceValue = defensePoints[index];
                }
            }

            serializedMap.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetMap);
            EditorSceneManager.MarkSceneDirty(targetMap.gameObject.scene);
        }

        private void CreateSpawnGate(Scene scene)
        {
            GameObject gateObject = new GameObject(BuildNextIndexedName(scene, "SpawnGate_"));
            Undo.RegisterCreatedObjectUndo(gateObject, "Create Spawn Gate");
            SceneManager.MoveGameObjectToScene(gateObject, scene);
            gateObject.transform.SetParent(targetMap != null ? targetMap.transform : null, false);
            gateObject.transform.position = targetMap != null ? targetMap.transform.position : Vector3.zero;

            EnemySpawnGate spawnGate = gateObject.AddComponent<EnemySpawnGate>();
            SerializedObject serializedGate = new SerializedObject(spawnGate);
            DrawStringPropertyImmediately(serializedGate, "gateId", gateObject.name);
            DrawStringPropertyImmediately(serializedGate, "displayName", gateObject.name);

            EnemyPath firstPath = CollectEnemyPaths(scene).FirstOrDefault();
            DefensePointFlag firstDefense = CollectDefensePoints(scene).FirstOrDefault();
            SetObjectProperty(serializedGate, "enemyPathReference", firstPath);
            SetObjectProperty(serializedGate, "targetDefensePointReference", firstDefense);
            serializedGate.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = gateObject;
            EditorGUIUtility.PingObject(gateObject);
            CollectSceneReferencesOnTargetMap();
        }

        private void CreateDefensePoint(Scene scene)
        {
            GameObject defenseObject = new GameObject(BuildNextIndexedName(scene, "DefensePoint_"));
            Undo.RegisterCreatedObjectUndo(defenseObject, "Create Defense Point");
            SceneManager.MoveGameObjectToScene(defenseObject, scene);
            defenseObject.transform.SetParent(targetMap != null ? targetMap.transform : null, false);
            defenseObject.transform.position = targetMap != null ? targetMap.transform.position : Vector3.zero;

            DefensePointFlag defensePoint = defenseObject.AddComponent<DefensePointFlag>();
            SerializedObject serializedDefensePoint = new SerializedObject(defensePoint);
            DrawStringPropertyImmediately(serializedDefensePoint, "pointId", defenseObject.name);
            DrawStringPropertyImmediately(serializedDefensePoint, "displayName", defenseObject.name);
            serializedDefensePoint.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = defenseObject;
            EditorGUIUtility.PingObject(defenseObject);
            CollectSceneReferencesOnTargetMap();
        }

        private void CreateEnemyPath(Scene scene)
        {
            GameObject pathObject = new GameObject(BuildNextIndexedName(scene, "EnemyPath_"));
            Undo.RegisterCreatedObjectUndo(pathObject, "Create Enemy Path");
            SceneManager.MoveGameObjectToScene(pathObject, scene);
            pathObject.transform.SetParent(targetMap != null ? targetMap.transform : null, false);
            pathObject.transform.position = targetMap != null ? targetMap.transform.position : Vector3.zero;

            EnemyPath enemyPath = pathObject.AddComponent<EnemyPath>();
            EnemyPathAuthoringUtility.EnsureWaypointRoot(enemyPath);

            Selection.activeObject = pathObject;
            EditorGUIUtility.PingObject(pathObject);
        }

        private void TryAdoptActiveSceneMap(bool force = false)
        {
            if (!force && !autoAdoptActiveScene && targetMap != null)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return;
            }

            BattlefieldMapDefinition sceneMap = TowerDefenseMapToolkitUtility.FindFirstComponentInScene<BattlefieldMapDefinition>(activeScene);
            if (sceneMap != null)
            {
                targetMap = sceneMap;
            }
        }

        private static List<EnemySpawnGate> CollectSpawnGates(Scene scene)
        {
            IEnumerable<EnemySpawnGate> gates = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnemySpawnGate>(true))
                .Where(gate => gate != null)
                .Distinct();

            return gates.OrderBy(gate => gate.name, StringComparer.Ordinal).ToList();
        }

        private static List<DefensePointFlag> CollectDefensePoints(Scene scene)
        {
            IEnumerable<DefensePointFlag> defensePoints = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DefensePointFlag>(true))
                .Where(point => point != null)
                .Distinct();

            return defensePoints.OrderBy(point => point.name, StringComparer.Ordinal).ToList();
        }

        private static List<EnemyPath> CollectEnemyPaths(Scene scene)
        {
            IEnumerable<EnemyPath> paths = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnemyPath>(true))
                .Where(path => path != null)
                .Distinct();

            return paths.OrderBy(path => path.name, StringComparer.Ordinal).ToList();
        }

        private IEnumerable<T> SortIfNeeded<T>(IEnumerable<T> source) where T : UnityEngine.Object
        {
            if (source == null)
            {
                return Array.Empty<T>();
            }

            return sortEntriesByName
                ? source.OrderBy(item => item != null ? item.name : string.Empty, StringComparer.Ordinal).ToList()
                : source.ToList();
        }

        private static void DrawSelectAndPingButtons(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            if (GUILayout.Button("Select", GUILayout.Width(62f)))
            {
                Selection.activeObject = gameObject;
            }

            if (GUILayout.Button("Ping", GUILayout.Width(52f)))
            {
                EditorGUIUtility.PingObject(gameObject);
            }
        }

        private static void DrawStringProperty(SerializedObject serializedObject, string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
        }

        private static void DrawStringPropertyImmediately(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void DrawObjectProperty(SerializedObject serializedObject, string propertyName, string label, Type objectType)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
        }

        private static void SetObjectProperty(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static string BuildNextIndexedName(Scene scene, string prefix)
        {
            int index = 1;
            while (true)
            {
                string candidateName = $"{prefix}{index:D2}";
                if (TowerDefenseMapToolkitUtility.FindObjectByName(scene, candidateName) == null)
                {
                    return candidateName;
                }

                index++;
            }
        }
    }
}
