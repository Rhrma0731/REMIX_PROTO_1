using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 캡슐 낙하 시 산개하는 장애물.
/// 프리팹에 BoxCollider, NavMeshObstacle(Carve On), Rigidbody 컴포넌트 수동 추가 필요.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshObstacle))]
public class ObstacleController : MonoBehaviour
{
    [SerializeField] private float _bounceDamping = 0.3f;
    [SerializeField] private int _bounceCount = 1;

    private Rigidbody _rb;
    private NavMeshObstacle _navObstacle;
    private float _groundY;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _navObstacle = GetComponent<NavMeshObstacle>();

        // 스폰 직후에는 물리 비활성 + NavMesh Carving 비활성
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        _navObstacle.carving = true;  // 착지 후 활성화 시 NavMesh 구멍 생성
        _navObstacle.enabled = false;
    }

    /// <summary>GachaCapsuleSpawner가 착지 직후 호출. 산개 방향과 힘을 받아 물리 날림.</summary>
    public void Launch(Vector3 scatterDir, float force)
    {
        _groundY = transform.position.y;

        _rb.isKinematic = false;
        _rb.useGravity = true;

        _rb.linearVelocity = Vector3.zero;
        // 수평 산개 + 약한 상승 호
        Vector3 impulse = scatterDir.normalized * force + Vector3.up * (force * 0.4f);
        _rb.AddForce(impulse, ForceMode.Impulse);

        StartCoroutine(SettleRoutine());
    }

    private IEnumerator SettleRoutine()
    {
        int bounces = 0;
        float timeout = 3f;
        float startTime = Time.time;

        yield return new WaitForSeconds(0.05f);

        while (bounces < _bounceCount && Time.time - startTime < timeout)
        {
            // 낙하 시작 대기
            while (_rb.linearVelocity.y >= -0.1f && Time.time - startTime < timeout)
                yield return null;

            // 바닥 도달 대기
            while (transform.position.y > _groundY + 0.05f && Time.time - startTime < timeout)
                yield return null;

            if (Time.time - startTime >= timeout) break;

            Vector3 vel = _rb.linearVelocity;
            float bounceY = Mathf.Abs(vel.y) * _bounceDamping;
            if (bounceY < 0.1f) break;

            _rb.linearVelocity = new Vector3(vel.x * _bounceDamping, bounceY, vel.z * _bounceDamping);
            bounces++;
            yield return null;
        }

        // 물리 정지 + 위치 바닥에 고정
        _rb.linearVelocity = Vector3.zero;
        _rb.useGravity = false;
        _rb.isKinematic = true;

        Vector3 finalPos = transform.position;
        finalPos.y = _groundY;
        transform.position = finalPos;

        // 착지 완료 → NavMesh Carving 시작
        _navObstacle.enabled = true;
    }
}
