using System;
using UnityEngine;

/// <summary>
/// [Modifier] 효과의 범위(AreaRadius)를 설정한다.
/// AreaRadius > 0이면 후속 Action(DealDamageAction 등)에서 OverlapSphere로 광역 처리한다.
///
/// 사용 예:
/// - 고압축 피스톤: Radius=2.0 (타격 지점 폭발)
/// - 사이렌 경광등: Radius=4.0 (주변 공포)
/// - 장난감 벌집: Radius=5.0 (넓은 범위 드론)
/// </summary>
[Serializable]
public class RadiusModifier : ModifierBase
{
    [Tooltip("광역 효과 범위")]
    [SerializeField] private float _radius = 2f;

    public float Radius { get => _radius; set => _radius = value; }

    protected override void OnExecute(ItemEffectContext context)
    {
        context.AreaRadius = _radius;
    }
}
