using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Coordinates relay coverage, relay numbering, tower numbering, and runtime power allocation.
/// The goal is to keep the phase-two power rules out of the main gameplay orchestrator.
/// </summary>
public sealed class TowerPowerGridCoordinator
{
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

            case TowerType.Defense:
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
            invalidReason = $"Not enough energy. Upgrade requires {upgradeCost} EN.";
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
            invalidReason = $"Not enough energy. Upgrade requires {upgradeCost} EN.";
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
            invalidReason = "Upgrade would exceed the available relay capacity.";
            return false;
        }

        foreach (KeyValuePair<DefenseTower, RelayTower> currentAssignment in currentSimulation.Assignments)
        {
            if (!upgradedSimulation.Assignments.ContainsKey(currentAssignment.Key))
            {
                invalidReason = "Upgrade would force at least one currently powered tower offline.";
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
            towers[i].SetPowerState(false, null);
        }

        if (relays.Count == 0 || towers.Count == 0)
        {
            return;
        }

        relays.Sort((a, b) => a.RelayNumber.CompareTo(b.RelayNumber));
        towers.Sort((a, b) => a.TowerNumber.CompareTo(b.TowerNumber));

        SimulationResult simulation = Simulate(relays, towers, null, null);
        ApplyAssignments(relays, towers, simulation.Assignments);
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
        Dictionary<DefenseTower, RelayTower> assignments)
    {
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
                    tower.SetPowerState(false, null);
                    continue;
                }

                remainingCapacity -= tower.PowerRequired;
                assignedLoad += tower.PowerRequired;
                tower.SetPowerState(true, relay);
            }

            relay.SetRuntimeLoad(assignedLoad);
        }

        int poweredTowerCount = towers.Count(tower => tower.IsPowered);
        int unpoweredTowerCount = towers.Count - poweredTowerCount;
        _logDiagnostic?.Invoke(
            $"Power grid recalculated: relays={relays.Count} powered={poweredTowerCount} offline={unpoweredTowerCount} relayLimit={RelayLimit}");
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
