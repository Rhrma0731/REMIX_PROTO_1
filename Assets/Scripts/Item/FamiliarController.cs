using UnityEngine;

/// <summary>
/// 패밀리어(펫/드론)의 동작 모드.
/// </summary>
public enum FamiliarMode
{
    /// <summary>플레이어 주변 공전 + 주기적 근접 공격</summary>
    Orbit,
    /// <summary>가장 가까운 적 추격 + 자폭</summary>
    Homing,

    // ── 미구현 모드 (현재 Orbit으로 폴백) ──────────────────────────────

    /// <summary>
    /// [미구현] 마우스 커서 방향으로 이동하며 틱 데미지.
    /// 현재는 Orbit으로 폴백된다.
    /// TODO: Mouse.current.position 기반 월드 좌표 추적 구현 필요
    /// </summary>
    MouseFollow,

    /// <summary>
    /// [미구현] 방 중앙 기준 플레이어 대칭 위치로 이동.
    /// 현재는 Orbit으로 폴백된다.
    /// TODO: RoomCenter 참조 + playerOffset 반전 이동 구현 필요
    /// </summary>
    Symmetric,

    /// <summary>
    /// [미구현] 플레이어 궤도를 돌며 적 투사체를 차단.
    /// 현재는 Orbit(공전만)으로 폴백된다.
    /// TODO: Collider 트리거 + Projectile 레이어 감지 후 Destroy 구현 필요
    /// </summary>
    OrbitalShield,
}

/// <summary>
/// SpawnFamiliarAction이 소환하는 패밀리어 MonoBehaviour.
///
/// ── Orbit 모드 (기본) ──────────────────────────────────────────────
///   플레이어를 중심으로 XZ 평면을 공전한다.
///   _attackInterval 초마다 _attackRadius 안의 가장 가까운 적에게 피해를 준다.
///   데미지는 PlayerStats.AttackDamage × _damageMultiplier로 자동 스케일된다.
///
/// ── Homing 모드 (자폭) ─────────────────────────────────────────────
///   가장 가까운 적(또는 Configure로 지정된 초기 타겟)을 향해 직선으로 돌진한다.
///   _detonationRadius 이내 접근 시 _explosionRadius 범위 광역 자폭 후 자기 파괴.
///   장난감 벌집(304) 아이템의 "유도 벌" 기믹에 사용.
///
/// ── SpawnFamiliarAction 연동 ────────────────────────────────────────
///   소환 직후 SpawnFamiliarAction이 Configure(context)를 호출한다.
///   context.IsHoming == true 이면 자동으로 Homing 모드로 전환.
///   context.TargetEnemy 가 있으면 초기 추격 타겟으로 설정.
///
/// ── 프리팹 설정 방법 ────────────────────────────────────────────────
///   ① SpriteRenderer + FamiliarController 컴포넌트 추가
///   ② EnemyLayer 를 "Enemy" 레이어로 설정 (WeaponController와 동일)
///   ③ SpawnFamiliarAction._familiarPrefab 에 드래그 연결
/// </summary>
public class FamiliarController : MonoBehaviour
{
    // ── Inspector : 공전 ───────────────────────────────────────────────

    [Header("공전 설정 (Orbit Mode)")]
    [Tooltip("플레이어로부터의 공전 반지름")]
    [SerializeField] private float _orbitRadius = 1.2f;

    [Tooltip("공전 속도 (도/초). 양수 = 반시계")]
    [SerializeField] private float _orbitSpeed = 90f;

    [Tooltip("플레이어 Y 기준 공전 높이 오프셋")]
    [SerializeField] private float _orbitHeightOffset = 0.3f;

    // ── Inspector : 전투 (Orbit) ───────────────────────────────────────

    [Header("전투 설정 (Orbit Mode)")]
    [Tooltip("공격 간격 (초)")]
    [SerializeField] private float _attackInterval = 1.5f;

    [Tooltip("적 감지 반지름 (이 안의 가장 가까운 적을 공격)")]
    [SerializeField] private float _attackRadius = 2.5f;

    [Tooltip("PlayerStats.AttackDamage 에 곱하는 데미지 배율. 0.5 = 50%")]
    [SerializeField] private float _damageMultiplier = 0.5f;

