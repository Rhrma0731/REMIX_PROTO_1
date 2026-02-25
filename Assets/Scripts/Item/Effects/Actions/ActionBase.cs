using System;

/// <summary>
/// 모든 Action 효과의 추상 베이스.
///
/// Action은 "무엇을 실행할 것인가?"를 정의한다.
/// T-M-A 파이프라인의 최종 단계로, ItemEffectContext에 담긴 데이터를 소비하여
/// 실제 게임플레이 효과를 실행한다.
///
/// 예시:
/// - DealDamageAction: 적/자신에게 피해 (AoE 지원)
/// - ApplyStatusAction: 기존 상태이상 시스템(StatusEffectManager) 브릿지
/// - HealSelfAction: 플레이어 체력 회복
/// - NullifyDamageAction: 피해 무효화 (근사 처리)
/// </summary>
[Serializable]
public abstract class ActionBase : ItemEffectBase
{
    public override ItemEffectRole Role => ItemEffectRole.Action;
}
