using System;
using UnityEngine;

/// <summary>
/// [Modifier] 공격에 원소/속성 태그를 부여한다.
/// 태그에 따라 자동으로 대응하는 상태이상 ID(StatusID)도 설정된다.
///
/// 태그 → 상태이상 매핑:
/// - Fire → ST_BURN (화상)
/// - Electric → ST_STUN (감전/기절)
/// - Poison → ST_SLOW (독/둔화)
/// - Ice → ST_SLOW (빙결/둔화)
///
/// 사용 예: 불량 가스 토치(Fire), 구리 선 다발(Electric)
/// </summary>
[Serializable]
public class AddTagModifier : ModifierBase
{
    [Tooltip("부여할 속성 태그 (Fire, Electric, Poison, Ice 등)")]
    [SerializeField] private string _tag = "Fire";

    public string Tag { get => _tag; set => _tag = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        context.ElementTag = _tag;

        // 태그에 따른 자동 상태이상 ID 매핑
        switch (_tag)
        {
            case "Fire":     context.StatusID = "ST_BURN"; break;
            case "Electric": context.StatusID = "ST_STUN"; break;
            case "Poison":   context.StatusID = "ST_SLOW"; break;
            case "Ice":      context.StatusID = "ST_SLOW"; break;
        }
    }
}
