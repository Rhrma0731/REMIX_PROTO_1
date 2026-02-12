using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardSlotUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Image _rarityFrame;
    [SerializeField] private Button _selectButton;

    private ItemData _itemData;
    private Action<ItemData> _onSelected;

    private static readonly Color[] RARITY_COLORS =
    {
        new Color(0.7f, 0.7f, 0.7f),   // Common — grey
        new Color(0.3f, 0.8f, 0.3f),   // Uncommon — green
        new Color(0.3f, 0.5f, 1.0f),   // Rare — blue
        new Color(1.0f, 0.75f, 0.2f)   // Legendary — gold
    };

    public void Setup(ItemData item, Action<ItemData> onSelected)
    {
        _itemData = item;
        _onSelected = onSelected;

        _iconImage.sprite = item.Icon;
        _nameText.text = item.ItemName;
        _descriptionText.text = item.Description;
        _rarityFrame.color = RARITY_COLORS[(int)item.Rarity];

        _selectButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        _selectButton.onClick.RemoveAllListeners();
        _onSelected?.Invoke(_itemData);
    }
}
