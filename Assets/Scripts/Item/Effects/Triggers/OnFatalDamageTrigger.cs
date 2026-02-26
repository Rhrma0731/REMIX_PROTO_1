using System;
using UnityEngine;

/// <summary>
/// [Trigger] 플레이어의 체력이 0 이하가 되어 죽기 직전에 발동.
/// PlayerEventManager.OnFatalDamage 이벤트를 구독한다.
///
/// PlayerStats.TakeDamage에서 HP가 0 이하가 된 직후, _isDead 설정 전에 방송됨.
/// 이 트리거에 연결된 ReviveAction이 체력을 회복하면 사망이 취소된다.
///
/// 사용 예:
/// - 고장난 태엽심장 (Dead Cat): 3회까지 부활, 최대 체력의 50%로 회복
/// - 비상 배터리 (1UP): 1회 부활, 최대 체력으로 회복
/// </summary>
[Serializable]
public class OnFatalDamageTrigger : TriggerBase
{
    [Tooltip("최대 발동 횟수. 0이면 무제한")]
    [SerializeField] private int _maxUses = 1;

    [NonSerialized] private int _usesRemaining;

    public int MaxUses { get => _maxUses; set => _maxUses = value; }
    public int UsesRemaining => _usesRemaining;

    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);
        _usesRemaining = _maxUses;

        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnFatalDamage += HandleFatalDamage;
    }

    private void HandleFatalDamage()
    {
        // 횟수 제한 체크 (0 = 무제한)
        if (_maxUses > 0 && _usesRemaining <= 0) return;

        if (_maxUses > 0)
            _usesRemaining--;

        var ctx = _cachedContext.Clone();
        ctx.TargetPosition = ctx.Player != null ? ctx.Player.transform.position : Vector3.zero;
        FireTrigger(ctx);
    }

    public override void Cleanup()
    {
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnFatalDamage -= HandleFatalDamage;
        base.Cleanup();
    }
}
