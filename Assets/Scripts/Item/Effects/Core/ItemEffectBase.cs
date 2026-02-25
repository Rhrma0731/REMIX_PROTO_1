using System;
using UnityEngine;

/// <summary>
/// 모든 아이템 효과의 추상 베이스 클래스.
///
/// [핵심 안전장치] Execute()는 이 클래스에서 봉인(non-virtual)되어 있으며,
/// 내부에서 Internal Cooldown(ICD, 최소 0.1초)을 강제 체크한 뒤
/// 서브클래스의 OnExecute()를 호출한다.
/// → 아이템 효과 간 무한 루프(Crash)를 구조적으로 방지한다.
///
/// 서브클래스는 OnExecute()만 오버라이드하면 된다.
/// </summary>
[Serializable]
public abstract class ItemEffectBase : IItemEffect
{
    [Tooltip("내부 쿨다운(초). 최소 0.1초가 강제됨 — 무한 루프 방지용")]
    [SerializeField] protected float _internalCooldown = 0.1f;

    // 런타임 전용 (직렬화 제외)
    [NonSerialized] private float _lastExecuteTime = -999f;
    [NonSerialized] private bool _initialized;

    /// <summary>T-M-A 역할 — 서브클래스가 반드시 지정</summary>
    public abstract ItemEffectRole Role { get; }

    /// <summary>Inspector/Generator에서 ICD를 설정할 수 있는 프로퍼티</summary>
    public float InternalCooldown
    {
        get => _internalCooldown;
        set => _internalCooldown = value;
    }

    public virtual void Initialize(ItemEffectContext context)
    {
        _initialized = true;
        _lastExecuteTime = -999f;
    }

    /// <summary>
    /// [봉인된 실행 메서드] ICD를 체크한 뒤 OnExecute()를 호출한다.
    /// 이 메서드를 오버라이드하지 말 것 — ICD 우회를 구조적으로 방지하기 위함.
    /// </summary>
    public void Execute(ItemEffectContext context)
    {
        if (!_initialized) return;

        float now = Time.time;
        float actualCooldown = Mathf.Max(0.1f, _internalCooldown);
        if (now - _lastExecuteTime < actualCooldown) return;

        _lastExecuteTime = now;
        OnExecute(context);
    }

    /// <summary>서브클래스가 구현할 실제 효과 로직</summary>
    protected abstract void OnExecute(ItemEffectContext context);

    public virtual void Cleanup()
    {
        _initialized = false;
    }
}
