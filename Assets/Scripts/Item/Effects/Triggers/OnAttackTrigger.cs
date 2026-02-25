using System;
using UnityEngine;

/// <summary>
/// [Trigger] 플레이어가 공격을 시도할 때 발동.
/// PlayerEventManager.OnAttack 이벤트를 구독한다.
///
/// 사용 예: 대형 파리채(공격 시 넓은 범위 휩쓸기), 고무줄 요요(공격 시 투사체 발사)
/// </summary>
[Serializable]
public class OnAttackTrigger : TriggerBase
{
    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnAttack += HandleAttack;
    }

    private void HandleAttack()
    {
        var ctx = _cachedContext.Clone();
        ctx.Damage = _cachedContext.Player != null ? _cachedContext.Player.AttackDamage : 0f;
        FireTrigger(ctx);
    }

    public override void Cleanup()
    {
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnAttack -= HandleAttack;
        base.Cleanup();
    }
}
