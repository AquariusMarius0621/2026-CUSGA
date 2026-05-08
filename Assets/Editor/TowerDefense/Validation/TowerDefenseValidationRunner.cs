using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerDefense.Editor
{
    /// <summary>
    /// Runs automated structural checks across the shared prototype scenes.
    ///
    /// Why this validator exists:
    /// 1. `SampleScene` is still the gameplay shell template for later maps.
    /// 2. `Level02 ~ Level04` now diverge in route topology and gate / defense-point counts.
    /// 3. Once scenes become larger and more hand-authored, "looks okay in Scene view" is no
    ///    longer enough to trust the map contract.
    ///
    /// The validator therefore checks two layers:
    /// - shared shell integrity (prefabs, menu wiring, prototype scene references)
    /// - per-level topology integrity (gate counts, defense-point counts, route alignment)
    /// </summary>
    public static class TowerDefenseValidationRunner
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string Level02ScenePath = "Assets/Scenes/Level02.unity";
        private const string Level03ScenePath = "Assets/Scenes/Level03.unity";
        private const string Level04ScenePath = "Assets/Scenes/Level04.unity";

        private const string RelayPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/RelayTowerPrototype.prefab";
        private const string SingleTargetPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/SingleTargetTowerPrototype.prefab";
        private const string SlowFieldPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/SlowFieldTowerPrototype.prefab";
        private const string BombardPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/BombardTowerPrototype.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/EnemyPrototype.prefab";
        private const string ShotTracePrefabPath = "Assets/Prefabs/TowerDefense/Vfx/ShotTrace.prefab";
        private const string SlowPulsePrefabPath = "Assets/Prefabs/TowerDefense/Vfx/SlowPulse.prefab";
        private const string BombProjectilePrefabPath = "Assets/Prefabs/TowerDefense/Vfx/BombProjectile.prefab";
        private const string BombExplosionPrefabPath = "Assets/Prefabs/TowerDefense/Vfx/BombExplosion.prefab";

        private const float EndpointTolerance = 0.35f;
        private const float AlignmentTolerance = 0.45f;
        private const int AlignmentSamplesPerSpan = 10;

        private sealed class LevelSceneExpectation
        {
            public string ScenePath;
            public int RequiredSpawnGateCount;
            public int RequiredDefensePointCount;
            public int RequiredPathCount;
            public bool RequireWaveSpawner;
        }

        private static readonly LevelSceneExpectation[] LevelExpectations =
        {
            new LevelSceneExpectation
            {
                ScenePath = Level02ScenePath,
                RequiredSpawnGateCount = 2,
                RequiredDefensePointCount = 1,
                RequiredPathCount = 2,
                RequireWaveSpawner = true
            },
            new LevelSceneExpectation
            {
                ScenePath = Level03ScenePath,
                RequiredSpawnGateCount = 3,
                RequiredDefensePointCount = 1,
                RequiredPathCount = 3,
                RequireWaveSpawner = true
            },
            new LevelSceneExpectation
            {
                ScenePath = Level04ScenePath,
                RequiredSpawnGateCount = 4,
                RequiredDefensePointCount = 2,
                RequiredPathCount = 4,
                RequireWaveSpawner = true
            }
        };

        /// <summary>
        /// Batch-mode entry point used by CI-style checks and local one-shot verification.
        /// </summary>
        public static void RunAll()
        {
            try
            {
                ValidateMainMenuScene();
                ValidatePrefabAssets();
                ValidateSampleSceneStructure();
                ValidateRelayCoverageShape();
                ValidateAuthoredLevelScenes();
                Debug.Log("TowerDefenseValidationRunner: all automated checks passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"TowerDefenseValidationRunner failed: {exception}");
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Tiny batch entry used only to prove that the editor assembly can compile and load.
        ///
        /// We keep this separate from the full validator because map scenes can legitimately fail
        /// gameplay-contract checks while the editor tooling itself is still syntactically valid.
        /// </summary>
        public static void CompilationSmokeCheck()
        {
            Debug.Log("TowerDefenseValidationRunner: editor compilation smoke check loaded successfully.");
            EditorApplication.Exit(0);
        }

        private static void ValidateMainMenuScene()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            MainMenuController controller = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null)
            {
                throw new InvalidOperationException("MainMenu scene is missing MainMenuController.");
            }

            Canvas mainCanvas = GetPrivateField<Canvas>(controller, "mainCanvas");
            RectTransform menuRoot = GetPrivateField<RectTransform>(controller, "menuRoot");
            Image backgroundPanel = GetPrivateField<Image>(controller, "backgroundPanel");
            Button startButton = GetPrivateField<Button>(controller, "startButton");
            if (mainCanvas == null || menuRoot == null || backgroundPanel == null || startButton == null)
            {
                throw new InvalidOperationException("MainMenuController still has missing scene references.");
            }
        }

        private static void ValidatePrefabAssets()
        {
            GameObject relayPrefab = LoadRequiredPrefab(RelayPrefabPath);
            GameObject singleTargetPrefab = LoadRequiredPrefab(SingleTargetPrefabPath);
            GameObject slowFieldPrefab = LoadRequiredPrefab(SlowFieldPrefabPath);
            GameObject bombardPrefab = LoadRequiredPrefab(BombardPrefabPath);
            GameObject enemyPrefab = LoadRequiredPrefab(EnemyPrefabPath);

            LoadRequiredPrefab(ShotTracePrefabPath);
            LoadRequiredPrefab(SlowPulsePrefabPath);
            LoadRequiredPrefab(BombProjectilePrefabPath);
            LoadRequiredPrefab(BombExplosionPrefabPath);

            if (relayPrefab.GetComponent<RelayTower>() == null)
            {
                throw new InvalidOperationException("Relay runtime prefab is missing RelayTower.");
            }

            AssertTowerPrefabType(singleTargetPrefab, TowerType.SingleTarget, "single-target");
            AssertTowerPrefabType(slowFieldPrefab, TowerType.SlowField, "slow-field");
            AssertTowerPrefabType(bombardPrefab, TowerType.Bombard, "bombard");

            if (enemyPrefab.GetComponent<Enemy>() == null)
            {
                throw new InvalidOperationException("Enemy runtime prefab is missing Enemy.");
            }
        }

        private static void ValidateSampleSceneStructure()
        {
            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

            TowerDefenseGame gameController = UnityEngine.Object.FindFirstObjectByType<TowerDefenseGame>();
            WaveSpawner waveSpawner = UnityEngine.Object.FindFirstObjectByType<WaveSpawner>();
            DefenseTower prototype = UnityEngine.Object.FindObjectsByType<DefenseTower>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(tower => tower.name == "DefenseTowerPrototype");
            RelayTower relayPrototype = UnityEngine.Object.FindObjectsByType<RelayTower>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(relay => relay.name == "RelayTowerPrototype");

            if (gameController == null || waveSpawner == null || prototype == null || relayPrototype == null)
            {
                throw new InvalidOperationException("SampleScene is missing one or more runtime prototypes.");
            }

            Transform feedbackRoot = GetPrivateField<Transform>(prototype, "feedbackRootReference");
            Transform typeSignatureRoot = GetPrivateField<Transform>(prototype, "typeSignatureRootReference");
            Transform levelMarkerRoot = GetPrivateField<Transform>(prototype, "levelMarkerRootReference");
            Transform relayVisualRoot = GetPrivateField<Transform>(relayPrototype, "visualRootReference");
            if (feedbackRoot == null || typeSignatureRoot == null || levelMarkerRoot == null || relayVisualRoot == null)
            {
                throw new InvalidOperationException("SampleScene prototype visual roots are still missing references.");
            }

            AssertPrefabAssetReference(GetPrivateField<UnityEngine.Object>(gameController, "relayTowerPrototypeReference"), RelayPrefabPath);
            AssertPrefabAssetReference(GetPrivateField<UnityEngine.Object>(gameController, "singleTargetTowerPrototypeReference"), SingleTargetPrefabPath);
            AssertPrefabAssetReference(GetPrivateField<UnityEngine.Object>(gameController, "slowFieldTowerPrototypeReference"), SlowFieldPrefabPath);
            AssertPrefabAssetReference(GetPrivateField<UnityEngine.Object>(gameController, "bombardTowerPrototypeReference"), BombardPrefabPath);
            AssertPrefabAssetReference(GetPrivateField<UnityEngine.Object>(waveSpawner, "enemyPrototypeReference"), EnemyPrefabPath);

            SerializedObject serializedPrototype = new SerializedObject(prototype);
            AssertSerializedAssetReference(serializedPrototype, "singleTargetTuning.shotTracePrefab", ShotTracePrefabPath);
            AssertSerializedAssetReference(serializedPrototype, "slowFieldTuning.slowPulsePrefab", SlowPulsePrefabPath);
            AssertSerializedAssetReference(serializedPrototype, "bombardTuning.bombProjectilePrefab", BombProjectilePrefabPath);
            AssertSerializedAssetReference(serializedPrototype, "bombardTuning.bombExplosionPrefab", BombExplosionPrefabPath);

            DefenseTower singleTargetInstance = UnityEngine.Object.Instantiate(prototype);
            DefenseTower slowFieldInstance = UnityEngine.Object.Instantiate(prototype);
            DefenseTower bombardInstance = UnityEngine.Object.Instantiate(prototype);

            try
            {
                singleTargetInstance.ConfigureBuildType(TowerType.SingleTarget);
                slowFieldInstance.ConfigureBuildType(TowerType.SlowField);
                bombardInstance.ConfigureBuildType(TowerType.Bombard);

                AssertOnlyExpectedFeedbackRoot(singleTargetInstance, "SingleTargetFeedbackRoot");
                AssertOnlyExpectedFeedbackRoot(slowFieldInstance, "SlowFieldFeedbackRoot");
                AssertOnlyExpectedFeedbackRoot(bombardInstance, "BombardFeedbackRoot");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(singleTargetInstance.gameObject);
                UnityEngine.Object.DestroyImmediate(slowFieldInstance.gameObject);
                UnityEngine.Object.DestroyImmediate(bombardInstance.gameObject);
            }
        }

        private static void ValidateRelayCoverageShape()
        {
            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

            RelayTower relayPrototype = UnityEngine.Object.FindObjectsByType<RelayTower>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(relay => relay.name == "RelayTowerPrototype");
            if (relayPrototype == null)
            {
                throw new InvalidOperationException("SampleScene is missing RelayTowerPrototype.");
            }

            float supplyRange = relayPrototype.SupplyRange;
            Vector3 center = relayPrototype.transform.position;
            Vector3 insideCorner = center + new Vector3(supplyRange * 0.95f, supplyRange * 0.95f, 0f);
            Vector3 outsideEdge = center + new Vector3(supplyRange * 1.05f, 0f, 0f);

            if (!relayPrototype.ContainsPoint(insideCorner))
            {
                throw new InvalidOperationException("Relay coverage is still not accepting square-corner points.");
            }

            if (relayPrototype.ContainsPoint(outsideEdge))
            {
                throw new InvalidOperationException("Relay coverage is still accepting points outside the square edge.");
            }
        }

        /// <summary>
        /// Validates all authored combat maps beyond the shared template scene.
        /// </summary>
        private static void ValidateAuthoredLevelScenes()
        {
            List<string> allFailures = new List<string>();
            foreach (LevelSceneExpectation expectation in LevelExpectations)
            {
                allFailures.AddRange(ValidateLevelScene(expectation));
            }

            if (allFailures.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("One or more authored level scenes failed validation:");
                foreach (string failure in allFailures)
                {
                    builder.AppendLine($"- {failure}");
                }

                throw new InvalidOperationException(builder.ToString());
            }
        }

        private static List<string> ValidateLevelScene(LevelSceneExpectation expectation)
        {
            EditorSceneManager.OpenScene(expectation.ScenePath, OpenSceneMode.Single);
            Scene activeScene = SceneManager.GetActiveScene();
            string sceneName = activeScene.name;

            List<string> failures = new List<string>();

            TowerDefenseGame towerDefenseGame = TowerDefenseMapToolkitUtility.FindFirstComponentInScene<TowerDefenseGame>(activeScene);
            BattlefieldMapDefinition mapDefinition = TowerDefenseMapToolkitUtility.FindFirstComponentInScene<BattlefieldMapDefinition>(activeScene);
            BuildZone buildZone = TowerDefenseMapToolkitUtility.FindFirstComponentInScene<BuildZone>(activeScene);
            Camera mainCamera = TowerDefenseMapToolkitUtility.FindFirstComponentInScene<Camera>(activeScene);
            WaveSpawner waveSpawner = TowerDefenseMapToolkitUtility.FindFirstComponentInScene<WaveSpawner>(activeScene);

            if (towerDefenseGame == null)
            {
                failures.Add($"{sceneName}: missing TowerDefenseGame.");
            }

            if (mapDefinition == null)
            {
                failures.Add($"{sceneName}: missing BattlefieldMapDefinition.");
            }

            if (buildZone == null)
            {
                failures.Add($"{sceneName}: missing BuildZone.");
            }

            if (mainCamera == null)
            {
                failures.Add($"{sceneName}: missing main Camera.");
            }

            if (expectation.RequireWaveSpawner && waveSpawner == null)
            {
                failures.Add($"{sceneName}: missing WaveSpawner.");
            }

            if (mapDefinition == null)
            {
                return failures;
            }

            mapDefinition.CollectSceneReferences();

            List<EnemySpawnGate> allSpawnGates = activeScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnemySpawnGate>(true))
                .Where(gate => gate != null)
                .Distinct()
                .OrderBy(gate => gate.name, StringComparer.Ordinal)
                .ToList();

            List<DefensePointFlag> allDefensePoints = activeScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DefensePointFlag>(true))
                .Where(point => point != null)
                .Distinct()
                .OrderBy(point => point.name, StringComparer.Ordinal)
                .ToList();

            List<EnemyPath> allEnemyPaths = activeScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnemyPath>(true))
                .Where(path => path != null)
                .Distinct()
                .OrderBy(path => path.name, StringComparer.Ordinal)
                .ToList();

            List<PathSurfaceSegment> roadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(activeScene);
            if (roadSegments.Count == 0)
            {
                failures.Add($"{sceneName}: no authored PathSegment_ road strips were found.");
            }

            if (allSpawnGates.Count != expectation.RequiredSpawnGateCount)
            {
                failures.Add($"{sceneName}: expected exactly {expectation.RequiredSpawnGateCount} spawn gates, but found {allSpawnGates.Count}.");
            }

            if (allDefensePoints.Count != expectation.RequiredDefensePointCount)
            {
                failures.Add($"{sceneName}: expected exactly {expectation.RequiredDefensePointCount} defense points, but found {allDefensePoints.Count}.");
            }

            if (allEnemyPaths.Count != expectation.RequiredPathCount)
            {
                failures.Add($"{sceneName}: expected exactly {expectation.RequiredPathCount} enemy paths, but found {allEnemyPaths.Count}.");
            }

            if (allEnemyPaths.Select(path => path.name).Distinct(StringComparer.Ordinal).Count() != allEnemyPaths.Count)
            {
                failures.Add($"{sceneName}: duplicate EnemyPath object names were found.");
            }

            if (allSpawnGates.Select(gate => gate.name).Distinct(StringComparer.Ordinal).Count() != allSpawnGates.Count)
            {
                failures.Add($"{sceneName}: duplicate EnemySpawnGate object names were found.");
            }

            if (allDefensePoints.Select(point => point.name).Distinct(StringComparer.Ordinal).Count() != allDefensePoints.Count)
            {
                failures.Add($"{sceneName}: duplicate DefensePointFlag object names were found.");
            }

            HashSet<EnemyPath> pathsUsedByGates = new HashSet<EnemyPath>();
            HashSet<DefensePointFlag> defensePointsUsedByGates = new HashSet<DefensePointFlag>();

            foreach (EnemySpawnGate spawnGate in allSpawnGates)
            {
                if (spawnGate.EnemyPath == null)
                {
                    failures.Add($"{sceneName}: {spawnGate.name} is missing its EnemyPath reference.");
                    continue;
                }

                if (!allEnemyPaths.Contains(spawnGate.EnemyPath))
                {
                    failures.Add($"{sceneName}: {spawnGate.name} points at an EnemyPath that is not part of the scene path set.");
                }
                else
                {
                    pathsUsedByGates.Add(spawnGate.EnemyPath);
                }

                if (spawnGate.TargetDefensePoint == null)
                {
                    failures.Add($"{sceneName}: {spawnGate.name} is missing its target DefensePointFlag reference.");
                }
                else if (!allDefensePoints.Contains(spawnGate.TargetDefensePoint))
                {
                    failures.Add($"{sceneName}: {spawnGate.name} points at a DefensePointFlag that is not part of the scene defense-point set.");
                }
                else
                {
                    defensePointsUsedByGates.Add(spawnGate.TargetDefensePoint);
                }

                float spawnOffset = Vector2.Distance(spawnGate.transform.position, spawnGate.GetSpawnPosition());
                if (spawnOffset > EndpointTolerance)
                {
                    failures.Add($"{sceneName}: {spawnGate.name} is {spawnOffset:0.00} units away from the first waypoint it actually uses.");
                }
            }

            foreach (EnemyPath enemyPath in allEnemyPaths)
            {
                List<Transform> waypoints = EnemyPathAuthoringUtility.GetWaypointChildren(enemyPath);
                if (waypoints.Count < 2)
                {
                    failures.Add($"{sceneName}: {enemyPath.name} has fewer than 2 waypoint objects.");
                    continue;
                }

                List<ToolkitIssue> alignmentIssues = TowerDefenseMapToolkitUtility.AnalyzeEnemyPathAlignment(
                    enemyPath,
                    roadSegments,
                    AlignmentTolerance,
                    AlignmentSamplesPerSpan);
                foreach (ToolkitIssue issue in alignmentIssues.Where(issue => issue.Severity != ToolkitIssueSeverity.Info))
                {
                    failures.Add($"{sceneName}: {issue.Category} - {issue.Message}");
                }
            }

            if (pathsUsedByGates.Count != expectation.RequiredPathCount)
            {
                failures.Add($"{sceneName}: expected {expectation.RequiredPathCount} gate-driven paths, but only {pathsUsedByGates.Count} unique paths are actually used by spawn gates.");
            }

            if (defensePointsUsedByGates.Count != expectation.RequiredDefensePointCount)
            {
                failures.Add($"{sceneName}: expected all {expectation.RequiredDefensePointCount} defense points to be targeted by at least one gate, but only {defensePointsUsedByGates.Count} are currently used.");
            }

            foreach (EnemyPath enemyPath in allEnemyPaths)
            {
                List<Transform> waypoints = EnemyPathAuthoringUtility.GetWaypointChildren(enemyPath);
                if (waypoints.Count == 0)
                {
                    continue;
                }

                DefensePointFlag matchingTarget = allSpawnGates
                    .Where(gate => gate != null && gate.EnemyPath == enemyPath)
                    .Select(gate => gate.TargetDefensePoint)
                    .FirstOrDefault(target => target != null);
                if (matchingTarget == null)
                {
                    failures.Add($"{sceneName}: {enemyPath.name} is not targeted by any configured spawn gate.");
                    continue;
                }

                float endDistance = Vector2.Distance(waypoints[waypoints.Count - 1].position, matchingTarget.WorldPosition);
                if (endDistance > EndpointTolerance)
                {
                    failures.Add($"{sceneName}: {enemyPath.name} ends {endDistance:0.00} units away from its target defense point {matchingTarget.name}.");
                }
            }

            if (waveSpawner != null)
            {
                SerializedObject serializedWaveSpawner = new SerializedObject(waveSpawner);
                SerializedProperty fallbackMapProperty = serializedWaveSpawner.FindProperty("battlefieldMapReference");
                SerializedProperty fallbackPathProperty = serializedWaveSpawner.FindProperty("enemyPathReference");

                if (fallbackMapProperty != null && fallbackMapProperty.objectReferenceValue != null && fallbackMapProperty.objectReferenceValue != mapDefinition)
                {
                    failures.Add($"{sceneName}: WaveSpawner fallback battlefieldMapReference does not point at the scene's BattlefieldMapDefinition.");
                }

                if (fallbackPathProperty != null && fallbackPathProperty.objectReferenceValue != null)
                {
                    EnemyPath fallbackPath = fallbackPathProperty.objectReferenceValue as EnemyPath;
                    if (fallbackPath == null || !allEnemyPaths.Contains(fallbackPath))
                    {
                        failures.Add($"{sceneName}: WaveSpawner fallback enemyPathReference points at a stale or non-scene EnemyPath.");
                    }
                }
            }

            if (towerDefenseGame != null)
            {
                SerializedObject serializedGame = new SerializedObject(towerDefenseGame);
                SerializedProperty mapReferenceProperty = serializedGame.FindProperty("battlefieldMapReference");
                if (mapReferenceProperty != null && mapReferenceProperty.objectReferenceValue != null && mapReferenceProperty.objectReferenceValue != mapDefinition)
                {
                    failures.Add($"{sceneName}: TowerDefenseGame battlefieldMapReference does not point at the scene's BattlefieldMapDefinition.");
                }
            }

            return failures;
        }

        private static void AssertOnlyExpectedFeedbackRoot(DefenseTower tower, string expectedChildName)
        {
            Transform feedbackRoot = GetPrivateField<Transform>(tower, "feedbackRootReference");
            if (feedbackRoot == null)
            {
                throw new InvalidOperationException("DefenseTower instance is missing feedbackRootReference.");
            }

            string[] childNames = Enumerable.Range(0, feedbackRoot.childCount)
                .Select(index => feedbackRoot.GetChild(index).name)
                .ToArray();

            if (childNames.Length != 1 || childNames[0] != expectedChildName)
            {
                throw new InvalidOperationException(
                    $"DefenseTower feedback roots are not isolated correctly. Expected only '{expectedChildName}', got [{string.Join(", ", childNames)}].");
            }
        }

        private static GameObject LoadRequiredPrefab(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing required prefab asset: {assetPath}");
            }

            return prefab;
        }

        private static void AssertTowerPrefabType(GameObject prefab, TowerType expectedType, string label)
        {
            DefenseTower tower = prefab.GetComponent<DefenseTower>();
            if (tower == null)
            {
                throw new InvalidOperationException($"{label} runtime prefab is missing DefenseTower.");
            }

            if (tower.BuildType != expectedType)
            {
                throw new InvalidOperationException(
                    $"Expected {label} runtime prefab to use tower type '{expectedType}', but found '{tower.BuildType}'.");
            }
        }

        private static void AssertPrefabAssetReference(UnityEngine.Object value, string expectedAssetPath)
        {
            if (value == null)
            {
                throw new InvalidOperationException($"Expected prefab asset reference '{expectedAssetPath}', but the reference is null.");
            }

            string actualAssetPath = AssetDatabase.GetAssetPath(value);
            if (!string.Equals(actualAssetPath, expectedAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected prefab asset reference '{expectedAssetPath}', but found '{actualAssetPath}'.");
            }
        }

        private static void AssertSerializedAssetReference(SerializedObject target, string propertyPath, string expectedAssetPath)
        {
            SerializedProperty property = target.FindProperty(propertyPath);
            if (property == null)
            {
                throw new InvalidOperationException($"Missing serialized property '{propertyPath}'.");
            }

            AssertPrefabAssetReference(property.objectReferenceValue, expectedAssetPath);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException($"Missing private field '{fieldName}' on {target.GetType().Name}.");
            }

            return (T)field.GetValue(target);
        }
    }
}
