using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// `PowerGridHudSnapshot` 是 HUD 需要看到的供电域摘要。
///
/// 这里故意不把整套继电器和塔对象直接暴露给 HUD，
/// 而是收口成一份“读数结果”：
/// - 现在有多少继电器。
/// - 现在有多少战斗塔在线 / 离线。
/// - 当前总负载和总供电量分别是多少。
/// - 此刻最值得告诉玩家的一句供电状态提示是什么。
///
/// 这样做以后，HUD 只依赖稳定的数据快照，
/// 不会反过来耦合供电判定过程里的内部容器和中间步骤。
/// </summary>
public readonly struct PowerGridHudSnapshot
{
    public PowerGridHudSnapshot(
        int relayCount,
        int relayLimit,
        int totalTowerCount,
        int poweredTowerCount,
        int offlineTowerCount,
        int assignedLoad,
        int totalCapacity,
        string statusMessage)
    {
        RelayCount = relayCount;
        RelayLimit = relayLimit;
        TotalTowerCount = totalTowerCount;
        PoweredTowerCount = poweredTowerCount;
        OfflineTowerCount = offlineTowerCount;
        AssignedLoad = assignedLoad;
        TotalCapacity = totalCapacity;
        StatusMessage = statusMessage ?? string.Empty;
    }

    public int RelayCount { get; }

    public int RelayLimit { get; }

    public int TotalTowerCount { get; }

    public int PoweredTowerCount { get; }

    public int OfflineTowerCount { get; }

    public int AssignedLoad { get; }

    public int TotalCapacity { get; }

    public string StatusMessage { get; }
}

