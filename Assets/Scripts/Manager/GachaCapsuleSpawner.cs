using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 시작 시 캡슐 낙하 연출 → 파티클 → 적/장애물 산개 스폰을 전담.
/// 씬의 GameManagers 오브젝트(또는 자식)에 배치하고 StageManager._capsuleSpawner에 연결한다.
/// </summary>
public class GachaCapsuleSpawner : MonoBehaviour
{
    [Header("Capsule")]
    [Tooltip("낙하할 캡슐 프리팹 (없으면 낙하 연출 생략)")]
    [SerializeField] private GameObject _capsulePrefab;
    [Tooltip("캡슐 낙하 시작 높이 (착지 지점 기준 +Y)")]
    [SerializeField] private float _dropStartY = 8f;
    [Tooltip("착지까지 소요 시간 (초)")]
    [SerializeField] private float _dropDuration = 0.7f;
    [Tooltip("착지 후 파티클 재생까지 딜레이")]
    [SerializeField] private float _openDelay = 0.1f;

    [Header("Open Effect")]
    [Tooltip("착지 시 재생할 파티클 프리팹 (없으면 생략)")]
    [SerializeField] private ParticleSystem _openParticlePrefab;

    [Header("Scatter")]
    [Tooltip("적 산개 힘 (LaunchFromCapsule launchForce)")]
    [SerializeField] private float _enemyLaunchForce = 4f;
    [Tooltip("장애물 초기 스폰 오프셋 최대 반경")]
    [SerializeField] private float _obstacleScatterRadius = 2f;
    [Tooltip("장애물 산개 힘 (ObstacleController.Launch force)")]
    [SerializeField] private float _obstacleLaunchForce = 2.5f;
    [Tooltip("산개 후 착지 완료 추정 대기 시간")]
    [SerializeField] private float _settleWait = 1.2f;

    private readonly List<ObstacleController> _activeObstacles = new List<ObstacleController>();

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    /// <summary>이전 웨이브 장애물을 모두 제거한다. StageManager가 웨이브 시작 전 호출.</summary>
    public void ClearObstacles()
    {
        foreach (var obs in _activeObstacles)
        {
            if (obs != null)
                Destroy(obs.gameObject);
        }
        _activeObstacles.Clear();
    }

    /// <summary>
    /// 캡슐 낙하 연출 후 적과 장애물을 산개 스폰한다.
    /// 완료되면 onComplete(spawnedEnemies) 를 호출한다.
    /// </summary>
    public void SpawnWave(
        WaveData wave,
        int totalEnemyCount,
        int obstacleCount,
        List<GameObject> obstaclePrefabs,
        Action<List<EnemyBase>> onComplete)
    {
        StartCoroutine(SpawnWaveRoutine(wave, totalEnemyCount, obstacleCount, obstaclePrefabs, onComplete));
    }

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private IEnumerator SpawnWaveRoutine(
        WaveData wave,
        int totalEnemyCount,
        int obstacleCount,
        List<GameObject> obstaclePrefabs,
        Action<List<EnemyBase>> onComplete)
    {
        // 웨이브 스폰 포인트 중 랜덤 1개를 캡슐 착지 지점으로 사용
        Transform spawnPoint = wave.SpawnPoints[UnityEngine.Random.Range(0, wave.SpawnPoints.Count)];
        Vector3 landPos = spawnPoint.position;
        Vector3 dropStart = landPos + Vector3.up * _dropStartY;

        // ── 캡슐 낙하 ──────────────────────────────────────
        GameObject capsule = null;
        if (_capsulePrefab != null)
            capsule = Instantiate(_capsulePrefab, dropStart, Quaternion.identity);

        float elapsed = 0f;
        while (elapsed < _dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _dropDuration);
            float eased = t * t * (3f - 2f * t); // SmoothStep

            if (capsule != null)
            {
                Vector3 pos = capsule.transform.position;
                pos.y = Mathf.Lerp(dropStart.y, landPos.y, eased);
                capsule.transform.position = pos;
            }

            yield return null;
        }

        // ── 착지 ───────────────────────────────────────────
        if (capsule != null)
            Destroy(capsule);

        // ── 오픈 파티클 ────────────────────────────────────
        if (_openParticlePrefab != null)
        {
            ParticleSystem fx = Instantiate(_openParticlePrefab, landPos, Quaternion.identity);
            float fxLifetime = fx.main.duration + fx.main.startLifetime.constantMax;
            Destroy(fx.gameObject, fxLifetime);
        }

        yield return new WaitForSeconds(_openDelay);

        // ── 적 스폰 + 산개 ─────────────────────────────────
        // Start()가 반드시 실행된 후 LaunchFromCapsule이 호출되도록 1프레임 대기
        List<EnemyBase> spawnedEnemies = new List<EnemyBase>();
        int baseCount = wave.EnemyPrefabs.Count;

        for (int i = 0; i < totalEnemyCount; i++)
        {
            GameObject prefab = wave.EnemyPrefabs[i % baseCount];
            GameObject spawned = Instantiate(prefab, landPos, Quaternion.identity);
            EnemyBase enemy = spawned.GetComponent<EnemyBase>();
            if (enemy != null)
                spawnedEnemies.Add(enemy);
        }

        // Start()가 모든 적에게 실행될 때까지 1프레임 대기
        yield return null;

        foreach (var enemy in spawnedEnemies)
        {
            Vector3 dir = GetRandomRadialDirection();
            enemy.LaunchFromCapsule(dir, _enemyLaunchForce);
        }

        // ── 장애물 스폰 + 산개 ─────────────────────────────
        if (obstaclePrefabs != null && obstaclePrefabs.Count > 0 && obstacleCount > 0)
        {
            for (int i = 0; i < obstacleCount; i++)
            {
                GameObject obsPrefab = obstaclePrefabs[i % obstaclePrefabs.Count];

                // 착지점 주변 랜덤 위치에 스폰
                Vector3 radialOffset = GetRandomRadialDirection() * UnityEngine.Random.Range(0f, _obstacleScatterRadius);
                Vector3 obsPos = landPos + new Vector3(radialOffset.x, 0f, radialOffset.z);

                GameObject obsObj = Instantiate(obsPrefab, obsPos, Quaternion.identity);
                ObstacleController obs = obsObj.GetComponent<ObstacleController>();
                if (obs != null)
                {
                    obs.Launch(GetRandomRadialDirection(), _obstacleLaunchForce);
                    _activeObstacles.Add(obs);
                }
            }
        }

        // ── 산개 착지 대기 ─────────────────────────────────
        yield return new WaitForSeconds(_settleWait);

        onComplete?.Invoke(spawnedEnemies);
    }

    private static Vector3 GetRandomRadialDirection()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }
}
