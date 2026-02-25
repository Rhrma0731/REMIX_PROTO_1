using System;
using UnityEngine;

/// <summary>
/// [Action] 대상에게 피해를 입힌다.
///
/// 동작 모드 (자동 판별):
/// 1. _targetSelf=true → 플레이어 자신에게 피해 (자해)
/// 2. AreaRadius > 0 → OverlapSphere로 광역 피해
/// 3. TargetEnemy != null → 단일 대상 피해
///
/// DamageMultiplier가 0이면 (ChanceGate 실패) 아무것도 하지 않는다.
///
/// 사용 예:
/// - 누전된 헤드폰: targetSelf=true, baseDamage=1 (자해)
/// - 고압축 피스톤: AreaRadius로 폭발 (AoE)
/// - 뚫어뻥: 단일 대상 추가 피해
/// </summary>
[Serializable]
public class DealDamageAction : ActionBase
{
    [Tooltip("기본 피해량. UsePlayerDamage가 true면 플레이어 공격력 기준으로 계산")]
    [SerializeField] private float _baseDamage = 10f;

    [Tooltip("true면 플레이어 AttackDamage × DamageMultiplier 사용")]
    [SerializeField] private bool _usePlayerDamage = true;

    [Tooltip("true면 대상이 플레이어 자신 (자해 데미지)")]
    [SerializeField] private bool _targetSelf;

    public float BaseDamage { get => _baseDamage; set => _baseDamage = value; }
    public bool UsePlayerDamage { get => _usePlayerDamage; set => _usePlayerDamage = value; }
    public bool TargetSelf { get => _targetSelf; set => _targetSelf = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        float damage = _usePlayerDamage && context.Player != null
            ? context.Player.AttackDamage * context.DamageMultiplier
            : _baseDamage * context.DamageMultiplier;

        // ChanceGate가 차단한 경우 (DamageMultiplier == 0)
        if (damage <= 0f) return;

        // ── 자해 데미지 ──
        if (_targetSelf)
        {
            context.Player?.TakeDamage(damage);
            return;
        }

        // ── 광역 피해 (AreaRadius > 0) ──
        if (context.AreaRadius > 0f)
        {
            Vector3 center = context.TargetPosition != Vector3.zero
                ? context.TargetPosition
                : (context.Player != null ? context.Player.transform.position : Vector3.zero);

            Collider[] hits = Physics.OverlapSphere(center, context.AreaRadius);
            foreach (var col in hits)
            {
                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;
                Vector3 dir = (enemy.transform.position - center).normalized;
                enemy.TakeDamage(damage, dir);
            }
            return;
        }

        // ── 단일 대상 피해 ──
        if (context.TargetEnemy != null && !context.TargetEnemy.IsDead)
        {
            context.TargetEnemy.TakeDamage(damage, context.HitDirection);
        }
    }
}
