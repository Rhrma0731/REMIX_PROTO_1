using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _weaponPivot;
    [SerializeField] private GameObject _weaponVisual;
    [SerializeField] private SpriteRenderer _weaponSprite;
    [SerializeField] private InputActionAsset _inputActions;

    [Header("Attack Timing")]
    [SerializeField] private float _attackDuration = 0.25f;
    [SerializeField] private float _attackCooldown = 0.4f;

    [Header("Hit Detection")]
    [SerializeField] private float _hitRadius = 0.8f;
    [SerializeField] private float _hitDamage = 20f;
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Spring Popup")]
    [SerializeField] private float _popupDuration = 0.12f;
    [SerializeField] private float _retractDuration = 0.08f;
    [SerializeField] private float _overshootScale = 1.3f;
    [SerializeField] private Vector3 _popupOffset = new Vector3(0f, 0f, 0.3f);

    // Events
    public event Action OnGlitchStart;
    public event Action OnGlitchEnd;

    private InputAction _aimAction;
    private InputAction _attackAction;

    private Camera _mainCamera;
    private Plane _groundPlane;
    private Vector3 _aimDirection = Vector3.forward;

    private float _attackTimer;
    private float _cooldownTimer;
    private bool _isAttacking;

    // Spring animation state
    private float _animTimer;
    private bool _isPopping;
    private bool _isRetracting;
    private Vector3 _baseLocalPos;
    private Vector3 _baseLocalScale;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _groundPlane = new Plane(Vector3.up, Vector3.zero);

        _baseLocalPos = _weaponVisual.transform.localPosition;
        _baseLocalScale = _weaponVisual.transform.localScale;

        _weaponVisual.SetActive(false);

        var playerMap = _inputActions.FindActionMap("Player");
        _aimAction = playerMap.FindAction("Aim");
        _attackAction = playerMap.FindAction("Attack");
    }

    private void OnEnable()
    {
        _aimAction.Enable();
        _attackAction.Enable();
    }

    private void OnDisable()
    {
        _aimAction.Disable();
        _attackAction.Disable();
    }

    private void Update()
    {
        UpdateAimDirection();
        RotatePivotToAim();
        HandleAttackInput();
        UpdateTimers();
        UpdateSpringAnimation();
        ApplyBillboard();
    }

    // --- Aim Tracking (always active) ---

    private void UpdateAimDirection()
    {
        Vector2 screenPos = _aimAction.ReadValue<Vector2>();
        Ray ray = _mainCamera.ScreenPointToRay(screenPos);
        _groundPlane.SetNormalAndPosition(Vector3.up, transform.position);

        if (_groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 direction = hitPoint - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                _aimDirection = direction.normalized;
            }
        }
    }

    private void RotatePivotToAim()
    {
        float angle = Mathf.Atan2(_aimDirection.x, _aimDirection.z) * Mathf.Rad2Deg;
        _weaponPivot.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    // --- Attack Flow ---

    private void HandleAttackInput()
    {
        if (_attackAction.WasPressedThisFrame() && _cooldownTimer <= 0f && !_isAttacking)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        _isAttacking = true;
        _attackTimer = _attackDuration;

        _weaponVisual.SetActive(true);
        _weaponVisual.transform.localPosition = _baseLocalPos;
        _weaponVisual.transform.localScale = Vector3.zero;
        _isPopping = true;
        _isRetracting = false;
        _animTimer = 0f;

        OnGlitchStart?.Invoke();

        PerformHitDetection();
    }

    private void PerformHitDetection()
    {
        Vector3 hitCenter = transform.position + _aimDirection * _hitRadius;
        Collider[] hits = Physics.OverlapSphere(hitCenter, _hitRadius, _enemyLayer);

        CombatFeedback feedback = CombatFeedback.Instance;

        foreach (Collider col in hits)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            HitResult result = feedback != null
                ? feedback.ProcessHit(_hitDamage)
                : new HitResult { Damage = _hitDamage, IsCritical = false };

            Vector3 hitPoint = col.ClosestPoint(hitCenter);
            Vector3 hitDirection = (enemy.transform.position - transform.position).normalized;

            enemy.TakeDamage(result.Damage, hitDirection);

            if (feedback != null)
            {
                feedback.PlayHitFeedback(enemy, hitPoint, hitDirection, result);
            }
        }
    }

    // --- Timers ---

    private void UpdateTimers()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (!_isAttacking) return;

        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f && !_isRetracting)
            BeginRetract();
    }

    private void BeginRetract()
    {
        _isPopping = false;
        _isRetracting = true;
        _animTimer = 0f;

        OnGlitchEnd?.Invoke();
    }

    private void EndAttack()
    {
        _isAttacking = false;
        _isRetracting = false;
        _cooldownTimer = _attackCooldown;

        _weaponVisual.transform.localScale = _baseLocalScale;
        _weaponVisual.transform.localPosition = _baseLocalPos;
        _weaponVisual.SetActive(false);
    }

    // --- Spring Popup / Retract ---

    private void UpdateSpringAnimation()
    {
        if (_isPopping)
        {
            _animTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_animTimer / _popupDuration);
            float eased = EaseOutBack(t);

            _weaponVisual.transform.localScale = _baseLocalScale * eased;
            _weaponVisual.transform.localPosition = _baseLocalPos + _popupOffset * eased;

            if (t >= 1f)
            {
                _isPopping = false;
                _weaponVisual.transform.localScale = _baseLocalScale;
                _weaponVisual.transform.localPosition = _baseLocalPos + _popupOffset;
            }
        }
        else if (_isRetracting)
        {
            _animTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_animTimer / _retractDuration);
            float eased = 1f - EaseInBack(t);

            _weaponVisual.transform.localScale = _baseLocalScale * eased;
            _weaponVisual.transform.localPosition = _baseLocalPos + _popupOffset * eased;

            if (t >= 1f)
                EndAttack();
        }
    }

    private float EaseOutBack(float t)
    {
        float c = (_overshootScale - 1f) * 1.70158f + 1.70158f;
        return 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseInBack(float t)
    {
        float c = 1.70158f;
        return (c + 1f) * t * t * t - c * t * t;
    }

    // --- Billboard ---

    private void ApplyBillboard()
    {
        if (!_weaponVisual.activeSelf) return;

        Transform camTransform = _mainCamera.transform;
        _weaponSprite.transform.rotation = Quaternion.LookRotation(
            camTransform.forward,
            camTransform.up
        );
    }

    // --- Public API ---

    public Vector3 GetAimDirection() => _aimDirection;
    public bool IsAttacking() => _isAttacking;
}
