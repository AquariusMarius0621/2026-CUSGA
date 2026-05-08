using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TowerDefense.Editor
{
    /// <summary>
    /// Main tab selector for the map toolkit window.
    ///
    /// The user asked for a practical toolbox rather than one monolithic wizard, so
    /// the window is split by authoring task instead of by underlying code module.
    /// That keeps the UX closer to how level work actually happens in Unity:
    /// validate route -> generate road -> sync template -> paint buildable area -> preview waves.
    /// </summary>
    internal enum MapToolkitTab
    {
        PathAlignment,
        RoadGenerator,
        TemplateSync,
        HealthCheck,
        ZoneBrush,
        WavePreview
    }

    /// <summary>
    /// Chooses how a diagonal gap between two points should be split into two orthogonal road segments.
    ///
    /// The generated roads in this project are intentionally orthogonal most of the time, so this
    /// single design choice is enough to keep the generator understandable without introducing a
    /// heavy spline system.
    /// </summary>
    internal enum GeneratedTurnMode
    {
        HorizontalThenVertical,
        VerticalThenHorizontal
    }

    /// <summary>
    /// Describes what the scene brush should create while dragging in Scene view.
    ///
    /// We keep the brush limited to the two authoring primitives that repeatedly cost time:
    /// buildable rectangles and placement blockers.
    /// </summary>
    internal enum AuthoringBrushMode
    {
        BuildZoneShape,
        PlacementBlocker
    }

    /// <summary>
    /// A light-weight severity bucket for validation and alignment findings.
    ///
    /// This lets the window present mixed results in one list while still making obvious red-flag
    /// issues stand out in Scene view and in the Inspector panel.
    /// </summary>
    internal enum ToolkitIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// One validation or alignment issue reported by the toolkit.
    ///
    /// The struct intentionally keeps both a text summary and world-space data so the same result
    /// can feed:
    /// - the textual list in the window
    /// - the red highlights in Scene view
    /// - the "ping / focus" authoring actions
    /// </summary>
    internal sealed class ToolkitIssue
    {
        public ToolkitIssueSeverity Severity { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public Object ContextObject { get; set; }
        public Vector3 WorldPositionA { get; set; }
        public Vector3? WorldPositionB { get; set; }
        public string SuggestedAction { get; set; }
    }

    /// <summary>
    /// Cached road segment data used by route validation and snapping.
    ///
    /// The current project roads are authored as rectangular path strips, so storing bounds plus a
    /// dominant axis is enough for useful tooling without adding runtime dependencies.
    /// </summary>
    internal sealed class PathSurfaceSegment
    {
        public GameObject GameObject { get; set; }
        public Bounds WorldBounds { get; set; }
        public bool IsHorizontal { get; set; }
        public Vector3 Center => WorldBounds.center;
        public float Width => WorldBounds.size.x;
        public float Height => WorldBounds.size.y;
    }

    /// <summary>
    /// One preview row for the wave previewer.
    ///
    /// The window shows these rows directly, so we keep them already formatted in author-facing
    /// language instead of forcing the UI code to rebuild summary strings every frame.
    /// </summary>
    internal sealed class WavePreviewRow
    {
        public string Title { get; set; }
        public int TotalEnemies { get; set; }
        public int TotalScrap { get; set; }
        public string GateBreakdown { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// One generated road batch summary entry.
    ///
    /// The second version of the toolkit starts surfacing generated grouping on purpose:
    /// once a scene has several routes, authors need a quick confirmation of
    /// "which group was generated just now and how many segments did it create?".
    /// </summary>
    internal sealed class GeneratedRoadGroupInfo
    {
        public string GroupName { get; set; }
        public int SegmentCount { get; set; }
    }

    /// <summary>
    /// Shared authoring helpers used by the toolkit window.
    ///
    /// Keeping these helpers together matters for maintainability:
    /// several of the requested tools all need the same low-level scene operations such as
    /// finding scene roots, collecting road segments, remapping HUD references, and sampling
    /// whether a point actually lies on a visible road strip.
    /// </summary>
    internal static class TowerDefenseMapToolkitUtility
    {
        internal const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        internal const string Level02ScenePath = "Assets/Scenes/Level02.unity";
        internal const string Level03ScenePath = "Assets/Scenes/Level03.unity";
        internal const string Level04ScenePath = "Assets/Scenes/Level04.unity";

        internal const string RelayPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/RelayTowerPrototype.prefab";
        internal const string SingleTargetPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/SingleTargetTowerPrototype.prefab";
        internal const string SlowFieldPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/SlowFieldTowerPrototype.prefab";
        internal const string BombardPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/BombardTowerPrototype.prefab";
        internal const string EnemyPrefabPath = "Assets/Prefabs/TowerDefense/Runtime/EnemyPrototype.prefab";

        /// <summary>
        /// Enumerates every object in a scene by walking all roots recursively.
        ///
        /// We use this instead of global `FindObjectsByType` whenever a tool needs to inspect
        /// specific scenes loaded additively, because global searches quickly become ambiguous
        /// once SampleScene and target scenes are open together.
        /// </summary>
        internal static IEnumerable<GameObject> EnumerateSceneObjects(Scene scene)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (GameObject child in EnumerateHierarchy(rootObject))
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Finds the first object by exact name within one scene.
        ///
        /// This project still benefits from explicit scene references first, but for editor-only
        /// template syncing it is reasonable to use stable hierarchy names to remap well-known
        /// UI and root objects across scenes.
        /// </summary>
        internal static GameObject FindObjectByName(Scene scene, string objectName)
        {
            return EnumerateSceneObjects(scene).FirstOrDefault(candidate => candidate.name == objectName);
        }

        /// <summary>
        /// Finds the first component of type `T` in the given scene.
        /// </summary>
        internal static T FindFirstComponentInScene<T>(Scene scene) where T : Component
        {
            return EnumerateSceneObjects(scene)
                .Select(candidate => candidate.GetComponent<T>())
                .FirstOrDefault(component => component != null);
        }

        /// <summary>
        /// Collects all road-surface segments authored in the scene.
        ///
        /// The convention is intentionally simple: any object whose name starts with `PathSegment_`
        /// and has either a `BoxCollider2D` or enough transform information to infer a rectangle
        /// counts as a road strip. This matches how the current maps are already authored.
        /// </summary>
        internal static List<PathSurfaceSegment> CollectPathSurfaceSegments(Scene scene)
        {
            List<PathSurfaceSegment> results = new List<PathSurfaceSegment>();
            foreach (GameObject sceneObject in EnumerateSceneObjects(scene))
            {
                if (!sceneObject.name.StartsWith("PathSegment_", StringComparison.Ordinal))
                {
                    continue;
                }

                Transform transform = sceneObject.transform;
                if (transform == null)
                {
                    continue;
                }

                BoxCollider2D boxCollider = sceneObject.GetComponent<BoxCollider2D>();
                Bounds bounds = boxCollider != null
                    ? boxCollider.bounds
                    : new Bounds(transform.position, new Vector3(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), 0.2f));

                if (bounds.size.x <= 0.001f || bounds.size.y <= 0.001f)
                {
                    continue;
                }

                results.Add(new PathSurfaceSegment
                {
                    GameObject = sceneObject,
                    WorldBounds = bounds,
                    IsHorizontal = bounds.size.x >= bounds.size.y
                });
            }

            return results;
        }

        /// <summary>
        /// Computes the nearest point on a rectangular road strip centerline.
        ///
        /// We snap to the centerline rather than to the rectangle surface because the user asked
        /// for movement routes to line up with the visible middle of the road, just like the
        /// current SampleScene does.
        /// </summary>
        internal static Vector3 GetNearestPointOnRoadCenterline(PathSurfaceSegment segment, Vector3 worldPosition)
        {
            Bounds bounds = segment.WorldBounds;
            if (segment.IsHorizontal)
            {
                float clampedX = Mathf.Clamp(worldPosition.x, bounds.min.x, bounds.max.x);
                return new Vector3(clampedX, bounds.center.y, worldPosition.z);
            }

            float clampedY = Mathf.Clamp(worldPosition.y, bounds.min.y, bounds.max.y);
            return new Vector3(bounds.center.x, clampedY, worldPosition.z);
        }

        /// <summary>
        /// Tests whether a point is close enough to any road centerline to count as aligned.
        ///
        /// The tolerance is author-facing rather than physics-facing: it answers
        /// "would a level author reasonably say this point sits on the road?".
        /// </summary>
        internal static bool TryFindNearestRoadPoint(
            List<PathSurfaceSegment> segments,
            Vector3 worldPosition,
            float tolerance,
            out PathSurfaceSegment nearestSegment,
            out Vector3 nearestRoadPoint,
            out float distance)
        {
            nearestSegment = null;
            nearestRoadPoint = worldPosition;
            distance = float.MaxValue;

            foreach (PathSurfaceSegment segment in segments)
            {
                Vector3 candidatePoint = GetNearestPointOnRoadCenterline(segment, worldPosition);
                float candidateDistance = Vector2.Distance(candidatePoint, worldPosition);
                if (candidateDistance < distance)
                {
                    distance = candidateDistance;
                    nearestRoadPoint = candidatePoint;
                    nearestSegment = segment;
                }
            }

            return nearestSegment != null && distance <= tolerance;
        }

        /// <summary>
        /// Samples a straight segment between two waypoints and checks whether the line is covered
        /// by authored road strips.
        ///
        /// The current map style is deliberately orthogonal, so this validation intentionally
        /// treats diagonal movement between consecutive waypoints as an error. If the author wants
        /// a turn, they should insert a visible corner by adding another waypoint.
        /// </summary>
        internal static List<ToolkitIssue> AnalyzeEnemyPathAlignment(
            EnemyPath enemyPath,
            List<PathSurfaceSegment> roadSegments,
            float pointTolerance,
            int samplesPerSpan,
            IReadOnlyCollection<int> segmentFilter = null)
        {
            List<ToolkitIssue> issues = new List<ToolkitIssue>();
            List<Transform> waypoints = EnemyPathAuthoringUtility.GetWaypointChildren(enemyPath);
            HashSet<int> includedSegments = segmentFilter != null && segmentFilter.Count > 0
                ? new HashSet<int>(segmentFilter.Where(segmentIndex => segmentIndex >= 0))
                : null;
            HashSet<int> includedWaypoints = null;
            if (includedSegments != null)
            {
                includedWaypoints = new HashSet<int>();
                foreach (int segmentIndex in includedSegments)
                {
                    includedWaypoints.Add(segmentIndex);
                    includedWaypoints.Add(segmentIndex + 1);
                }
            }

            for (int waypointIndex = 0; waypointIndex < waypoints.Count; waypointIndex++)
            {
                if (includedWaypoints != null && !includedWaypoints.Contains(waypointIndex))
                {
                    continue;
                }

                Transform waypoint = waypoints[waypointIndex];
                if (waypoint == null)
                {
                    continue;
                }

                if (!TryFindNearestRoadPoint(roadSegments, waypoint.position, pointTolerance, out _, out Vector3 nearestPoint, out float distance))
                {
                    issues.Add(new ToolkitIssue
                    {
                        Severity = ToolkitIssueSeverity.Error,
                        Category = "Waypoint Off Road",
                        Message = $"{enemyPath.name} / Point #{waypointIndex + 1:D2} is {distance:0.00} units away from the nearest road centerline.",
                        ContextObject = waypoint,
                        WorldPositionA = waypoint.position,
                        SuggestedAction = "Snap this waypoint back onto the nearest road centerline."
                    });
                }
            }

            for (int segmentIndex = 0; segmentIndex < waypoints.Count - 1; segmentIndex++)
            {
                if (includedSegments != null && !includedSegments.Contains(segmentIndex))
                {
                    continue;
                }

                Transform start = waypoints[segmentIndex];
                Transform end = waypoints[segmentIndex + 1];
                if (start == null || end == null)
                {
                    continue;
                }

                Vector3 delta = end.position - start.position;
                bool vertical = Mathf.Abs(delta.x) <= 0.001f;
                bool horizontal = Mathf.Abs(delta.y) <= 0.001f;
                if (!vertical && !horizontal)
                {
                    issues.Add(new ToolkitIssue
                    {
                        Severity = ToolkitIssueSeverity.Error,
                        Category = "Diagonal Route Segment",
                        Message = $"{enemyPath.name} / #{segmentIndex + 1:D2} -> #{segmentIndex + 2:D2} is diagonal, so enemies would appear to cut across the road art.",
                        ContextObject = enemyPath,
                        WorldPositionA = start.position,
                        WorldPositionB = end.position,
                        SuggestedAction = "Insert a corner waypoint or snap one of the points onto the intended road turn."
                    });
                    continue;
                }

                int effectiveSamples = Mathf.Max(2, samplesPerSpan);
                for (int sampleIndex = 0; sampleIndex <= effectiveSamples; sampleIndex++)
                {
                    float t = sampleIndex / (float)effectiveSamples;
                    Vector3 samplePoint = Vector3.Lerp(start.position, end.position, t);
                    if (!TryFindNearestRoadPoint(roadSegments, samplePoint, pointTolerance, out _, out _, out _))
                    {
                        issues.Add(new ToolkitIssue
                        {
                            Severity = ToolkitIssueSeverity.Error,
                            Category = "Route Span Missing Road Coverage",
                            Message = $"{enemyPath.name} / #{segmentIndex + 1:D2} -> #{segmentIndex + 2:D2} has samples that fall outside any authored road surface.",
                            ContextObject = enemyPath,
                            WorldPositionA = start.position,
                            WorldPositionB = end.position,
                            SuggestedAction = "Generate or extend road segments, or pull the waypoints back onto the visible route."
                        });
                        break;
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Snaps one waypoint to the nearest authored road centerline.
        /// </summary>
        internal static bool TrySnapWaypointToNearestRoad(Transform waypoint, List<PathSurfaceSegment> roadSegments, float snapTolerance)
        {
            return TrySnapTransformToNearestRoadCenterline(
                waypoint,
                roadSegments,
                snapTolerance,
                "Snap Waypoint To Road Centerline");
        }

        /// <summary>
        /// Snaps any authored scene transform onto the nearest visible road centerline.
        ///
        /// Keeping this generic matters for map work: waypoints, spawn gates, and defense points
        /// all need the same spatial rule, and duplicating three slightly different versions would
        /// quickly make authoring fixes drift apart.
        /// </summary>
        internal static bool TrySnapTransformToNearestRoadCenterline(
            Transform sceneTransform,
            List<PathSurfaceSegment> roadSegments,
            float snapTolerance,
            string undoActionName)
        {
            if (sceneTransform == null)
            {
                return false;
            }

            if (!TryFindNearestRoadPoint(roadSegments, sceneTransform.position, float.MaxValue, out _, out Vector3 nearestPoint, out float distance))
            {
                return false;
            }

            if (distance > snapTolerance)
            {
                return false;
            }

            Undo.RecordObject(sceneTransform, string.IsNullOrWhiteSpace(undoActionName) ? "Snap Transform To Road Centerline" : undoActionName);
            sceneTransform.position = new Vector3(nearestPoint.x, nearestPoint.y, sceneTransform.position.z);
            EditorUtility.SetDirty(sceneTransform);
            EditorSceneManager.MarkSceneDirty(sceneTransform.gameObject.scene);
            return true;
        }

        /// <summary>
        /// Builds one orthogonal road strip between two points, or two strips if the points form a corner.
        /// </summary>
        internal static List<(Vector3 start, Vector3 end)> BuildOrthogonalSegments(Vector3 start, Vector3 end, GeneratedTurnMode turnMode)
        {
            List<(Vector3 start, Vector3 end)> segments = new List<(Vector3 start, Vector3 end)>();
            if (Mathf.Abs(start.x - end.x) <= 0.001f || Mathf.Abs(start.y - end.y) <= 0.001f)
            {
                segments.Add((start, end));
                return segments;
            }

            Vector3 corner = turnMode == GeneratedTurnMode.HorizontalThenVertical
                ? new Vector3(end.x, start.y, start.z)
                : new Vector3(start.x, end.y, start.z);

            segments.Add((start, corner));
            segments.Add((corner, end));
            return segments;
        }

        /// <summary>
        /// Chooses the turn split that best matches already-authored road strips in the scene.
        ///
        /// This is intentionally a "best effort" heuristic. The point is not to magically infer
        /// level design intent in every case, but to reduce how often the generator picks the
        /// obviously wrong elbow when the map already contains nearby roads.
        /// </summary>
        internal static GeneratedTurnMode ChooseBestGeneratedTurnMode(
            Vector3 start,
            Vector3 end,
            List<PathSurfaceSegment> referenceRoadSegments,
            GeneratedTurnMode fallbackMode)
        {
            if (referenceRoadSegments == null || referenceRoadSegments.Count == 0)
            {
                return fallbackMode;
            }

            if (Mathf.Abs(start.x - end.x) <= 0.001f || Mathf.Abs(start.y - end.y) <= 0.001f)
            {
                return fallbackMode;
            }

            List<(Vector3 start, Vector3 end)> candidateA = BuildOrthogonalSegments(start, end, GeneratedTurnMode.HorizontalThenVertical);
            List<(Vector3 start, Vector3 end)> candidateB = BuildOrthogonalSegments(start, end, GeneratedTurnMode.VerticalThenHorizontal);

            float scoreA = ScoreGeneratedSegmentsAgainstReferenceRoads(candidateA, referenceRoadSegments);
            float scoreB = ScoreGeneratedSegmentsAgainstReferenceRoads(candidateB, referenceRoadSegments);
            return scoreA <= scoreB ? GeneratedTurnMode.HorizontalThenVertical : GeneratedTurnMode.VerticalThenHorizontal;
        }

        /// <summary>
        /// Estimates a good road thickness from the current map instead of forcing the user to
        /// type the same thickness every time.
        /// </summary>
        internal static float EstimatePreferredRoadThickness(List<PathSurfaceSegment> referenceRoadSegments, float fallbackThickness)
        {
            if (referenceRoadSegments == null || referenceRoadSegments.Count == 0)
            {
                return fallbackThickness;
            }

            List<float> minorAxes = referenceRoadSegments
                .Select(segment => Mathf.Min(segment.Width, segment.Height))
                .Where(length => length > 0.05f)
                .OrderBy(length => length)
                .ToList();
            if (minorAxes.Count == 0)
            {
                return fallbackThickness;
            }

            return minorAxes[minorAxes.Count / 2];
        }

        /// <summary>
        /// Creates or clones one road strip object.
        ///
        /// If no explicit template is provided, we first try to clone an existing `PathSegment_*`
        /// object from the scene so the generated road automatically matches the current art style.
        /// Only when that fails do we fall back to a minimal blocker-only rectangle.
        /// </summary>
        internal static GameObject CreateRoadSegmentInstance(
            Scene scene,
            Transform parent,
            GameObject explicitTemplate,
            string objectName,
            Vector3 center,
            Vector2 size)
        {
            GameObject segmentObject = null;
            GameObject sceneTemplate = explicitTemplate;
            if (sceneTemplate == null)
            {
                sceneTemplate = EnumerateSceneObjects(scene)
                    .FirstOrDefault(candidate => candidate.name.StartsWith("PathSegment_", StringComparison.Ordinal));
            }

            if (sceneTemplate != null)
            {
                segmentObject = Object.Instantiate(sceneTemplate);
                segmentObject.name = objectName;
            }
            else
            {
                segmentObject = new GameObject(objectName);
                segmentObject.AddComponent<SpriteRenderer>();
                segmentObject.AddComponent<BoxCollider2D>();
                segmentObject.AddComponent<PlacementBlocker>();
            }

            Undo.RegisterCreatedObjectUndo(segmentObject, "鐢熸垚璺緞璺");
            SceneManager.MoveGameObjectToScene(segmentObject, scene);
            segmentObject.transform.SetParent(parent, false);
            segmentObject.transform.position = center;
            segmentObject.transform.rotation = Quaternion.identity;
            segmentObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            BoxCollider2D blockerCollider = segmentObject.GetComponent<BoxCollider2D>();
            if (blockerCollider != null)
            {
                blockerCollider.isTrigger = true;
                blockerCollider.size = Vector2.one;
                blockerCollider.offset = Vector2.zero;
            }

            return segmentObject;
        }

        /// <summary>
        /// Ensures a named root exists directly under a scene root or map root.
        /// </summary>
        internal static Transform EnsureNamedRoot(Scene scene, string rootName)
        {
            GameObject existing = FindObjectByName(scene, rootName);
            if (existing != null)
            {
                return existing.transform;
            }

            GameObject rootObject = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(rootObject, $"鍒涘缓 {rootName}");
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            return rootObject.transform;
        }

        /// <summary>
        /// Ensures a `ZoneShapes` root for the current BuildZone.
        /// </summary>
        internal static Transform EnsureZoneShapeRoot(BuildZone buildZone)
        {
            if (buildZone == null)
            {
                return null;
            }

            Transform existingRoot = buildZone.ZoneShapeRoot;
            if (existingRoot != null)
            {
                return existingRoot;
            }

            Transform hierarchyRoot = buildZone.transform.Find("ZoneShapes");
            if (hierarchyRoot != null)
            {
                return hierarchyRoot;
            }

            GameObject rootObject = new GameObject("ZoneShapes");
            Undo.RegisterCreatedObjectUndo(rootObject, "Create ZoneShapes Root");
            hierarchyRoot = rootObject.transform;
            hierarchyRoot.SetParent(buildZone.transform, false);
            hierarchyRoot.localPosition = Vector3.zero;
            hierarchyRoot.localRotation = Quaternion.identity;
            hierarchyRoot.localScale = Vector3.one;
            return hierarchyRoot;
        }

        /// <summary>
        /// Creates one rectangular brush result in scene space.
        /// </summary>
        internal static GameObject CreateBrushRectangle(
            Scene scene,
            AuthoringBrushMode brushMode,
            Transform parent,
            Rect worldRect,
            string blockerReason,
            Color previewColor)
        {
            Vector3 center = new Vector3(worldRect.center.x, worldRect.center.y, 0f);
            Vector3 scale = new Vector3(Mathf.Max(0.2f, worldRect.width), Mathf.Max(0.2f, worldRect.height), 1f);

            GameObject createdObject = new GameObject(brushMode == AuthoringBrushMode.BuildZoneShape ? "ZoneShape_Box" : "PlacementBlocker_Box");
            Undo.RegisterCreatedObjectUndo(createdObject, "鍒涘缓鍦板浘鐢荤瑪鐭╁舰");
            SceneManager.MoveGameObjectToScene(createdObject, scene);
            createdObject.transform.SetParent(parent, false);
            createdObject.transform.position = center;
            createdObject.transform.localScale = scale;

            BoxCollider2D collider = createdObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            SpriteRenderer spriteRenderer = createdObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = previewColor;

            if (brushMode == AuthoringBrushMode.PlacementBlocker)
            {
                PlacementBlocker placementBlocker = createdObject.AddComponent<PlacementBlocker>();
                SerializedObject serializedBlocker = new SerializedObject(placementBlocker);
                SerializedProperty reasonProperty = serializedBlocker.FindProperty("blockerReason");
                if (reasonProperty != null)
                {
                    reasonProperty.stringValue = string.IsNullOrWhiteSpace(blockerReason)
                        ? "Enemy path area. Structures cannot be deployed here."
                        : blockerReason;
                    serializedBlocker.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return createdObject;
        }

        /// <summary>
        /// Syncs the shared HUD root and core scene references from SampleScene into a target scene.
        ///
        /// The key design rule here is "sync the shared shell, keep the map-specific geometry":
        /// we replace / rebuild shared UI and rebind scene references, but we deliberately do not
        /// touch path coordinates, defense point positions, or hand-authored blocker layouts.
        /// </summary>
        internal static void SyncSceneFromSample(Scene sampleScene, Scene targetScene)
        {
            GameObject sampleHud = FindObjectByName(sampleScene, "HUDCanvas");
            GameObject targetHud = FindObjectByName(targetScene, "HUDCanvas");
            if (sampleHud != null)
            {
                if (targetHud != null)
                {
                    Undo.DestroyObjectImmediate(targetHud);
                }

                GameObject clonedHud = Object.Instantiate(sampleHud);
                clonedHud.name = sampleHud.name;
                SceneManager.MoveGameObjectToScene(clonedHud, targetScene);
                Undo.RegisterCreatedObjectUndo(clonedHud, "鍚屾 HUDCanvas");
            }

            EnsureNamedRoot(targetScene, "PlacedTowers");
            EnsureNamedRoot(targetScene, "PlacementPreviewRoot");
            EnsureNamedRoot(targetScene, "EnemiesRoot");

            TowerDefenseGame targetGame = FindFirstComponentInScene<TowerDefenseGame>(targetScene);
            if (targetGame != null)
            {
                SerializedObject serializedGame = new SerializedObject(targetGame);
                AssignObjectReferenceByName(serializedGame, "mainCameraReference", FindFirstComponentInScene<Camera>(targetScene));
                AssignObjectReferenceByAssetPath(serializedGame, "relayTowerPrototypeReference", RelayPrefabPath);
                AssignObjectReferenceByAssetPath(serializedGame, "singleTargetTowerPrototypeReference", SingleTargetPrefabPath);
                AssignObjectReferenceByAssetPath(serializedGame, "slowFieldTowerPrototypeReference", SlowFieldPrefabPath);
                AssignObjectReferenceByAssetPath(serializedGame, "bombardTowerPrototypeReference", BombardPrefabPath);
                AssignObjectReferenceByName(serializedGame, "placedTowerRootReference", FindObjectByName(targetScene, "PlacedTowers")?.transform);
                AssignObjectReferenceByName(serializedGame, "placementPreviewRootReference", FindObjectByName(targetScene, "PlacementPreviewRoot")?.transform);
                AssignObjectReferenceByName(serializedGame, "buildZoneReference", FindFirstComponentInScene<BuildZone>(targetScene));
                AssignObjectReferenceByName(serializedGame, "battlefieldMapReference", FindFirstComponentInScene<BattlefieldMapDefinition>(targetScene));
                AssignObjectReferenceByName(serializedGame, "scrapTextReference", FindTextByName(targetScene, "ScrapText"));
                AssignObjectReferenceByName(serializedGame, "baseHealthTextReference", FindTextByName(targetScene, "BaseHealthText"));
                AssignObjectReferenceByName(serializedGame, "waveTextReference", FindTextByName(targetScene, "WaveText"));
                AssignObjectReferenceByName(serializedGame, "selectionTextReference", FindTextByName(targetScene, "SelectionText"));
                AssignObjectReferenceByName(serializedGame, "relayTowerButtonReference", FindComponentByName<Button>(targetScene, "RelayTowerButton"));
                AssignObjectReferenceByName(serializedGame, "defenseTowerButtonReference", FindComponentByName<Button>(targetScene, "DefenseTowerButton"));
                AssignObjectReferenceByName(serializedGame, "slowFieldTowerButtonReference", FindComponentByName<Button>(targetScene, "SlowFieldTowerButton"));
                AssignObjectReferenceByName(serializedGame, "bombardTowerButtonReference", FindComponentByName<Button>(targetScene, "BombardTowerButton"));
                AssignObjectReferenceByName(serializedGame, "clearSelectionButtonReference", FindComponentByName<Button>(targetScene, "ClearSelectionButton"));
                AssignObjectReferenceByName(serializedGame, "gameOverPanelReference", FindObjectByName(targetScene, "GameOverPanel"));
                AssignObjectReferenceByName(serializedGame, "gameOverTitleReference", FindTextByName(targetScene, "GameOverTitle"));
                AssignObjectReferenceByName(serializedGame, "gameOverHintReference", FindTextByName(targetScene, "GameOverHint"));
                AssignObjectReferenceByName(serializedGame, "dragPreviewPanelReference", FindObjectByName(targetScene, "DragPreviewPanel"));
                AssignObjectReferenceByName(serializedGame, "dragPreviewLabelReference", FindTextByName(targetScene, "DragPreviewLabel"));
                serializedGame.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetGame);
            }

            WaveSpawner targetSpawner = FindFirstComponentInScene<WaveSpawner>(targetScene);
            if (targetSpawner != null)
            {
                SerializedObject serializedSpawner = new SerializedObject(targetSpawner);
                AssignObjectReferenceByName(serializedSpawner, "battlefieldMapReference", FindFirstComponentInScene<BattlefieldMapDefinition>(targetScene));
                AssignObjectReferenceByName(serializedSpawner, "enemyPathReference", FindFirstComponentInScene<EnemyPath>(targetScene));
                AssignObjectReferenceByAssetPath(serializedSpawner, "enemyPrototypeReference", EnemyPrefabPath);
                AssignObjectReferenceByName(serializedSpawner, "enemyRootReference", FindObjectByName(targetScene, "EnemiesRoot")?.transform);
                serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetSpawner);
            }

            BattlefieldMapDefinition targetMap = FindFirstComponentInScene<BattlefieldMapDefinition>(targetScene);
            if (targetMap != null)
            {
                targetMap.CollectSceneReferences();
                EditorUtility.SetDirty(targetMap);
            }

            EditorSceneManager.MarkSceneDirty(targetScene);
        }

        /// <summary>
        /// Compares the current scene HUD against SampleScene using object-name matching.
        ///
        /// This stays intentionally narrow: it only compares a set of well-known, author-facing
        /// HUD text nodes and button hints. That is enough to catch the drift the user has been
        /// complaining about without turning the check into an expensive full-scene diff.
        /// </summary>
        internal static List<ToolkitIssue> CompareHudAgainstSample(Scene sampleScene, Scene targetScene)
        {
            List<ToolkitIssue> issues = new List<ToolkitIssue>();
            string[] textNames =
            {
                "ScrapText",
                "BaseHealthText",
                "WaveText",
                "SelectionText",
                "GameOverTitle",
                "GameOverHint",
                "RelayIconText",
                "DefenseIconText",
                "SlowFieldIconText",
                "BombardIconText"
            };

            foreach (string textName in textNames)
            {
                TMP_Text sampleText = FindTextByName(sampleScene, textName);
                TMP_Text targetText = FindTextByName(targetScene, textName);
                if (sampleText == null || targetText == null)
                {
                    continue;
                }

                if (!string.Equals(sampleText.text, targetText.text, StringComparison.Ordinal))
                {
                    issues.Add(new ToolkitIssue
                    {
                        Severity = ToolkitIssueSeverity.Warning,
                        Category = "HUD Text Mismatch",
                        Message = $"{textName} does not match the SampleScene HUD text.",
                        ContextObject = targetText,
                        WorldPositionA = Vector3.zero,
                        SuggestedAction = "Run the SampleScene template sync so the shared HUD text stays consistent."
                    });
                }

                if (sampleText.font != targetText.font)
                {
                    issues.Add(new ToolkitIssue
                    {
                        Severity = ToolkitIssueSeverity.Warning,
                        Category = "HUD Font Mismatch",
                        Message = $"{textName} is using a different font than SampleScene.",
                        ContextObject = targetText,
                        WorldPositionA = Vector3.zero,
                        SuggestedAction = "Run the SampleScene template sync so the shared HUD font stays consistent."
                    });
                }
            }

            return issues;
        }

        /// <summary>
        /// Produces a basic wave preview from the scene's fallback `WaveSpawner` data.
        ///
        /// Even though the project also has more advanced wave assets, the runtime currently still
        /// uses the scene-spawned fallback arrays in several places. This preview therefore starts
        /// from the scene object first because that is what the user most immediately needs while
        /// authoring maps.
        /// </summary>
        internal static List<WavePreviewRow> BuildWavePreviewFromScene(WaveSpawner waveSpawner, BattlefieldMapDefinition mapDefinition)
        {
            List<WavePreviewRow> rows = new List<WavePreviewRow>();
            if (waveSpawner == null)
            {
                return rows;
            }

             if (waveSpawner.WaveCatalogAsset != null && waveSpawner.EnemyCatalogAsset != null && waveSpawner.WaveCatalogAsset.Waves.Length > 0)
            {
                return BuildWavePreviewFromCatalog(waveSpawner.WaveCatalogAsset, waveSpawner.EnemyCatalogAsset, mapDefinition);
            }

            SerializedObject serializedSpawner = new SerializedObject(waveSpawner);
            SerializedProperty wavesProperty = serializedSpawner.FindProperty("waves");
            if (wavesProperty == null || !wavesProperty.isArray)
            {
                return rows;
            }

            for (int waveIndex = 0; waveIndex < wavesProperty.arraySize; waveIndex++)
            {
                SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(waveIndex);
                int enemyCount = Mathf.Max(0, waveProperty.FindPropertyRelative("enemyCount")?.intValue ?? 0);
                int scrapReward = Mathf.Max(0, waveProperty.FindPropertyRelative("enemyScrapReward")?.intValue ?? 0);
                Dictionary<string, int> gateCounts = new Dictionary<string, int>();
                for (int enemyIndex = 0; enemyIndex < enemyCount; enemyIndex++)
                {
                    string gateName = "FallbackPath";
                    if (mapDefinition != null && mapDefinition.TryGetSpawnGateBySequence(enemyIndex, out EnemySpawnGate spawnGate) && spawnGate != null)
                    {
                        gateName = spawnGate.DisplayName;
                    }

                    gateCounts[gateName] = gateCounts.TryGetValue(gateName, out int existing) ? existing + 1 : 1;
                }

                rows.Add(new WavePreviewRow
                {
                    Title = $"Wave {waveIndex + 1:D2}",
                    TotalEnemies = enemyCount,
                    TotalScrap = enemyCount * scrapReward,
                    GateBreakdown = string.Join(" / ", gateCounts.Select(pair => $"{pair.Key}: {pair.Value}")),
                    Note = $"MoveSpeed={waveProperty.FindPropertyRelative("moveSpeed")?.floatValue ?? 0f:0.00}, HP={waveProperty.FindPropertyRelative("enemyHealth")?.intValue ?? 0}"
                });
            }

            return rows;
        }

        /// <summary>
        /// Produces a preview from an advanced `WaveCatalogAsset`.
        ///
        /// The preview still distributes enemies across spawn gates in round-robin order, because
        /// that mirrors the scene-authored multi-gate behavior already established by the runtime
        /// fallback spawner.
        /// </summary>
        internal static List<WavePreviewRow> BuildWavePreviewFromCatalog(
            WaveCatalogAsset catalogAsset,
            EnemyCatalogAsset enemyCatalogAsset,
            BattlefieldMapDefinition mapDefinition)
        {
            List<WavePreviewRow> rows = new List<WavePreviewRow>();
            if (catalogAsset == null)
            {
                return rows;
            }

            int gateSequence = 0;
            WaveCatalogAsset.WaveEntry[] waves = catalogAsset.Waves;
            for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                WaveCatalogAsset.WaveEntry wave = waves[waveIndex];
                Dictionary<string, int> gateCounts = new Dictionary<string, int>();
                int totalEnemies = 0;
                int totalScrap = 0;

                foreach (WaveCatalogAsset.SpawnGroup spawnGroup in wave.SpawnGroups)
                {
                    if (spawnGroup == null)
                    {
                        continue;
                    }

                    int groupScrapReward = 0;
                    if (enemyCatalogAsset != null && enemyCatalogAsset.TryGetDefinition(spawnGroup.EnemyType, out EnemyCatalogAsset.EnemyArchetypeDefinition definition))
                    {
                        groupScrapReward = definition.ScrapReward;
                    }

                    for (int enemyIndex = 0; enemyIndex < spawnGroup.EnemyCount; enemyIndex++)
                    {
                        string gateName = "FallbackPath";
                        if (mapDefinition != null && mapDefinition.TryGetSpawnGateBySequence(gateSequence, out EnemySpawnGate spawnGate) && spawnGate != null)
                        {
                            gateName = spawnGate.DisplayName;
                        }

                        gateCounts[gateName] = gateCounts.TryGetValue(gateName, out int existing) ? existing + 1 : 1;
                        gateSequence++;
                        totalEnemies++;
                        totalScrap += groupScrapReward;
                    }
                }

                rows.Add(new WavePreviewRow
                {
                    Title = string.IsNullOrWhiteSpace(wave.DisplayName) ? $"Wave {waveIndex + 1:D2}" : wave.DisplayName,
                    TotalEnemies = totalEnemies,
                    TotalScrap = totalScrap,
                    GateBreakdown = string.Join(" / ", gateCounts.Select(pair => $"{pair.Key}: {pair.Value}")),
                    Note = wave.DesignerNote
                });
            }

            return rows;
        }

        internal static TMP_Text FindTextByName(Scene scene, string objectName)
        {
            GameObject gameObject = FindObjectByName(scene, objectName);
            return gameObject != null ? gameObject.GetComponent<TMP_Text>() : null;
        }

        internal static T FindComponentByName<T>(Scene scene, string objectName, bool includeInactive = true) where T : Component
        {
            GameObject gameObject = FindObjectByName(scene, objectName);
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

        internal static Vector3 GuiPointToWorldOnXYPlane(Vector2 guiPosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (plane.Raycast(ray, out float enterDistance))
            {
                return ray.GetPoint(enterDistance);
            }

            return Vector3.zero;
        }

        private static IEnumerable<GameObject> EnumerateHierarchy(GameObject rootObject)
        {
            yield return rootObject;

            Transform rootTransform = rootObject.transform;
            for (int childIndex = 0; childIndex < rootTransform.childCount; childIndex++)
            {
                Transform childTransform = rootTransform.GetChild(childIndex);
                if (childTransform == null)
                {
                    continue;
                }

                foreach (GameObject childObject in EnumerateHierarchy(childTransform.gameObject))
                {
                    yield return childObject;
                }
            }
        }

        private static void AssignObjectReferenceByName(SerializedObject target, string propertyPath, Object value)
        {
            SerializedProperty property = target.FindProperty(propertyPath);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        /// <summary>
        /// Ensures a child group root exists under the chosen road parent.
        ///
        /// Grouping generated road segments by path makes the hierarchy much easier to scan and
        /// lets the author rebuild one route without touching another.
        /// </summary>
        internal static Transform EnsureGeneratedRoadGroupRoot(Transform parent, string groupName)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(groupName);
            if (existing != null)
            {
                return existing;
            }

            GameObject groupObject = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(groupObject, $"鍒涘缓璺緞鍒嗙粍 {groupName}");
            groupObject.transform.SetParent(parent, false);
            groupObject.transform.localPosition = Vector3.zero;
            groupObject.transform.localRotation = Quaternion.identity;
            groupObject.transform.localScale = Vector3.one;
            return groupObject.transform;
        }

        internal static string BuildGeneratedRoadGroupName(string prefix, EnemyPath enemyPath)
        {
            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "GeneratedRoad_" : prefix;
            string safeName = enemyPath != null ? SanitizeHierarchyName(enemyPath.name) : "Path";
            return $"{safePrefix}{safeName}";
        }

        internal static string BuildGeneratedSegmentName(string prefix, int startIndex, int zeroBasedIndex, bool useTwoDigits)
        {
            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "PathSegment_" : prefix;
            int index = Mathf.Max(1, startIndex + zeroBasedIndex);
            string number = useTwoDigits ? index.ToString("D2") : index.ToString();
            return $"{safePrefix}{number}";
        }

        private static string SanitizeHierarchyName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "Path";
            }

            char[] invalidCharacters = System.IO.Path.GetInvalidFileNameChars();
            return new string(rawName.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray())
                .Replace(' ', '_');
        }

        private static float ScoreGeneratedSegmentsAgainstReferenceRoads(
            List<(Vector3 start, Vector3 end)> generatedSegments,
            List<PathSurfaceSegment> referenceRoadSegments)
        {
            float totalScore = 0f;

            foreach ((Vector3 start, Vector3 end) generatedSegment in generatedSegments)
            {
                bool horizontal = Mathf.Abs(generatedSegment.start.y - generatedSegment.end.y) <= 0.001f;
                float segmentLength = horizontal
                    ? Mathf.Abs(generatedSegment.end.x - generatedSegment.start.x)
                    : Mathf.Abs(generatedSegment.end.y - generatedSegment.start.y);
                Vector3 center = (generatedSegment.start + generatedSegment.end) * 0.5f;

                float bestScore = float.MaxValue;
                foreach (PathSurfaceSegment referenceRoad in referenceRoadSegments)
                {
                    if (referenceRoad.IsHorizontal != horizontal)
                    {
                        continue;
                    }

                    float centerlineDistance = horizontal
                        ? Mathf.Abs(center.y - referenceRoad.Center.y) + Mathf.Abs(center.x - referenceRoad.Center.x) * 0.15f
                        : Mathf.Abs(center.x - referenceRoad.Center.x) + Mathf.Abs(center.y - referenceRoad.Center.y) * 0.15f;
                    float referenceLength = horizontal ? referenceRoad.Width : referenceRoad.Height;
                    float lengthPenalty = Mathf.Abs(segmentLength - referenceLength) * 0.08f;
                    float score = centerlineDistance + lengthPenalty;
                    if (score < bestScore)
                    {
                        bestScore = score;
                    }
                }

                totalScore += bestScore == float.MaxValue ? 1000f : bestScore;
            }

            return totalScore;
        }

        private static void AssignObjectReferenceByAssetPath(SerializedObject target, string propertyPath, string assetPath)
        {
            SerializedProperty property = target.FindProperty(propertyPath);
            if (property == null)
            {
                return;
            }

            Type expectedType = typeof(Object);
            if (property.objectReferenceValue != null)
            {
                expectedType = property.objectReferenceValue.GetType();
            }

            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        }
    }

    /// <summary>
    /// The unified authoring toolkit window requested by the user.
    ///
    /// This is intentionally a first-version toolbox:
    /// every requested tool category exists and is usable, but the implementation favors
    /// reliability and scene-author friendliness over hidden automation magic.
    /// </summary>
    public sealed class TowerDefenseMapToolkitWindow : EditorWindow
    {
        [NonSerialized] private static TowerDefenseMapToolkitWindow s_activeWindow;

        [SerializeField] private MapToolkitTab activeTab = MapToolkitTab.PathAlignment;
        [SerializeField] private EnemyPath targetPath;
        [SerializeField] private BattlefieldMapDefinition targetMap;
        [SerializeField] private WaveSpawner targetWaveSpawner;
        [SerializeField] private BuildZone targetBuildZone;
        [SerializeField] private WaveCatalogAsset previewWaveCatalog;
        [SerializeField] private EnemyCatalogAsset previewEnemyCatalog;
        [SerializeField] private GameObject roadTemplate;
        [SerializeField] private Transform roadParent;
        [SerializeField] private GeneratedTurnMode generatedTurnMode = GeneratedTurnMode.HorizontalThenVertical;
        [SerializeField] private bool autoFitGeneratedTurnsToExistingRoads = true;
        [SerializeField] private bool autoInheritRoadThickness = true;
        [SerializeField] private bool autoSnapSpawnGatesAfterRoadGeneration = true;
        [SerializeField] private bool autoSnapDefensePointsAfterRoadGeneration = true;
        [SerializeField] private float generatedRoadThickness = 1.8f;
        [SerializeField] private bool clearExistingGeneratedRoads;
        [SerializeField] private bool groupGeneratedRoadsByPath = true;
        [SerializeField] private string generatedRoadGroupPrefix = "GeneratedRoad_";
        [SerializeField] private string generatedSegmentPrefix = "PathSegment_";
        [SerializeField] private int generatedSegmentStartIndex = 1;
        [SerializeField] private bool useTwoDigitSegmentNumbering = true;
        [SerializeField] private float alignmentTolerance = 0.35f;
        [SerializeField] private int pathSampleCount = 8;
        [SerializeField] private float snapDistanceLimit = 4f;
        [SerializeField] private bool bulkRepairSnapWaypointIssues = true;
        [SerializeField] private bool bulkRepairMoveSpawnGates = true;
        [SerializeField] private bool bulkRepairRegenerateRoads = true;
        [SerializeField] private bool runHudComparisonAgainstSample = true;
        [SerializeField] private bool syncLevel02 = true;
        [SerializeField] private bool syncLevel03 = true;
        [SerializeField] private bool syncLevel04 = true;
        [SerializeField] private bool syncHudCanvas = true;
        [SerializeField] private bool syncCoreReferences = true;
        [SerializeField] private AuthoringBrushMode brushMode = AuthoringBrushMode.BuildZoneShape;
        [SerializeField] private Transform blockerBrushParent;
        [SerializeField] private string blockerBrushReason = "Enemy path area. Structures cannot be deployed here.";
        [SerializeField] private Color brushPreviewColor = new Color(1f, 0.5f, 0.2f, 0.18f);
        [SerializeField] private bool brushActive;
        [SerializeField] private string healthReportFolder = "Assets/Docs/MapHealthReports";
        [SerializeField] private string levelReportFolder = "Assets/Docs/LevelReports";
        [SerializeField] private bool showHealthInfoIssues = true;
        [SerializeField] private bool showHealthWarningIssues = true;
        [SerializeField] private bool showHealthErrorIssues = true;

        private readonly List<ToolkitIssue> _issues = new List<ToolkitIssue>();
        private readonly List<WavePreviewRow> _wavePreviewRows = new List<WavePreviewRow>();
        private readonly List<GeneratedRoadGroupInfo> _generatedRoadGroups = new List<GeneratedRoadGroupInfo>();
        private Vector3 _brushStartWorld;
        private Vector3 _brushCurrentWorld;
        private bool _isBrushDragging;

        [MenuItem("Tools/Tower Defense/Map Development Toolkit")]
        public static void OpenWindow()
        {
            TowerDefenseMapToolkitWindow window = GetWindow<TowerDefenseMapToolkitWindow>("Map Toolkit");
            window.minSize = new Vector2(620f, 420f);
            window.TryAdoptSceneContext();
        }

        private void OnEnable()
        {
            s_activeWindow = this;
            SceneView.duringSceneGui += OnSceneViewGui;
            TryAdoptSceneContext();
        }

        private void OnDisable()
        {
            if (s_activeWindow == this)
            {
                s_activeWindow = null;
            }

            SceneView.duringSceneGui -= OnSceneViewGui;
            brushActive = false;
            _isBrushDragging = false;
        }

        private void OnGUI()
        {
            DrawContextBar();
            EditorGUILayout.Space(8f);

            activeTab = (MapToolkitTab)GUILayout.Toolbar(
                (int)activeTab,
                new[]
                {
                    "Path Check",
                    "Road Build",
                    "Template Sync",
                    "Health Check",
                    "Zone Brush",
                    "Wave Preview"
                });

            EditorGUILayout.Space(10f);

            switch (activeTab)
            {
                case MapToolkitTab.PathAlignment:
                    DrawPathAlignmentTab();
                    break;
                case MapToolkitTab.RoadGenerator:
                    DrawRoadGeneratorTab();
                    break;
                case MapToolkitTab.TemplateSync:
                    DrawTemplateSyncTab();
                    break;
                case MapToolkitTab.HealthCheck:
                    DrawHealthCheckTab();
                    break;
                case MapToolkitTab.ZoneBrush:
                    DrawZoneBrushTab();
                    break;
                case MapToolkitTab.WavePreview:
                    DrawWavePreviewTab();
                    break;
            }
        }

        private void DrawContextBar()
        {
            EditorGUILayout.LabelField("Scene Context", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            targetMap = (BattlefieldMapDefinition)EditorGUILayout.ObjectField("Map", targetMap, typeof(BattlefieldMapDefinition), true);
            targetPath = (EnemyPath)EditorGUILayout.ObjectField("Enemy Path", targetPath, typeof(EnemyPath), true);
            targetWaveSpawner = (WaveSpawner)EditorGUILayout.ObjectField("Wave Spawner", targetWaveSpawner, typeof(WaveSpawner), true);
            targetBuildZone = (BuildZone)EditorGUILayout.ObjectField("Build Zone", targetBuildZone, typeof(BuildZone), true);
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }

            if (GUILayout.Button("Adopt Current Scene Selection"))
            {
                TryAdoptSceneContext();
            }
        }

        private void DrawPathAlignmentTab()
        {
            EditorGUILayout.LabelField("Path-Road Alignment Validator", EditorStyles.boldLabel);
            alignmentTolerance = EditorGUILayout.Slider("Point Tolerance", alignmentTolerance, 0.05f, 1.5f);
            pathSampleCount = EditorGUILayout.IntSlider("Samples Per Span", pathSampleCount, 2, 24);
            snapDistanceLimit = EditorGUILayout.Slider("Snap Distance Limit", snapDistanceLimit, 0.2f, 10f);
            bulkRepairSnapWaypointIssues = EditorGUILayout.Toggle("Bulk Repair: Snap Waypoints", bulkRepairSnapWaypointIssues);
            bulkRepairMoveSpawnGates = EditorGUILayout.Toggle("Bulk Repair: Move Spawn Gates", bulkRepairMoveSpawnGates);
            bulkRepairRegenerateRoads = EditorGUILayout.Toggle("Bulk Repair: Regenerate Roads", bulkRepairRegenerateRoads);
            EditorGUILayout.HelpBox(
                "Selected segment mode works like a map-author shortcut instead of a hidden rule. " +
                "Select two adjacent waypoints to validate exactly that span. " +
                "If you select only one waypoint, the tool validates the segment after it, or the previous segment when the point is the tail.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(targetPath == null))
            {
                if (GUILayout.Button("Analyze Current Path"))
                {
                    AnalyzeOnePath(targetPath);
                }
            }

            using (new EditorGUI.DisabledScope(targetPath == null))
            {
                if (GUILayout.Button("Analyze Selected Segment"))
                {
                    AnalyzeSelectedSegment();
                }
            }

            using (new EditorGUI.DisabledScope(targetMap == null))
            {
                if (GUILayout.Button("Analyze All Paths In Map"))
                {
                    AnalyzeAllPathsInCurrentScene();
                }
            }

            if (GUILayout.Button("Clear Findings"))
            {
                _issues.Clear();
                Repaint();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_issues.Count == 0))
            {
                if (GUILayout.Button("Snap Waypoints For All Point Issues"))
                {
                    SnapAllWaypointIssues();
                }
            }

            using (new EditorGUI.DisabledScope(_issues.Count == 0 && targetMap == null))
            {
                if (GUILayout.Button("Bulk Repair Current Scene"))
                {
                    BulkRepairCurrentSceneIssues();
                }
            }

            using (new EditorGUI.DisabledScope(targetPath == null))
            {
                if (GUILayout.Button("Bulk Repair Selected EnemyPath"))
                {
                    BulkRepairSelectedPath();
                }
            }

            using (new EditorGUI.DisabledScope(Selection.activeTransform == null))
            {
                if (GUILayout.Button("Snap Selected Transform To Nearest Road"))
                {
                    SnapSelectedTransformToRoad();
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawIssueList();
        }

        private void DrawRoadGeneratorTab()
        {
            EditorGUILayout.LabelField("Road Segment Auto Generator", EditorStyles.boldLabel);
            roadParent = (Transform)EditorGUILayout.ObjectField("Road Parent", roadParent, typeof(Transform), true);
            roadTemplate = (GameObject)EditorGUILayout.ObjectField("Road Template", roadTemplate, typeof(GameObject), true);
            generatedTurnMode = (GeneratedTurnMode)EditorGUILayout.EnumPopup("Turn Mode", generatedTurnMode);
            autoFitGeneratedTurnsToExistingRoads = EditorGUILayout.Toggle("Auto Fit Turn Mode To Existing Roads", autoFitGeneratedTurnsToExistingRoads);
            autoInheritRoadThickness = EditorGUILayout.Toggle("Auto Inherit Road Thickness", autoInheritRoadThickness);
            autoSnapSpawnGatesAfterRoadGeneration = EditorGUILayout.Toggle("Auto Snap Spawn Gates", autoSnapSpawnGatesAfterRoadGeneration);
            autoSnapDefensePointsAfterRoadGeneration = EditorGUILayout.Toggle("Auto Snap Defense Point", autoSnapDefensePointsAfterRoadGeneration);
            generatedRoadThickness = EditorGUILayout.Slider("Road Thickness", generatedRoadThickness, 0.6f, 4f);
            clearExistingGeneratedRoads = EditorGUILayout.Toggle("Replace Existing PathSegment_* Under Parent", clearExistingGeneratedRoads);
            groupGeneratedRoadsByPath = EditorGUILayout.Toggle("Group Generated Roads By Path", groupGeneratedRoadsByPath);
            generatedRoadGroupPrefix = EditorGUILayout.TextField("Road Group Prefix", generatedRoadGroupPrefix);
            generatedSegmentPrefix = EditorGUILayout.TextField("Road Segment Prefix", generatedSegmentPrefix);
            generatedSegmentStartIndex = EditorGUILayout.IntField("Segment Start Index", generatedSegmentStartIndex);
            useTwoDigitSegmentNumbering = EditorGUILayout.Toggle("Use Two-Digit Numbering", useTwoDigitSegmentNumbering);

            EditorGUILayout.HelpBox(
                "Workflow: order the waypoints with the Enemy Path Tool first, then come back here to generate or rebuild authored road strips. " +
                "The generator keeps roads orthogonal, adds BoxCollider2D, and also creates PlacementBlocker support so the road layout stays playable.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(targetPath == null))
            {
                if (GUILayout.Button("Generate Road From Current Enemy Path"))
                {
                    GenerateRoadFromPath(targetPath);
                }
            }

            if (_generatedRoadGroups.Count > 0)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Last Generated Groups", EditorStyles.boldLabel);
                foreach (GeneratedRoadGroupInfo groupInfo in _generatedRoadGroups)
                {
                    EditorGUILayout.HelpBox($"{groupInfo.GroupName} | Segments: {groupInfo.SegmentCount}", MessageType.None);
                }
            }
        }

        private void DrawTemplateSyncTab()
        {
            EditorGUILayout.LabelField("SampleScene Template Sync", EditorStyles.boldLabel);
            syncHudCanvas = EditorGUILayout.Toggle("Sync HUDCanvas", syncHudCanvas);
            syncCoreReferences = EditorGUILayout.Toggle("Sync Core Scene References", syncCoreReferences);
            syncLevel02 = EditorGUILayout.ToggleLeft("Apply To Level02", syncLevel02);
            syncLevel03 = EditorGUILayout.ToggleLeft("Apply To Level03", syncLevel03);
            syncLevel04 = EditorGUILayout.ToggleLeft("Apply To Level04", syncLevel04);

            EditorGUILayout.HelpBox(
                "This sync only touches the shared shell: HUD, tower buttons, prototype references, PlacedTowers, PlacementPreviewRoot, and the shared TowerDefenseGame / WaveSpawner wiring. " +
                "It intentionally does not move waypoints, road coordinates, tower pads, or hand-authored map dressing.",
                MessageType.Warning);

            if (GUILayout.Button("Sync Selected Scenes From SampleScene"))
            {
                SyncScenesFromSample();
            }
        }

        private void DrawHealthCheckTab()
        {
            EditorGUILayout.LabelField("Level Health Checker", EditorStyles.boldLabel);
            runHudComparisonAgainstSample = EditorGUILayout.Toggle("Compare HUD Against SampleScene", runHudComparisonAgainstSample);
            healthReportFolder = EditorGUILayout.TextField("Health Report Folder", healthReportFolder);
            levelReportFolder = EditorGUILayout.TextField("Level Report Folder", levelReportFolder);
            EditorGUILayout.LabelField("Severity Filter", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            showHealthErrorIssues = EditorGUILayout.ToggleLeft("Error", showHealthErrorIssues, GUILayout.Width(90f));
            showHealthWarningIssues = EditorGUILayout.ToggleLeft("Warning", showHealthWarningIssues, GUILayout.Width(90f));
            showHealthInfoIssues = EditorGUILayout.ToggleLeft("Info", showHealthInfoIssues, GUILayout.Width(90f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                $"Visible Findings: {GetVisibleIssuesForCurrentTab().Count} / Total Findings: {_issues.Count}",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Check Current Scene"))
            {
                RunHealthCheckOnCurrentScene();
            }

            using (new EditorGUI.DisabledScope(_issues.Count == 0))
            {
                if (GUILayout.Button("Export Findings Markdown"))
                {
                    ExportHealthCheckReport();
                }
            }

            using (new EditorGUI.DisabledScope(targetMap == null && targetWaveSpawner == null))
            {
                if (GUILayout.Button("Export Level Design Report"))
                {
                    ExportLevelDesignReport();
                }
            }

            if (GUILayout.Button("Clear Findings"))
            {
                _issues.Clear();
                Repaint();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            DrawIssueList();
        }

        private void DrawZoneBrushTab()
        {
            EditorGUILayout.LabelField("BuildZone / Blocker Brush", EditorStyles.boldLabel);
            brushMode = (AuthoringBrushMode)EditorGUILayout.EnumPopup("Brush Mode", brushMode);
            targetBuildZone = (BuildZone)EditorGUILayout.ObjectField("Target BuildZone", targetBuildZone, typeof(BuildZone), true);
            blockerBrushParent = (Transform)EditorGUILayout.ObjectField("Blocker Parent", blockerBrushParent, typeof(Transform), true);
            blockerBrushReason = EditorGUILayout.TextField("Blocker Reason", blockerBrushReason);
            brushPreviewColor = EditorGUILayout.ColorField("Preview Color", brushPreviewColor);

            EditorGUILayout.HelpBox(
                "Once the brush is active, drag in Scene view to create a rectangle. BuildZone mode creates a BoxCollider2D under ZoneShapes, while Blocker mode creates a PlacementBlocker rectangle.",
                MessageType.Info);

            string toggleLabel = brushActive ? "Stop Brush" : "Start Brush";
            if (GUILayout.Button(toggleLabel))
            {
                brushActive = !brushActive;
                _isBrushDragging = false;
                SceneView.RepaintAll();
            }
        }

        private void DrawWavePreviewTab()
        {
            EditorGUILayout.LabelField("Wave Previewer", EditorStyles.boldLabel);
            previewWaveCatalog = (WaveCatalogAsset)EditorGUILayout.ObjectField("Wave Catalog", previewWaveCatalog, typeof(WaveCatalogAsset), false);
            previewEnemyCatalog = (EnemyCatalogAsset)EditorGUILayout.ObjectField("Enemy Catalog", previewEnemyCatalog, typeof(EnemyCatalogAsset), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview From Current Scene WaveSpawner"))
            {
                _wavePreviewRows.Clear();
                _wavePreviewRows.AddRange(TowerDefenseMapToolkitUtility.BuildWavePreviewFromScene(targetWaveSpawner, targetMap));
            }

            using (new EditorGUI.DisabledScope(previewWaveCatalog == null))
            {
                if (GUILayout.Button("Preview From WaveCatalogAsset"))
                {
                    _wavePreviewRows.Clear();
                    _wavePreviewRows.AddRange(TowerDefenseMapToolkitUtility.BuildWavePreviewFromCatalog(previewWaveCatalog, previewEnemyCatalog, targetMap));
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_wavePreviewRows.Count == 0)
            {
                EditorGUILayout.HelpBox("Choose a preview source first. The window will show enemy count, scrap reward, and the gate rotation breakdown for each wave.", MessageType.Info);
                return;
            }

            foreach (WavePreviewRow row in _wavePreviewRows)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(row.Title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Enemies: {row.TotalEnemies} | Scrap: {row.TotalScrap}");
                EditorGUILayout.LabelField($"Gates: {row.GateBreakdown}");
                if (!string.IsNullOrWhiteSpace(row.Note))
                {
                    EditorGUILayout.LabelField($"Note: {row.Note}", EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawIssueList()
        {
            List<ToolkitIssue> visibleIssues = GetVisibleIssuesForCurrentTab();
            if (visibleIssues.Count == 0)
            {
                string message = _issues.Count == 0
                    ? "There are no findings to show right now."
                    : "The current severity filter hides all captured findings."
                    ;
                EditorGUILayout.HelpBox(message, MessageType.None);
                return;
            }

            foreach (ToolkitIssue issue in visibleIssues)
            {
                MessageType messageType = issue.Severity == ToolkitIssueSeverity.Error
                    ? MessageType.Error
                    : issue.Severity == ToolkitIssueSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox($"[{issue.Category}] {issue.Message}\nSuggested Action: {issue.SuggestedAction}", messageType);
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(issue.ContextObject == null))
                {
                    if (GUILayout.Button("Ping"))
                    {
                        EditorGUIUtility.PingObject(issue.ContextObject);
                        Selection.activeObject = issue.ContextObject;
                    }
                }

                if (GUILayout.Button("Jump To Object"))
                {
                    JumpToIssue(issue);
                }

                if (GUILayout.Button("Focus Scene"))
                {
                    SceneView.lastActiveSceneView?.Frame(new Bounds(issue.WorldPositionA, Vector3.one * 0.5f), false);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Returns the issue subset that should currently be visible in the window and Scene view.
        ///
        /// The severity filter is intentionally scoped to the health-check workflow. Path-alignment
        /// work usually wants the raw result list, while health-check work benefits from quickly
        /// drilling down to only errors or only warnings.
        /// </summary>
        private List<ToolkitIssue> GetVisibleIssuesForCurrentTab()
        {
            if (activeTab != MapToolkitTab.HealthCheck)
            {
                return _issues.ToList();
            }

            return _issues.Where(ShouldDisplayHealthIssue).ToList();
        }

        private bool ShouldDisplayHealthIssue(ToolkitIssue issue)
        {
            if (issue == null)
            {
                return false;
            }

            return issue.Severity switch
            {
                ToolkitIssueSeverity.Error => showHealthErrorIssues,
                ToolkitIssueSeverity.Warning => showHealthWarningIssues,
                _ => showHealthInfoIssues
            };
        }

        private void JumpToIssue(ToolkitIssue issue)
        {
            if (issue == null)
            {
                return;
            }

            if (issue.ContextObject != null)
            {
                Selection.activeObject = issue.ContextObject;
                EditorGUIUtility.PingObject(issue.ContextObject);
            }

            Bounds focusBounds = BuildIssueFocusBounds(issue);
            SceneView.lastActiveSceneView?.Frame(focusBounds, false);
        }

        private static Bounds BuildIssueFocusBounds(ToolkitIssue issue)
        {
            if (issue.ContextObject is Renderer renderer)
            {
                return renderer.bounds;
            }

            if (issue.ContextObject is Collider2D collider2D)
            {
                return collider2D.bounds;
            }

            if (issue.ContextObject is Component component)
            {
                return new Bounds(component.transform.position, Vector3.one * 0.8f);
            }

            if (issue.ContextObject is GameObject gameObject)
            {
                return new Bounds(gameObject.transform.position, Vector3.one * 0.8f);
            }

            if (issue.WorldPositionB.HasValue)
            {
                Vector3 center = (issue.WorldPositionA + issue.WorldPositionB.Value) * 0.5f;
                Vector3 size = new Vector3(
                    Mathf.Max(0.8f, Mathf.Abs(issue.WorldPositionA.x - issue.WorldPositionB.Value.x)),
                    Mathf.Max(0.8f, Mathf.Abs(issue.WorldPositionA.y - issue.WorldPositionB.Value.y)),
                    0.8f);
                return new Bounds(center, size);
            }

            return new Bounds(issue.WorldPositionA, Vector3.one * 0.8f);
        }

        private void AnalyzeOnePath(EnemyPath enemyPath)
        {
            _issues.Clear();
            if (enemyPath == null)
            {
                return;
            }

            List<PathSurfaceSegment> roadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(enemyPath.gameObject.scene);
            _issues.AddRange(TowerDefenseMapToolkitUtility.AnalyzeEnemyPathAlignment(enemyPath, roadSegments, alignmentTolerance, pathSampleCount));
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Analyzes only the segment the author is currently focusing on.
        ///
        /// This is intentionally selection-driven because that matches the practical map-editing
        /// workflow: the author notices one suspicious elbow, clicks the local points, and wants
        /// the validator to stop shouting about the rest of the path for a moment.
        /// </summary>
        private void AnalyzeSelectedSegment()
        {
            _issues.Clear();
            if (targetPath == null)
            {
                return;
            }

            if (!TryResolveSelectedSegmentIndices(targetPath, out List<int> segmentIndices, out string failureMessage))
            {
                ShowNotification(new GUIContent(failureMessage));
                SceneView.RepaintAll();
                return;
            }

            RemoveNotification();
            List<PathSurfaceSegment> roadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(targetPath.gameObject.scene);
            _issues.AddRange(TowerDefenseMapToolkitUtility.AnalyzeEnemyPathAlignment(
                targetPath,
                roadSegments,
                alignmentTolerance,
                pathSampleCount,
                segmentIndices));
            SceneView.RepaintAll();
        }

        private void AnalyzeAllPathsInCurrentScene()
        {
            _issues.Clear();

            Scene activeScene = SceneManager.GetActiveScene();
            List<PathSurfaceSegment> roadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(activeScene);
            foreach (EnemyPath enemyPath in activeScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyPath>(true)))
            {
                _issues.AddRange(TowerDefenseMapToolkitUtility.AnalyzeEnemyPathAlignment(enemyPath, roadSegments, alignmentTolerance, pathSampleCount));
            }

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Resolves which local path span the author currently means by "this segment".
        ///
        /// Priority order:
        /// 1. Two adjacent selected waypoints.
        /// 2. One selected waypoint plus its next span.
        /// 3. If the selected waypoint is the tail, fall back to the previous span.
        ///
        /// This keeps the rule teachable and easy to remember inside Scene view.
        /// </summary>
        private bool TryResolveSelectedSegmentIndices(EnemyPath enemyPath, out List<int> segmentIndices, out string failureMessage)
        {
            segmentIndices = new List<int>();
            failureMessage = "Select one waypoint or two adjacent waypoints on the current EnemyPath.";

            if (enemyPath == null)
            {
                failureMessage = "No EnemyPath is assigned for segment analysis.";
                return false;
            }

            List<Transform> waypoints = EnemyPathAuthoringUtility.GetWaypointChildren(enemyPath);
            if (waypoints.Count < 2)
            {
                failureMessage = "The current EnemyPath needs at least two waypoints before a segment can be checked.";
                return false;
            }

            List<Transform> selectedWaypoints = EnemyPathAuthoringUtility.GetSelectedExistingWaypoints(enemyPath);
            if (selectedWaypoints.Count == 2)
            {
                int firstIndex = waypoints.IndexOf(selectedWaypoints[0]);
                int secondIndex = waypoints.IndexOf(selectedWaypoints[1]);
                if (firstIndex >= 0 && secondIndex >= 0 && Mathf.Abs(firstIndex - secondIndex) == 1)
                {
                    segmentIndices.Add(Mathf.Min(firstIndex, secondIndex));
                    return true;
                }

                failureMessage = "When two waypoints are selected, they need to be adjacent so the tool knows exactly which span to inspect.";
                return false;
            }

            Transform activeWaypoint = Selection.activeTransform != null && waypoints.Contains(Selection.activeTransform)
                ? Selection.activeTransform
                : null;
            if (activeWaypoint == null)
            {
                return false;
            }

            int activeIndex = waypoints.IndexOf(activeWaypoint);
            if (activeIndex < 0)
            {
                return false;
            }

            if (activeIndex < waypoints.Count - 1)
            {
                segmentIndices.Add(activeIndex);
                return true;
            }

            if (activeIndex > 0)
            {
                segmentIndices.Add(activeIndex - 1);
                return true;
            }

            return false;
        }

        private void SnapAllWaypointIssues()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            List<PathSurfaceSegment> roadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(activeScene);

            foreach (ToolkitIssue issue in _issues)
            {
                if (issue.ContextObject is Transform transform)
                {
                    TowerDefenseMapToolkitUtility.TrySnapWaypointToNearestRoad(transform, roadSegments, snapDistanceLimit);
                }
            }

            if (targetPath != null)
            {
                targetPath.EditorRefreshAuthoringState();
            }

            AnalyzeAllPathsInCurrentScene();
        }

        private void BulkRepairSelectedPath()
        {
            if (targetPath == null)
            {
                return;
            }

            AnalyzeOnePath(targetPath);
            List<ToolkitIssue> scopedIssues = _issues.Where(issue => IssueBelongsToPath(issue, targetPath)).ToList();
            _issues.Clear();
            _issues.AddRange(scopedIssues);
            BulkRepairCurrentSceneIssues();
            AnalyzeOnePath(targetPath);
        }

        /// <summary>
        /// Applies a best-effort batch repair over the currently reported issues.
        ///
        /// The second version of the toolkit is intentionally explicit about what "bulk repair"
        /// means in this project:
        /// - snap misaligned waypoints back to authored road centerlines
        /// - move spawn gates onto the first waypoint they actually use
        /// - regenerate road strips for paths whose spans are still reported as diagonal or uncovered
        ///
        /// This keeps the action predictable and easy to undo, while still saving a lot of manual
        /// cleanup time during map iteration.
        /// </summary>
        private void BulkRepairCurrentSceneIssues()
        {
            if (_issues.Count == 0)
            {
                AnalyzeAllPathsInCurrentScene();
            }

            Scene activeScene = SceneManager.GetActiveScene();
            List<PathSurfaceSegment> roadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(activeScene);
            HashSet<EnemyPath> pathsNeedingRoadRegeneration = new HashSet<EnemyPath>();

            foreach (ToolkitIssue issue in _issues)
            {
                if (bulkRepairSnapWaypointIssues && issue.ContextObject is Transform waypointTransform)
                {
                    TowerDefenseMapToolkitUtility.TrySnapWaypointToNearestRoad(waypointTransform, roadSegments, snapDistanceLimit);
                    continue;
                }

                if (bulkRepairMoveSpawnGates && issue.ContextObject is EnemySpawnGate spawnGate && issue.Category == "Spawn Gate Off First Waypoint")
                {
                    Undo.RecordObject(spawnGate.transform, "鎵归噺淇鍑烘€彛浣嶇疆");
                    Vector3 spawnPosition = spawnGate.GetSpawnPosition();
                    spawnGate.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, spawnGate.transform.position.z);
                    EditorUtility.SetDirty(spawnGate);
                    EditorSceneManager.MarkSceneDirty(spawnGate.gameObject.scene);
                    continue;
                }

                if (bulkRepairRegenerateRoads && issue.ContextObject is EnemyPath brokenPath)
                {
                    if (issue.Category == "Diagonal Route Segment" || issue.Category == "Route Span Missing Road Coverage")
                    {
                        pathsNeedingRoadRegeneration.Add(brokenPath);
                    }
                }
            }

            if (bulkRepairRegenerateRoads)
            {
                foreach (EnemyPath brokenPath in pathsNeedingRoadRegeneration)
                {
                    GenerateRoadFromPath(brokenPath);
                }
            }

            AnalyzeAllPathsInCurrentScene();
        }

        private void SnapSelectedTransformToRoad()
        {
            if (Selection.activeTransform == null)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            List<PathSurfaceSegment> roadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(activeScene);
            if (TowerDefenseMapToolkitUtility.TrySnapTransformToNearestRoadCenterline(
                    Selection.activeTransform,
                    roadSegments,
                    snapDistanceLimit,
                    "Snap Selected Transform To Road Centerline") && targetPath != null)
            {
                targetPath.EditorRefreshAuthoringState();
            }

            AnalyzeAllPathsInCurrentScene();
        }

        private void GenerateRoadFromPath(EnemyPath enemyPath)
        {
            if (enemyPath == null)
            {
                return;
            }

            Scene scene = enemyPath.gameObject.scene;
            List<PathSurfaceSegment> referenceRoadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(scene);
            float effectiveRoadThickness = autoInheritRoadThickness
                ? TowerDefenseMapToolkitUtility.EstimatePreferredRoadThickness(referenceRoadSegments, generatedRoadThickness)
                : generatedRoadThickness;
            Transform baseParent = roadParent != null ? roadParent : TowerDefenseMapToolkitUtility.EnsureNamedRoot(scene, "BattlefieldDecor");
            Transform parent = baseParent;
            if (groupGeneratedRoadsByPath)
            {
                string groupName = TowerDefenseMapToolkitUtility.BuildGeneratedRoadGroupName(generatedRoadGroupPrefix, enemyPath);
                parent = TowerDefenseMapToolkitUtility.EnsureGeneratedRoadGroupRoot(baseParent, groupName);
            }

            if (clearExistingGeneratedRoads)
            {
                List<GameObject> existingSegments = parent.Cast<Transform>()
                    .Where(child => child != null && child.name.StartsWith(generatedSegmentPrefix, StringComparison.Ordinal))
                    .Select(child => child.gameObject)
                    .ToList();

                foreach (GameObject existingSegment in existingSegments)
                {
                    Undo.DestroyObjectImmediate(existingSegment);
                }
            }

            _generatedRoadGroups.RemoveAll(groupInfo =>
                string.Equals(groupInfo.GroupName, parent != null ? parent.name : string.Empty, StringComparison.Ordinal));

            List<Transform> waypoints = EnemyPathAuthoringUtility.GetWaypointChildren(enemyPath);
            List<(Vector3 start, Vector3 end)> generatedSegments = new List<(Vector3 start, Vector3 end)>();
            for (int index = 0; index < waypoints.Count - 1; index++)
            {
                if (waypoints[index] == null || waypoints[index + 1] == null)
                {
                    continue;
                }

                GeneratedTurnMode effectiveTurnMode = autoFitGeneratedTurnsToExistingRoads
                    ? TowerDefenseMapToolkitUtility.ChooseBestGeneratedTurnMode(
                        waypoints[index].position,
                        waypoints[index + 1].position,
                        referenceRoadSegments,
                        generatedTurnMode)
                    : generatedTurnMode;

                generatedSegments.AddRange(TowerDefenseMapToolkitUtility.BuildOrthogonalSegments(
                    waypoints[index].position,
                    waypoints[index + 1].position,
                    effectiveTurnMode));
            }

            for (int segmentIndex = 0; segmentIndex < generatedSegments.Count; segmentIndex++)
            {
                (Vector3 start, Vector3 end) = generatedSegments[segmentIndex];
                Vector3 center = (start + end) * 0.5f;
                bool horizontal = Mathf.Abs(start.y - end.y) <= 0.001f;
                Vector2 size = horizontal
                    ? new Vector2(Mathf.Abs(end.x - start.x) + effectiveRoadThickness, effectiveRoadThickness)
                    : new Vector2(effectiveRoadThickness, Mathf.Abs(end.y - start.y) + effectiveRoadThickness);

                string segmentName = TowerDefenseMapToolkitUtility.BuildGeneratedSegmentName(
                    generatedSegmentPrefix,
                    generatedSegmentStartIndex,
                    segmentIndex,
                    useTwoDigitSegmentNumbering);
                TowerDefenseMapToolkitUtility.CreateRoadSegmentInstance(scene, parent, roadTemplate, segmentName, center, size);
            }

            if (parent != null)
            {
                _generatedRoadGroups.Add(new GeneratedRoadGroupInfo
                {
                    GroupName = parent.name,
                    SegmentCount = generatedSegments.Count
                });
            }

            List<PathSurfaceSegment> generatedRoadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(scene);
            SnapRouteEndpointsAfterRoadGeneration(enemyPath, generatedRoadSegments);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>
        /// Keeps authored route endpoints aligned with the newly generated road.
        ///
        /// This closes a very common map-authoring gap: the road gets regenerated correctly, but
        /// the spawn marker or defense marker stays in its old off-center position and immediately
        /// makes the scene look inconsistent again.
        /// </summary>
        private void SnapRouteEndpointsAfterRoadGeneration(EnemyPath enemyPath, List<PathSurfaceSegment> roadSegments)
        {
            if (enemyPath == null || roadSegments == null || roadSegments.Count == 0)
            {
                return;
            }

            List<Transform> waypoints = EnemyPathAuthoringUtility.GetWaypointChildren(enemyPath);
            if (waypoints.Count == 0)
            {
                return;
            }

            Vector3 startPoint = ResolveNearestRoadCenter(roadSegments, waypoints[0].position);
            Vector3 endPoint = ResolveNearestRoadCenter(roadSegments, waypoints[waypoints.Count - 1].position);

            IEnumerable<EnemySpawnGate> sceneSpawnGates = enemyPath.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnemySpawnGate>(true))
                .Where(spawnGate => spawnGate != null && spawnGate.EnemyPath == enemyPath);

            List<EnemySpawnGate> connectedSpawnGates = sceneSpawnGates.ToList();
            if (autoSnapSpawnGatesAfterRoadGeneration)
            {
                foreach (EnemySpawnGate spawnGate in connectedSpawnGates)
                {
                    MoveSceneTransformToWorldPoint(spawnGate.transform, startPoint, "Snap Spawn Gate To Generated Road");
                }
            }

            if (!autoSnapDefensePointsAfterRoadGeneration)
            {
                return;
            }

            HashSet<DefensePointFlag> defensePoints = new HashSet<DefensePointFlag>(
                connectedSpawnGates
                    .Select(spawnGate => spawnGate.TargetDefensePoint)
                    .Where(defensePoint => defensePoint != null));

            if (defensePoints.Count == 0)
            {
                BattlefieldMapDefinition mapDefinition = targetMap != null && targetMap.gameObject.scene == enemyPath.gameObject.scene
                    ? targetMap
                    : TowerDefenseMapToolkitUtility.FindFirstComponentInScene<BattlefieldMapDefinition>(enemyPath.gameObject.scene);
                if (mapDefinition != null && mapDefinition.TryGetPrimaryDefensePoint(out DefensePointFlag primaryDefensePoint) && primaryDefensePoint != null)
                {
                    defensePoints.Add(primaryDefensePoint);
                }
            }

            foreach (DefensePointFlag defensePoint in defensePoints)
            {
                MoveSceneTransformToWorldPoint(defensePoint.transform, endPoint, "Snap Defense Point To Generated Road");
            }
        }

        private static Vector3 ResolveNearestRoadCenter(List<PathSurfaceSegment> roadSegments, Vector3 preferredPoint)
        {
            return TowerDefenseMapToolkitUtility.TryFindNearestRoadPoint(
                roadSegments,
                preferredPoint,
                float.MaxValue,
                out _,
                out Vector3 nearestRoadPoint,
                out _)
                ? nearestRoadPoint
                : preferredPoint;
        }

        private static void MoveSceneTransformToWorldPoint(Transform targetTransform, Vector3 worldPoint, string undoActionName)
        {
            if (targetTransform == null)
            {
                return;
            }

            Undo.RecordObject(targetTransform, string.IsNullOrWhiteSpace(undoActionName) ? "Move Scene Transform" : undoActionName);
            targetTransform.position = new Vector3(worldPoint.x, worldPoint.y, targetTransform.position.z);
            EditorUtility.SetDirty(targetTransform);
            EditorSceneManager.MarkSceneDirty(targetTransform.gameObject.scene);
        }

        private void SyncScenesFromSample()
        {
            string[] targetScenePaths = BuildSelectedTemplateTargets();
            if (targetScenePaths.Length == 0)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene originalActiveScene = SceneManager.GetActiveScene();
            Scene sampleScene = EditorSceneManager.OpenScene(TowerDefenseMapToolkitUtility.SampleScenePath, OpenSceneMode.Additive);

            try
            {
                foreach (string targetScenePath in targetScenePaths)
                {
                    Scene targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Additive);
                    try
                    {
                        if (syncHudCanvas || syncCoreReferences)
                        {
                            TowerDefenseMapToolkitUtility.SyncSceneFromSample(sampleScene, targetScene);
                        }

                        EditorSceneManager.SaveScene(targetScene);
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(targetScene, true);
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(sampleScene, true);
            }

            if (!string.IsNullOrWhiteSpace(originalActiveScene.path))
            {
                EditorSceneManager.OpenScene(originalActiveScene.path, OpenSceneMode.Single);
            }
        }

        private void RunHealthCheckOnCurrentScene()
        {
            _issues.Clear();
            Scene activeScene = SceneManager.GetActiveScene();

            TowerDefenseGame towerDefenseGame = TowerDefenseMapToolkitUtility.FindFirstComponentInScene<TowerDefenseGame>(activeScene);
            WaveSpawner waveSpawner = targetWaveSpawner != null ? targetWaveSpawner : TowerDefenseMapToolkitUtility.FindFirstComponentInScene<WaveSpawner>(activeScene);
            BattlefieldMapDefinition mapDefinition = targetMap != null ? targetMap : TowerDefenseMapToolkitUtility.FindFirstComponentInScene<BattlefieldMapDefinition>(activeScene);
            List<PathSurfaceSegment> roadSegments = TowerDefenseMapToolkitUtility.CollectPathSurfaceSegments(activeScene);

            if (towerDefenseGame == null)
            {
                _issues.Add(new ToolkitIssue
                {
                    Severity = ToolkitIssueSeverity.Error,
                    Category = "Missing TowerDefenseGame",
                    Message = "The current scene does not contain a TowerDefenseGame.",
                    SuggestedAction = "Add a TowerDefenseGame scene object or sync the shared gameplay shell from SampleScene."
                });
            }

            if (mapDefinition == null)
            {
                _issues.Add(new ToolkitIssue
                {
                    Severity = ToolkitIssueSeverity.Error,
                    Category = "Missing BattlefieldMapDefinition",
                    Message = "The current scene does not contain a BattlefieldMapDefinition.",
                    SuggestedAction = "Add a BattlefieldMapDefinition and collect the scene-authored references."
                });
            }

            if (mapDefinition != null)
            {
                mapDefinition.CollectSceneReferences();

                if (mapDefinition.BuildZone == null)
                {
                    _issues.Add(new ToolkitIssue
                    {
                        Severity = ToolkitIssueSeverity.Error,
                        Category = "缂哄皯 BuildZone",
                        Message = "BattlefieldMapDefinition is not wired to a BuildZone.",
                        ContextObject = mapDefinition,
                        SuggestedAction = "Add a BuildZone or run the scene reference collection again."
                    });
                }

                if (!mapDefinition.HasAnyValidSpawnGate())
                {
                    _issues.Add(new ToolkitIssue
                    {
                        Severity = ToolkitIssueSeverity.Error,
                        Category = "Missing Spawn Gate",
                        Message = "The current map does not contain a valid EnemySpawnGate.",
                        ContextObject = mapDefinition,
                        SuggestedAction = "Add an EnemySpawnGate and wire it to an EnemyPath."
                    });
                }

                if (!mapDefinition.TryGetPrimaryDefensePoint(out DefensePointFlag defensePoint))
                {
                    _issues.Add(new ToolkitIssue
                    {
                        Severity = ToolkitIssueSeverity.Error,
                        Category = "Missing Defense Point",
                        Message = "The current map does not contain a valid DefensePointFlag.",
                        ContextObject = mapDefinition,
                        SuggestedAction = "Add a DefensePointFlag and keep the last road span aligned to it."
                    });
                }
            }

            foreach (EnemyPath enemyPath in activeScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyPath>(true)))
            {
                _issues.AddRange(TowerDefenseMapToolkitUtility.AnalyzeEnemyPathAlignment(enemyPath, roadSegments, alignmentTolerance, pathSampleCount));
            }

            if (mapDefinition != null)
            {
                for (int gateIndex = 0; gateIndex < mapDefinition.SpawnGateCount; gateIndex++)
                {
                    if (!mapDefinition.TryGetSpawnGateBySequence(gateIndex, out EnemySpawnGate spawnGate) || spawnGate == null)
                    {
                        continue;
                    }

                    Vector3 spawnPosition = spawnGate.GetSpawnPosition();
                    float distance = Vector2.Distance(spawnGate.transform.position, spawnPosition);
                    if (distance > 0.3f)
                    {
                        _issues.Add(new ToolkitIssue
                        {
                            Severity = ToolkitIssueSeverity.Warning,
                            Category = "Spawn Gate Off First Waypoint",
                            Message = $"{spawnGate.name} is {distance:0.00} units away from the first waypoint it actually uses.",
                            ContextObject = spawnGate,
                            WorldPositionA = spawnGate.transform.position,
                            WorldPositionB = spawnPosition,
                            SuggestedAction = "Move the spawn gate back onto its first waypoint."
                        });
                    }
                }
            }

            if (runHudComparisonAgainstSample && !string.IsNullOrWhiteSpace(activeScene.path))
            {
                Scene sampleScene = EditorSceneManager.OpenScene(TowerDefenseMapToolkitUtility.SampleScenePath, OpenSceneMode.Additive);
                try
                {
                    _issues.AddRange(TowerDefenseMapToolkitUtility.CompareHudAgainstSample(sampleScene, activeScene));
                }
                finally
                {
                    EditorSceneManager.CloseScene(sampleScene, true);
                    EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Additive);
                }
            }

            if (waveSpawner == null)
            {
                _issues.Add(new ToolkitIssue
                {
                    Severity = ToolkitIssueSeverity.Warning,
                    Category = "Missing WaveSpawner",
                    Message = "The current scene does not contain a WaveSpawner.",
                    SuggestedAction = "Add a WaveSpawner or sync the shared gameplay shell from SampleScene."
                });
            }

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Exports the current issue list as a Markdown checklist.
        ///
        /// The goal is not to produce a perfect report format for every future workflow.
        /// It is to give the map author a concrete, shareable TODO list he can keep beside
        /// the editor while fixing one scene at a time.
        /// </summary>
        private void ExportHealthCheckReport()
        {
            List<ToolkitIssue> visibleIssues = GetVisibleIssuesForCurrentTab();
            if (visibleIssues.Count == 0)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            string sceneName = !string.IsNullOrWhiteSpace(activeScene.name) ? activeScene.name : "Scene";
            string folderPath = string.IsNullOrWhiteSpace(healthReportFolder) ? "Assets/Docs/MapHealthReports" : healthReportFolder;
            string absoluteFolderPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), folderPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            System.IO.Directory.CreateDirectory(absoluteFolderPath);

            string safeSceneName = string.Concat(sceneName.Select(character => char.IsLetterOrDigit(character) ? character : '_'));
            string fileName = $"{safeSceneName}_HealthReport_{DateTime.Now:yyyyMMdd_HHmmss}.md";
            string assetPath = $"{folderPath.TrimEnd('/')}/{fileName}";
            string absolutePath = System.IO.Path.Combine(absoluteFolderPath, fileName);

            IEnumerable<ToolkitIssue> orderedIssues = visibleIssues
                .OrderByDescending(issue => issue.Severity)
                .ThenBy(issue => issue.Category)
                .ThenBy(issue => issue.Message);

            List<string> lines = new List<string>
            {
                $"# {sceneName} Health Report",
                string.Empty,
                $"- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"- Scene Path: {activeScene.path}",
                $"- Visible Issues: {visibleIssues.Count}",
                $"- All Captured Issues: {_issues.Count}",
                string.Empty,
                "## Checklist",
                string.Empty
            };

            foreach (ToolkitIssue issue in orderedIssues)
            {
                string severity = issue.Severity.ToString().ToUpperInvariant();
                string objectName = issue.ContextObject != null ? issue.ContextObject.name : "(no object)";
                lines.Add($"- [ ] [{severity}] {issue.Category} | {objectName}");
                lines.Add($"  - Message: {issue.Message}");
                if (!string.IsNullOrWhiteSpace(issue.SuggestedAction))
                {
                    lines.Add($"  - Suggested Action: {issue.SuggestedAction}");
                }

                if (issue.WorldPositionB.HasValue)
                {
                    lines.Add($"  - World Span: {issue.WorldPositionA} -> {issue.WorldPositionB.Value}");
                }
                else
                {
                    lines.Add($"  - World Position: {issue.WorldPositionA}");
                }

                lines.Add(string.Empty);
            }

            System.IO.File.WriteAllLines(absolutePath, lines);
            AssetDatabase.Refresh();
            Debug.Log($"[MapToolkit] Health report exported: {assetPath}");
        }

        /// <summary>
        /// Exports a planner-facing report instead of a pure issue checklist.
        /// </summary>
        private void ExportLevelDesignReport()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            BattlefieldMapDefinition effectiveMap = targetMap != null ? targetMap : TowerDefenseMapToolkitUtility.FindFirstComponentInScene<BattlefieldMapDefinition>(activeScene);
            WaveSpawner effectiveWaveSpawner = targetWaveSpawner != null ? targetWaveSpawner : TowerDefenseMapToolkitUtility.FindFirstComponentInScene<WaveSpawner>(activeScene);

            string folderPath = string.IsNullOrWhiteSpace(levelReportFolder) ? "Assets/Docs/LevelReports" : levelReportFolder;
            string absoluteFolderPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), folderPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            System.IO.Directory.CreateDirectory(absoluteFolderPath);

            string sceneName = !string.IsNullOrWhiteSpace(activeScene.name) ? activeScene.name : "Scene";
            string safeSceneName = string.Concat(sceneName.Select(character => char.IsLetterOrDigit(character) ? character : '_'));
            string fileName = $"{safeSceneName}_LevelDesignReport_{DateTime.Now:yyyyMMdd_HHmmss}.md";
            string assetPath = $"{folderPath.TrimEnd('/')}/{fileName}";
            string absolutePath = System.IO.Path.Combine(absoluteFolderPath, fileName);

            string markdown = LevelDesignReportBuilder.BuildMarkdown(activeScene, effectiveMap, effectiveWaveSpawner);
            System.IO.File.WriteAllText(absolutePath, markdown);
            AssetDatabase.Refresh();
            Debug.Log($"[MapToolkit] Level design report exported: {assetPath}");
        }

        private void OnSceneViewGui(SceneView sceneView)
        {
            if (s_activeWindow != this)
            {
                return;
            }

            DrawIssueGizmos();

            if (!brushActive)
            {
                return;
            }

            HandleBrushInput(Event.current);
        }

        private void DrawIssueGizmos()
        {
            List<ToolkitIssue> visibleIssues = GetVisibleIssuesForCurrentTab();
            if (visibleIssues.Count == 0)
            {
                return;
            }

            foreach (ToolkitIssue issue in visibleIssues)
            {
                Color color = issue.Severity == ToolkitIssueSeverity.Error
                    ? new Color(1f, 0.22f, 0.22f, 0.95f)
                    : issue.Severity == ToolkitIssueSeverity.Warning
                        ? new Color(1f, 0.72f, 0.22f, 0.92f)
                        : new Color(0.3f, 0.85f, 1f, 0.92f);

                Handles.color = color;
                Handles.DrawSolidDisc(issue.WorldPositionA, Vector3.forward, 0.14f);
                Handles.Label(issue.WorldPositionA + Vector3.up * 0.22f, issue.Category);

                if (issue.WorldPositionB.HasValue)
                {
                    Handles.DrawLine(issue.WorldPositionA, issue.WorldPositionB.Value);
                    Handles.DrawSolidDisc(issue.WorldPositionB.Value, Vector3.forward, 0.10f);
                }
            }
        }

        private void HandleBrushInput(Event currentEvent)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Vector3 worldPoint = TowerDefenseMapToolkitUtility.GuiPointToWorldOnXYPlane(currentEvent.mousePosition);

            if (currentEvent.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                _brushStartWorld = worldPoint;
                _brushCurrentWorld = worldPoint;
                _isBrushDragging = true;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && _isBrushDragging && currentEvent.button == 0)
            {
                _brushCurrentWorld = worldPoint;
                currentEvent.Use();
                SceneView.RepaintAll();
            }
            else if (currentEvent.type == EventType.MouseUp && _isBrushDragging && currentEvent.button == 0)
            {
                _brushCurrentWorld = worldPoint;
                CreateBrushResult();
                _isBrushDragging = false;
                currentEvent.Use();
                SceneView.RepaintAll();
            }

            if (_isBrushDragging)
            {
                Rect rect = BuildRect(_brushStartWorld, _brushCurrentWorld);
                DrawBrushPreview(rect);
            }
        }

        private void CreateBrushResult()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Rect worldRect = BuildRect(_brushStartWorld, _brushCurrentWorld);
            if (worldRect.width <= 0.05f || worldRect.height <= 0.05f)
            {
                return;
            }

            if (brushMode == AuthoringBrushMode.BuildZoneShape)
            {
                Transform zoneRoot = TowerDefenseMapToolkitUtility.EnsureZoneShapeRoot(targetBuildZone);
                if (zoneRoot == null)
                {
                    Debug.LogWarning("BuildZone brush requires a target BuildZone.");
                    return;
                }

                TowerDefenseMapToolkitUtility.CreateBrushRectangle(activeScene, AuthoringBrushMode.BuildZoneShape, zoneRoot, worldRect, string.Empty, brushPreviewColor);
                targetBuildZone.CollectZoneShapeColliders();
                EditorUtility.SetDirty(targetBuildZone);
                EditorSceneManager.MarkSceneDirty(activeScene);
                return;
            }

            Transform parent = blockerBrushParent != null
                ? blockerBrushParent
                : targetMap != null
                    ? targetMap.transform
                    : null;
            if (parent == null)
            {
                parent = TowerDefenseMapToolkitUtility.EnsureNamedRoot(activeScene, "BattlefieldDecor");
            }

            TowerDefenseMapToolkitUtility.CreateBrushRectangle(activeScene, AuthoringBrushMode.PlacementBlocker, parent, worldRect, blockerBrushReason, brushPreviewColor);
            EditorSceneManager.MarkSceneDirty(activeScene);
        }

        private void DrawBrushPreview(Rect worldRect)
        {
            Handles.color = new Color(brushPreviewColor.r, brushPreviewColor.g, brushPreviewColor.b, 0.95f);
            Vector3 bottomLeft = new Vector3(worldRect.xMin, worldRect.yMin, 0f);
            Vector3 bottomRight = new Vector3(worldRect.xMax, worldRect.yMin, 0f);
            Vector3 topRight = new Vector3(worldRect.xMax, worldRect.yMax, 0f);
            Vector3 topLeft = new Vector3(worldRect.xMin, worldRect.yMax, 0f);
            Handles.DrawSolidRectangleWithOutline(
                new[] { bottomLeft, bottomRight, topRight, topLeft },
                new Color(brushPreviewColor.r, brushPreviewColor.g, brushPreviewColor.b, 0.12f),
                brushPreviewColor);
        }

        private void TryAdoptSceneContext()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            targetMap = targetMap != null ? targetMap : TowerDefenseMapToolkitUtility.FindFirstComponentInScene<BattlefieldMapDefinition>(activeScene);
            targetPath = targetPath != null ? targetPath : TowerDefenseMapToolkitUtility.FindFirstComponentInScene<EnemyPath>(activeScene);
            targetWaveSpawner = targetWaveSpawner != null ? targetWaveSpawner : TowerDefenseMapToolkitUtility.FindFirstComponentInScene<WaveSpawner>(activeScene);
            targetBuildZone = targetBuildZone != null ? targetBuildZone : TowerDefenseMapToolkitUtility.FindFirstComponentInScene<BuildZone>(activeScene);
            roadParent = roadParent != null ? roadParent : TowerDefenseMapToolkitUtility.FindObjectByName(activeScene, "BattlefieldDecor")?.transform;
            blockerBrushParent = blockerBrushParent != null ? blockerBrushParent : roadParent;
        }

        private static bool IssueBelongsToPath(ToolkitIssue issue, EnemyPath enemyPath)
        {
            if (issue == null || enemyPath == null)
            {
                return false;
            }

            if (issue.ContextObject == enemyPath)
            {
                return true;
            }

            if (issue.ContextObject is Transform transform)
            {
                Transform waypointRoot = enemyPath.WaypointRoot != null ? enemyPath.WaypointRoot : enemyPath.transform;
                return transform.IsChildOf(waypointRoot);
            }

            if (issue.ContextObject is EnemySpawnGate spawnGate)
            {
                return spawnGate.EnemyPath == enemyPath;
            }

            return false;
        }

        private string[] BuildSelectedTemplateTargets()
        {
            List<string> targets = new List<string>();
            if (syncLevel02)
            {
                targets.Add(TowerDefenseMapToolkitUtility.Level02ScenePath);
            }

            if (syncLevel03)
            {
                targets.Add(TowerDefenseMapToolkitUtility.Level03ScenePath);
            }

            if (syncLevel04)
            {
                targets.Add(TowerDefenseMapToolkitUtility.Level04ScenePath);
            }

            return targets.ToArray();
        }

        private Rect BuildRect(Vector3 start, Vector3 end)
        {
            float minX = Mathf.Min(start.x, end.x);
            float maxX = Mathf.Max(start.x, end.x);
            float minY = Mathf.Min(start.y, end.y);
            float maxY = Mathf.Max(start.y, end.y);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static string BuildPathSegmentName(int index)
        {
            int alphabetIndex = index % 26;
            int cycle = index / 26;
            char letter = (char)('A' + alphabetIndex);
            return cycle == 0 ? $"PathSegment_{letter}" : $"PathSegment_{letter}{cycle + 1}";
        }
    }
}

