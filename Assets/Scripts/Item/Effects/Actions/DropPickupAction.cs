using System;
using UnityEngine;

/// <summary>
/// [Action] 플레이어 주변에 픽업 아이템(코인, 하트 등) 프리팹을 드롭하는 액션.
///
/// 지정된 프리팹을 플레이어 주변 무작위 위치에 N개 생성한다.
/// _scatterRadius로 드롭 범위를 조절하고, Y축 오프셋으로 자연스러운 낙하를 연출할 수 있다.
///
/// [프리팹 참조]
/// - _pickupPrefab: Inspector에서 직접 연결
/// - _pickupResourcePath: Resources 폴더 경로 (프리팹이 null일 때 폴백)
///
/// 사용 예:
/// - 구멍난 주머니: OnRoomClearTrigger + ChanceGate(0.3) + DropPickupAction(코인 ×3)
/// - 삐에로 코: OnKillTrigger + ChanceGate(0.1) + DropPickupAction(하트 ×1)
/// </summary>
[Serializable]
public class DropPickupAction : ActionBase
{
    [Header("프리팹")]
    [Tooltip("드롭할 픽업 프리팹. null이면 ResourcePath에서 로드 시도")]
    [SerializeField] private GameObject _pickupPrefab;

    [Tooltip("Resources 폴더 경로 (프리팹이 null일 때 폴백). 예: Pickups/CoinPickup")]
    [SerializeField] private string _pickupResourcePath;

    [Header("드롭 설정")]
    [Tooltip("한 번에 드롭하는 개수")]
    [SerializeField] private int _dropCount = 1;

    [Tooltip("플레이어 중심으로부터의 드롭 산포 반경")]
    [SerializeField] private float _scatterRadius = 0.8f;

    [Tooltip("드롭 시 Y축 시작 오프셋 (위에서 떨어지는 연출)")]
    [SerializeField] private float _dropHeightOffset = 0.5f;

    public GameObject PickupPrefab { get => _pickupPrefab; set => _pickupPrefab = value; }
    public string PickupResourcePath { get => _pickupResourcePath; set => _pickupResourcePath = value; }
    public int DropCount { get => _dropCount; set => _dropCount = value; }
    public float ScatterRadius { get => _scatterRadius; set => _scatterRadius = value; }
    public float DropHeightOffset { get => _dropHeightOffset; set => _dropHeightOffset = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        if (context.Player == null) return;
        // ChanceGate 실패 시 DamageMultiplier == 0 → 드롭도 차단
        if (context.DamageMultiplier <= 0f) return;

        // 프리팹 결정
        GameObject prefab = _pickupPrefab;
        if (prefab == null && !string.IsNullOrEmpty(_pickupResourcePath))
            prefab = Resources.Load<GameObject>(_pickupResourcePath);

        if (prefab == null)
        {
            Debug.LogWarning("[DropPickupAction] 프리팹이 null — pickupPrefab 또는 pickupResourcePath를 설정하세요.");
            return;
        }

        Vector3 center = context.TargetPosition != Vector3.zero
            ? context.TargetPosition
            : context.Player.transform.position;

        for (int i = 0; i < _dropCount; i++)
        {
            // XZ 평면에 무작위 분산
            Vector2 random2D = UnityEngine.Random.insideUnitCircle * _scatterRadius;
            Vector3 spawnPos = center + new Vector3(random2D.x, _dropHeightOffset, random2D.y);

            UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
}
