using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Chase,
    Attack,
    Stun,
    Die
}

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected float _maxHp = 100f;
    [SerializeField] protected float _moveSpeed = 3f;
    [SerializeField] protected float _attackRange = 1.5f;

    [Header("Toy Knockback (3cm Scale)")]
    [SerializeField] protected float _knockbackForce = 3.5f;
    [SerializeField] protected float _knockbackUpForce = 1.2f;
    [SerializeField] protected float _bounceDamping = 0.4f;
    [SerializeField] protected int _bounceCount = 2;
    [SerializeField] protected float _mass = 0.3f;

    [Header("Miniature Physics")]
    [SerializeField] protected float _gravityScale = 0.5f;
    [SerializeField] protected float _dustParticleScale = 0.02f;

    [Header("Stun")]
    [SerializeField] protected float _stunDuration = 0.5f;

    [Header("Visual")]
    [SerializeField] protected Material _flashMaterial;
    [SerializeField] protected SpriteRenderer _spriteRenderer;

    // Events
    public event Action<EnemyState> OnStateChanged;
    public event Action<float> OnDamageTaken;
    public event Action OnDeath;

    protected NavMeshAgent _agent;
    protected Rigidbody _rb;
    [SerializeField]
    protected Transform _player;
    protected Camera _mainCamera;

    protected EnemyState _currentState;
    protected float _currentHp;
    protected float _stunTimer;
    protected bool _isDead;
    private Coroutine _bounceCoroutine;

    // --- Lifecycle ---

    protected virtual void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        _mainCamera = Camera.main;

        SetupAgent();
        SetupRigidbody();

        if (_flashMaterial != null)
        {
            _spriteRenderer.material = _flashMaterial;
        }
    }

    protected virtual void Start()
    {
        FindPlayer();

        _currentHp = _maxHp;
        TransitionTo(EnemyState.Chase);

        // Initialize health bar if present
        var healthBar = GetComponentInChildren<EnemyHealthBar>(true);
        if (healthBar != null) healthBar.Init(this);

    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            return;
        }

        if (PlayerStats.Instance != null)
        {
            _player = PlayerStats.Instance.transform;
        }
    }

    protected virtual void Update()
    {
        if (_isDead) return;

        ApplyBillboard();

        switch (_currentState)
        {
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Stun:
                UpdateStun();
                break;
        }
    }

    // --- Setup ---

    private void SetupAgent()
    {
        _agent.speed = _moveSpeed;
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
    }

    private void SetupRigidbody()
    {
        _rb.mass = _mass;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        Physics.gravity = new Vector3(0f, -9.81f * _gravityScale, 0f);
    }

    // --- FSM ---

    protected void TransitionTo(EnemyState newState)
    {
        if (_isDead && newState != EnemyState.Die) return;

        ExitState(_currentState);
        _currentState = newState;
        EnterState(newState);
        OnStateChanged?.Invoke(newState);
    }

    protected virtual void EnterState(EnemyState state)
    {
        bool canControlAgent = _agent != null && _agent.enabled && _agent.isOnNavMesh;
        switch (state)
        {
            case EnemyState.Chase:
                if (canControlAgent) _agent.isStopped = false;
                break;
            
            case EnemyState.Attack:
                if (canControlAgent) _agent.isStopped = true;
                OnEnterAttack();
                break;
            
            case EnemyState.Stun:
               
                if (canControlAgent) _agent.isStopped = true;
                _stunTimer = _stunDuration;
                break;
            
            case EnemyState.Die:
                if (canControlAgent) _agent.isStopped = true;
                _agent.enabled = false; 
                OnEnterDie();
                break;
        }
    }

    protected virtual void ExitState(EnemyState state) { }

    protected virtual void UpdateChase()
    {
        if (_player == null)
        {
            FindPlayer();
            if (_player == null) return;
        }

        
        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.SetDestination(_player.position);
        }

        UpdateFacingDirection();

        if (GetDistanceToPlayer() <= _attackRange)
        {
            TransitionTo(EnemyState.Attack);
        }
    }

    protected abstract void OnEnterAttack();
    protected abstract void UpdateAttack();

    protected virtual void UpdateStun()
    {
        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0f)
        {
            TransitionTo(EnemyState.Chase);
        }
    }

    protected virtual void OnEnterDie()
    {
        _isDead = true;
        OnDeath?.Invoke();
    }

    // --- Damage + Toy Knockback ---

    public virtual void TakeDamage(float damage, Vector3 hitDirection)
    {
        if (_isDead) return;

        _currentHp -= damage;
        OnDamageTaken?.Invoke(damage);

        ApplyToyKnockback(hitDirection, damage);

        if (_currentHp <= 0f)
        {
            _currentHp = 0f;
            TransitionTo(EnemyState.Die);
        }
        else if (_currentState != EnemyState.Stun)
        {
            TransitionTo(EnemyState.Stun);
        }
    }

    private float _groundY;

    private void ApplyToyKnockback(Vector3 hitDirection, float damage)
    {
        // Remember ground level before leaving NavMesh
        _groundY = transform.position.y;

        // Disable NavMesh during knockback so physics can take over
        _agent.enabled = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;

        // Lightweight plastic/tin can launch — strong initial pop with upward arc
        float damageMult = Mathf.Clamp(damage / 20f, 0.8f, 2.5f);
        Vector3 force = hitDirection.normalized * _knockbackForce * damageMult;
        force.y = _knockbackUpForce * damageMult;

        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(force, ForceMode.Impulse);

        // Start bounce sequence
        if (_bounceCoroutine != null)
            StopCoroutine(_bounceCoroutine);
        _bounceCoroutine = StartCoroutine(BounceRoutine());
    }

    private IEnumerator BounceRoutine()
    {
        int bounces = 0;

        // Wait until airborne phase ends (moving downward and near ground)
        yield return new WaitForSeconds(0.05f);

        while (bounces < _bounceCount)
        {
            // Wait until falling and close to ground
            yield return new WaitUntil(() => _rb.linearVelocity.y < -0.1f);
            yield return new WaitUntil(() => transform.position.y <= _groundY + 0.05f);

            // Bounce: reflect Y velocity with damping
            Vector3 vel = _rb.linearVelocity;
            float bounceY = Mathf.Abs(vel.y) * _bounceDamping;

            if (bounceY < 0.2f) break;

            _rb.linearVelocity = new Vector3(
                vel.x * _bounceDamping,
                bounceY,
                vel.z * _bounceDamping
            );

            bounces++;
        }

        // Settle: snap to ground, re-enable NavMesh
        _rb.linearVelocity = Vector3.zero;
        _rb.useGravity = false;
        Vector3 pos = transform.position;
        pos.y = _groundY;
        transform.position = pos;

        _rb.isKinematic = true;
        if (!_isDead)
        {
            _agent.enabled = true;
            _agent.Warp(transform.position);
        }

        _bounceCoroutine = null;
    }

    // --- Billboard ---

    protected void ApplyBillboard()
    {
        Transform camTransform = _mainCamera.transform;
        _spriteRenderer.transform.rotation = Quaternion.LookRotation(
            camTransform.forward,
            camTransform.up
        );
    }

    protected void UpdateFacingDirection()
    {
        if (_player == null) return;
        _spriteRenderer.flipX = (_player.position - transform.position).x < 0f;
    }

    // --- Helpers ---

    protected float GetDistanceToPlayer()
    {
        if (_player == null) return float.MaxValue;
        Vector3 diff = _player.position - transform.position;
        diff.y = 0f;
        return diff.magnitude;
    }

    public SpriteRenderer GetSpriteRenderer() => _spriteRenderer;
    public float GetHpRatio() => _currentHp / _maxHp;
    public EnemyState GetCurrentState() => _currentState;
    public float GetDustParticleScale() => _dustParticleScale;
}
