using System;
using UnityEngine;

/// <summary>
/// [Modifier] 파이프라인 컨텍스트의 수치를 변형하는 범용 스탯 모디파이어.
/// 데미지 배율, 범위 추가 등을 이 하나의 클래스로 처리한다.
///
/// 사용 예:
/// - 대형 파리채: DamageMultiplier=1.0 (기본) + AreaRadiusAdd=2.5 (넓은 범위)
/// - 고무줄 요요: DamageMultiplier=0.7 (30% 감소)
/// - 카운터 센서: DamageMultiplier=3.0 (3배 데미지)
/// </summary>
[Serializable]
public class StatModifier : ModifierBase
{
    [Header("데미지 변형")]
    [Tooltip("데미지에 곱해지는 배율. 1.0 = 변화 없음, 0.5 = 절반, 2.0 = 두 배")]
    [SerializeField] private float _damageMultiplier = 1f;

    [Header("범위 변형")]
    [Tooltip("효과 범위 추가값. 0 = 변화 없음")]
    [SerializeField] private float _areaRadiusAdd;

    public float DamageMultiplier { get => _damageMultiplier; set => _damageMultiplier = value; }
    public float AreaRadiusAdd { get => _areaRadiusAdd; set => _areaRadiusAdd = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        context.DamageMultiplier *= _damageMultiplier;
        context.AreaRadius += _areaRadiusAdd;
    }
}
