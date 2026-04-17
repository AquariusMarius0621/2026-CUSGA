using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Phase-two runtime implementation for relay nodes.
///
/// We keep the implementation split from `RelayTower.cs` for two practical reasons:
/// 1. The Unity scene script asset should stay stable and easy to recognize in the Inspector.
/// 2. The power-grid logic can keep growing without turning the script asset file into a giant mixed-purpose blob.
///
/// The relay is intentionally editor-friendly:
/// - supply range is adjustable in the Inspector
/// - supply capacity is adjustable in the Inspector
/// - the coverage preview uses Gizmos
/// - no visual dependency is hardcoded to a specific art asset
/// </summary>
public partial class RelayTower
{
    [Header("Supply")]
    [SerializeField] private float supplyRange = 4.5f;
    [SerializeField] private int baseSupplyCapacity = 6;
    [SerializeField] private int supplyCapacityPerUpgrade = 2;

    [Header("Progression")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int maxLevel = 3;
    [SerializeField] private int upgradeCostBase = 20;
    [SerializeField] private int upgradeCostPerLevel = 10;

    [Header("Visuals")]
    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color saturatedColor = new Color(1f, 0.5f, 0.18f, 1f);
    [SerializeField] private Color gizmoColor = new Color(1f, 0.78f, 0.28f, 0.8f);

    private SpriteRenderer _spriteRenderer;
    private int _currentAssignedLoad;

    public int RelayNumber { get; private set; } = 100;
    public int CurrentLevel => Mathf.Max(1, currentLevel);
    public int MaxLevel => Mathf.Max(1, maxLevel);
    public int SupplyCapacity => Mathf.Max(0, baseSupplyCapacity + (CurrentLevel - 1) * supplyCapacityPerUpgrade);
    public float SupplyRange => Mathf.Max(0.1f, supplyRange);
    public int CurrentAssignedLoad => _currentAssignedLoad;
    public int RemainingCapacity => Mathf.Max(0, SupplyCapacity - _currentAssignedLoad);

    public Bounds CoverageBounds =>
        new Bounds(transform.position, new Vector3(SupplyRange * 2f, SupplyRange * 2f, 0.01f));

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        RefreshVisualState();
    }

    public void AssignRelayNumber(int relayNumber)
    {
        RelayNumber = Mathf.Clamp(relayNumber, 1, 100);
    }

    public void ResetRuntimeLoad()
    {
        _currentAssignedLoad = 0;
        RefreshVisualState();
    }

    public void SetRuntimeLoad(int assignedLoad)
    {
        _currentAssignedLoad = Mathf.Max(0, assignedLoad);
        RefreshVisualState();
    }

    public bool CanUpgrade => CurrentLevel < MaxLevel;

    public int GetUpgradeCost()
    {
        return upgradeCostBase + (CurrentLevel - 1) * upgradeCostPerLevel;
    }

    public int PreviewUpgradedSupplyCapacity()
    {
        if (!CanUpgrade)
        {
            return SupplyCapacity;
        }

        return Mathf.Max(0, baseSupplyCapacity + CurrentLevel * supplyCapacityPerUpgrade);
    }

    public void ApplyUpgrade()
    {
        if (!CanUpgrade)
        {
            return;
        }

        currentLevel++;
        RefreshVisualState();
    }

    public bool ContainsPoint(Vector3 worldPosition)
    {
        return (worldPosition - transform.position).sqrMagnitude <= SupplyRange * SupplyRange;
    }

    private void RefreshVisualState()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        _spriteRenderer.color = RemainingCapacity > 0 ? normalColor : saturatedColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, SupplyRange);
    }

    private void OnDestroy()
    {
        if (TowerDefenseGame.Instance != null)
        {
            TowerDefenseGame.Instance.NotifyStructureTopologyChanged();
        }
    }

    private void OnMouseDown()
    {
        if (TowerDefenseGame.Instance == null)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        TowerDefenseGame.Instance.SelectPlacedStructure(this);
    }
}
