using System.Collections.Generic;
using UnityEngine;

public enum BodyPart
{
    Head,
    Body,
    ArmLeft,
    ArmRight,
    Legs
}

public enum ItemCategory
{
    Form,
    Modifier,
    Special
}

public enum ItemRarity
{
    Normal,
    Rare,
    Epic,
    Legend
}

public enum StatType
{
    None,
    MaxHealth,
    AttackDamage,
    AttackSpeed,
    MoveSpeed,
    Range,
    KnockbackForce,
    CritChance,
    CritMultiplier,
    CollectionRange
}

[System.Serializable]
public struct StatEntry
{
    public StatType Type;
    public float Value;
}

[CreateAssetMenu(fileName = "NewItem", menuName = "GlitchDuck/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string ItemID;
    public string KR_Name;
    [Tooltip("조합 시 무기 이름에 사용되는 키워드")]
    public string Keyword;
    [TextArea(2, 4)]
    public string Description;

    [Header("분류")]
    public ItemCategory Category;
    public ItemRarity Rarity;

    [Header("로직")]
    [Tooltip("무기 이름 조합 시 우선순위 (높을수록 앞에 배치)")]
    public int Priority;
    [Tooltip("상태 이상 ID (예: ST_SLOW, ST_BURN). 없으면 비워둠")]
    public string Status_ID;
    [Tooltip("상태 이상 발동 확률 (0~1). 1.0 = 100% 확정, 0.1 = 10% 확률)")]
    [Range(0f, 1f)]
    public float StatusTriggerChance = 1f;

    [Header("스탯 보너스")]
    public List<StatEntry> StatBonuses = new List<StatEntry>();

    [Header("파워")]
    [Tooltip("아이템의 파워 점수 — 난이도별 보상 조합에 사용")]
    public int PowerScore = 1;

    [Header("외형")]
    public Sprite Icon;
    public BodyPart TargetBodyPart;
    public Sprite AppearanceSprite;
    [Tooltip("Sorting offset added on top of the body part base order")]
    public int SortingOrderOffset = 1;
    [Tooltip("파츠 localScale (x, y). 기본값 (1, 1) = 크기 변화 없음")]
    public Vector2 PartScale = Vector2.one;
    [Tooltip("파츠 색상 틴트. 기본값 White = 색상 변화 없음")]
    public Color PartColor = Color.white;

    [Header("Form 무기 오버라이드")]
    [Tooltip("Form 아이템 장착 시 무기 스프라이트 변경")]
    public Sprite FormWeaponSprite;
    [Tooltip("Form 아이템 장착 시 무기 사거리 변경 (0이면 변경 없음)")]
    public float FormHitRadius;

    [Header("월드 표시")]
    public Sprite PedestalSprite;

    // --- Stat helpers ---
    private float GetBonus(StatType type)
    {
        float total = 0f;
        if (StatBonuses == null) return total;
        foreach (var entry in StatBonuses)
            if (entry.Type == type) total += entry.Value;
        return total;
    }

    public float BonusHp => GetBonus(StatType.MaxHealth);
    public float BonusMoveSpeed => GetBonus(StatType.MoveSpeed);
    public float BonusAttackDamage => GetBonus(StatType.AttackDamage);
    public float BonusAttackSpeed => GetBonus(StatType.AttackSpeed);
    public float BonusRange => GetBonus(StatType.Range);
    public float BonusKnockbackForce => GetBonus(StatType.KnockbackForce);
    public float BonusCritChance => GetBonus(StatType.CritChance);
    public float BonusCritMultiplier => GetBonus(StatType.CritMultiplier);
    public float BonusCollectionRange => GetBonus(StatType.CollectionRange);
}
