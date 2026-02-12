using UnityEngine;

public enum BodyPart
{
    Head,
    Body,
    ArmLeft,
    ArmRight,
    Legs
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}

[CreateAssetMenu(fileName = "NewItem", menuName = "GlitchDuck/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string ItemName;
    [TextArea(2, 4)] public string Description;
    public Sprite Icon;
    public ItemRarity Rarity;

    [Header("Appearance Change")]
    public BodyPart TargetBodyPart;
    public Sprite AppearanceSprite;
    [Tooltip("Sorting offset added on top of the body part base order")]
    public int SortingOrderOffset = 1;

    [Header("Stat Modifiers")]
    public float BonusHp;
    public float BonusMoveSpeed;
    public float BonusAttackDamage;
    public float BonusAttackSpeed;

    [Header("World Display")]
    public Sprite PedestalSprite;
}
