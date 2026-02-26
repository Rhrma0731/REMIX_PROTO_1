using System;
using UnityEngine;

/// <summary>
/// [Modifier] 파이프라인 컨텍스트에 반사(바운스) 속성을 부여한다.
///
/// 이 모디파이어가 설정한 BounceCount 값을 후속 Action이 읽어
/// 투사체나 이펙트가 벽/적에 닿아도 소멸하지 않고 N회 튕기도록 처리한다.
///
/// [연쇄 감쇄]
/// 반사할 때마다 DamageMultiplier에 _damageDecayPerBounce를 곱하여
/// 데미지가 점진적으로 감소하게 할 수 있다.
///
/// 사용 예:
/// - 고무 팔 (105): bounceCount=2, damageDecay=0.7 (반사마다 30% 데미지 감소)
/// - 핀볼 너클: bounceCount=5, damageDecay=1.0 (감쇄 없이 5회 반사)
/// - 벽꿍 거미: bounceCount=3, damageDecay=0.5 (벽에 3회 반사, 절반씩 감소)
/// </summary>
[Serializable]
public class BounceModifier : ModifierBase
{
    [Tooltip("최대 반사 횟수")]
    [SerializeField] private int _bounceCount = 2;

    [Tooltip("반사할 때마다 데미지에 곱해지는 감쇄 배율. 1.0 = 감쇄 없음, 0.7 = 30% 감소")]
    [Range(0.1f, 1f)]
    [SerializeField] private float _damageDecayPerBounce = 1f;

    public int BounceCount { get => _bounceCount; set => _bounceCount = value; }
    public float DamageDecayPerBounce { get => _damageDecayPerBounce; set => _damageDecayPerBounce = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        context.BounceCount += _bounceCount;
        context.BounceDecay *= _damageDecayPerBounce;
    }
}
