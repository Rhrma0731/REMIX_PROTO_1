using UnityEngine;

public class StickyHand : EnemyBase
{
    [Header("StickyHand — Wind-Up Attack")]
    [SerializeField] private float _windUpDuration = 0.8f;
    [SerializeField] private float _slamDuration = 0.15f;
    [SerializeField] private float _slamRecovery = 0.4f;
    [SerializeField] private float _slamDamage = 15f;
    [SerializeField] private float _slamRadius = 1.0f;

    [Header("Wind-Up Bobbing")]
    [SerializeField] private float _bobAmplitude = 0.03f;
    [SerializeField] private float _bobFrequency = 12f;

    [Header("Slam VFX")]
    [SerializeField] private ParticleSystem _slamDustVFX;
    [SerializeField] private LayerMask _playerLayer;

    private enum SlamPhase { WindUp, Slam, Recovery }

    private SlamPhase _slamPhase;
    private float _phaseTimer;
    private float _currentBobOffsetY;
    private Vector3 _slamTarget;

    protected override void Awake()
    {
        base.Awake();

        // Override base stats for StickyHand
        _maxHp = 60f;
        _moveSpeed = 3.0f;
        _attackRange = 1.5f;

        // Miniature toy physics
        _knockbackForce = 1.5f;
        _gravityScale = 0.4f;
        _dustParticleScale = 0.015f;

        if (_slamDustVFX != null)
        {
            var main = _slamDustVFX.main;
            main.startSizeMultiplier = _dustParticleScale;
        }
    }

    protected override void Start()
    {
        base.Start();

        // Re-apply speed after base.Start
        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
        }
    }

    // --- Attack FSM ---

    protected override void OnEnterAttack()
    {
        Debug.Log("[StickyHand] OnEnterAttack — WindUp started");
        _slamPhase = SlamPhase.WindUp;
        _phaseTimer = _windUpDuration;
        _currentBobOffsetY = 0f;

        // Lock slam target at the moment attack begins
        if (_player != null)
        {
            _slamTarget = _player.position;
        }
    }

    protected override void UpdateAttack()
    {
        _phaseTimer -= Time.deltaTime;

        switch (_slamPhase)
        {
            case SlamPhase.WindUp:
                AnimateWindUp();
                if (_phaseTimer <= 0f) BeginSlam();
                break;

            case SlamPhase.Slam:
                if (_phaseTimer <= 0f) FinishSlam();
                break;

            case SlamPhase.Recovery:
                if (_phaseTimer <= 0f) EndAttack();
                break;
        }
    }

    // --- Wind-Up: body bobbing ---

    private void AnimateWindUp()
    {
        float elapsed = _windUpDuration - _phaseTimer;

        // Accelerating bob — frequency increases as slam approaches
        float urgency = 1f + (elapsed / _windUpDuration) * 2f;
        float newBobY = Mathf.Sin(elapsed * _bobFrequency * urgency) * _bobAmplitude;

        // Apply delta offset so we don't overwrite NavMeshAgent position
        float deltaY = newBobY - _currentBobOffsetY;
        transform.position += new Vector3(0f, deltaY, 0f);
        _currentBobOffsetY = newBobY;
    }

    // --- Slam ---

    private void BeginSlam()
    {
        _slamPhase = SlamPhase.Slam;
        _phaseTimer = _slamDuration;

        // Remove remaining bob offset
        transform.position -= new Vector3(0f, _currentBobOffsetY, 0f);
        _currentBobOffsetY = 0f;

        // Damage check
        Collider[] hits = Physics.OverlapSphere(transform.position, _slamRadius, _playerLayer);
        Debug.Log($"[StickyHand] BeginSlam — pos={transform.position}, radius={_slamRadius}, layerMask={_playerLayer.value}, hits={hits.Length}");
        foreach (Collider hit in hits)
        {
            Debug.Log($"[StickyHand] Hit: {hit.name}, PlayerStats.Instance={PlayerStats.Instance != null}");
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.TakeDamage(_slamDamage);
            }
        }

        // Miniature dust burst
        if (_slamDustVFX != null)
        {
            _slamDustVFX.Play();
        }
    }

    private void FinishSlam()
    {
        _slamPhase = SlamPhase.Recovery;
        _phaseTimer = _slamRecovery;
    }

    private void EndAttack()
    {
        // If player still in range, attack again; otherwise chase
        float distance = GetDistanceToPlayer();
        if (distance <= _attackRange)
        {
            TransitionTo(EnemyState.Attack);
        }
        else
        {
            TransitionTo(EnemyState.Chase);
        }
    }

    // --- Die Override ---

    protected override void OnEnterDie()
    {
        base.OnEnterDie();

        // Remove any remaining bob offset
        transform.position -= new Vector3(0f, _currentBobOffsetY, 0f);
        _currentBobOffsetY = 0f;

        // TODO: drop parts / play death animation / return to pool
        Destroy(gameObject, 1f);
    }
}
