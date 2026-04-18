using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// `Enemy` is the runtime bridge for path-following, health, and hit feedback.
///
/// At this stage of the project, the enemy still intentionally stays lightweight:
/// - it follows a fixed path
/// - it takes damage from towers
/// - it damages the base if it reaches the endpoint
///
/// The extra layer added in this pass is "readability feedback":
/// we want the player to immediately see the difference between
/// a precise hit, a slow-field application, and a bombard blast.
///
/// This script therefore owns:
/// 1. Movement along `EnemyPath`
/// 2. Health and health-bar updates
/// 3. Lightweight body flash / slow tint / scale pulse feedback
///
/// We keep the feedback inside the enemy instead of scattering it across towers,
/// because the enemy is the one object that knows how it should visually react when struck.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour
{
    /// <summary>
    /// `DamageFeedbackType` lets towers tell the enemy what kind of hit just happened.
    ///
    /// This keeps feedback intent explicit:
    /// - single-target tower: precise direct hit
    /// - slow-field tower: control hit
    /// - bombard tower: heavier blast hit
    /// </summary>
    public enum DamageFeedbackType
    {
        Standard,
        SlowField,
        Bombard
    }

    /// <summary>
    /// Active enemies are tracked in a flat list so towers can scan targets cheaply.
    /// This is still a very reasonable structure for a prototype tower-defense scale.
    /// </summary>
    private static readonly List<Enemy> ActiveEnemies = new List<Enemy>();

    [Header("Movement")]

    /// <summary>
    /// Distance tolerance used to decide whether the enemy has effectively reached a waypoint.
    /// </summary>
    [SerializeField] private float reachWaypointDistance = 0.05f;

    [Header("Body Look")]

    /// <summary>
    /// Base body color when the enemy is in a neutral state.
    /// </summary>
    [SerializeField] private Color bodyColor = new Color(0.9f, 0.25f, 0.25f, 1f);

    /// <summary>
    /// While the enemy is slowed, we blend the body toward this color.
    /// This is a simple, art-replacement-friendly way to show control status.
    /// </summary>
    [SerializeField] private Color slowTintColor = new Color(0.42f, 0.95f, 0.9f, 1f);

    /// <summary>
    /// Precise hits use a bright flash so the player can read single-target focus fire.
    /// </summary>
    [SerializeField] private Color standardHitFlashColor = new Color(1f, 0.96f, 0.9f, 1f);

    /// <summary>
    /// Bombard hits use a warmer flash so blast impact reads heavier than a normal shot.
    /// </summary>
    [SerializeField] private Color bombardHitFlashColor = new Color(1f, 0.74f, 0.45f, 1f);

    [Header("Body Feedback Timing")]
    [SerializeField] private float standardHitFlashDuration = 0.08f;
    [SerializeField] private float bombardHitFlashDuration = 0.16f;
    [SerializeField] private float slowFeedbackFlashDuration = 0.1f;
    [SerializeField] private float standardHitPulseScale = 1.05f;
    [SerializeField] private float bombardHitPulseScale = 1.13f;
    [SerializeField] private float slowHitPulseScale = 1.04f;
    [SerializeField] private float hitPulseDuration = 0.12f;

    [Header("Health Bar Visuals")]
    [SerializeField] private Color healthBarFillColor = new Color(0.2f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color healthBarBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("Health Bar References")]

    /// <summary>
    /// Scene-authored explicit reference chain for the health bar.
    /// This keeps the enemy prefab resilient against hierarchy renames.
    /// </summary>
    [SerializeField] private Transform healthBarRootReference;
    [SerializeField] private Transform healthBarFillReference;
    [SerializeField] private SpriteRenderer healthBarFillRendererReference;
    [SerializeField] private SpriteRenderer healthBarBackgroundRendererReference;

    private SpriteRenderer _spriteRenderer;
    private Transform _healthBarRoot;
    private Transform _healthBarFill;
    private SpriteRenderer _healthBarFillRenderer;
    private SpriteRenderer _healthBarBackgroundRenderer;

    private EnemyPath _path;
    private float _moveSpeed;
    private float _slowMultiplier = 1f;
    private float _slowTimer;
    private int _maxHealth;
    private int _currentHealth;
    private int _scrapRewardOnDeath;
    private int _targetWaypointIndex;
    private bool _hasReachedBase;

    private Vector3 _baseScale = Vector3.one;
    private float _hitFlashTimer;
    private float _hitFlashDuration;
    private Color _hitFlashColor = Color.white;
    private float _pulseTimer;
    private float _pulseDuration;
    private float _pulseScaleMultiplier = 1f;

    public static int ActiveEnemyCount => ActiveEnemies.Count;

    public static Enemy GetActiveEnemy(int index)
    {
        return ActiveEnemies[index];
    }

    /// <summary>
    /// This remains a simple presentation hook for end-state cleanup.
    /// </summary>
    public void SetHealthBarVisible(bool visible)
    {
        CacheReferences();

        if (_healthBarRoot != null)
        {
            _healthBarRoot.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Editor-time helper: if the fill transform is assigned but its renderer is not,
    /// we can safely derive that reference without relying on object names.
    /// </summary>
    private void OnValidate()
    {
        if (healthBarFillReference != null && healthBarFillRendererReference == null)
        {
            healthBarFillRendererReference = healthBarFillReference.GetComponent<SpriteRenderer>();
        }
    }

    private void Awake()
    {
        CacheReferences();
        _baseScale = transform.localScale;
        ApplyVisualTheme();
        RefreshBodyVisualState();
        SetHealthBarVisible(true);
    }

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
        {
            ActiveEnemies.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    /// <summary>
    /// Spawn-time runtime setup.
    /// We intentionally reuse one prototype and inject path / speed / health / scrap reward per wave.
    /// </summary>
    public void Initialize(EnemyPath path, float moveSpeed, int maxHealth, int scrapRewardOnDeath = 0)
    {
        CacheReferences();
        _baseScale = transform.localScale;
        ApplyVisualTheme();

        _path = path;
        _moveSpeed = moveSpeed;
        _maxHealth = Mathf.Max(1, maxHealth);
        _currentHealth = _maxHealth;
        _scrapRewardOnDeath = Mathf.Max(0, scrapRewardOnDeath);
        _targetWaypointIndex = 1;
        _hasReachedBase = false;
        _slowMultiplier = 1f;
        _slowTimer = 0f;
        _hitFlashTimer = 0f;
        _pulseTimer = 0f;
        _pulseScaleMultiplier = 1f;

        if (_path != null)
        {
            transform.position = _path.GetSpawnPosition();
        }

        RefreshBodyVisualState();
        UpdateHealthBar();
    }

    /// <summary>
    /// `Update()` keeps movement and presentation feedback in one place.
    ///
    /// We advance timers first, so body tint / hit flash / pulse stay responsive,
    /// then continue with waypoint motion if the match is still active.
    /// </summary>
    private void Update()
    {
        AdvanceFeedbackTimers();
        RefreshBodyVisualState();

        if (TowerDefenseGame.Instance != null && TowerDefenseGame.Instance.IsGameOver)
        {
            return;
        }

        if (_path == null || _path.WaypointCount == 0 || _hasReachedBase)
        {
            return;
        }

        if (_targetWaypointIndex >= _path.WaypointCount)
        {
            ReachBase();
            return;
        }

        Vector3 targetPosition = _path.GetWaypointPosition(_targetWaypointIndex);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * _slowMultiplier * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) <= reachWaypointDistance)
        {
            _targetWaypointIndex++;

            if (_targetWaypointIndex >= _path.WaypointCount)
            {
                ReachBase();
            }
        }
    }

    /// <summary>
    /// Standard damage entry point kept for compatibility with existing callers.
    /// </summary>
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, DamageFeedbackType.Standard);
    }

    /// <summary>
    /// Extended damage entry point with explicit feedback intent.
    /// Towers use this overload when they want the enemy to react differently to different hit families.
    /// </summary>
    public void TakeDamage(int amount, DamageFeedbackType feedbackType)
    {
        if (amount <= 0 || _currentHealth <= 0)
        {
            return;
        }

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        TriggerHitFeedback(feedbackType);
        UpdateHealthBar();

        if (_currentHealth == 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Applying slow now also counts as a visible combat event.
    /// This matters because the slow tower intentionally starts with zero damage,
    /// so the player still needs some immediate feedback that the control effect landed.
    /// </summary>
    public void ApplySlow(float slowMultiplier, float duration)
    {
        if (_currentHealth <= 0)
        {
            return;
        }

        _slowMultiplier = Mathf.Clamp(Mathf.Min(_slowMultiplier, slowMultiplier), 0.15f, 1f);
        _slowTimer = Mathf.Max(_slowTimer, duration);
        TriggerHitFeedback(DamageFeedbackType.SlowField);
    }

    private void ReachBase()
    {
        if (_hasReachedBase)
        {
            return;
        }

        _hasReachedBase = true;
        TowerDefenseGame.Instance?.DamageBase(1);
        Destroy(gameObject);
    }

    private void Die()
    {
        // Enemy death is now part of the economy loop:
        // after a legal kill, the current wave definition can immediately feed scrap back into the player's resource pool.
        if (_scrapRewardOnDeath > 0 && TowerDefenseGame.Instance != null && !TowerDefenseGame.Instance.IsGameOver)
        {
            TowerDefenseGame.Instance.AddScrap(_scrapRewardOnDeath);
        }

        Destroy(gameObject);
    }

    private void UpdateHealthBar()
    {
        if (_healthBarFill == null)
        {
            return;
        }

        float healthRatio = _maxHealth <= 0 ? 0f : (float)_currentHealth / _maxHealth;

        Vector3 fillScale = _healthBarFill.localScale;
        fillScale.x = healthRatio;
        _healthBarFill.localScale = fillScale;

        Vector3 fillPosition = _healthBarFill.localPosition;
        fillPosition.x = (healthRatio - 1f) * 0.5f;
        _healthBarFill.localPosition = fillPosition;
    }

    private void CacheReferences()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_healthBarRoot == null)
        {
            _healthBarRoot = healthBarRootReference;
        }

        if (_healthBarFill == null)
        {
            _healthBarFill = healthBarFillReference;
        }

        if (_healthBarFillRenderer == null)
        {
            _healthBarFillRenderer = healthBarFillRendererReference;
            if (_healthBarFillRenderer == null && _healthBarFill != null)
            {
                _healthBarFillRenderer = _healthBarFill.GetComponent<SpriteRenderer>();
            }
        }

        if (_healthBarBackgroundRenderer == null)
        {
            _healthBarBackgroundRenderer = healthBarBackgroundRendererReference;
        }
    }

    private void ApplyVisualTheme()
    {
        if (_healthBarFillRenderer != null)
        {
            _healthBarFillRenderer.color = healthBarFillColor;
        }

        if (_healthBarBackgroundRenderer != null)
        {
            _healthBarBackgroundRenderer.color = healthBarBackgroundColor;
        }
    }

    /// <summary>
    /// A single helper makes the combat reaction readable without introducing a heavyweight status-effect system.
    /// </summary>
    private void TriggerHitFeedback(DamageFeedbackType feedbackType)
    {
        switch (feedbackType)
        {
            case DamageFeedbackType.Bombard:
                _hitFlashColor = bombardHitFlashColor;
                _hitFlashDuration = bombardHitFlashDuration;
                _hitFlashTimer = bombardHitFlashDuration;
                _pulseScaleMultiplier = bombardHitPulseScale;
                _pulseDuration = hitPulseDuration;
                _pulseTimer = hitPulseDuration;
                break;

            case DamageFeedbackType.SlowField:
                _hitFlashColor = slowTintColor;
                _hitFlashDuration = slowFeedbackFlashDuration;
                _hitFlashTimer = slowFeedbackFlashDuration;
                _pulseScaleMultiplier = slowHitPulseScale;
                _pulseDuration = hitPulseDuration;
                _pulseTimer = hitPulseDuration;
                break;

            default:
                _hitFlashColor = standardHitFlashColor;
                _hitFlashDuration = standardHitFlashDuration;
                _hitFlashTimer = standardHitFlashDuration;
                _pulseScaleMultiplier = standardHitPulseScale;
                _pulseDuration = hitPulseDuration;
                _pulseTimer = hitPulseDuration;
                break;
        }
    }

    private void AdvanceFeedbackTimers()
    {
        if (_slowTimer > 0f)
        {
            _slowTimer -= Time.deltaTime;
            if (_slowTimer <= 0f)
            {
                _slowTimer = 0f;
                _slowMultiplier = 1f;
            }
        }

        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer = Mathf.Max(0f, _hitFlashTimer - Time.deltaTime);
        }

        if (_pulseTimer > 0f)
        {
            _pulseTimer = Mathf.Max(0f, _pulseTimer - Time.deltaTime);
        }
    }

    /// <summary>
    /// Body feedback is layered in a deterministic order:
    /// 1. base color
    /// 2. slow tint
    /// 3. temporary hit flash
    ///
    /// This gives us readable combat feedback without needing per-hit child effects on every enemy.
    /// </summary>
    private void RefreshBodyVisualState()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        Color bodyResult = bodyColor;
        if (_slowTimer > 0f)
        {
            bodyResult = Color.Lerp(bodyResult, slowTintColor, 0.42f);
        }

        if (_hitFlashTimer > 0f && _hitFlashDuration > 0.0001f)
        {
            float flashStrength = Mathf.Clamp01(_hitFlashTimer / _hitFlashDuration);
            bodyResult = Color.Lerp(bodyResult, _hitFlashColor, flashStrength);
        }

        _spriteRenderer.color = bodyResult;

        float pulseScale = 1f;
        if (_pulseTimer > 0f && _pulseDuration > 0.0001f)
        {
            float pulseProgress = 1f - Mathf.Clamp01(_pulseTimer / _pulseDuration);
            pulseScale = 1f + Mathf.Sin(pulseProgress * Mathf.PI) * (_pulseScaleMultiplier - 1f);
        }

        transform.localScale = _baseScale * pulseScale;
    }
}
