using System;
using UnityEngine;

/// <summary>
/// [Action] 지정 위치에 광역 폭발을 발생시킨다.
///
/// 폭발 중심: context.TargetPosition (0이면 플레이어 위치 사용)
/// 데미지:    플레이어 AttackDamage × context.DamageMultiplier × _damageMultiplier
/// 범위:      _radius (Physics.OverlapSphere)
/// 넉백:      _causeKnockback = true 이면 폭발 중심에서 바깥 방향으로 TakeDamage 적용
///
/// 사용 예:
/// - 7 Seals (601): OnMeleeHit → SpawnExplosionAction (아군 폼 처치 시 폭발)
/// - 2Spooky   :   OnKill     → SpawnExplosionAction (처치 시 폭발)
/// - Athame     :   OnMeleeHit → SpawnExplosionAction (크리티컬 폭발)
/// - 302 AA건전지: OnDash + AddTag(Fire) → SpawnExplosionAction (대시 폭발)
/// </summary>
[Serializable]
public class SpawnExplosionAction : ActionBase
{
    [Tooltip("폭발 반경 (미니어처 스케일 기준, 월드 단위)")]
    [SerializeField] private float _radius = 2f;

    [Tooltip("폭발 위력 계수. 최종 데미지 = 플레이어 ATK × context.DamageMultiplier × 이 값")]
    [SerializeField] private float _damageMultiplier = 1f;

    [Tooltip("true면 폭발 중심에서 바깥 방향으로 넉백 포함 데미지")]
    [SerializeField] private bool _causeKnockback = true;

    public float Radius            { get => _radius;           set => _radius = value; }
    public float DamageMultiplier  { get => _damageMultiplier; set => _damageMultiplier = value; }
    public bool  CauseKnockback    { get => _causeKnockback;   set => _causeKnockback = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        // ── 폭발 중심 결정 ─────────────────────────────────────────
        Vector3 center = context.TargetPosition != Vector3.zero
            ? context.TargetPosition
            : (context.TargetEnemy != null
                ? context.TargetEnemy.transform.position
                : (context.Player != null ? context.Player.transform.position : Vector3.zero));

        // ── 데미지 계산 ────────────────────────────────────────────
        float baseDamage = context.Player != null ? context.Player.AttackDamage : 1f;
        float finalDamage = baseDamage * context.DamageMultiplier * _damageMultiplier;

        if (finalDamage <= 0f) return;

        // ── VFX (먼저 재생) ────────────────────────────────────────
        ItemEffectVFX.EnsureInstance().PlayExplosionEffect(center, _radius);

        // ── 광역 피해 ──────────────────────────────────────────────
        Collider[] hits = Physics.OverlapSphere(center, _radius);
        foreach (var col in hits)
        {
            var enemy = col.GetComponent<EnemyBase>();
            if (enemy == null || enemy.IsDead) continue;

            Vector3 dir = _causeKnockback
                ? (enemy.transform.position - center).normalized
                : Vector3.zero;

            enemy.TakeDamage(finalDamage, dir);
        }

        Debug.Log($"[TMA] SpawnExplosionAction: center={center} radius={_radius} dmg={finalDamage} hits={hits.Length}");
    }
}
