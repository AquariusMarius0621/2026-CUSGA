using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Basic offensive tower.
///
/// In phase two it gains a real power state:
/// - the tower stays on the field when offline
/// - the tower stops attacking while offline
/// - the tower exposes its power requirement in the Inspector
///
/// This matches the new gameplay rule while still keeping the combat script
/// small and easy to tune from the editor.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DefenseTower : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float attackRange = 2.8f;
    [SerializeField] private float attackInterval = 0.65f;
    [SerializeField] private int baseDamagePerShot = 1;
    [SerializeField] private int damagePerUpgrade = 1;
    [SerializeField] private int basePowerRequired = 2;
    [SerializeField] private int powerRequiredPerUpgrade = 1;

    [Header("Progression")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int maxLevel = 3;
    [SerializeField] private int upgradeCostBase = 30;
    [SerializeField] private int upgradeCostPerLevel = 15;

    [Header("Visuals")]
    [SerializeField] private Color poweredColor = new Color(0.2f, 0.55f, 1f, 1f);
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private Color offlineColor = new Color(0.24f, 0.28f, 0.36f, 1f);

    private SpriteRenderer _spriteRenderer;
    private float _attackTimer;

    public int TowerNumber { get; private set; } = 100;
    public int CurrentLevel => Mathf.Max(1, currentLevel);
    public int MaxLevel => Mathf.Max(1, maxLevel);
    public int DamagePerShot => Mathf.Max(0, baseDamagePerShot + (CurrentLevel - 1) * damagePerUpgrade);
    public int PowerRequired => Mathf.Max(0, basePowerRequired + (CurrentLevel - 1) * powerRequiredPerUpgrade);
    public bool IsPowered { get; private set; } = true;
    public RelayTower AssignedRelay { get; private set; }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        RefreshVisualState();
    }

    private void Update()
    {
        if (TowerDefenseGame.Instance == null || TowerDefenseGame.Instance.IsGameOver || !IsPowered)
        {
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer < attackInterval)
        {
            return;
        }

        _attackTimer -= attackInterval;

        Enemy target = FindClosestTarget();
        if (target == null)
        {
            return;
        }

        target.TakeDamage(DamagePerShot);
        StartCoroutine(FlashRoutine());
    }

    public void AssignTowerNumber(int towerNumber)
    {
        TowerNumber = Mathf.Clamp(towerNumber, 1, 100);
    }

    public void SetPowerState(bool isPowered, RelayTower assignedRelay)
    {
        IsPowered = isPowered;
        AssignedRelay = assignedRelay;
        RefreshVisualState();
    }

    public bool CanUpgrade => CurrentLevel < MaxLevel;

    public int GetUpgradeCost()
    {
        return upgradeCostBase + (CurrentLevel - 1) * upgradeCostPerLevel;
    }

    public int PreviewUpgradedPowerRequired()
    {
        if (!CanUpgrade)
        {
            return PowerRequired;
        }

        return Mathf.Max(0, basePowerRequired + CurrentLevel * powerRequiredPerUpgrade);
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

    private Enemy FindClosestTarget()
    {
        float maxDistanceSqr = attackRange * attackRange;
        float closestDistanceSqr = float.MaxValue;
        Enemy bestTarget = null;

        for (int i = 0; i < Enemy.ActiveEnemyCount; i++)
        {
            Enemy candidate = Enemy.GetActiveEnemy(i);
            if (candidate == null)
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr > maxDistanceSqr || distanceSqr >= closestDistanceSqr)
            {
                continue;
            }

            closestDistanceSqr = distanceSqr;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private IEnumerator FlashRoutine()
    {
        if (_spriteRenderer == null || !IsPowered)
        {
            yield break;
        }

        _spriteRenderer.color = flashColor;
        yield return null;

        if (_spriteRenderer != null)
        {
            RefreshVisualState();
        }
    }

    private void RefreshVisualState()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        _spriteRenderer.color = IsPowered ? poweredColor : offlineColor;
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
