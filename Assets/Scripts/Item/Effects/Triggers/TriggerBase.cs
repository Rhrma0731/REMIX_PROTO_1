using System;
using UnityEngine;

/// <summary>
/// 모든 Trigger 효과의 추상 베이스.
///
/// Trigger는 "언제 발동할 것인가?"를 정의한다.
/// ActiveItemPipeline이 SetPipelineCallback()으로 콜백을 설정하면,
/// 서브클래스는 조건 충족 시 FireTrigger()를 호출하여 파이프라인을 가동시킨다.
///
/// [주의] OnExecute()는 비어 있다. Trigger는 이벤트 핸들러 → FireTrigger() 경로를 사용한다.
/// </summary>
[Serializable]
public abstract class TriggerBase : ItemEffectBase
{
    public override ItemEffectRole Role => ItemEffectRole.Trigger;

    /// <summary>ActiveItemPipeline이 등록한 파이프라인 실행 콜백</summary>
    [NonSerialized] private Action<ItemEffectContext> _pipelineCallback;

    /// <summary>Initialize에서 캐시한 기본 컨텍스트 (Clone()하여 사용)</summary>
    [NonSerialized] protected ItemEffectContext _cachedContext;

    /// <summary>ActiveItemPipeline이 파이프라인 실행 콜백을 등록한다</summary>
    public void SetPipelineCallback(Action<ItemEffectContext> callback)
    {
        _pipelineCallback = callback;
    }

    public override void Initialize(ItemEffectContext context)
    {
        base.Initialize(context);
        _cachedContext = context;
    }

    /// <summary>
    /// 트리거 조건이 충족되었을 때 호출.
    /// 컨텍스트에 타임스탬프를 찍고 파이프라인 콜백을 실행한다.
    /// </summary>
    protected void FireTrigger(ItemEffectContext context)
    {
        context.TriggerTime = Time.time;
        _pipelineCallback?.Invoke(context);
    }

    /// <summary>Trigger는 Execute() 경로를 사용하지 않음 — FireTrigger() 사용</summary>
    protected override void OnExecute(ItemEffectContext context) { }
}
