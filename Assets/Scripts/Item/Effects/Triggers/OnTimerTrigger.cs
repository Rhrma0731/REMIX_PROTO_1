using System;
using UnityEngine;

/// <summary>
/// [Trigger] 매 N초마다 주기적으로 발동.
/// PlayerEventManager.OnSecondTick을 구독하여 내부 타이머로 간격을 측정한다.
///
/// 사용 예: 누전된 헤드폰(3초마다 자해), 리사이클 렌즈(10초마다 아이템 리롤)
/// </summary>
[Serializable]
public class OnTimerTrigger : TriggerBase
{
    [Tooltip("발동 간격(초). 예: 3이면 3초마다 발동")]
    [SerializeField] private float _interval = 3f;

    [NonSerialized] private float _timer;

    /// <summary>Inspector/Generator에서 간격을 설정할 수 있는 프로퍼티</summary>
    public float Interval { get => _interval; set => _interval = value; }

    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);
        _timer = 0f;
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnSecondTick += HandleTick;
    }

    private void HandleTick()
    {
        _timer += 1f;
        if (_timer >= _interval)
        {
            _timer -= _interval;
            var ctx = _cachedContext.Clone();
            ctx.Damage = _cachedContext.Player != null ? _cachedContext.Player.AttackDamage : 0f;
            FireTrigger(ctx);
        }
    }

    public override void Cleanup()
    {
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnSecondTick -= HandleTick;
        base.Cleanup();
    }
}
