using System;
using UnityEngine;

/// <summary>
/// [Trigger] 플레이어가 피해를 받았을 때 발동.
/// PlayerEventManager.OnTakeDamage 이벤트를 구독한다.
///
/// 사용 예: 망가진 핀볼 범퍼(피격 시 반사 데미지), 뽁뽁이 갑옷(피격 시 무효화)
/// [시너지] 누전된 헤드폰(자해 3초마다) + 이 트리거를 쓰는 Body 아이템 = 자동 발동 시너지
/// </summary>
[Serializable]
public class OnTakeDamageTrigger : TriggerBase
{
    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnTakeDamage += HandleTakeDamage;
    }

    private void HandleTakeDamage(float damage)
    {
        var ctx = _cachedContext.Clone();
        ctx.Damage = damage;
        FireTrigger(ctx);
    }

    public override void Cleanup()
    {
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnTakeDamage -= HandleTakeDamage;
        base.Cleanup();
    }
}
