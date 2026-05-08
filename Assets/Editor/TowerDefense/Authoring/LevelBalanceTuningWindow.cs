using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TowerDefense.Editor
{
    internal enum BalancePresetKind
    {
        Simple,
        Standard,
        Hard
    }

    /// <summary>
    /// One-stop tuning console for level designers and gameplay designers.
    ///
    /// Design intent:
    /// - The user asked for a planner-facing tool, not another scattered set of Inspectors.
    /// - Current balance knobs live in several places:
    ///   - scene objects (`TowerDefenseGame`, `WaveSpawner`, `BattlefieldMapDefinition`)
    ///   - referenced prefabs (`RelayTower`, `DefenseTower`)
    /// - Designers should be able to open one window, inspect the current level, and adjust
    ///   the major balance parameters without hunting through the hierarchy and asset folders.
    ///
    /// Scope of this first version:
    /// - current level economy and placement rules
    /// - route / relay scene limits
    /// - wave timing and wave-by-wave enemy values
    /// - relay prefab tuning
    /// - three combat tower prefab tunings
    /// - quick batch helpers for common balance passes
    ///
    /// Non-goals:
    /// - replacing every Inspector in the project
    /// - hiding scene ownership; the scene and prefab assets still remain the source of truth
    /// - inventing a new runtime data model just for tooling
    /// </summary>
    public sealed class LevelBalanceTuningWindow : EditorWindow
    {
        /// <summary>
        /// One preset bundle used by the planner-facing preset buttons.
        ///
        /// These presets intentionally operate on top of the current authored numbers instead of
        /// introducing a second hidden balance database. That keeps the workflow transparent:
        /// the scene and prefab assets still remain the single source of truth.
        /// </summary>
        private sealed class BalancePresetDefinition
        {
            public BalancePresetKind Kind;
            public string Label;
            public string Description;
            public float StartingScrapMultiplier = 1f;
            public float StartingBaseHealthMultiplier = 1f;
            public float BuildCostMultiplier = 1f;
            public float UpgradeCostMultiplier = 1f;
            public int RelayLimitDelta;
            public float WaveCountMultiplier = 1f;
            public float WaveHealthMultiplier = 1f;
            public float WaveSpeedMultiplier = 1f;
            public float WaveRewardMultiplier = 1f;
            public float WaveIntervalMultiplier = 1f;
            public float RelayRangeMultiplier = 1f;
            public int RelayCapacityDelta;
            public float TowerRangeMultiplier = 1f;
            public float TowerAttackIntervalMultiplier = 1f;
            public float TowerDamageMultiplier = 1f;
            public float TowerPowerMultiplier = 1f;
            public float BombRadiusMultiplier = 1f;
            public float SlowStrengthMultiplier = 1f;
        }

        [SerializeField] private TowerDefenseGame currentGame;
        [SerializeField] private WaveSpawner currentWaveSpawner;
        [SerializeField] private BattlefieldMapDefinition currentMap;

        [SerializeField] private bool showPresets = true;
        [SerializeField] private bool showCoreEconomy = true;
        [SerializeField] private bool showWaveTuning = true;
        [SerializeField] private bool showFallbackSceneWaveArray = false;
        [SerializeField] private bool showRelayTuning = true;
        [SerializeField] private bool showSingleTargetTuning = true;
        [SerializeField] private bool showSlowFieldTuning = true;
        [SerializeField] private bool showBombardTuning = true;
        [SerializeField] private bool showQuickBatchTools = true;
        [SerializeField] private bool showAdvancedRawEditors = false;

        [SerializeField] private float waveCountMultiplier = 1.1f;
        [SerializeField] private float waveHealthMultiplier = 1.15f;
        [SerializeField] private float waveSpeedMultiplier = 1.05f;
        [SerializeField] private float waveRewardMultiplier = 1f;
        [SerializeField] private float waveIntervalMultiplier = 0.95f;
        [SerializeField] private float buildCostMultiplier = 1.1f;
        [SerializeField] private float upgradeCostMultiplier = 1.1f;

        [MenuItem("Tools/Tower Defense/Authoring/Level Balance Tuning Console")]
        public static void OpenWindow()
        {
            LevelBalanceTuningWindow window = GetWindow<LevelBalanceTuningWindow>("Level Balance");
            window.minSize = new Vector2(720f, 520f);
            window.AdoptCurrentSceneContext();
        }

        private void OnEnable()
        {
            AdoptCurrentSceneContext();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(8f);

            if (currentGame == null && currentWaveSpawner == null && currentMap == null)
            {
                EditorGUILayout.HelpBox(
                    "No level context is assigned yet. Open a gameplay scene and press 'Adopt Current Scene'.",
                    MessageType.Warning);
                return;
            }

            DrawSceneSummary();
            EditorGUILayout.Space(8f);

            showPresets = EditorGUILayout.Foldout(showPresets, "Preset Difficulty Profiles", true);
            if (showPresets)
            {
                DrawPresetSection();
            }

            showCoreEconomy = EditorGUILayout.Foldout(showCoreEconomy, "Core Economy And Placement", true);
            if (showCoreEconomy)
            {
                DrawCoreEconomySection();
            }

            showWaveTuning = EditorGUILayout.Foldout(showWaveTuning, "Wave Tuning", true);
            if (showWaveTuning)
            {
                DrawWaveSection();
            }

            showRelayTuning = EditorGUILayout.Foldout(showRelayTuning, "Relay Prototype Tuning", true);
            if (showRelayTuning)
            {
                DrawRelaySection();
            }

            showSingleTargetTuning = EditorGUILayout.Foldout(showSingleTargetTuning, "Single Target Tower Tuning", true);
            if (showSingleTargetTuning)
            {
                DrawDefenseTowerSection(
                    "singleTargetTowerPrototypeReference",
                    "singleTargetTuning",
                    "Single-target turret tuning asset");
            }

            showSlowFieldTuning = EditorGUILayout.Foldout(showSlowFieldTuning, "Slow Field Tower Tuning", true);
            if (showSlowFieldTuning)
            {
                DrawDefenseTowerSection(
                    "slowFieldTowerPrototypeReference",
                    "slowFieldTuning",
                    "Slow-field tower tuning asset");
            }

            showBombardTuning = EditorGUILayout.Foldout(showBombardTuning, "Bombard Tower Tuning", true);
            if (showBombardTuning)
            {
                DrawDefenseTowerSection(
                    "bombardTowerPrototypeReference",
                    "bombardTuning",
                    "Bombard tower tuning asset");
            }

            showQuickBatchTools = EditorGUILayout.Foldout(showQuickBatchTools, "Quick Batch Helpers", true);
            if (showQuickBatchTools)
            {
                DrawQuickBatchSection();
            }

            showAdvancedRawEditors = EditorGUILayout.Foldout(showAdvancedRawEditors, "Advanced Raw Editors", true);
            if (showAdvancedRawEditors)
            {
                DrawAdvancedRawEditors();
            }
        }

        /// <summary>
        /// Keeps the top bar practical and safe.
        ///
        /// Designers often bounce between scenes while tuning, so the window always exposes:
        /// - a refresh button for current scene adoption
        /// - a save button for the active scene and dirty assets
        /// - explicit object fields in case the user wants to pin a different context manually
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Level Balance Tuning Console", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Adopt Current Scene", GUILayout.Width(160f)))
            {
                AdoptCurrentSceneContext();
            }

            if (GUILayout.Button("Save Scene + Assets", GUILayout.Width(160f)))
            {
                SaveCurrentWork();
            }
            EditorGUILayout.EndHorizontal();

            currentGame = (TowerDefenseGame)EditorGUILayout.ObjectField("TowerDefenseGame", currentGame, typeof(TowerDefenseGame), true);
            currentWaveSpawner = (WaveSpawner)EditorGUILayout.ObjectField("WaveSpawner", currentWaveSpawner, typeof(WaveSpawner), true);
            currentMap = (BattlefieldMapDefinition)EditorGUILayout.ObjectField("BattlefieldMap", currentMap, typeof(BattlefieldMapDefinition), true);
        }

        private void DrawSceneSummary()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            string sceneName = string.IsNullOrWhiteSpace(activeScene.name) ? "(No Scene)" : activeScene.name;

            int gateCount = currentMap != null ? currentMap.SpawnGateCount : 0;
            int defenseCount = currentMap != null ? currentMap.DefensePointCount : 0;
            int relayLimit = currentMap != null ? currentMap.RelayLimit : 0;

            EditorGUILayout.HelpBox(
                $"Scene: {sceneName}\n" +
                $"Spawn Gates: {gateCount} | Defense Points: {defenseCount} | Relay Limit: {relayLimit}\n" +
                $"This window edits the current level scene plus the prototype prefabs referenced by this scene.",
                MessageType.Info);
        }

        /// <summary>
        /// Draws one-click preset actions for planners.
        ///
        /// The presets are intentionally opinionated:
        /// - Easy gives the player more economy and slightly gentler waves
        /// - Standard is the neutral reference point
        /// - Hard squeezes economy and makes waves scale up faster
        ///
        /// The section also mirrors the preset multipliers into the existing batch-helper fields
        /// so designers can see the numbers they just applied.
        /// </summary>
        private void DrawPresetSection()
        {
            EditorGUILayout.HelpBox(
                "Preset buttons apply directly on top of the current authored level numbers. " +
                "Use them as a fast difficulty pass, then fine-tune the exact values below.",
                MessageType.Warning);

            DrawPresetCard(BalancePresetKind.Simple);
            DrawPresetCard(BalancePresetKind.Standard);
            DrawPresetCard(BalancePresetKind.Hard);
        }

        private void DrawPresetCard(BalancePresetKind presetKind)
        {
            BalancePresetDefinition preset = GetPresetDefinition(presetKind);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(preset.Label, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(preset.Description, EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button($"Apply {preset.Label} Preset"))
            {
                ApplyPreset(preset);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCoreEconomySection()
        {
            if (currentGame == null)
            {
                EditorGUILayout.HelpBox("TowerDefenseGame is required for economy tuning.", MessageType.Warning);
                return;
            }

            SerializedObject serializedGame = new SerializedObject(currentGame);
            serializedGame.Update();

            EditorGUILayout.LabelField("Starting Resources", EditorStyles.miniBoldLabel);
            DrawPropertyField(serializedGame, "startingScrap");
            DrawPropertyField(serializedGame, "startingBaseHealth");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Build Costs", EditorStyles.miniBoldLabel);
            DrawPropertyField(serializedGame, "relayTowerCost");
            DrawPropertyField(serializedGame, "singleTargetTowerCost");
            DrawPropertyField(serializedGame, "slowFieldTowerCost");
            DrawPropertyField(serializedGame, "bombardTowerCost");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Placement Rules", EditorStyles.miniBoldLabel);
            DrawPropertyField(serializedGame, "relayPlacementRadius");
            DrawPropertyField(serializedGame, "defensePlacementRadius");
            DrawPropertyField(serializedGame, "relayExpansionSquareSize");
            DrawPropertyField(serializedGame, "defenseExpansionSquareSize");
            DrawPropertyField(serializedGame, "initialPlacementSquareCenter");
            DrawPropertyField(serializedGame, "initialPlacementSquareSize");

            serializedGame.ApplyModifiedProperties();
            EditorUtility.SetDirty(currentGame);

            if (currentMap != null)
            {
                SerializedObject serializedMap = new SerializedObject(currentMap);
                serializedMap.Update();
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Map Limit", EditorStyles.miniBoldLabel);
                DrawPropertyField(serializedMap, "relayLimit");
                serializedMap.ApplyModifiedProperties();
                EditorUtility.SetDirty(currentMap);
            }
        }

        private void DrawWaveSection()
        {
            if (currentWaveSpawner == null)
            {
                EditorGUILayout.HelpBox("WaveSpawner is required for wave tuning.", MessageType.Warning);
                return;
            }

            SerializedObject serializedSpawner = new SerializedObject(currentWaveSpawner);
            serializedSpawner.Update();

            EditorGUILayout.LabelField("Wave Timeline", EditorStyles.miniBoldLabel);
            DrawPropertyField(serializedSpawner, "initialDelay");
            DrawPropertyField(serializedSpawner, "delayBetweenWaves");
            DrawPropertyField(serializedSpawner, "routePreviewLeadTime");
            DrawPropertyField(serializedSpawner, "continueCampaignAfterClear");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Wave Authoring Source", EditorStyles.miniBoldLabel);
            DrawPropertyField(serializedSpawner, "waveCatalogAsset");
            DrawPropertyField(serializedSpawner, "enemyCatalogAsset");

            EditorGUILayout.Space(4f);
            WaveCatalogAsset resolvedWaveCatalog = ResolveWaveCatalogAsset();
            if (resolvedWaveCatalog != null)
            {
                EditorGUILayout.HelpBox(
                    "This scene is using WaveCatalogAsset as the primary planner workflow. " +
                    "Edit wave groups here, and only keep the fallback scene-wave array for legacy compatibility.",
                    MessageType.Info);

                SerializedObject serializedCatalog = new SerializedObject(resolvedWaveCatalog);
                serializedCatalog.Update();
                DrawPropertyField(serializedCatalog, "waves", includeChildren: true);
                serializedCatalog.ApplyModifiedProperties();
                EditorUtility.SetDirty(resolvedWaveCatalog);
                DrawPingButton(resolvedWaveCatalog, "Ping Wave Catalog");

                showFallbackSceneWaveArray = EditorGUILayout.Foldout(showFallbackSceneWaveArray, "Fallback Scene Wave Array", true);
                if (showFallbackSceneWaveArray)
                {
                    DrawPropertyField(serializedSpawner, "waves", includeChildren: true);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No WaveCatalogAsset is assigned yet, so the scene is still using the older fallback wave array.",
                    MessageType.Warning);
                EditorGUILayout.LabelField("Fallback Scene Wave List", EditorStyles.miniBoldLabel);
                DrawPropertyField(serializedSpawner, "waves", includeChildren: true);
            }

            serializedSpawner.ApplyModifiedProperties();
            EditorUtility.SetDirty(currentWaveSpawner);
        }

        private void DrawRelaySection()
        {
            RelayTower relayPrototype = ResolveRelayPrototype();
            if (relayPrototype == null)
            {
                EditorGUILayout.HelpBox("Relay prototype could not be resolved from TowerDefenseGame.", MessageType.Warning);
                return;
            }

            SerializedObject serializedRelay = new SerializedObject(relayPrototype);
            serializedRelay.Update();

            DrawPropertyField(serializedRelay, "supplyRange");
            DrawPropertyField(serializedRelay, "baseSupplyCapacity");
            DrawPropertyField(serializedRelay, "supplyCapacityPerUpgrade");
            DrawPropertyField(serializedRelay, "currentLevel");
            DrawPropertyField(serializedRelay, "maxLevel");
            DrawPropertyField(serializedRelay, "upgradeCostBase");
            DrawPropertyField(serializedRelay, "upgradeCostPerLevel");

            serializedRelay.ApplyModifiedProperties();
            EditorUtility.SetDirty(relayPrototype);

            DrawPingButton(relayPrototype, "Ping Relay Prototype");
        }

        private void DrawDefenseTowerSection(string prefabPropertyName, string tuningPropertyName, string missingMessage)
        {
            DefenseTower towerPrototype = ResolveDefensePrototype(prefabPropertyName);
            if (towerPrototype == null)
            {
                EditorGUILayout.HelpBox($"Could not resolve {missingMessage}.", MessageType.Warning);
                return;
            }

            SerializedObject serializedTower = new SerializedObject(towerPrototype);
            serializedTower.Update();

            DrawPropertyField(serializedTower, "buildType");
            DrawPropertyField(serializedTower, "currentLevel");
            DrawPropertyField(serializedTower, "maxLevel");

            EditorGUILayout.Space(4f);
            DrawTuningSubset(serializedTower, tuningPropertyName);

            serializedTower.ApplyModifiedProperties();
            EditorUtility.SetDirty(towerPrototype);

            DrawPingButton(towerPrototype, "Ping Tower Prototype");
        }

        /// <summary>
        /// Exposes only the fields that genuinely influence balance.
        ///
        /// The nested tuning blocks also contain visual and feedback settings.
        /// For a planner-facing balance console, we keep the first layer focused on gameplay math:
        /// range, interval, damage, power, and upgrade costs.
        /// </summary>
        private void DrawTuningSubset(SerializedObject serializedTower, string tuningPropertyName)
        {
            string[] propertyPaths =
            {
                $"{tuningPropertyName}.attackRange",
                $"{tuningPropertyName}.attackRangePerUpgrade",
                $"{tuningPropertyName}.attackInterval",
                $"{tuningPropertyName}.attackIntervalPerUpgradeDelta",
                $"{tuningPropertyName}.baseDamage",
                $"{tuningPropertyName}.damagePerUpgrade",
                $"{tuningPropertyName}.basePowerRequired",
                $"{tuningPropertyName}.powerRequiredPerUpgrade",
                $"{tuningPropertyName}.upgradeCostBase",
                $"{tuningPropertyName}.upgradeCostPerLevel",
                $"{tuningPropertyName}.slowMultiplier",
                $"{tuningPropertyName}.slowMultiplierPerUpgradeDelta",
                $"{tuningPropertyName}.slowDuration",
                $"{tuningPropertyName}.slowDurationPerUpgrade",
                $"{tuningPropertyName}.bombFlightTime",
                $"{tuningPropertyName}.bombFlightTimePerUpgradeDelta",
                $"{tuningPropertyName}.bombRadius",
                $"{tuningPropertyName}.bombRadiusPerUpgrade"
            };

            foreach (string propertyPath in propertyPaths)
            {
                DrawPropertyField(serializedTower, propertyPath);
            }
        }

        private void DrawQuickBatchSection()
        {
            EditorGUILayout.HelpBox(
                "These helpers are meant for fast balance passes. " +
                "They only touch numeric gameplay values and leave references / visuals alone.",
                MessageType.None);

            waveCountMultiplier = EditorGUILayout.FloatField("Wave Count Multiplier", waveCountMultiplier);
            waveHealthMultiplier = EditorGUILayout.FloatField("Wave Health Multiplier", waveHealthMultiplier);
            waveSpeedMultiplier = EditorGUILayout.FloatField("Wave Speed Multiplier", waveSpeedMultiplier);
            waveRewardMultiplier = EditorGUILayout.FloatField("Wave Reward Multiplier", waveRewardMultiplier);
            waveIntervalMultiplier = EditorGUILayout.FloatField("Wave Interval Multiplier", waveIntervalMultiplier);
            buildCostMultiplier = EditorGUILayout.FloatField("Build Cost Multiplier", buildCostMultiplier);
            upgradeCostMultiplier = EditorGUILayout.FloatField("Upgrade Cost Multiplier", upgradeCostMultiplier);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(currentWaveSpawner == null))
            {
                if (GUILayout.Button("Scale Current Scene Waves"))
                {
                    ApplyWaveMultipliers();
                }
            }

            using (new EditorGUI.DisabledScope(currentGame == null))
            {
                if (GUILayout.Button("Scale Build Costs"))
                {
                    ApplyBuildCostMultiplier();
                }
            }

            using (new EditorGUI.DisabledScope(currentGame == null))
            {
                if (GUILayout.Button("Scale Upgrade Costs"))
                {
                    ApplyUpgradeCostMultiplier();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static BalancePresetDefinition GetPresetDefinition(BalancePresetKind presetKind)
        {
            return presetKind switch
            {
                BalancePresetKind.Simple => new BalancePresetDefinition
                {
                    Kind = presetKind,
                    Label = "Simple",
                    Description = "More starting safety, cheaper growth, and lighter waves.",
                    StartingScrapMultiplier = 1.25f,
                    StartingBaseHealthMultiplier = 1.25f,
                    BuildCostMultiplier = 0.9f,
                    UpgradeCostMultiplier = 0.9f,
                    RelayLimitDelta = 1,
                    WaveCountMultiplier = 0.85f,
                    WaveHealthMultiplier = 0.85f,
                    WaveSpeedMultiplier = 0.9f,
                    WaveRewardMultiplier = 1.05f,
                    WaveIntervalMultiplier = 1.1f,
                    RelayRangeMultiplier = 1.05f,
                    RelayCapacityDelta = 1,
                    TowerRangeMultiplier = 1.05f,
                    TowerAttackIntervalMultiplier = 0.95f,
                    TowerDamageMultiplier = 1.1f,
                    TowerPowerMultiplier = 0.9f,
                    BombRadiusMultiplier = 1.08f,
                    SlowStrengthMultiplier = 0.92f
                },
                BalancePresetKind.Hard => new BalancePresetDefinition
                {
                    Kind = presetKind,
                    Label = "Hard",
                    Description = "Tighter economy, denser waves, and harsher combat pressure.",
                    StartingScrapMultiplier = 0.85f,
                    StartingBaseHealthMultiplier = 0.85f,
                    BuildCostMultiplier = 1.12f,
                    UpgradeCostMultiplier = 1.15f,
                    RelayLimitDelta = -1,
                    WaveCountMultiplier = 1.2f,
                    WaveHealthMultiplier = 1.2f,
                    WaveSpeedMultiplier = 1.1f,
                    WaveRewardMultiplier = 1.12f,
                    WaveIntervalMultiplier = 0.9f,
                    RelayRangeMultiplier = 0.96f,
                    RelayCapacityDelta = -1,
                    TowerRangeMultiplier = 0.96f,
                    TowerAttackIntervalMultiplier = 1.06f,
                    TowerDamageMultiplier = 0.95f,
                    TowerPowerMultiplier = 1.1f,
                    BombRadiusMultiplier = 0.94f,
                    SlowStrengthMultiplier = 1.08f
                },
                _ => new BalancePresetDefinition
                {
                    Kind = presetKind,
                    Label = "Standard",
                    Description = "Neutral baseline pass with no directional bias.",
                    StartingScrapMultiplier = 1f,
                    StartingBaseHealthMultiplier = 1f,
                    BuildCostMultiplier = 1f,
                    UpgradeCostMultiplier = 1f,
                    RelayLimitDelta = 0,
                    WaveCountMultiplier = 1f,
                    WaveHealthMultiplier = 1f,
                    WaveSpeedMultiplier = 1f,
                    WaveRewardMultiplier = 1f,
                    WaveIntervalMultiplier = 1f,
                    RelayRangeMultiplier = 1f,
                    RelayCapacityDelta = 0,
                    TowerRangeMultiplier = 1f,
                    TowerAttackIntervalMultiplier = 1f,
                    TowerDamageMultiplier = 1f,
                    TowerPowerMultiplier = 1f,
                    BombRadiusMultiplier = 1f,
                    SlowStrengthMultiplier = 1f
                }
            };
        }

        /// <summary>
        /// Applies one preset into the current level context.
        ///
        /// The method intentionally reuses the same scene/prefab sources already used by the
        /// tuning window itself. That means a preset never edits hidden duplicate data; it only
        /// changes the exact scene objects and prototype prefabs the planner is already looking at.
        /// </summary>
        private void ApplyPreset(BalancePresetDefinition preset)
        {
            if (preset == null)
            {
                return;
            }

            // Mirror the preset into the batch helper fields so the UI reflects the new pass.
            waveCountMultiplier = preset.WaveCountMultiplier;
            waveHealthMultiplier = preset.WaveHealthMultiplier;
            waveSpeedMultiplier = preset.WaveSpeedMultiplier;
            waveRewardMultiplier = preset.WaveRewardMultiplier;
            waveIntervalMultiplier = preset.WaveIntervalMultiplier;
            buildCostMultiplier = preset.BuildCostMultiplier;
            upgradeCostMultiplier = preset.UpgradeCostMultiplier;

            ApplyPresetToCoreEconomy(preset);
            ApplyPresetToMapLimit(preset);
            ApplyPresetToWaves(preset);
            ApplyPresetToRelay(preset);
            ApplyPresetToCombatTower("singleTargetTowerPrototypeReference", "singleTargetTuning", preset);
            ApplyPresetToCombatTower("slowFieldTowerPrototypeReference", "slowFieldTuning", preset);
            ApplyPresetToCombatTower("bombardTowerPrototypeReference", "bombardTuning", preset);

            SaveCurrentWork();
        }

        private void ApplyPresetToCoreEconomy(BalancePresetDefinition preset)
        {
            if (currentGame == null)
            {
                return;
            }

            SerializedObject serializedGame = new SerializedObject(currentGame);
            ScaleIntProperty(serializedGame.FindProperty("startingScrap"), preset.StartingScrapMultiplier);
            ScaleIntProperty(serializedGame.FindProperty("startingBaseHealth"), preset.StartingBaseHealthMultiplier);
            ScaleIntProperty(serializedGame.FindProperty("relayTowerCost"), preset.BuildCostMultiplier);
            ScaleIntProperty(serializedGame.FindProperty("singleTargetTowerCost"), preset.BuildCostMultiplier);
            ScaleIntProperty(serializedGame.FindProperty("slowFieldTowerCost"), preset.BuildCostMultiplier);
            ScaleIntProperty(serializedGame.FindProperty("bombardTowerCost"), preset.BuildCostMultiplier);
            serializedGame.ApplyModifiedProperties();
            EditorUtility.SetDirty(currentGame);
            EditorSceneManager.MarkSceneDirty(currentGame.gameObject.scene);
        }

        private void ApplyPresetToMapLimit(BalancePresetDefinition preset)
        {
            if (currentMap == null)
            {
                return;
            }

            SerializedObject serializedMap = new SerializedObject(currentMap);
            SerializedProperty relayLimitProperty = serializedMap.FindProperty("relayLimit");
            if (relayLimitProperty != null)
            {
                relayLimitProperty.intValue = Mathf.Max(0, relayLimitProperty.intValue + preset.RelayLimitDelta);
            }

            serializedMap.ApplyModifiedProperties();
            EditorUtility.SetDirty(currentMap);
            EditorSceneManager.MarkSceneDirty(currentMap.gameObject.scene);
        }

        private void ApplyPresetToWaves(BalancePresetDefinition preset)
        {
            if (currentWaveSpawner == null)
            {
                return;
            }

            SerializedObject serializedSpawner = new SerializedObject(currentWaveSpawner);
            ScaleFloatProperty(serializedSpawner.FindProperty("initialDelay"), preset.WaveIntervalMultiplier, minimum: 0f);
            ScaleFloatProperty(serializedSpawner.FindProperty("delayBetweenWaves"), preset.WaveIntervalMultiplier, minimum: 0f);
            ScaleFloatProperty(serializedSpawner.FindProperty("routePreviewLeadTime"), preset.WaveIntervalMultiplier, minimum: 0f);

            WaveCatalogAsset resolvedWaveCatalog = ResolveWaveCatalogAsset();
            EnemyCatalogAsset resolvedEnemyCatalog = ResolveEnemyCatalogAsset();
            if (resolvedWaveCatalog != null)
            {
                ApplyCatalogWaveMultipliers(resolvedWaveCatalog, resolvedEnemyCatalog, preset.WaveCountMultiplier, preset.WaveIntervalMultiplier, preset.WaveSpeedMultiplier, preset.WaveHealthMultiplier, preset.WaveRewardMultiplier);
            }
            else
            {
                SerializedProperty wavesProperty = serializedSpawner.FindProperty("waves");
                if (wavesProperty != null && wavesProperty.isArray)
                {
                    for (int waveIndex = 0; waveIndex < wavesProperty.arraySize; waveIndex++)
                    {
                        SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(waveIndex);
                        ScaleIntProperty(waveProperty.FindPropertyRelative("enemyCount"), preset.WaveCountMultiplier);
                        ScaleFloatProperty(waveProperty.FindPropertyRelative("spawnInterval"), preset.WaveIntervalMultiplier, minimum: 0.05f);
                        ScaleFloatProperty(waveProperty.FindPropertyRelative("moveSpeed"), preset.WaveSpeedMultiplier, minimum: 0.05f);
                        ScaleIntProperty(waveProperty.FindPropertyRelative("enemyHealth"), preset.WaveHealthMultiplier);
                        ScaleIntProperty(waveProperty.FindPropertyRelative("enemyScrapReward"), preset.WaveRewardMultiplier);
                    }
                }
            }

            serializedSpawner.ApplyModifiedProperties();
            EditorUtility.SetDirty(currentWaveSpawner);
            EditorSceneManager.MarkSceneDirty(currentWaveSpawner.gameObject.scene);
        }

        private void ApplyPresetToRelay(BalancePresetDefinition preset)
        {
            RelayTower relayPrototype = ResolveRelayPrototype();
            if (relayPrototype == null)
            {
                return;
            }

            SerializedObject relaySerialized = new SerializedObject(relayPrototype);
            ScaleFloatProperty(relaySerialized.FindProperty("supplyRange"), preset.RelayRangeMultiplier, minimum: 0.1f);

            SerializedProperty baseCapacityProperty = relaySerialized.FindProperty("baseSupplyCapacity");
            if (baseCapacityProperty != null)
            {
                baseCapacityProperty.intValue = Mathf.Max(0, baseCapacityProperty.intValue + preset.RelayCapacityDelta);
            }

            ScaleIntProperty(relaySerialized.FindProperty("upgradeCostBase"), preset.UpgradeCostMultiplier);
            ScaleIntProperty(relaySerialized.FindProperty("upgradeCostPerLevel"), preset.UpgradeCostMultiplier);
            relaySerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(relayPrototype);
        }

        private void ApplyPresetToCombatTower(string prefabPropertyName, string tuningPropertyName, BalancePresetDefinition preset)
        {
            DefenseTower towerPrototype = ResolveDefensePrototype(prefabPropertyName);
            if (towerPrototype == null)
            {
                return;
            }

            SerializedObject serializedTower = new SerializedObject(towerPrototype);
            ScaleFloatProperty(serializedTower.FindProperty($"{tuningPropertyName}.attackRange"), preset.TowerRangeMultiplier, minimum: 0.1f);
            ScaleFloatProperty(serializedTower.FindProperty($"{tuningPropertyName}.attackInterval"), preset.TowerAttackIntervalMultiplier, minimum: 0.05f);
            ScaleIntProperty(serializedTower.FindProperty($"{tuningPropertyName}.baseDamage"), preset.TowerDamageMultiplier);
            ScaleIntProperty(serializedTower.FindProperty($"{tuningPropertyName}.damagePerUpgrade"), preset.TowerDamageMultiplier);
            ScaleIntProperty(serializedTower.FindProperty($"{tuningPropertyName}.basePowerRequired"), preset.TowerPowerMultiplier);
            ScaleIntProperty(serializedTower.FindProperty($"{tuningPropertyName}.powerRequiredPerUpgrade"), preset.TowerPowerMultiplier);
            ScaleIntProperty(serializedTower.FindProperty($"{tuningPropertyName}.upgradeCostBase"), preset.UpgradeCostMultiplier);
            ScaleIntProperty(serializedTower.FindProperty($"{tuningPropertyName}.upgradeCostPerLevel"), preset.UpgradeCostMultiplier);

            // Only the relevant tuning families meaningfully use these fields.
            ScaleFloatProperty(serializedTower.FindProperty($"{tuningPropertyName}.bombRadius"), preset.BombRadiusMultiplier, minimum: 0.1f);
            ScaleFloatProperty(serializedTower.FindProperty($"{tuningPropertyName}.slowMultiplier"), preset.SlowStrengthMultiplier, minimum: 0.15f);
            ScaleFloatProperty(serializedTower.FindProperty($"{tuningPropertyName}.slowDuration"), preset.TowerRangeMultiplier, minimum: 0f);

            serializedTower.ApplyModifiedProperties();
            EditorUtility.SetDirty(towerPrototype);
        }

        private void DrawAdvancedRawEditors()
        {
            DrawRawObjectEditor("TowerDefenseGame Raw", currentGame);
            DrawRawObjectEditor("WaveSpawner Raw", currentWaveSpawner);
            DrawRawObjectEditor("BattlefieldMap Raw", currentMap);
            DrawRawObjectEditor("Relay Prototype Raw", ResolveRelayPrototype());
            DrawRawObjectEditor("Single Target Prototype Raw", ResolveDefensePrototype("singleTargetTowerPrototypeReference"));
            DrawRawObjectEditor("Slow Field Prototype Raw", ResolveDefensePrototype("slowFieldTowerPrototypeReference"));
            DrawRawObjectEditor("Bombard Prototype Raw", ResolveDefensePrototype("bombardTowerPrototypeReference"));
        }

        private void DrawRawObjectEditor(string title, Object targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);

            SerializedObject serializedObject = new SerializedObject(targetObject);
            serializedObject.Update();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }

                enterChildren = false;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetObject);
        }

        private void ApplyWaveMultipliers()
        {
            if (currentWaveSpawner == null)
            {
                return;
            }

            SerializedObject serializedSpawner = new SerializedObject(currentWaveSpawner);
            WaveCatalogAsset resolvedWaveCatalog = ResolveWaveCatalogAsset();
            EnemyCatalogAsset resolvedEnemyCatalog = ResolveEnemyCatalogAsset();
            if (resolvedWaveCatalog != null)
            {
                ApplyCatalogWaveMultipliers(resolvedWaveCatalog, resolvedEnemyCatalog, waveCountMultiplier, waveIntervalMultiplier, waveSpeedMultiplier, waveHealthMultiplier, waveRewardMultiplier);
            }
            else
            {
                SerializedProperty wavesProperty = serializedSpawner.FindProperty("waves");
                if (wavesProperty == null || !wavesProperty.isArray)
                {
                    return;
                }

                for (int waveIndex = 0; waveIndex < wavesProperty.arraySize; waveIndex++)
                {
                    SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(waveIndex);
                    ScaleIntProperty(waveProperty.FindPropertyRelative("enemyCount"), waveCountMultiplier);
                    ScaleFloatProperty(waveProperty.FindPropertyRelative("spawnInterval"), waveIntervalMultiplier, minimum: 0.05f);
                    ScaleFloatProperty(waveProperty.FindPropertyRelative("moveSpeed"), waveSpeedMultiplier, minimum: 0.05f);
                    ScaleIntProperty(waveProperty.FindPropertyRelative("enemyHealth"), waveHealthMultiplier);
                    ScaleIntProperty(waveProperty.FindPropertyRelative("enemyScrapReward"), waveRewardMultiplier);
                }
            }

            serializedSpawner.ApplyModifiedProperties();
            EditorUtility.SetDirty(currentWaveSpawner);
            EditorSceneManager.MarkSceneDirty(currentWaveSpawner.gameObject.scene);
        }

        private void ApplyBuildCostMultiplier()
        {
            if (currentGame == null)
            {
                return;
            }

            SerializedObject serializedGame = new SerializedObject(currentGame);
            ScaleIntProperty(serializedGame.FindProperty("relayTowerCost"), buildCostMultiplier);
            ScaleIntProperty(serializedGame.FindProperty("singleTargetTowerCost"), buildCostMultiplier);
            ScaleIntProperty(serializedGame.FindProperty("slowFieldTowerCost"), buildCostMultiplier);
            ScaleIntProperty(serializedGame.FindProperty("bombardTowerCost"), buildCostMultiplier);
            serializedGame.ApplyModifiedProperties();
            EditorUtility.SetDirty(currentGame);
            EditorSceneManager.MarkSceneDirty(currentGame.gameObject.scene);
        }

        private void ApplyUpgradeCostMultiplier()
        {
            RelayTower relayPrototype = ResolveRelayPrototype();
            if (relayPrototype != null)
            {
                SerializedObject relaySerialized = new SerializedObject(relayPrototype);
                ScaleIntProperty(relaySerialized.FindProperty("upgradeCostBase"), upgradeCostMultiplier);
                ScaleIntProperty(relaySerialized.FindProperty("upgradeCostPerLevel"), upgradeCostMultiplier);
                relaySerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(relayPrototype);
            }

            ApplyTowerUpgradeCostMultiplier("singleTargetTowerPrototypeReference", "singleTargetTuning");
            ApplyTowerUpgradeCostMultiplier("slowFieldTowerPrototypeReference", "slowFieldTuning");
            ApplyTowerUpgradeCostMultiplier("bombardTowerPrototypeReference", "bombardTuning");

            AssetDatabase.SaveAssets();
        }

        private void ApplyTowerUpgradeCostMultiplier(string prefabPropertyName, string tuningPropertyName)
        {
            DefenseTower towerPrototype = ResolveDefensePrototype(prefabPropertyName);
            if (towerPrototype == null)
            {
                return;
            }

            SerializedObject serializedTower = new SerializedObject(towerPrototype);
            ScaleIntProperty(serializedTower.FindProperty($"{tuningPropertyName}.upgradeCostBase"), upgradeCostMultiplier);
            ScaleIntProperty(serializedTower.FindProperty($"{tuningPropertyName}.upgradeCostPerLevel"), upgradeCostMultiplier);
            serializedTower.ApplyModifiedProperties();
            EditorUtility.SetDirty(towerPrototype);
        }

        private void AdoptCurrentSceneContext()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            currentGame ??= FindFirstComponentInScene<TowerDefenseGame>(activeScene);
            currentWaveSpawner ??= FindFirstComponentInScene<WaveSpawner>(activeScene);
            currentMap ??= FindFirstComponentInScene<BattlefieldMapDefinition>(activeScene);
        }

        private RelayTower ResolveRelayPrototype()
        {
            GameObject relayPrototypeObject = ResolvePrototypeObject("relayTowerPrototypeReference");
            return relayPrototypeObject != null ? relayPrototypeObject.GetComponent<RelayTower>() : null;
        }

        private DefenseTower ResolveDefensePrototype(string propertyName)
        {
            GameObject prototypeObject = ResolvePrototypeObject(propertyName);
            return prototypeObject != null ? prototypeObject.GetComponent<DefenseTower>() : null;
        }

        private GameObject ResolvePrototypeObject(string propertyName)
        {
            if (currentGame == null)
            {
                return null;
            }

            SerializedObject serializedGame = new SerializedObject(currentGame);
            SerializedProperty property = serializedGame.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as GameObject : null;
        }

        private WaveCatalogAsset ResolveWaveCatalogAsset()
        {
            if (currentWaveSpawner == null)
            {
                return null;
            }

            SerializedObject serializedSpawner = new SerializedObject(currentWaveSpawner);
            SerializedProperty property = serializedSpawner.FindProperty("waveCatalogAsset");
            return property != null ? property.objectReferenceValue as WaveCatalogAsset : null;
        }

        private EnemyCatalogAsset ResolveEnemyCatalogAsset()
        {
            if (currentWaveSpawner == null)
            {
                return null;
            }

            SerializedObject serializedSpawner = new SerializedObject(currentWaveSpawner);
            SerializedProperty property = serializedSpawner.FindProperty("enemyCatalogAsset");
            return property != null ? property.objectReferenceValue as EnemyCatalogAsset : null;
        }

        private static T FindFirstComponentInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault(component => component != null);
        }

        private static void DrawPropertyField(SerializedObject serializedObject, string propertyPath, bool includeChildren = false)
        {
            if (serializedObject == null)
            {
                return;
            }

            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, includeChildren);
            }
        }

        private static void ScaleIntProperty(SerializedProperty property, float multiplier)
        {
            if (property == null)
            {
                return;
            }

            int currentValue = property.intValue;
            property.intValue = Mathf.Max(0, Mathf.RoundToInt(currentValue * multiplier));
        }

        private static void ScaleFloatProperty(SerializedProperty property, float multiplier, float minimum)
        {
            if (property == null)
            {
                return;
            }

            float currentValue = property.floatValue;
            property.floatValue = Mathf.Max(minimum, currentValue * multiplier);
        }

        private static void ApplyCatalogWaveMultipliers(
            WaveCatalogAsset waveCatalogAsset,
            EnemyCatalogAsset enemyCatalogAsset,
            float countMultiplier,
            float intervalMultiplier,
            float speedMultiplier,
            float healthMultiplier,
            float rewardMultiplier)
        {
            if (waveCatalogAsset == null)
            {
                return;
            }

            SerializedObject serializedCatalog = new SerializedObject(waveCatalogAsset);
            SerializedProperty wavesProperty = serializedCatalog.FindProperty("waves");
            HashSet<EnemyArchetypeId> usedArchetypes = new HashSet<EnemyArchetypeId>();

            if (wavesProperty != null && wavesProperty.isArray)
            {
                for (int waveIndex = 0; waveIndex < wavesProperty.arraySize; waveIndex++)
                {
                    SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(waveIndex);
                    SerializedProperty groupsProperty = waveProperty.FindPropertyRelative("spawnGroups");
                    if (groupsProperty == null || !groupsProperty.isArray)
                    {
                        continue;
                    }

                    for (int groupIndex = 0; groupIndex < groupsProperty.arraySize; groupIndex++)
                    {
                        SerializedProperty groupProperty = groupsProperty.GetArrayElementAtIndex(groupIndex);
                        ScaleIntProperty(groupProperty.FindPropertyRelative("enemyCount"), countMultiplier);
                        ScaleFloatProperty(groupProperty.FindPropertyRelative("spawnInterval"), intervalMultiplier, minimum: 0.05f);

                        SerializedProperty enemyTypeProperty = groupProperty.FindPropertyRelative("enemyType");
                        if (enemyTypeProperty != null)
                        {
                            usedArchetypes.Add((EnemyArchetypeId)enemyTypeProperty.enumValueIndex);
                        }
                    }
                }
            }

            serializedCatalog.ApplyModifiedProperties();
            EditorUtility.SetDirty(waveCatalogAsset);

            if (enemyCatalogAsset == null)
            {
                return;
            }

            SerializedObject serializedEnemyCatalog = new SerializedObject(enemyCatalogAsset);
            SerializedProperty definitionsProperty = serializedEnemyCatalog.FindProperty("definitions");
            if (definitionsProperty != null && definitionsProperty.isArray)
            {
                for (int definitionIndex = 0; definitionIndex < definitionsProperty.arraySize; definitionIndex++)
                {
                    SerializedProperty definitionProperty = definitionsProperty.GetArrayElementAtIndex(definitionIndex);
                    SerializedProperty archetypeProperty = definitionProperty.FindPropertyRelative("archetypeId");
                    if (archetypeProperty == null)
                    {
                        continue;
                    }

                    EnemyArchetypeId archetypeId = (EnemyArchetypeId)archetypeProperty.enumValueIndex;
                    if (!usedArchetypes.Contains(archetypeId))
                    {
                        continue;
                    }

                    ScaleFloatProperty(definitionProperty.FindPropertyRelative("moveSpeed"), speedMultiplier, minimum: 0.05f);
                    ScaleIntProperty(definitionProperty.FindPropertyRelative("maxHealth"), healthMultiplier);
                    ScaleIntProperty(definitionProperty.FindPropertyRelative("scrapReward"), rewardMultiplier);
                }
            }

            serializedEnemyCatalog.ApplyModifiedProperties();
            EditorUtility.SetDirty(enemyCatalogAsset);
        }

        private static void DrawPingButton(Object targetObject, string buttonLabel)
        {
            if (targetObject == null)
            {
                return;
            }

            if (GUILayout.Button(buttonLabel, GUILayout.Width(180f)))
            {
                EditorGUIUtility.PingObject(targetObject);
                Selection.activeObject = targetObject;
            }
        }

        private static void SaveCurrentWork()
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.Refresh();
        }
    }
}
