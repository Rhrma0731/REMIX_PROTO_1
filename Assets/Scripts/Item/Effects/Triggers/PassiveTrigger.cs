using System;
using UnityEngine;

/// <summary>
/// [Trigger] 패시브 — 장착 즉시 1회 발동. _continuous가 true면 매초 재실행.
///
/// 사용 예:
/// - 안마기 모터: 패시브 스탯 변형 (1회 발동)
/// - 망가진 나침반: 조건부 이속 증가 (continuous=true로 매초 체크)
/// - 철제 깔때기: 장착 즉시 효과 적용 (1회 발동)
/// </summary>
[Serializable]
public class PassiveTrigger : TriggerBase
{
    [Tooltip("true면 매초 파이프라인을 재실행. false면 장착 시 1회만 발동")]
    [SerializeField] private bool _continuous;

    /// <summary>Inspector/Generator에서 연속 실행 여부를 설정하는 프로퍼티</summary>
    public bool Continuous { get => _continuous; set => _continuous = value; }

    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);

        // 장착 즉시 1회 발동
        var ctx = _cachedContext.Clone();
        FireTrigger(ctx);

        // 연속 모드: 매초 재실행
        if (_continuous && PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnSecondTick += HandleTick;
    }

    private void HandleTick()
    {
        var ctx = _cachedContext.Clone();
        FireTrigger(ctx);
    }

    public override void Cleanup()
    {
        if (_continuous && PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnSecondTick -= HandleTick;
        base.Cleanup();
    }
}
