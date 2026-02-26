using System;
using UnityEngine;

/// <summary>
/// [Action] 플레이어를 부활시킨다.
///
/// OnFatalDamageTrigger와 함께 사용하여 Dead Cat / 1UP 기믹을 구현한다.
/// PlayerStats.Revive()를 호출하여 _isDead를 해제하고 체력을 회복한 뒤,
/// 지정된 시간 동안 무적을 부여한다.
///
/// [동작 순서]
/// 1. PlayerStats.Revive(회복량) → _isDead 해제 + 체력 설정
/// 2. PlayerStats.SetInvincible(무적 시간) → 부활 직후 보호
/// 3. VFX 재생 (금색 파티클)
///
/// 사용 예:
/// - 고장난 태엽심장: healPercent=50, invincibleDuration=2.0
/// - 비상 배터리: healPercent=100, invincibleDuration=3.0
/// </summary>
[Serializable]
public class ReviveAction : ActionBase
{
    [Header("회복")]
    [Tooltip("최대 체력의 몇 %로 부활할 것인가 (50 = 반피)")]
    [Range(1f, 100f)]
    [SerializeField] private float _healPercent = 50f;

    [Header("무적")]
    [Tooltip("부활 후 무적 시간(초)")]
    [SerializeField] private float _invincibleDuration = 2f;

    public float HealPercent { get => _healPercent; set => _healPercent = value; }
    public float InvincibleDuration { get => _invincibleDuration; set => _invincibleDuration = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        if (context.Player == null) return;

        float healAmount = context.Player.MaxHp * (_healPercent / 100f);
        context.Player.Revive(healAmount);
        context.Player.SetInvincible(_invincibleDuration);

        ItemEffectVFX.Instance?.PlayReviveEffect(context.Player.transform);
    }
}
