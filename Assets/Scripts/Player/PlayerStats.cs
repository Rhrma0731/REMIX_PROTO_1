using System;
using UnityEngine;

[Serializable]
public struct StatBlock
{
    public float MaxHp;
    public float MoveSpeed;
    public float AttackDamage;
    public float AttackSpeed;
    public float Range;
    public float KnockbackForce;
    public float CritChance;
    public float CritMultiplier;
    public float CollectionRange;

    public static StatBlock operator +(StatBlock a, StatBlock b)
    {
        return new StatBlock
        {
            MaxHp = a.MaxHp + b.MaxHp,
            MoveSpeed = a.MoveSpeed + b.MoveSpeed,
            AttackDamage = a.AttackDamage + b.AttackDamage,
            AttackSpeed = a.AttackSpeed + b.AttackSpeed,
            Range = a.Range + b.Range,
            KnockbackForce = a.KnockbackForce + b.KnockbackForce,
            CritChance = a.CritChance + b.CritChance,
            CritMultiplier = a.CritMultiplier + b.CritMultiplier,
            CollectionRange = a.CollectionRange + b.CollectionRange,
        };
    }
}

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    [SerializeField] private float _baseMaxHp = 100f;
    [SerializeField] private float _baseMoveSpeed = 5.5f;
    [SerializeField] private float _baseAttackDamage = 20f;
    [SerializeField] private float _baseAttackSpeed = 1f;
    [SerializeField] private float _baseCritChance = 0.1f;
    [SerializeField] private float _baseCritMultiplier = 2f;

    // Events
    public event Action<StatBlock> OnStatsChanged;
    public event Action<float, float> OnHpChanged; // current, max
    public event Action OnPlayerDeath;

    private StatBlock _bonusStats;
    private float _currentHp;
    private bool _isDead;
    private float _invincibleUntil = -1f;

    public bool IsDead => _isDead;

    // Computed totals
    public float MaxHp => _baseMaxHp + _bonusStats.MaxHp;
    public float MoveSpeed => Mathf.Max(0.5f, _baseMoveSpeed + _bonusStats.MoveSpeed);
    public float AttackDamage => Mathf.Max(1f, _baseAttackDamage + _bonusStats.AttackDamage);
    public float AttackSpeed => Mathf.Max(0.1f, _baseAttackSpeed + _bonusStats.AttackSpeed);
    public float CritChance => Mathf.Clamp01(_baseCritChance + _bonusStats.CritChance);
    public float CritMultiplier => Mathf.Max(1f, _baseCritMultiplier + _bonusStats.CritMultiplier);
    public float CurrentHp => _currentHp;

    // Additive bonus accessors (no base — these start at 0 and grow from items)
    public float BonusRange => _bonusStats.Range;
    public float BonusKnockbackForce => _bonusStats.KnockbackForce;
    public float BonusCollectionRange => _bonusStats.CollectionRange;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _bonusStats = new StatBlock();
        _currentHp = MaxHp;
    }

    /// <summary>
    /// Apply stat bonuses from an acquired item.
    /// </summary>
    public void ApplyItem(ItemData item)
    {
        _bonusStats.MaxHp += item.BonusHp;
        _bonusStats.MoveSpeed += item.BonusMoveSpeed;
        _bonusStats.AttackDamage += item.BonusAttackDamage;
        _bonusStats.AttackSpeed += item.BonusAttackSpeed;
        _bonusStats.Range += item.BonusRange;
        _bonusStats.KnockbackForce += item.BonusKnockbackForce;
        _bonusStats.CritChance += item.BonusCritChance;
        _bonusStats.CritMultiplier += item.BonusCritMultiplier;
        _bonusStats.CollectionRange += item.BonusCollectionRange;

        // Heal proportional to HP bonus
        if (item.BonusHp > 0f)
        {
            _currentHp = Mathf.Min(_currentHp + item.BonusHp, MaxHp);
            OnHpChanged?.Invoke(_currentHp, MaxHp);
        }

        OnStatsChanged?.Invoke(GetTotalStats());
    }

    public bool IsInvincible => Time.time < _invincibleUntil;

    /// <summary>지정 시간(초) 동안 무적 상태를 부여한다.</summary>
    public void SetInvincible(float duration)
    {
        _invincibleUntil = Mathf.Max(_invincibleUntil, Time.time + duration);
    }

    public void TakeDamage(float damage)
    {
        if (_isDead) return;
        if (IsInvincible) return;

        _currentHp = Mathf.Max(0f, _currentHp - damage);
        PlayerEventManager.Instance?.BroadcastTakeDamage(damage);
        OnHpChanged?.Invoke(_currentHp, MaxHp);

        if (_currentHp <= 0f)
        {
            // 사망 판정 전 OnFatalDamage 방송 — ReviveAction 등이 체력을 회복할 기회
            PlayerEventManager.Instance?.BroadcastFatalDamage();

            // 부활 효과가 체력을 회복했으면 사망 취소
            if (_currentHp > 0f) return;

            _isDead = true;
            OnPlayerDeath?.Invoke();
        }
    }

    /// <summary>
    /// 사망 상태를 해제하고 체력을 회복한다. OnFatalDamage 핸들러에서 호출.
    /// </summary>
    public void Revive(float hp)
    {
        _isDead = false;
        _currentHp = Mathf.Min(hp, MaxHp);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public void Heal(float amount)
    {
        _currentHp = Mathf.Min(_currentHp + amount, MaxHp);
        PlayerEventManager.Instance?.BroadcastHeal(amount);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public StatBlock GetTotalStats()
    {
        return new StatBlock
        {
            MaxHp = MaxHp,
            MoveSpeed = MoveSpeed,
            AttackDamage = AttackDamage,
            AttackSpeed = AttackSpeed,
            CritChance = CritChance,
            CritMultiplier = CritMultiplier,
        };
    }

    /// <summary>
    /// Reset all bonus stats (new run).
    /// </summary>
    public void ResetStats()
    {
        _bonusStats = new StatBlock();
        _currentHp = MaxHp;
        OnStatsChanged?.Invoke(GetTotalStats());
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }
}
