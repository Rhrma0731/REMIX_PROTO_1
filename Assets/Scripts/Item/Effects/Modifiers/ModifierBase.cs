using System;

/// <summary>
/// 모든 Modifier 효과의 추상 베이스.
///
/// Modifier는 "어떻게 변형할 것인가?"를 정의한다.
/// 파이프라인을 흐르는 ItemEffectContext의 값을 변형(수정)한다.
///
/// 예시:
/// - StatModifier: 데미지 배율 조정
/// - AddTagModifier: 원소 속성 부여 (Fire, Electric 등)
/// - ChanceGateModifier: 확률 체크 실패 시 파이프라인 무력화
/// - RadiusModifier: 광역 효과 범위 설정
/// </summary>
[Serializable]
public abstract class ModifierBase : ItemEffectBase
{
    public override ItemEffectRole Role => ItemEffectRole.Modifier;
}
