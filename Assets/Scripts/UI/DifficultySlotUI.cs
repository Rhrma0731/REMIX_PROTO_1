using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultySlotUI : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _difficultyName;
    [SerializeField] private TextMeshProUGUI _monsterHint;
    [SerializeField] private TextMeshProUGUI _rewardHint;
    [SerializeField] private Button _selectButton;

    private static readonly Color COLOR_EASY = new Color(0.3f, 0.8f, 0.3f, 1f);
    private static readonly Color COLOR_NORMAL = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color COLOR_HARD = new Color(0.9f, 0.25f, 0.25f, 1f);

    private DifficultyLevel _level;
    private Action<DifficultyLevel> _onSelected;

    public void Setup(DifficultySettings settings, DifficultyLevel level, Action<DifficultyLevel> onSelected)
    {
        _level = level;
        _onSelected = onSelected;

        _difficultyName.text = settings.DisplayName;
        _monsterHint.text = settings.MonsterCountHint;
        _rewardHint.text = settings.RewardGradeHint;

        switch (level)
        {
            case DifficultyLevel.Easy:   _background.color = COLOR_EASY;   break;
            case DifficultyLevel.Normal: _background.color = COLOR_NORMAL; break;
            case DifficultyLevel.Hard:   _background.color = COLOR_HARD;   break;
        }

        _selectButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        _selectButton.onClick.RemoveAllListeners();
        _onSelected?.Invoke(_level);
    }
}
