using System;
using UnityEngine;

[Serializable]
public struct StatBlock
{
    public float MaxHp;
    public float MoveSpeed;
    public float AttackDamage;
    public float AttackSpeed;
    public float CritChance;
    public float CritMultiplier;

    public static StatBlock operator +(StatBlock a, StatBlock b)
    {
        return new StatBlock
        {
            MaxHp = a.MaxHp + b.MaxHp,
            MoveSpeed = a.MoveSpeed + b.MoveSpeed,
            AttackDamage = a.AttackDamage + b.AttackDamage,
            AttackSpeed = a.AttackSpeed + b.AttackSpeed,
            CritChance = a.CritChance + b.CritChance,
            CritMultiplier = a.CritMultiplier + b.CritMultiplier,
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

    private StatBlock _bonusStats;
    private float _currentHp;

    // Computed totals
    public float MaxHp => _baseMaxHp + _bonusStats.MaxHp;
    public float MoveSpeed => Mathf.Max(0.5f, _baseMoveSpeed + _bonusStats.MoveSpeed);
    public float AttackDamage => Mathf.Max(1f, _baseAttackDamage + _bonusStats.AttackDamage);
    public float AttackSpeed => Mathf.Max(0.1f, _baseAttackSpeed + _bonusStats.AttackSpeed);
    public float CritChance => Mathf.Clamp01(_baseCritChance + _bonusStats.CritChance);
    public float CritMultiplier => Mathf.Max(1f, _baseCritMultiplier + _bonusStats.CritMultiplier);
    public float CurrentHp => _currentHp;

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

        // Heal proportional to HP bonus
        if (item.BonusHp > 0f)
        {
            _currentHp = Mathf.Min(_currentHp + item.BonusHp, MaxHp);
            OnHpChanged?.Invoke(_currentHp, MaxHp);
        }

        OnStatsChanged?.Invoke(GetTotalStats());
    }

    public void TakeDamage(float damage)
    {
        _currentHp = Mathf.Max(0f, _currentHp - damage);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public void Heal(float amount)
    {
        _currentHp = Mathf.Min(_currentHp + amount, MaxHp);
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
