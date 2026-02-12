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

    // --- Lifecycle ---

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        _baseCameraSize = _mainCamera.orthographicSize;
    }

    private void Start()
    {
        RewardSystemManager.Instance.OnRewardSequenceComplete += OnRewardComplete;
        BeginStage(0);
    }

    private void OnDestroy()
    {
        if (RewardSystemManager.Instance != null)
            RewardSystemManager.Instance.OnRewardSequenceComplete -= OnRewardComplete;
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

    public string GetStageLabel()
    {
        return $"{_currentStageIndex + 1}-{_currentWaveIndex + 1}";
    }

    // --- Wave Spawn ---

    private void SpawnCurrentWave()
    {
        StageData stage = _stages[_currentStageIndex];
        WaveData wave = stage.Waves[_currentWaveIndex];

        _activeEnemies.Clear();

        foreach (GameObject prefab in wave.EnemyPrefabs)
        {
            Transform point = wave.SpawnPoints[UnityEngine.Random.Range(0, wave.SpawnPoints.Count)];
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

    // --- Core Loop: Kill → Reward → Next Wave ---

    private IEnumerator WaveClearedSequence()
    {
        _waitingForReward = true;

        // Minimal delay — keep dopamine loop tight
        yield return new WaitForSeconds(_delayBeforeReward);

        // Pick 3 random rewards
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
            // Stage complete → next stage
            OnStageCleared?.Invoke(_currentStageIndex);
            BeginStage(_currentStageIndex + 1);
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
        List<ItemData> pool = new List<ItemData>(_rewardPool);
        List<ItemData> picked = new List<ItemData>();

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            picked.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return picked;
    }
}