    // ── Inspector : 자폭 (Homing) ──────────────────────────────────────

    [Header("자폭 설정 (Homing Mode)")]
    [Tooltip("추격 이동 속도 (유닛/초)")]
    [SerializeField] private float _homingSpeed = 6f;

    [Tooltip("자폭 판정 거리 — 타겟과 이 거리 이내면 폭발")]
    [SerializeField] private float _detonationRadius = 0.4f;

    [Tooltip("폭발 피해 반지름")]
    [SerializeField] private float _explosionRadius = 1.5f;

    [Tooltip("자폭 데미지 배율 (PlayerStats.AttackDamage 기준)")]
    [SerializeField] private float _explosionDamageMultiplier = 1.5f;

    // ── Inspector : 공통 ───────────────────────────────────────────────

    [Header("공통")]
    [Tooltip("적 레이어 마스크. WeaponController 와 동일하게 'Enemy' 레이어로 설정")]
    [SerializeField] private LayerMask _enemyLayer;

    [Tooltip("공격 / 자폭 VFX 플래시 색상")]
    [SerializeField] private Color _attackColor = new Color(1f, 0.5f, 0f);

    [Tooltip("SpriteRenderer (없으면 자동 탐색)")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("수명")]
    [Tooltip("소환 후 자동 소멸 시간(초). 0이면 무제한")]
    [SerializeField] private float _lifetime = 0f;

    [Header("기본 모드")]
    [Tooltip("Inspector 기본 동작 모드. Configure()로 런타임에 덮어쓸 수 있음")]
    [SerializeField] private FamiliarMode _defaultMode = FamiliarMode.Orbit;

    // ── 런타임 상태 ────────────────────────────────────────────────────

    private FamiliarMode _runtimeMode;
    private Transform    _playerTransform;
    private Camera       _mainCamera;
    private float        _orbitAngle;
    private float        _attackTimer;
    private float        _lifetimeTimer;
    private EnemyBase    _homingTarget;

    // ── Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _mainCamera  = Camera.main;
        _runtimeMode = _defaultMode;

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        FindPlayer();

