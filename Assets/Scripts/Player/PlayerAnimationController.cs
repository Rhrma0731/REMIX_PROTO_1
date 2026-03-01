using UnityEngine;

/// <summary>
/// GlitchDuck의 이동 방향에 따라 애니메이션과 스프라이트 방향을 제어합니다.
///
/// WalkState (int) 값:
///   0 = front  — dir.z <= 0이고 side가 아닐 때 (정면, 기본)
///   1 = side   — |dir.x| > |dir.z| 일 때 (좌우)
///   2 = back   — dir.z > 0이고 side가 아닐 때 (후면)
///
/// flipX: side_Walk 스프라이트는 오른쪽 기준 → 왼쪽 이동 시 뒤집음.
///
/// 파츠 설계 메모:
///   현재는 "Body" 자식 하나만 flipX 처리합니다.
///   파츠 추가 시 PlayerAppearance._slotMap 등록 Renderer들도 동일하게 처리 필요.
/// </summary>
[RequireComponent(typeof(Animator), typeof(PlayerMovement))]
public class PlayerAnimationController : MonoBehaviour
{
    // WalkState 값 상수
    const int STATE_FRONT = 0;
    const int STATE_SIDE  = 1;
    const int STATE_BACK  = 2;

    static readonly int k_WalkState = Animator.StringToHash("WalkState");

    Animator _animator;
    PlayerMovement _movement;
    SpriteRenderer _bodyRenderer;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<PlayerMovement>();

        Transform bodyT = transform.Find("Body");
        if (bodyT != null)
            _bodyRenderer = bodyT.GetComponent<SpriteRenderer>();
        else
            Debug.LogWarning("[PlayerAnimationController] 'Body' 자식 오브젝트를 찾지 못했습니다.");
    }

    void Update()
    {
        Vector3 dir = _movement.MoveDirection;
        bool isMoving = dir.sqrMagnitude > 0.01f;

        // 이동 방향에 따른 상태 결정
        int walkState = STATE_FRONT;
        if (isMoving)
        {
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
                walkState = STATE_SIDE;
            else if (dir.z > 0f)
                walkState = STATE_BACK;
            // dir.z <= 0 이면 STATE_FRONT 유지
        }

        _animator.speed = isMoving ? 1f : 0f;
        _animator.SetInteger(k_WalkState, walkState);

        // flipX: 옆 방향 이동 시에만 적용
        if (_bodyRenderer != null)
            _bodyRenderer.flipX = (walkState == STATE_SIDE) && (dir.x < 0f);
    }
}