/// <summary>
/// Coordinates relay coverage, relay numbering, tower numbering, and runtime power allocation.
/// The goal is to keep the phase-two power rules out of the main gameplay orchestrator.
/// </summary>
public sealed class TowerPowerGridCoordinator
{
    private sealed class TowerOfflineReason
    {
        public TowerOfflineReason(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    private sealed class RelayEvaluation
    {
        public RelayTower Relay { get; set; }
        public List<DefenseTower> WorkingTowers { get; } = new List<DefenseTower>();
        public int RemainingCapacity { get; set; }
    }

    private readonly Func<BattlefieldMapDefinition> _mapDefinitionQuery;
    private readonly Action<string> _logDiagnostic;
    private readonly Stack<int> _relayNumbers = new Stack<int>();
    private readonly Stack<int> _towerNumbers = new Stack<int>();

    private Transform _placedTowerRoot;

    public TowerPowerGridCoordinator(
        Func<BattlefieldMapDefinition> mapDefinitionQuery,
        Action<string> logDiagnostic)
    {
        _mapDefinitionQuery = mapDefinitionQuery;
        _logDiagnostic = logDiagnostic;

        for (int value = 100; value >= 1; value--)
        {
            _relayNumbers.Push(value);
            _towerNumbers.Push(value);
        }
    }

    public int RelayLimit => _mapDefinitionQuery != null && _mapDefinitionQuery() != null
        ? _mapDefinitionQuery().RelayLimit
        : int.MaxValue;

    public void BindPlacedTowerRoot(Transform placedTowerRoot)
    {
        _placedTowerRoot = placedTowerRoot;
        AssignNumbersToExistingStructures();
    }

    public int GetPlacedRelayCount()
    {
        CollectRuntimeStructures(out List<RelayTower> relays, out _);
        return relays.Count;
    }

    public bool CanPlaceRelay(out string invalidReason)
    {
        int relayLimit = RelayLimit;
        int placedRelayCount = GetPlacedRelayCount();
        if (placedRelayCount >= relayLimit)
        {
            invalidReason = $"Relay limit reached. This map allows at most {relayLimit} relay nodes.";
            return false;
        }

        invalidReason = string.Empty;
        return true;
    }

    public bool IsWithinAnyRelayCoverage(Vector3 worldPosition)
    {
        CollectRuntimeStructures(out List<RelayTower> relays, out _);
        for (int i = 0; i < relays.Count; i++)
        {
            if (relays[i].ContainsPoint(worldPosition))
            {
                return true;
            }
        }

        return false;
    }

    public Bounds GetRelayCoverageBounds()
    {
        CollectRuntimeStructures(out List<RelayTower> relays, out _);
        if (relays.Count == 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Bounds bounds = relays[0].CoverageBounds;
        for (int i = 1; i < relays.Count; i++)
        {
            bounds.Encapsulate(relays[i].CoverageBounds.min);
            bounds.Encapsulate(relays[i].CoverageBounds.max);
        }

        return bounds;
    }

    /// <summary>
    /// 组装一份给 HUD 使用的供电摘要。
    ///
    /// 这一步的重点不是“再次参与供电判定”，
    /// 而是把当前已经生效的结果翻译成玩家能快速读懂的局势信息。
    /// </summary>
    public PowerGridHudSnapshot GetHudSnapshot()
    {
        CollectRuntimeStructures(out List<RelayTower> relays, out List<DefenseTower> towers);

        int relayCount = relays.Count;
        int totalTowerCount = towers.Count;
        int poweredTowerCount = towers.Count(tower => tower != null && tower.IsPowered);
        int offlineTowerCount = Mathf.Max(0, totalTowerCount - poweredTowerCount);
        int totalCapacity = relays.Sum(relay => relay != null ? relay.SupplyCapacity : 0);
        int assignedLoad = relays.Sum(relay => relay != null ? relay.CurrentAssignedLoad : 0);

        string statusMessage;
        if (relayCount == 0)
        {
            statusMessage = totalTowerCount > 0
                ? "No relay coverage is active. Deployed towers will stay offline."
                : "Place a relay first to open the power network.";
        }
        else if (totalTowerCount == 0)
        {
            statusMessage = relayCount >= RelayLimit
                ? "Relay network ready, but the relay limit is already full."
                : "Relay network ready. Deploy combat towers inside relay coverage.";
        }
        else if (offlineTowerCount > 0)
        {
            statusMessage = $"{offlineTowerCount} tower(s) offline. Expand capacity or reposition the next build.";
        }
        else if (relayCount >= RelayLimit)
        {
            statusMessage = "All towers are powered. Further expansion now depends on upgrading existing relays.";
        }
        else if (totalCapacity > 0 && assignedLoad >= totalCapacity)
        {
            statusMessage = "Grid is saturated. New builds or upgrades may trip supply immediately.";
        }
        else
        {
            statusMessage = "Grid stable. Current relay capacity is covering all deployed towers.";
        }

        return new PowerGridHudSnapshot(
            relayCount: relayCount,
            relayLimit: RelayLimit,
            totalTowerCount: totalTowerCount,
            poweredTowerCount: poweredTowerCount,
            offlineTowerCount: offlineTowerCount,
            assignedLoad: assignedLoad,
            totalCapacity: totalCapacity,
            statusMessage: statusMessage);
    }

    public void RegisterPlacedStructure(GameObject structureObject, TowerType towerType)
    {
        if (structureObject == null)
        {
            return;
        }

        switch (towerType)
        {
            case TowerType.Relay:
            {
                RelayTower relayTower = structureObject.GetComponent<RelayTower>();
                if (relayTower != null && relayTower.RelayNumber >= 100 && _relayNumbers.Count > 0)
                {
                    relayTower.AssignRelayNumber(_relayNumbers.Pop());
                }

                break;
            }

            case TowerType.SingleTarget:
            case TowerType.SlowField:
            case TowerType.Bombard:
            {
                DefenseTower defenseTower = structureObject.GetComponent<DefenseTower>();
                if (defenseTower != null && defenseTower.TowerNumber >= 100 && _towerNumbers.Count > 0)
                {
                    defenseTower.AssignTowerNumber(_towerNumbers.Pop());
                }

                break;
            }
        }

        RecalculatePowerDistribution();
    }

    public void NotifyTopologyChanged()
    {
        RecalculatePowerDistribution();
    }

    public bool CanUpgradeRelay(RelayTower relay, int availableEnergy, out int upgradeCost, out string invalidReason)
    {
        upgradeCost = 0;
        invalidReason = string.Empty;

        if (relay == null)
        {
            invalidReason = "No relay is selected.";
            return false;
        }

        if (!relay.CanUpgrade)
        {
            invalidReason = "Relay is already at max level.";
            return false;
        }

        upgradeCost = relay.GetUpgradeCost();
        if (availableEnergy < upgradeCost)
        {
            invalidReason = $"Not enough scrap. Upgrade requires {upgradeCost} SCRAP.";
            return false;
        }

        return true;
    }

    public bool CanUpgradeDefenseTower(DefenseTower tower, int availableEnergy, out int upgradeCost, out string invalidReason)
    {
        upgradeCost = 0;
        invalidReason = string.Empty;

        if (tower == null)
        {
            invalidReason = "No defense tower is selected.";
            return false;
        }

        if (!tower.CanUpgrade)
        {
            invalidReason = "Defense tower is already at max level.";
            return false;
        }

        upgradeCost = tower.GetUpgradeCost();
        if (availableEnergy < upgradeCost)
        {
            invalidReason = $"Not enough scrap. Upgrade requires {upgradeCost} SCRAP.";
            return false;
        }

        CollectRuntimeStructures(out List<RelayTower> relays, out List<DefenseTower> towers);
        SimulationResult currentSimulation = Simulate(relays, towers, null, null);

        Dictionary<DefenseTower, int> towerPowerOverrides = new Dictionary<DefenseTower, int>
        {
            [tower] = tower.PreviewUpgradedPowerRequired()
        };

        SimulationResult upgradedSimulation = Simulate(relays, towers, null, towerPowerOverrides);
        if (!upgradedSimulation.Assignments.ContainsKey(tower))
        {
            Dictionary<DefenseTower, TowerOfflineReason> upgradeReasons = BuildOfflineReasons(
                relays,
                towers,
                upgradedSimulation.Evaluations,
                upgradedSimulation.Assignments);
            invalidReason = upgradeReasons.TryGetValue(tower, out TowerOfflineReason reason)
                ? $"Upgrade blocked: {reason.Message}"
                : "Upgrade blocked: this tower would lose its power supply.";
            return false;
        }

        foreach (KeyValuePair<DefenseTower, RelayTower> currentAssignment in currentSimulation.Assignments)
        {
            if (!upgradedSimulation.Assignments.ContainsKey(currentAssignment.Key))
            {
                invalidReason = $"Upgrade blocked: tower #{currentAssignment.Key.TowerNumber} would be forced offline.";
                return false;
            }
        }

        return true;
    }

    public void ApplyRelayUpgrade(RelayTower relay)
    {
        if (relay == null)
        {
            return;
        }

        relay.ApplyUpgrade();
        RecalculatePowerDistribution();
    }

    public void ApplyDefenseTowerUpgrade(DefenseTower tower)
    {
        if (tower == null)
        {
            return;
        }

        tower.ApplyUpgrade();
        RecalculatePowerDistribution();
    }

    public void RecalculatePowerDistribution()
    {
        CollectRuntimeStructures(out List<RelayTower> relays, out List<DefenseTower> towers);

        for (int i = 0; i < relays.Count; i++)
        {
            relays[i].ResetRuntimeLoad();
        }

        for (int i = 0; i < towers.Count; i++)
        {
            towers[i].SetPowerState(false, null, "Awaiting power evaluation.");
        }

        if (relays.Count == 0 || towers.Count == 0)
        {
            return;
        }

        relays.Sort((a, b) => a.RelayNumber.CompareTo(b.RelayNumber));
        towers.Sort((a, b) => a.TowerNumber.CompareTo(b.TowerNumber));

        SimulationResult simulation = Simulate(relays, towers, null, null);
        Dictionary<DefenseTower, TowerOfflineReason> offlineReasons = BuildOfflineReasons(relays, towers, simulation.Evaluations, simulation.Assignments);
        ApplyAssignments(relays, towers, simulation.Assignments, offlineReasons);
    }

    private SimulationResult Simulate(
        List<RelayTower> relays,
        List<DefenseTower> towers,
        Dictionary<RelayTower, int> relayCapacityOverrides,
        Dictionary<DefenseTower, int> towerPowerOverrides)
    {
        Dictionary<RelayTower, HashSet<DefenseTower>> exclusions = relays.ToDictionary(
            relay => relay,
            relay => new HashSet<DefenseTower>());

        Dictionary<RelayTower, RelayEvaluation> evaluations = EvaluateAllRelays(relays, towers, exclusions, relayCapacityOverrides, towerPowerOverrides);

        // This loop is the runtime equivalent of the documented "3号过程":
        // overlapping towers are finally owned by the smallest relay number that can currently support them,
        // and the other relays rerun their local 2号过程 with that tower excluded.
        bool changed;
        int guard = relays.Count * Math.Max(1, towers.Count);
        do
        {
            changed = false;

            for (int towerIndex = 0; towerIndex < towers.Count; towerIndex++)
            {
                DefenseTower tower = towers[towerIndex];
                List<RelayTower> supportingRelays = relays
                    .Where(relay => evaluations.TryGetValue(relay, out RelayEvaluation evaluation) && evaluation.WorkingTowers.Contains(tower))
                    .OrderBy(relay => relay.RelayNumber)
                    .ToList();

                if (supportingRelays.Count <= 1)
                {
                    continue;
                }

                for (int relayIndex = 1; relayIndex < supportingRelays.Count; relayIndex++)
                {
                    RelayTower relay = supportingRelays[relayIndex];
                    if (exclusions[relay].Add(tower))
                    {
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                evaluations = EvaluateAllRelays(relays, towers, exclusions, relayCapacityOverrides, towerPowerOverrides);
            }
        }
        while (changed && --guard > 0);

        Dictionary<DefenseTower, RelayTower> assignments = BuildPreferredAssignments(relays, evaluations, towers);
        return new SimulationResult
        {
            Evaluations = evaluations,
            Assignments = assignments
        };
    }

    private Dictionary<RelayTower, RelayEvaluation> EvaluateAllRelays(
        List<RelayTower> relays,
        List<DefenseTower> towers,
        Dictionary<RelayTower, HashSet<DefenseTower>> exclusions,
        Dictionary<RelayTower, int> relayCapacityOverrides,
        Dictionary<DefenseTower, int> towerPowerOverrides)
    {
        Dictionary<RelayTower, RelayEvaluation> evaluations = new Dictionary<RelayTower, RelayEvaluation>();
        for (int relayIndex = 0; relayIndex < relays.Count; relayIndex++)
        {
            RelayTower relay = relays[relayIndex];
            HashSet<DefenseTower> relayExclusions = exclusions.TryGetValue(relay, out HashSet<DefenseTower> existingExclusions)
                ? existingExclusions
                : null;
            evaluations[relay] = EvaluateSingleRelay(relay, towers, relayExclusions, relayCapacityOverrides, towerPowerOverrides);
        }

        return evaluations;
    }

    private static RelayEvaluation EvaluateSingleRelay(
        RelayTower relay,
        List<DefenseTower> towers,
        HashSet<DefenseTower> exclusions,
        Dictionary<RelayTower, int> relayCapacityOverrides,
        Dictionary<DefenseTower, int> towerPowerOverrides)
    {
        RelayEvaluation evaluation = new RelayEvaluation
        {
            Relay = relay,
            RemainingCapacity = GetRelayCapacity(relay, relayCapacityOverrides)
        };

        for (int towerIndex = 0; towerIndex < towers.Count; towerIndex++)
        {
            DefenseTower tower = towers[towerIndex];
            if (!relay.ContainsPoint(tower.transform.position))
            {
                continue;
            }

            if (exclusions != null && exclusions.Contains(tower))
            {
                continue;
            }

            int powerRequired = GetTowerPowerRequired(tower, towerPowerOverrides);
            if (powerRequired > evaluation.RemainingCapacity)
            {
                break;
            }

            evaluation.WorkingTowers.Add(tower);
            evaluation.RemainingCapacity -= powerRequired;
        }

        return evaluation;
    }

    private static Dictionary<DefenseTower, RelayTower> BuildPreferredAssignments(
        List<RelayTower> relays,
        Dictionary<RelayTower, RelayEvaluation> evaluations,
        List<DefenseTower> towers)
    {
        Dictionary<DefenseTower, RelayTower> assignments = new Dictionary<DefenseTower, RelayTower>();

        for (int towerIndex = 0; towerIndex < towers.Count; towerIndex++)
        {
            DefenseTower tower = towers[towerIndex];
            RelayTower bestRelay = null;

            for (int relayIndex = 0; relayIndex < relays.Count; relayIndex++)
            {
                RelayTower relay = relays[relayIndex];
                if (!evaluations.TryGetValue(relay, out RelayEvaluation evaluation) || !evaluation.WorkingTowers.Contains(tower))
                {
                    continue;
                }

                if (bestRelay == null || relay.RelayNumber < bestRelay.RelayNumber)
                {
                    bestRelay = relay;
                }
            }

            if (bestRelay != null)
            {
                assignments[tower] = bestRelay;
            }
        }

        return assignments;
    }

    private void ApplyAssignments(
        List<RelayTower> relays,
        List<DefenseTower> towers,
        Dictionary<DefenseTower, RelayTower> assignments,
        Dictionary<DefenseTower, TowerOfflineReason> offlineReasons)
    {
        HashSet<DefenseTower> poweredTowers = new HashSet<DefenseTower>();

        foreach (RelayTower relay in relays)
        {
            int remainingCapacity = relay.SupplyCapacity;
            int assignedLoad = 0;

            List<DefenseTower> relayTowers = towers
                .Where(tower => assignments.TryGetValue(tower, out RelayTower assignedRelay) && assignedRelay == relay)
                .OrderBy(tower => tower.TowerNumber)
                .ToList();

            for (int towerIndex = 0; towerIndex < relayTowers.Count; towerIndex++)
            {
                DefenseTower tower = relayTowers[towerIndex];
                if (tower.PowerRequired > remainingCapacity)
                {
                    tower.SetPowerState(false, null, $"Relay #{relay.RelayNumber} no longer has enough remaining capacity.");
                    continue;
                }

                remainingCapacity -= tower.PowerRequired;
                assignedLoad += tower.PowerRequired;
                poweredTowers.Add(tower);
                tower.SetPowerState(true, relay, $"Powered by relay #{relay.RelayNumber}. Remaining relay capacity: {remainingCapacity}.");
            }

            relay.SetRuntimeLoad(assignedLoad);
        }

        for (int towerIndex = 0; towerIndex < towers.Count; towerIndex++)
        {
            DefenseTower tower = towers[towerIndex];
            if (poweredTowers.Contains(tower))
            {
                continue;
            }

            string message = offlineReasons != null && offlineReasons.TryGetValue(tower, out TowerOfflineReason reason)
                ? reason.Message
                : "Tower is offline.";
            tower.SetPowerState(false, null, message);
        }

        int poweredTowerCount = towers.Count(tower => tower.IsPowered);
        int unpoweredTowerCount = towers.Count - poweredTowerCount;
        _logDiagnostic?.Invoke(
            $"Power grid recalculated: relays={relays.Count} powered={poweredTowerCount} offline={unpoweredTowerCount} relayLimit={RelayLimit}");
    }

    private static Dictionary<DefenseTower, TowerOfflineReason> BuildOfflineReasons(
        List<RelayTower> relays,
        List<DefenseTower> towers,
        Dictionary<RelayTower, RelayEvaluation> evaluations,
        Dictionary<DefenseTower, RelayTower> assignments)
    {
        Dictionary<DefenseTower, TowerOfflineReason> reasons = new Dictionary<DefenseTower, TowerOfflineReason>();

        for (int towerIndex = 0; towerIndex < towers.Count; towerIndex++)
        {
            DefenseTower tower = towers[towerIndex];
            if (assignments.ContainsKey(tower))
            {
                continue;
            }

            List<RelayTower> coveringRelays = relays
                .Where(relay => relay.ContainsPoint(tower.transform.position))
                .OrderBy(relay => relay.RelayNumber)
                .ToList();

            if (coveringRelays.Count == 0)
            {
                reasons[tower] = new TowerOfflineReason("Offline: not inside any relay coverage.");
                continue;
            }

            RelayTower lowestRelay = coveringRelays[0];
            if (coveringRelays.Count == 1)
            {
                reasons[tower] = new TowerOfflineReason(
                    $"Offline: relay #{lowestRelay.RelayNumber} ran out of capacity before this tower's turn.");
                continue;
            }

            bool anyRelayHasWorkingTower = coveringRelays.Any(relay =>
                evaluations.TryGetValue(relay, out RelayEvaluation evaluation) && evaluation.WorkingTowers.Count > 0);

            reasons[tower] = anyRelayHasWorkingTower
                ? new TowerOfflineReason(
                    $"Offline: covered by multiple relays, but higher-priority towers consumed all available supply before this tower could be powered.")
                : new TowerOfflineReason(
                    $"Offline: covered by relays, but no relay can currently reserve enough capacity for this tower.");
        }

        return reasons;
    }

    private static int GetRelayCapacity(RelayTower relay, Dictionary<RelayTower, int> relayCapacityOverrides)
    {
        if (relayCapacityOverrides != null && relayCapacityOverrides.TryGetValue(relay, out int overriddenCapacity))
        {
            return Mathf.Max(0, overriddenCapacity);
        }

        return relay != null ? relay.SupplyCapacity : 0;
    }

    private static int GetTowerPowerRequired(DefenseTower tower, Dictionary<DefenseTower, int> towerPowerOverrides)
    {
        if (towerPowerOverrides != null && towerPowerOverrides.TryGetValue(tower, out int overriddenPowerRequired))
        {
            return Mathf.Max(0, overriddenPowerRequired);
        }

        return tower != null ? tower.PowerRequired : 0;
    }

    private void CollectRuntimeStructures(out List<RelayTower> relays, out List<DefenseTower> towers)
    {
        relays = new List<RelayTower>();
        towers = new List<DefenseTower>();

        if (_placedTowerRoot == null)
        {
            return;
        }

        for (int index = 0; index < _placedTowerRoot.childCount; index++)
        {
            Transform child = _placedTowerRoot.GetChild(index);
            if (child == null || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            RelayTower relayTower = child.GetComponent<RelayTower>();
            if (relayTower != null)
            {
                relays.Add(relayTower);
            }

            DefenseTower defenseTower = child.GetComponent<DefenseTower>();
            if (defenseTower != null)
            {
                towers.Add(defenseTower);
            }
        }
    }

    private void AssignNumbersToExistingStructures()
    {
        CollectRuntimeStructures(out List<RelayTower> relays, out List<DefenseTower> towers);

        relays.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        towers.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        for (int relayIndex = 0; relayIndex < relays.Count; relayIndex++)
        {
            if (relays[relayIndex].RelayNumber >= 100 && _relayNumbers.Count > 0)
            {
                relays[relayIndex].AssignRelayNumber(_relayNumbers.Pop());
            }
        }

        for (int towerIndex = 0; towerIndex < towers.Count; towerIndex++)
        {
            if (towers[towerIndex].TowerNumber >= 100 && _towerNumbers.Count > 0)
            {
                towers[towerIndex].AssignTowerNumber(_towerNumbers.Pop());
            }
        }
    }

    private sealed class SimulationResult
    {
        public Dictionary<RelayTower, RelayEvaluation> Evaluations { get; set; }
        public Dictionary<DefenseTower, RelayTower> Assignments { get; set; }
    }
}
