using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifficultySelectManager : MonoBehaviour
{
    public static DifficultySelectManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject _difficultyPanel;
    [SerializeField] private List<DifficultySlotUI> _slots;

    [Header("Fade")]
    [SerializeField] private float _fadeDuration = 0.3f;

    [Header("Difficulty Presets")]
    [SerializeField] private List<DifficultySettings> _presets = new List<DifficultySettings>
    {
        new DifficultySettings
        {
            DisplayName = "쉬움",
            EnemyCountMultiplier = 0.7f,
            MinRarity = ItemRarity.Normal,
            MaxRarity = ItemRarity.Rare,
            MinPowerScore = 3,
            MaxPowerScore = 8,
            ObstacleBaseCount = 2,
            MonsterCountHint = "적 수 감소",
            RewardGradeHint = "일반 보상"
        },
        new DifficultySettings
        {
            DisplayName = "보통",
            EnemyCountMultiplier = 1.0f,
            MinRarity = ItemRarity.Normal,
            MaxRarity = ItemRarity.Rare,
            MinPowerScore = 6,
            MaxPowerScore = 14,
            ObstacleBaseCount = 4,
            MonsterCountHint = "기본 적 수",
            RewardGradeHint = "희귀 보상 가능"
        },
        new DifficultySettings
        {
            DisplayName = "어려움",
            EnemyCountMultiplier = 1.5f,
            MinRarity = ItemRarity.Rare,
            MaxRarity = ItemRarity.Legend,
            MinPowerScore = 12,
            MaxPowerScore = 20,
            ObstacleBaseCount = 6,
            MonsterCountHint = "적 수 증가",
            RewardGradeHint = "전설 보상 가능"
        }
    };

    // Events
    public event Action<DifficultyLevel> OnDifficultySelected;

    // Current selection accessors
    public float EnemyCountMultiplier { get; private set; } = 1f;
    public ItemRarity MinRarity { get; private set; } = ItemRarity.Normal;
    public ItemRarity MaxRarity { get; private set; } = ItemRarity.Legend;
    public int MinPowerScore { get; private set; } = 3;
    public int MaxPowerScore { get; private set; } = 20;
    public int ObstacleBaseCount { get; private set; } = 3;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_difficultyPanel != null)
        {
            _canvasGroup = _difficultyPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = _difficultyPanel.AddComponent<CanvasGroup>();

            _canvasGroup.alpha = 0f;
            _difficultyPanel.SetActive(false);
        }
    }

    public void ShowSelection()
    {
        if (_difficultyPanel == null) return;

        Time.timeScale = 0f;

        // Setup slots
        DifficultyLevel[] levels = { DifficultyLevel.Easy, DifficultyLevel.Normal, DifficultyLevel.Hard };
        for (int i = 0; i < _slots.Count && i < _presets.Count; i++)
        {
            _slots[i].Setup(_presets[i], levels[i], OnSlotClicked);
        }

        _difficultyPanel.SetActive(true);
        StartCoroutine(FadeIn());
    }

    private void OnSlotClicked(DifficultyLevel level)
    {
        int index = (int)level;
        if (index < _presets.Count)
        {
            DifficultySettings settings = _presets[index];
            EnemyCountMultiplier = settings.EnemyCountMultiplier;
            MinRarity = settings.MinRarity;
            MaxRarity = settings.MaxRarity;
            MinPowerScore = settings.MinPowerScore;
            MaxPowerScore = settings.MaxPowerScore;
            ObstacleBaseCount = settings.ObstacleBaseCount;
        }

        StartCoroutine(FadeOutThenNotify(level));
    }

    private IEnumerator FadeIn()
    {
        _canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutThenNotify(DifficultyLevel level)
    {
        float elapsed = 0f;
        float duration = _fadeDuration * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _difficultyPanel.SetActive(false);

        Time.timeScale = 1f;
        OnDifficultySelected?.Invoke(level);
    }
}
