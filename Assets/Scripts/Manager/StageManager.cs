using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveData
{
    [Tooltip("Prefabs to spawn this wave")]
    public List<GameObject> EnemyPrefabs;

    [Tooltip("Spawn points (random pick per enemy)")]
    public List<Transform> SpawnPoints;
}

[Serializable]
public class StageData
{
    public string StageName;
    public List<WaveData> Waves;
}

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Definitions")]
    [SerializeField] private List<StageData> _stages;

    [Header("Reward Item Pool")]
    [SerializeField] private List<ItemData> _rewardPool;

    [Header("Timing — Dopamine Loop")]
    [SerializeField] private float _delayBeforeReward = 0.15f;
    [SerializeField] private float _delayBeforeNextWave = 0.3f;

    [Header("Camera Transition")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private float _zoomOutAmount = 1.5f;
    [SerializeField] private float _zoomOutDuration = 0.25f;
    [SerializeField] private float _zoomInDuration = 0.35f;

    // Events
    public event Action<int, int> OnWaveStarted;       // stageIndex, waveIndex
    public event Action<int, int> OnWaveCleared;
    public event Action<int> OnStageStart;              // stageIndex — capsule drop hook
    public event Action<int> OnStageCleared;
    public event Action OnAllStagesComplete;

    private int _currentStageIndex;
    private int _currentWaveIndex;
    private List<EnemyBase> _activeEnemies = new List<EnemyBase>();
    private float _baseCameraSize;
    private bool _waitingForReward;
    private bool _waitingForDifficulty;

    // --- Lifecycle ---

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        _baseCameraSize = _mainCamera.orthographic ? _mainCamera.orthographicSize : 5f;

        // ItemDatabase가 있으면 항상 우선 로드 (Inspector 수동 연결보다 우선)
        var db = Resources.Load<ItemDatabase>("ItemDatabase");
        if (db != null && db.Count > 0)
        {
            _rewardPool = new List<ItemData>(db.Items);
            Debug.Log($"[StageManager] ItemDatabase에서 보상 풀 로드 완료 ({_rewardPool.Count}개)");
        }
        else if (_rewardPool == null || _rewardPool.Count == 0)
        {
            Debug.LogWarning("[StageManager] ItemDatabase를 찾을 수 없고 보상 풀도 비어있습니다. Assets/Resources/ItemDatabase.asset 경로를 확인하세요.");
        }
    }

    private void Start()
    {
        Debug.Log($"[StageManager] Start — stages={(_stages != null ? _stages.Count : 0)}, rewardPool={(_rewardPool != null ? _rewardPool.Count : 0)}");

        if (RewardSystemManager.Instance != null)
            RewardSystemManager.Instance.OnRewardSequenceComplete += OnRewardComplete;

        if (DifficultySelectManager.Instance != null)
            DifficultySelectManager.Instance.OnDifficultySelected += OnDifficultySelected;

        if (_stages != null && _stages.Count > 0)
        {
            if (DifficultySelectManager.Instance != null)
            {
                _waitingForDifficulty = true;
                DifficultySelectManager.Instance.ShowSelection();
            }
            else
            {
                BeginStage(0);
            }
        }
        else
        {
            Debug.LogWarning("[StageManager] No stages configured!");
        }
    }

    private void OnDestroy()
    {
        if (RewardSystemManager.Instance != null)
            RewardSystemManager.Instance.OnRewardSequenceComplete -= OnRewardComplete;

        if (DifficultySelectManager.Instance != null)
            DifficultySelectManager.Instance.OnDifficultySelected -= OnDifficultySelected;
    }

    // --- Public API ---

    public void BeginStage(int stageIndex)
    {
        if (stageIndex >= _stages.Count)
        {
            OnAllStagesComplete?.Invoke();
            return;
        }

        _currentStageIndex = stageIndex;
        _currentWaveIndex = 0;

        OnStageStart?.Invoke(_currentStageIndex);

        StartCoroutine(StageTransitionThenSpawn());
    }

    private void OnDifficultySelected(DifficultyLevel level)
    {
        _waitingForDifficulty = false;
        BeginStage(_currentStageIndex);
    }

    public string GetStageLabel()
    {
        return $"{_currentStageIndex + 1}-{_currentWaveIndex + 1}";
    }

    // --- Wave Spawn ---

    private void SpawnCurrentWave()
    {
        StageData stage = _stages[_currentStageIndex];
        WaveData wave = stage.Waves[_currentWaveIndex];

        int baseCount = wave.EnemyPrefabs.Count;
        float multiplier = DifficultySelectManager.Instance != null
            ? DifficultySelectManager.Instance.EnemyCountMultiplier
            : 1f;
        int totalCount = Mathf.RoundToInt(baseCount * multiplier);
        totalCount = Mathf.Max(1, totalCount);

        Debug.Log($"[StageManager] SpawnCurrentWave — stage={_currentStageIndex}, wave={_currentWaveIndex}, base={baseCount}, multiplier={multiplier}, total={totalCount}");

        _activeEnemies.Clear();

        for (int i = 0; i < totalCount; i++)
        {
            GameObject prefab = wave.EnemyPrefabs[i % baseCount];
            Transform point = wave.SpawnPoints[UnityEngine.Random.Range(0, wave.SpawnPoints.Count)];
            Debug.Log($"[StageManager] Spawning {prefab.name} at {point.position}");
            GameObject spawned = Instantiate(prefab, point.position, Quaternion.identity);

            EnemyBase enemy = spawned.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.OnDeath += () => OnEnemyDied(enemy);
                _activeEnemies.Add(enemy);
            }
        }

        OnWaveStarted?.Invoke(_currentStageIndex, _currentWaveIndex);
    }

    // --- Enemy Death Tracking ---

    private void OnEnemyDied(EnemyBase enemy)
    {
        _activeEnemies.Remove(enemy);

        if (_activeEnemies.Count <= 0 && !_waitingForReward)
        {
            OnWaveCleared?.Invoke(_currentStageIndex, _currentWaveIndex);
            StartCoroutine(WaveClearedSequence());
        }
    }

    // --- Core Loop: Kill → Next Wave / (Last Wave) → Reward → Next Stage ---

    private IEnumerator WaveClearedSequence()
    {
        _waitingForReward = true;

        // Minimal delay — keep dopamine loop tight
        yield return new WaitForSeconds(_delayBeforeReward);

        // 마지막 웨이브가 아니면 보상 없이 즉시 다음 웨이브로
        StageData stage = _stages[_currentStageIndex];
        bool isLastWave = _currentWaveIndex >= stage.Waves.Count - 1;

        if (!isLastWave || RewardSystemManager.Instance == null || _rewardPool == null || _rewardPool.Count < 3)
        {
            _waitingForReward = false;
            AdvanceWave();
            yield break;
        }

        // 스테이지 마지막 웨이브 클리어 → 보상 선택 후 다음 스테이지로
        List<ItemData> choices = PickRandomRewards(3);
        RewardSystemManager.Instance.ShowRewards(choices);

        // Wait handled by OnRewardComplete callback
    }

    private void OnRewardComplete()
    {
        _waitingForReward = false;
        AdvanceWave();
    }

    private void AdvanceWave()
    {
        StageData stage = _stages[_currentStageIndex];
        _currentWaveIndex++;

        if (_currentWaveIndex >= stage.Waves.Count)
        {
            // Stage complete → show difficulty selection before next stage
            OnStageCleared?.Invoke(_currentStageIndex);
            _currentStageIndex++;

            if (_currentStageIndex >= _stages.Count)
            {
                OnAllStagesComplete?.Invoke();
                return;
            }

            if (DifficultySelectManager.Instance != null)
            {
                _waitingForDifficulty = true;
                DifficultySelectManager.Instance.ShowSelection();
            }
            else
            {
                BeginStage(_currentStageIndex);
            }
        }
        else
        {
            // Next wave in same stage
            StartCoroutine(NextWaveSequence());
        }
    }

    private IEnumerator NextWaveSequence()
    {
        yield return new WaitForSeconds(_delayBeforeNextWave);

        // Camera transition then spawn
        yield return StartCoroutine(StageTransitionThenSpawn());
    }

    // --- Camera Stage Transition ---

    private IEnumerator StageTransitionThenSpawn()
    {
        // Zoom out
        yield return StartCoroutine(AnimateCameraZoom(
            _baseCameraSize,
            _baseCameraSize + _zoomOutAmount,
            _zoomOutDuration
        ));

        // Spawn at the peak of zoom-out
        SpawnCurrentWave();

        // Zoom back in
        yield return StartCoroutine(AnimateCameraZoom(
            _baseCameraSize + _zoomOutAmount,
            _baseCameraSize,
            _zoomInDuration
        ));
    }

    private IEnumerator AnimateCameraZoom(float from, float to, float duration)
    {
        if (!_mainCamera.orthographic) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // SmoothStep for natural camera feel
            float eased = t * t * (3f - 2f * t);

            _mainCamera.orthographicSize = Mathf.Lerp(from, to, eased);
            yield return null;
        }

        _mainCamera.orthographicSize = to;
    }

    // --- Reward Pool ---

    private List<ItemData> PickRandomRewards(int count)
    {
        // Get difficulty constraints
        ItemRarity minRarity = ItemRarity.Normal;
        ItemRarity maxRarity = ItemRarity.Legend;
        int minPower = 3;
        int maxPower = 20;

        if (DifficultySelectManager.Instance != null)
        {
            minRarity = DifficultySelectManager.Instance.MinRarity;
            maxRarity = DifficultySelectManager.Instance.MaxRarity;
            minPower = DifficultySelectManager.Instance.MinPowerScore;
            maxPower = DifficultySelectManager.Instance.MaxPowerScore;
        }

        // Filter by rarity range
        List<ItemData> filtered = _rewardPool.FindAll(item =>
            item.Rarity >= minRarity && item.Rarity <= maxRarity);

        if (filtered.Count < count)
            filtered = new List<ItemData>(_rewardPool);

        // Try to find a combination within PowerScore range (max 50 attempts)
        for (int attempt = 0; attempt < 50; attempt++)
        {
            List<ItemData> pool = new List<ItemData>(filtered);
            List<ItemData> picked = new List<ItemData>();

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }

            if (picked.Count < count) break;

            int totalPower = 0;
            foreach (var item in picked)
                totalPower += item.PowerScore;

            if (totalPower >= minPower && totalPower <= maxPower)
                return picked;
        }

        // Fallback: greedy pick within budget
        return PickRewardsGreedy(filtered, count, minPower, maxPower);
    }

    private List<ItemData> PickRewardsGreedy(List<ItemData> candidates, int count, int minPower, int maxPower)
    {
        // Shuffle candidates
        List<ItemData> shuffled = new List<ItemData>(candidates);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        List<ItemData> picked = new List<ItemData>();
        int currentPower = 0;
        int remaining = count;

        foreach (var item in shuffled)
        {
            if (picked.Count >= count) break;

            int afterAdd = currentPower + item.PowerScore;
            int slotsLeft = remaining - 1;

            // Check if adding this item still allows reaching minPower with remaining slots
            // and doesn't overshoot maxPower even with minimum-score items in remaining slots
            if (slotsLeft > 0)
            {
                if (afterAdd > maxPower) continue;
            }
            else
            {
                // Last slot: total must be within range
                if (afterAdd < minPower || afterAdd > maxPower) continue;
            }

            picked.Add(item);
            currentPower = afterAdd;
            remaining--;
        }

        // If greedy couldn't fill all slots, fill remaining with random items
        if (picked.Count < count)
        {
            List<ItemData> pool = new List<ItemData>(candidates);
            foreach (var p in picked) pool.Remove(p);

            while (picked.Count < count && pool.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                picked.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
        }

        return picked;
    }
}
