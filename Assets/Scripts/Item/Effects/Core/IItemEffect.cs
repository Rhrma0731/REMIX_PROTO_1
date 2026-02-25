/// <summary>
/// T-M-A 파이프라인에서 아이템 효과의 역할을 정의하는 열거형.
/// Trigger(발동 조건) → Modifier(수치 변형) → Action(실행 결과) 순서로 처리된다.
/// </summary>
public enum ItemEffectRole
{
    Trigger,   // 언제 발동할 것인가? (예: 공격 시, 피격 시, 3초마다)
    Modifier,  // 어떻게 변형할 것인가? (예: 데미지 2배, 화염 속성 부여)
    Action     // 무엇을 실행할 것인가? (예: 피해 적용, 상태이상 부여, 회복)
}

/// <summary>
/// 모든 아이템 효과 모듈이 구현해야 할 공통 인터페이스.
/// Trigger, Modifier, Action 모두 이 인터페이스를 통해 T-M-A 파이프라인에 참여한다.
/// [SerializeReference]로 ItemData에 저장되므로 MonoBehaviour가 아닌 순수 C# 클래스로 구현할 것.
/// </summary>
public interface IItemEffect
{
    /// <summary>T-M-A 파이프라인에서의 역할 (Trigger / Modifier / Action)</summary>
    ItemEffectRole Role { get; }

    /// <summary>아이템 장착 시 초기화. 이벤트 구독, 캐시 설정 등을 수행한다.</summary>
    void Initialize(ItemEffectContext context);

    /// <summary>효과 실행. ItemEffectBase에서 ICD(내부 쿨타임)를 강제한다.</summary>
    void Execute(ItemEffectContext context);

    /// <summary>아이템 해제 시 정리. 이벤트 구독 해제, 리소스 반환 등을 수행한다.</summary>
    void Cleanup();
}
