using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [Header("Magnet")]
    [SerializeField] private float _magnetRange = 3f;
    [SerializeField] private float _pickupRange = 0.3f;
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _acceleration = 20f;

    [Header("Spawn Pop")]
    [SerializeField] private float _popForce = 0.3f;
    [SerializeField] private float _popUpForce = 0.5f;
    [SerializeField] private float _landTimeout = 1.5f;

    private Transform _player;
    private Rigidbody _rb;
    private bool _landed;
    private float _currentSpeed;
    private float _spawnTime;
    private float _groundY;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _spawnTime = Time.time;
        _groundY = transform.position.y;

        Billboard.Ensure(gameObject);

        // Random pop on spawn
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 force = new Vector3(randomDir.x * _popForce, _popUpForce, randomDir.y * _popForce);
        _rb.AddForce(force, ForceMode.Impulse);

        // Find player
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
        else if (PlayerStats.Instance != null)
            _player = PlayerStats.Instance.transform;
    }

    private void Update()
    {
        if (_player == null) return;

        // Wait until landed
        if (!_landed)
        {
            bool timedOut = Time.time - _spawnTime > _landTimeout;
            bool falling = Time.time - _spawnTime > 0.1f
                           && _rb.linearVelocity.y < 0f
                           && transform.position.y <= _groundY + 0.05f;

            if (falling || timedOut)
            {
                _landed = true;
                _rb.linearVelocity = Vector3.zero;
                _rb.isKinematic = true;

                Vector3 pos = transform.position;
                pos.y = _groundY;
                transform.position = pos;
            }
            return;
        }

        // Magnet pull
        Vector3 diff = _player.position - transform.position;
        float dist = diff.magnitude;

        if (dist <= _pickupRange)
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddCoins(1);
            Destroy(gameObject);
            return;
        }

        float effectiveMagnetRange = _magnetRange +
            (PlayerStats.Instance != null ? PlayerStats.Instance.BonusCollectionRange : 0f);

        if (dist <= effectiveMagnetRange)
        {
            _currentSpeed += _acceleration * Time.deltaTime;
            _currentSpeed = Mathf.Min(_currentSpeed, _moveSpeed);
            transform.position += diff.normalized * _currentSpeed * Time.deltaTime;
        }
    }
}
