using System;
using UnityEngine;

/// <summary>
/// [Action] 받은 피해를 무효화한다.
///
/// [구현 방식] 피격 트리거(OnTakeDamageTrigger)에서 전달받은 데미지만큼
/// 즉시 회복하여 피해를 상쇄하는 근사치 처리.
///
/// TODO: PlayerStats에 DamageNullify 플래그를 추가하면
///       TakeDamage 호출 전에 차단하는 완전한 구현이 가능.
///
/// 사용 예:
/// - 뽁뽁이 갑옷: 10초 쿨다임으로 피해 1회 무효화
/// - 양은 냄비: 30% 확률로 투사체 피해 무효화
/// </summary>
[Serializable]
public class NullifyDamageAction : ActionBase
{
    protected override void OnExecute(ItemEffectContext context)
    {
        // 피격 트리거에서 전달받은 데미지만큼 즉시 회복하여 무효화 근사
        if (context.Player != null && context.Damage > 0f)
        {
            context.Player.Heal(context.Damage);
        }
    }
}
