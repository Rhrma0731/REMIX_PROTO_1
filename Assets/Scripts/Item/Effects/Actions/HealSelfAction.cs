using System;
using UnityEngine;

/// <summary>
/// [Action] 플레이어의 체력을 회복한다.
///
/// _percentOfMax가 true면 _healAmount를 최대 체력의 백분율(%)로 사용한다.
/// DamageMultiplier가 0이면 (ChanceGate 실패) 아무것도 하지 않는다.
///
/// 사용 예:
/// - 삐에로 코: healAmount=10 (처치 시 10% 확률로 고정 회복)
/// </summary>
[Serializable]
public class HealSelfAction : ActionBase
{
    [Tooltip("회복량. PercentOfMax가 true면 최대 체력의 백분율(%)")]
    [SerializeField] private float _healAmount = 5f;

    [Tooltip("true면 HealAmount를 최대 체력의 백분율(%)로 사용")]
    [SerializeField] private bool _percentOfMax;

    public float HealAmount { get => _healAmount; set => _healAmount = value; }
    public bool PercentOfMax { get => _percentOfMax; set => _percentOfMax = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        if (context.Player == null) return;
        // ChanceGate 실패 시 DamageMultiplier == 0 → 회복도 차단
        if (context.DamageMultiplier <= 0f) return;

        float amount = _percentOfMax
            ? context.Player.MaxHp * (_healAmount / 100f)
            : _healAmount;

        context.Player.Heal(amount);
    }
}
