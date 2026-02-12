using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5.5f;
    [SerializeField] private InputActionAsset _inputActions;

    private InputAction _moveAction;
    private Vector3 _moveDirection;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        var playerMap = _inputActions.FindActionMap("Player");
        _moveAction = playerMap.FindAction("Move");
    }

    private void OnEnable()
    {
        _moveAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
    }

    private void Update()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        _moveDirection = new Vector3(input.x, 0f, input.y).normalized;
    }

    private void FixedUpdate()
    {
        if (_moveDirection.sqrMagnitude < 0.01f)
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 velocity = _moveDirection * _moveSpeed;
        _rb.linearVelocity = new Vector3(velocity.x, _rb.linearVelocity.y, velocity.z);
    }
}
