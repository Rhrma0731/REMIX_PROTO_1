using System;
using UnityEngine;

/// <summary>
/// [Action] 플레이어 주변에 Familiar(패밀리어/펫) 프리팹을 소환하는 액션.
///
/// 소환된 프리팹은 자체 MonoBehaviour 컴포넌트로 다음을 처리한다:
/// - 플레이어 주변 공전 또는 추종 이동
/// - 접촉 데미지 / 투사체 발사 등
///
/// [소환 방식]
/// - _familiarPrefab: 직접 프리팹 레퍼런스 (Inspector에서 드래그)
/// - _familiarResourcePath: Resources 폴더 경로 (프리팹 레퍼런스가 null일 때 폴백)
///
/// 중복 소환 방지: _maxInstances로 최대 동시 소환 수를 제한한다.
///
/// 사용 예:
/// - 분실된 병정 (Brother Bobby): 플레이어 뒤를 따라다니며 투사체 발사
/// - 본드 슬라임 (Meat Boy): 플레이어 주변을 공전하며 접촉 데미지
/// </summary>
[Serializable]
public class SpawnFamiliarAction : ActionBase
{
    [Header("프리팹")]
    [Tooltip("소환할 Familiar 프리팹. null이면 ResourcePath에서 로드 시도")]
    [SerializeField] private GameObject _familiarPrefab;

    [Tooltip("Resources 폴더 경로 (프리팹이 null일 때 폴백). 예: Familiars/BondSlime")]
    [SerializeField] private string _familiarResourcePath;

    [Header("소환 설정")]
    [Tooltip("플레이어로부터의 초기 소환 거리")]
    [SerializeField] private float _spawnDistance = 0.5f;

    [Tooltip("최대 동시 소환 수. 0이면 무제한")]
    [SerializeField] private int _maxInstances = 1;

    public GameObject FamiliarPrefab { get => _familiarPrefab; set => _familiarPrefab = value; }
    public string FamiliarResourcePath { get => _familiarResourcePath; set => _familiarResourcePath = value; }
    public float SpawnDistance { get => _spawnDistance; set => _spawnDistance = value; }
    public int MaxInstances { get => _maxInstances; set => _maxInstances = value; }

    [NonSerialized] private int _currentInstances;

    protected override void OnExecute(ItemEffectContext context)
    {
        if (context.Player == null) return;

        // 최대 소환 수 체크
        if (_maxInstances > 0 && _currentInstances >= _maxInstances) return;

        // 프리팹 결정
        GameObject prefab = _familiarPrefab;
        if (prefab == null && !string.IsNullOrEmpty(_familiarResourcePath))
            prefab = Resources.Load<GameObject>(_familiarResourcePath);

        if (prefab == null)
        {
            Debug.LogWarning("[SpawnFamiliarAction] 프리팹이 null — familiarPrefab 또는 familiarResourcePath를 설정하세요.");
            return;
        }

        // 플레이어 주변 랜덤 위치에 소환
        Vector3 playerPos = context.Player.transform.position;
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spawnDistance;
        Vector3 spawnPos = playerPos + offset;

        GameObject instance = UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        _currentInstances++;

        // Familiar이 파괴될 때 카운트 감소
        var tracker = instance.AddComponent<FamiliarDestroyTracker>();
        tracker.Initialize(this);

        ItemEffectVFX.Instance?.PlayHealEffect(instance.transform);
    }

    /// <summary>Familiar이 파괴될 때 호출 — 소환 카운트 감소</summary>
    internal void OnFamiliarDestroyed()
    {
        _currentInstances = Mathf.Max(0, _currentInstances - 1);
    }
}

/// <summary>
/// Familiar 오브젝트에 자동 부착되어 파괴 시 SpawnFamiliarAction에 통보하는 헬퍼.
/// </summary>
public class FamiliarDestroyTracker : MonoBehaviour
{
    private SpawnFamiliarAction _owner;

    public void Initialize(SpawnFamiliarAction owner)
    {
        _owner = owner;
    }

    private void OnDestroy()
    {
        _owner?.OnFamiliarDestroyed();
    }
}
