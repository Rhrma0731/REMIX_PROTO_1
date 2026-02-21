using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "GlitchDuck/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> _items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => _items;
    public int Count => _items.Count;

    private Dictionary<string, ItemData> _idLookup;

    /// <summary>
    /// ItemID로 아이템 검색. 첫 호출 시 딕셔너리 캐싱.
    /// </summary>
    public ItemData GetByID(string itemID)
    {
        if (_idLookup == null)
            RebuildLookup();

        _idLookup.TryGetValue(itemID, out ItemData item);
        return item;
    }

    /// <summary>
    /// 카테고리별 아이템 필터링.
    /// </summary>
    public List<ItemData> GetByCategory(ItemCategory category)
    {
        var result = new List<ItemData>();
        foreach (var item in _items)
        {
            if (item != null && item.Category == category)
                result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// 레어리티별 아이템 필터링.
    /// </summary>
    public List<ItemData> GetByRarity(ItemRarity rarity)
    {
        var result = new List<ItemData>();
        foreach (var item in _items)
        {
            if (item != null && item.Rarity == rarity)
                result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// 레어리티 범위로 아이템 필터링.
    /// </summary>
    public List<ItemData> GetByRarityRange(ItemRarity min, ItemRarity max)
    {
        var result = new List<ItemData>();
        foreach (var item in _items)
        {
            if (item != null && item.Rarity >= min && item.Rarity <= max)
                result.Add(item);
        }
        return result;
    }

    private void RebuildLookup()
    {
        _idLookup = new Dictionary<string, ItemData>();
        foreach (var item in _items)
        {
            if (item != null && !string.IsNullOrEmpty(item.ItemID))
                _idLookup[item.ItemID] = item;
        }
    }

    private void OnEnable()
    {
        _idLookup = null;
    }
}
