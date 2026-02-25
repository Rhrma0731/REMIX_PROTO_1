using System;
using UnityEngine;

/// <summary>
/// [Trigger] 플레이어가 대시할 때 발동.
/// PlayerEventManager.OnDash 이벤트를 구독한다.
///
/// [참고] 현재 PlayerMovement에 대시 기능이 미구현 상태.
/// 대시 구현 후 PlayerEventManager.BroadcastDash()를 호출하면 이 트리거가 작동한다.
///
/// 사용 예: 낡은 선풍기 모터(대시 시 소용돌이), 고장난 토스터(대시 시 화염)
/// </summary>
[Serializable]
public class OnDashTrigger : TriggerBase
{
    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnDash += HandleDash;
    }

    private void HandleDash()
    {
        var ctx = _cachedContext.Clone();
        ctx.Damage = _cachedContext.Player != null ? _cachedContext.Player.AttackDamage : 0f;
        FireTrigger(ctx);
    }

    public override void Cleanup()
    {
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnDash -= HandleDash;
        base.Cleanup();
    }
}
