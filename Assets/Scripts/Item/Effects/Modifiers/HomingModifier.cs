using System;
using UnityEngine;

/// <summary>
/// [Modifier] 파이프라인 컨텍스트에 유도(호밍) 속성을 부여한다.
///
/// 이 모디파이어가 설정한 IsHoming/HomingStrength 값을 후속 Action이 읽어
/// 발생한 투사체나 이펙트가 가장 가까운 적을 향해 방향을 틀도록 처리한다.
///
/// [HomingStrength 가이드]
/// - 90: 느린 유도 (큰 원호를 그리며 접근)
/// - 180: 보통 유도 (부드러운 추적)
/// - 360+: 강한 유도 (거의 직진 추적)
///
/// 사용 예:
/// - 자석 팔 (304 미사일 발사기): strength=180 (보통 유도 미사일)
/// - 본드 더미 (저속 유도): strength=60 (느릿하게 따라감)
/// - 레이저 유도: strength=360 (즉시 방향 전환)
/// </summary>
[Serializable]
public class HomingModifier : ModifierBase
{
    [Tooltip("유도 강도 (도/초). 값이 클수록 빠르게 방향 전환")]
    [SerializeField] private float _homingStrength = 180f;

    [Tooltip("유도 탐지 반경. 이 범위 안에 적이 없으면 직진")]
    [SerializeField] private float _detectionRadius = 5f;

    public float HomingStrength { get => _homingStrength; set => _homingStrength = value; }
    public float DetectionRadius { get => _detectionRadius; set => _detectionRadius = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        context.IsHoming = true;
        context.HomingStrength = _homingStrength;

        // 유도 대상 자동 설정: TargetEnemy가 없으면 가장 가까운 적을 찾아 설정
        if (context.TargetEnemy == null && context.Player != null)
        {
            context.TargetEnemy = FindNearestEnemy(context.Player.transform.position);
        }
    }

    private EnemyBase FindNearestEnemy(Vector3 origin)
    {
        Collider[] hits = Physics.OverlapSphere(origin, _detectionRadius);
        EnemyBase nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var col in hits)
        {
            var enemy = col.GetComponent<EnemyBase>();
            if (enemy == null || enemy.IsDead) continue;

            float dist = (enemy.transform.position - origin).sqrMagnitude;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
