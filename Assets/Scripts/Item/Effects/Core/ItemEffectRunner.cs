using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장착된 아이템의 T-M-A 이펙트 파이프라인 생명주기를 관리한다.
///
/// PlayerAppearance.EquipItem()에서 RegisterItem()을 호출하면
/// 해당 아이템의 Effects를 Role별(Trigger/Modifier/Action)로 분류하고
/// Trigger들을 PlayerEventManager 이벤트에 연결한다.
///
/// [씬 배치] Player 루트 오브젝트에 컴포넌트로 추가 (PlayerAppearance 옆).
/// </summary>
public class ItemEffectRunner : MonoBehaviour
{
    public static ItemEffectRunner Instance { get; private set; }

    private List<ActiveItemPipeline> _activePipelines = new List<ActiveItemPipeline>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 아이템의 T-M-A 이펙트를 등록하고 활성화한다.
    /// Effects 리스트가 비어있으면 (기존 스탯 전용 아이템) 무시된다.
    /// </summary>
    public void RegisterItem(ItemData item)
    {
        if (item.Effects == null || item.Effects.Count == 0) return;

        var context = BuildBaseContext(item);
        var pipeline = new ActiveItemPipeline(item, context);
        pipeline.Initialize();
        _activePipelines.Add(pipeline);
    }

    /// <summary>
    /// 모든 이펙트를 정리한다. 새 런 시작 시 PlayerAppearance.ResetAllParts()에서 호출.
    /// </summary>
    public void ClearAll()
    {
        foreach (var p in _activePipelines)
            p.Cleanup();
        _activePipelines.Clear();
    }

    /// <summary>
    /// 아이템용 기본 컨텍스트를 생성한다.
    /// Trigger가 발동할 때 이 컨텍스트를 Clone()하여 파이프라인에 전달한다.
    /// </summary>
    private ItemEffectContext BuildBaseContext(ItemData item)
    {
        return new ItemEffectContext
        {
            Player = PlayerStats.Instance,
            EventManager = PlayerEventManager.Instance,
            Weapon = FindAnyObjectByType<WeaponController>(),
            SourceItem = item,
            DamageMultiplier = 1f,
        };
    }
}

/// <summary>
/// 단일 아이템의 활성 T-M-A 파이프라인.
///
/// 아이템의 Effects 리스트를 Role별로 분류하고,
/// Trigger 발동 시 Modifier → Action 순서로 파이프라인을 실행한다.
///
/// [무한 루프 방지] PipelineDepth가 MAX_PIPELINE_DEPTH를 초과하면 실행을 중단한다.
/// 예: 자해 아이템 → 피격 트리거 → 자해 아이템... 이런 체인이 3단계까지만 허용됨.
/// </summary>
public class ActiveItemPipeline
{
    private ItemData _item;
    private ItemEffectContext _baseContext;
    private List<IItemEffect> _triggers = new List<IItemEffect>();
    private List<IItemEffect> _modifiers = new List<IItemEffect>();
    private List<IItemEffect> _actions = new List<IItemEffect>();

    public ActiveItemPipeline(ItemData item, ItemEffectContext context)
    {
        _item = item;
        _baseContext = context;

        // Effects를 역할별로 분류
        foreach (var effect in item.Effects)
        {
            if (effect == null) continue;
            switch (effect.Role)
            {
                case ItemEffectRole.Trigger:  _triggers.Add(effect);  break;
                case ItemEffectRole.Modifier: _modifiers.Add(effect); break;
                case ItemEffectRole.Action:   _actions.Add(effect);   break;
            }
        }
    }

    /// <summary>
    /// 모든 효과를 초기화한다. Trigger에는 파이프라인 콜백을 설정한다.
    /// </summary>
    public void Initialize()
    {
        // Trigger에 파이프라인 실행 콜백 등록
        foreach (var t in _triggers)
        {
            if (t is TriggerBase trigger)
                trigger.SetPipelineCallback(ExecutePipeline);
            t.Initialize(_baseContext);
        }

        foreach (var m in _modifiers) m.Initialize(_baseContext);
        foreach (var a in _actions) a.Initialize(_baseContext);
    }

    /// <summary>
    /// Trigger가 발동하면 호출됨.
    /// PipelineDepth를 체크하여 무한 연쇄를 방지한 뒤
    /// Modifier → Action 순서로 실행한다.
    /// </summary>
    public void ExecutePipeline(ItemEffectContext triggerContext)
    {
        // 무한 루프 방지: 연쇄 깊이 초과 시 실행 중단
        if (triggerContext.PipelineDepth >= ItemEffectContext.MAX_PIPELINE_DEPTH) return;
        triggerContext.PipelineDepth++;

        // Modifier 순서대로 컨텍스트 변형
        foreach (var mod in _modifiers)
            mod.Execute(triggerContext);

        // Action 순서대로 실행
        foreach (var act in _actions)
            act.Execute(triggerContext);
    }

    /// <summary>모든 효과를 정리한다 (이벤트 구독 해제 등).</summary>
    public void Cleanup()
    {
        foreach (var t in _triggers) t.Cleanup();
        foreach (var m in _modifiers) m.Cleanup();
        foreach (var a in _actions) a.Cleanup();
    }
}
