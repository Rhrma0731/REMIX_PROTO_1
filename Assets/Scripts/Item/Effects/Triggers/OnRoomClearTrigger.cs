using System;
using UnityEngine;

/// <summary>
/// [Trigger] 웨이브(방)를 클리어했을 때 발동.
/// PlayerEventManager.OnRoomClear 이벤트를 구독한다.
///
/// StageManager가 웨이브 클리어 시 BroadcastRoomClear()를 호출하면 발동된다.
///
/// 사용 예:
/// - 구멍난 주머니 (Bum Friend): 방 클리어 시 30% 확률로 코인 드롭
/// - 행운의 동전 (D20): 방 클리어 시 모든 드롭 아이템 리롤
/// - 응급 키트: 방 클리어 시 고정 체력 회복
/// </summary>
[Serializable]
public class OnRoomClearTrigger : TriggerBase
{
    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnRoomClear += HandleRoomClear;
    }

    private void HandleRoomClear()
    {
        var ctx = _cachedContext.Clone();
        ctx.TargetPosition = ctx.Player != null ? ctx.Player.transform.position : Vector3.zero;
        FireTrigger(ctx);
    }

    public override void Cleanup()
    {
        if (PlayerEventManager.Instance != null)
            PlayerEventManager.Instance.OnRoomClear -= HandleRoomClear;
        base.Cleanup();
    }
}
