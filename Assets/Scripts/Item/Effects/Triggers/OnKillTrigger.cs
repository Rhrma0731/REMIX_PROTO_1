using System;
using UnityEngine;

/// <summary>
/// [Trigger] 적을 처치했을 때 발동.
/// PlayerEventManager.OnKillEnemy 이벤트를 구독한다.
///
/// 사용 예: 삐에로 코(처치 시 10% 확률 체력 드롭)
/// </summary>
[Serializable]
public class OnKillTrigger : TriggerBase
{
    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnKillEnemy += HandleKill;
    }

    private void HandleKill(EnemyBase enemy)
    {
        var ctx = _cachedContext.Clone();
        ctx.TargetEnemy = enemy;
        ctx.TargetPosition = enemy != null ? enemy.transform.position : Vector3.zero;
        FireTrigger(ctx);
    }

    public override void Cleanup()
    {
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnKillEnemy -= HandleKill;
        base.Cleanup();
    }
}
