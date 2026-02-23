using System;
using UnityEngine;

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}

[Serializable]
public class DifficultySettings
{
    public string DisplayName;
    public float EnemyCountMultiplier = 1f;
    public ItemRarity MinRarity;
    public ItemRarity MaxRarity;

    [Tooltip("보상 3개의 PowerScore 합계 최솟값")]
    public int MinPowerScore = 3;
    [Tooltip("보상 3개의 PowerScore 합계 최댓값")]
    public int MaxPowerScore = 10;

    [Tooltip("이 난이도에서 스폰될 장애물 기본 개수")]
    public int ObstacleBaseCount = 3;

    [Tooltip("UI hint text for monster count")]
    public string MonsterCountHint;

    [Tooltip("UI hint text for reward grade")]
    public string RewardGradeHint;
}
