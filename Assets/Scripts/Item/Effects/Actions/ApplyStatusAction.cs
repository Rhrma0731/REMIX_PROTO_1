using System;
using UnityEngine;

/// <summary>
/// [Action] 기존 StatusEffectManager의 상태이상을 적용한다.
/// T-M-A 시스템과 기존 상태이상 시스템(ST_BURN, ST_SLOW 등)을 연결하는 브릿지 역할.
///
/// _statusID가 지정되면 해당 ID를 사용하고,
/// 비어있으면 컨텍스트의 StatusID(AddTagModifier가 설정)를 사용한다.
///
/// 사용 예:
/// - 낡은 프라이팬: StatusID="ST_STUN" (기절)
/// - 불량 가스 토치: StatusID="" → AddTagModifier가 설정한 ST_BURN 사용
/// - 구리 선 다발: StatusID="ST_STUN" (감전)
/// </summary>
[Serializable]
public class ApplyStatusAction : ActionBase
{
    [Tooltip("적용할 상태이상 ID (ST_BURN, ST_SLOW, ST_STUN 등). 비워두면 컨텍스트의 StatusID 사용")]
    [SerializeField] private string _statusID;

    [Tooltip("발동 확률 (0~1). 1.0 = 100%")]
    [Range(0f, 1f)]
    [SerializeField] private float _chance = 1f;

    public string StatusID { get => _statusID; set => _statusID = value; }
    public float Chance { get => _chance; set => _chance = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        // ChanceGate에 의해 차단된 경우
        if (context.StatusChance <= 0f) return;

        string statusToApply = !string.IsNullOrEmpty(_statusID) ? _statusID : context.StatusID;
        if (string.IsNullOrEmpty(statusToApply)) return;

        // 자체 확률 체크
        float effectiveChance = _chance * context.StatusChance;
        if (effectiveChance < 1f && UnityEngine.Random.value > effectiveChance) return;

        // 기존 StatusEffectManager 브릿지
        if (context.TargetEnemy != null && !context.TargetEnemy.IsDead)
        {
            StatusEffectManager.Instance?.ApplySingleEffect(context.TargetEnemy, statusToApply);
        }
    }
}
