using UnityEngine;

/// <summary>
/// T-M-A 파이프라인을 흐르는 데이터 컨테이너.
///
/// 흐름: Trigger가 생성 → Modifier가 변형 → Action이 소비
///
/// 예시: OnAttackTrigger가 Damage=10으로 생성
///       → StatModifier가 DamageMultiplier를 2.0으로 변형
///       → DealDamageAction이 최종 20 데미지를 적용
/// </summary>
public class ItemEffectContext
{
    // ── 소스 참조 (Initialize 시 한 번 설정) ──────────────────────
    public PlayerStats Player;
    public PlayerEventManager EventManager;
    public WeaponController Weapon;
    public ItemData SourceItem;

    // ── 타겟 정보 (Trigger가 설정) ────────────────────────────────
    /// <summary>공격 대상 적. null이면 자기 자신 또는 범위 대상</summary>
    public EnemyBase TargetEnemy;
    /// <summary>효과 발생 위치 (폭발 중심점 등)</summary>
    public Vector3 TargetPosition;
    /// <summary>타격 방향 (넉백 등에 사용)</summary>
    public Vector3 HitDirection;

    // ── 파이프라인 데이터 (Modifier가 변형) ───────────────────────
    /// <summary>기본 피해량 (Trigger가 설정, Modifier가 변형)</summary>
    public float Damage;
    /// <summary>피해 배율. 1.0 = 변화 없음. ChanceGate 실패 시 0으로 설정됨</summary>
    public float DamageMultiplier = 1f;
    /// <summary>적용할 상태이상 ID (ST_BURN, ST_SLOW 등)</summary>
    public string StatusID;
    /// <summary>상태이상 발동 확률. ChanceGate 실패 시 0으로 설정됨</summary>
    public float StatusChance = 1f;
    /// <summary>광역 효과 범위. 0이면 단일 대상</summary>
    public float AreaRadius;
    /// <summary>치명타 여부</summary>
    public bool IsCritical;
    /// <summary>원소/속성 태그 (Fire, Electric, Poison 등)</summary>
    public string ElementTag;

    // ── 궤적/형태 변형 (Modifier가 설정, Action이 소비) ────────────
    /// <summary>남은 반사(바운스) 횟수. 0이면 반사 없음</summary>
    public int BounceCount;
    /// <summary>반사 시 데미지 감쇄 배율. 1.0 = 감쇄 없음</summary>
    public float BounceDecay = 1f;
    /// <summary>유도 여부. true면 가장 가까운 적을 향해 방향 전환</summary>
    public bool IsHoming;
    /// <summary>유도 강도 (도/초). 값이 클수록 빠르게 추적</summary>
    public float HomingStrength;

    // ── 무한 루프 방지 ────────────────────────────────────────────
    /// <summary>현재 파이프라인 연쇄 깊이. MAX_PIPELINE_DEPTH 초과 시 실행 중단</summary>
    public int PipelineDepth;
    /// <summary>최대 연쇄 깊이 — 아이템 간 이벤트 체인이 이 깊이를 넘으면 차단됨</summary>
    public const int MAX_PIPELINE_DEPTH = 3;

    // ── 메타 ──────────────────────────────────────────────────────
    /// <summary>트리거가 발동된 Time.time</summary>
    public float TriggerTime;

    /// <summary>
    /// 현재 컨텍스트를 복제한다.
    /// Trigger에서 파이프라인 실행 전 반드시 Clone()하여 독립된 데이터로 넘겨야 한다.
    /// (같은 컨텍스트를 공유하면 Modifier 간 간섭 발생)
    /// </summary>
    public ItemEffectContext Clone()
    {
        return new ItemEffectContext
        {
            Player = Player,
            EventManager = EventManager,
            Weapon = Weapon,
            SourceItem = SourceItem,
            TargetEnemy = TargetEnemy,
            TargetPosition = TargetPosition,
            HitDirection = HitDirection,
            Damage = Damage,
            DamageMultiplier = DamageMultiplier,
            StatusID = StatusID,
            StatusChance = StatusChance,
            AreaRadius = AreaRadius,
            IsCritical = IsCritical,
            ElementTag = ElementTag,
            BounceCount = BounceCount,
            BounceDecay = BounceDecay,
            IsHoming = IsHoming,
            HomingStrength = HomingStrength,
            PipelineDepth = PipelineDepth,
            TriggerTime = TriggerTime,
        };
    }
}
