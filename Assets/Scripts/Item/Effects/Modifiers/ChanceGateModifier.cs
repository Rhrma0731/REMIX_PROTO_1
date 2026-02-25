using System;
using UnityEngine;

/// <summary>
/// [Modifier] 확률 게이트.
/// 지정된 확률로 통과하지 못하면 DamageMultiplier와 StatusChance를 0으로 만들어
/// 후속 Action이 의미 있는 효과를 내지 못하게 차단한다.
///
/// 사용 예:
/// - 낡은 프라이팬: Chance=0.25 (25% 확률 기절)
/// - 정전기 스웨터: Chance=0.30 (30% 확률 연쇄 번개)
/// - 삐에로 코: Chance=0.10 (10% 확률 체력 드롭)
/// </summary>
[Serializable]
public class ChanceGateModifier : ModifierBase
{
    [Tooltip("통과 확률 (0~1). 0.3 = 30% 확률로 발동")]
    [Range(0f, 1f)]
    [SerializeField] private float _chance = 1f;

    public float Chance { get => _chance; set => _chance = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        if (UnityEngine.Random.value > _chance)
        {
            // 확률 실패 → 파이프라인 무력화
            context.DamageMultiplier = 0f;
            context.StatusChance = 0f;
        }
    }
}