        // 여러 패밀리어가 동시 소환될 때 겹치지 않도록 시작 각도 무작위화
        _orbitAngle  = Random.Range(0f, 360f);
        _attackTimer = _attackInterval;
        _lifetimeTimer = _lifetime;
    }

    private void Update()
    {
        if (_playerTransform == null) { FindPlayer(); return; }

        switch (_runtimeMode)
        {
            case FamiliarMode.Orbit:  UpdateOrbit();  break;
            case FamiliarMode.Homing: UpdateHoming(); break;

            // 미구현 모드 — 구현 전까지 Orbit으로 폴백
            case FamiliarMode.MouseFollow:   // TODO: 마우스 커서 추적 이동
            case FamiliarMode.Symmetric:     // TODO: 방 중앙 기준 플레이어 대칭 이동
            case FamiliarMode.OrbitalShield: // TODO: 투사체 차단 궤도
            default:
                UpdateOrbit();
                break;
        }

        ApplyBillboard();
        UpdateLifetime();
    }

    // ── 외부 설정 API ──────────────────────────────────────────────────

    /// <summary>
    /// SpawnFamiliarAction이 소환 직후 호출하는 초기화 메서드.
    /// context.IsHoming == true 이면 Homing 모드로 전환.
    /// context.TargetEnemy 가 있으면 초기 추격 타겟으로 설정.
    /// </summary>
    public void Configure(ItemEffectContext context)
    {
        if (context == null) return;

        if (context.IsHoming)
            _runtimeMode = FamiliarMode.Homing;

        if (context.TargetEnemy != null && !context.TargetEnemy.IsDead)
            _homingTarget = context.TargetEnemy;
    }

    // ── Orbit 모드 ─────────────────────────────────────────────────────

    private void UpdateOrbit()
    {
        // XZ 평면 공전
        _orbitAngle += _orbitSpeed * Time.deltaTime;
        float rad = _orbitAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * _orbitRadius,
            _orbitHeightOffset,
            Mathf.Sin(rad) * _orbitRadius
        );
        transform.position = _playerTransform.position + offset;

        // 주기적 공격
        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            TryOrbitAttack();
            _attackTimer = _attackInterval;
        }
    }

    private void TryOrbitAttack()
    {
        EnemyBase target = FindNearestEnemy(transform.position, _attackRadius);
        if (target == null) return;

        float   damage = CalculateDamage(_damageMultiplier);
        Vector3 dir    = (target.transform.position - transform.position).normalized;

        target.TakeDamage(damage, dir);

        // T-M-A 이벤트 체인 지원 — WeaponController와 동일한 방식
        PlayerEventManager.Instance?.BroadcastDealDamage(target, damage);
        if (target.IsDead)
            PlayerEventManager.Instance?.BroadcastKillEnemy(target);

        ItemEffectVFX.Instance?.PlayDamageFlash(target, _attackColor);
    }

    // ── Homing 모드 ────────────────────────────────────────────────────

    private void UpdateHoming()
    {
        // 타겟 소실 시 재탐색
        if (_homingTarget == null || _homingTarget.IsDead)
            _homingTarget = FindNearestEnemy(transform.position, 99f);

        if (_homingTarget == null) return; // 씬에 적 없음 — 대기

        // XZ 평면에서 타겟을 향해 이동 (Y축 고정)
        Vector3 toTarget = _homingTarget.transform.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= _detonationRadius)
        {
            Detonate();
            return;
        }

        transform.position += toTarget.normalized * _homingSpeed * Time.deltaTime;
    }

    private void Detonate()
    {
        float damage = CalculateDamage(_explosionDamageMultiplier);

        Collider[] hits = _enemyLayer.value != 0
            ? Physics.OverlapSphere(transform.position, _explosionRadius, _enemyLayer)
            : Physics.OverlapSphere(transform.position, _explosionRadius);

        foreach (Collider col in hits)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy == null || enemy.IsDead) continue;

            Vector3 dir = (enemy.transform.position - transform.position).normalized;
            enemy.TakeDamage(damage, dir);

            PlayerEventManager.Instance?.BroadcastDealDamage(enemy, damage);
            if (enemy.IsDead)
                PlayerEventManager.Instance?.BroadcastKillEnemy(enemy);

            ItemEffectVFX.Instance?.PlayDamageFlash(enemy, _attackColor);
        }

        // 자폭 VFX (노란 버스트)
        ItemEffectVFX.Instance?.PlayReviveEffect(transform);

        Destroy(gameObject);
    }

    // ── 유틸리티 ───────────────────────────────────────────────────────

    /// <summary>origin 중심 radius 범위 내 살아있는 적 중 가장 가까운 것을 반환.</summary>
    private EnemyBase FindNearestEnemy(Vector3 origin, float radius)
    {
        Collider[] hits = _enemyLayer.value != 0
            ? Physics.OverlapSphere(origin, radius, _enemyLayer)
            : Physics.OverlapSphere(origin, radius);

        EnemyBase nearest     = null;
        float     nearestSqDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy == null || enemy.IsDead) continue;

            float sqDist = (col.transform.position - origin).sqrMagnitude;
            if (sqDist < nearestSqDist)
            {
                nearestSqDist = sqDist;
                nearest       = enemy;
            }
        }

        return nearest;
    }

    /// <summary>PlayerStats.AttackDamage × multiplier. PlayerStats 없으면 10 기준.</summary>
    private float CalculateDamage(float multiplier)
    {
        float baseAtk = PlayerStats.Instance != null ? PlayerStats.Instance.AttackDamage : 10f;
        return Mathf.Max(1f, baseAtk * multiplier);
    }

    private void FindPlayer()
    {
        if (PlayerStats.Instance != null)
            _playerTransform = PlayerStats.Instance.transform;
    }

    /// <summary>EnemyBase.ApplyBillboard()와 동일한 방식으로 카메라를 향해 회전.</summary>
    private void ApplyBillboard()
    {
        if (_mainCamera == null || _spriteRenderer == null) return;
        _spriteRenderer.transform.rotation = Quaternion.LookRotation(
            _mainCamera.transform.forward,
            _mainCamera.transform.up
        );
    }

    private void UpdateLifetime()
    {
        if (_lifetime <= 0f) return;
        _lifetimeTimer -= Time.deltaTime;
        if (_lifetimeTimer <= 0f)
            Destroy(gameObject);
    }
}
