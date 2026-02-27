using System;
using UnityEngine;

/// <summary>
/// [Action] 피의 서약 — 체력을 1로 깎고 소모량 비율에 비례한 스탯을 영구 증가시킨다.
///
/// OnRoomClearTrigger와 함께 사용.
///   ratio = (현재HP - 1) / 최대HP
///   AttackDamage += ratio × AttackDamageScale
///   MoveSpeed    += ratio × MoveSpeedScale
///
/// 현재 HP가 이미 1 이하이면 아무 효과도 없다.
/// 스탯 증가는 PlayerStats.AddDynamicBonus()를 통해 영구 적용된다.
/// </summary>
[Serializable]
public class BloodOathAction : ActionBase
{
    [Header("스탯 증가 배율")]
    [Tooltip("(소모 체력 / 최대 체력) × 이 값 만큼 공격력 영구 증가")]
    [SerializeField] private float _attackDamageScale = 5f;

    [Tooltip("(소모 체력 / 최대 체력) × 이 값 만큼 이동속도 영구 증가")]
    [SerializeField] private float _moveSpeedScale = 1f;

    public float AttackDamageScale { get => _attackDamageScale; set => _attackDamageScale = value; }
    public float MoveSpeedScale    { get => _moveSpeedScale;    set => _moveSpeedScale    = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        PlayerStats player = context.Player;
        if (player == null) return;

        float currentHp = player.CurrentHp;
        float maxHp     = player.MaxHp;

        // 체력이 이미 1 이하면 효과 없음
        if (currentHp <= 1f) return;

        float ratio = (currentHp - 1f) / maxHp;

        // 체력을 1로 강제 설정 (TakeDamage 우회 — 사망 이벤트 없음)
        player.SetHpDirect(1f);

        // 비율에 비례한 스탯 영구 증가
        float atkBonus = ratio * _attackDamageScale;
        float spdBonus = ratio * _moveSpeedScale;

        player.AddDynamicBonus(StatType.AttackDamage, atkBonus);
        player.AddDynamicBonus(StatType.MoveSpeed,    spdBonus);

        Debug.Log($"[Blood Oath] {currentHp:F0}/{maxHp:F0} → 1HP | 비율 {ratio:P0} → ATK+{atkBonus:F2} SPD+{spdBonus:F2}");
    }
}
