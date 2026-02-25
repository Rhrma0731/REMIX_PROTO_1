using System;
using UnityEngine;

/// <summary>
/// [Trigger] 근접 공격이 적에게 적중했을 때 발동.
/// PlayerEventManager.OnDealDamage 이벤트를 구독한다.
///
/// OnAttackTrigger와의 차이: OnAttack은 공격 "시도" 시 발동, OnMeleeHit은 실제 "적중" 시 발동.
/// 사용 예: 낡은 프라이팬(적중 시 기절), 정전기 스웨터(적중 시 연쇄 번개)
/// </summary>
[Serializable]
public class OnMeleeHitTrigger : TriggerBase
{
    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnDealDamage += HandleHit;
    }

    private void HandleHit(EnemyBase enemy, float damage)
    {
        var ctx = _cachedContext.Clone();
        ctx.TargetEnemy = enemy;
        ctx.Damage = damage;

        if (enemy != null)
        {
            ctx.TargetPosition = enemy.transform.position;
            ctx.HitDirection = _cachedContext.Player != null
                ? (enemy.transform.position - _cachedContext.Player.transform.position).normalized
                : Vector3.forward;
        }

        FireTrigger(ctx);
    }

    public override void Cleanup()
    {
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnDealDamage -= HandleHit;
        base.Cleanup();
    }
}
