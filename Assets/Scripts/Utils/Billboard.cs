using UnityEngine;

/// <summary>
/// Attach to any GameObject with a 2D sprite to make it always face the camera.
/// Uses LateUpdate to run after all camera movement is resolved.
/// </summary>
public class Billboard : MonoBehaviour
{
    public enum Mode
    {
        FullRotation,
        YAxisOnly
    }

    [SerializeField] private Mode _mode = Mode.FullRotation;
    [SerializeField] private bool _flipWithMovement;

    private Camera _mainCamera;
    private SpriteRenderer _spriteRenderer;
    private Vector3 _lastPosition;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (_mainCamera == null) return;

        ApplyBillboard();

        if (_flipWithMovement && _spriteRenderer != null)
        {
            UpdateFlip();
        }

        _lastPosition = transform.position;
    }

    private void ApplyBillboard()
    {
        Transform cam = _mainCamera.transform;

        switch (_mode)
        {
            case Mode.FullRotation:
                transform.rotation = Quaternion.LookRotation(cam.forward, cam.up);
                break;

            case Mode.YAxisOnly:
                Vector3 forward = cam.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
                break;
        }
    }

    private void UpdateFlip()
    {
        Vector3 delta = transform.position - _lastPosition;
        if (Mathf.Abs(delta.x) > 0.001f)
        {
            _spriteRenderer.flipX = delta.x < 0f;
        }
    }

    /// <summary>
    /// Ensures the target GameObject has a Billboard component. Adds one if missing.
    /// Call this when dynamically spawning sprite objects.
    /// </summary>
    public static Billboard Ensure(GameObject target, Mode mode = Mode.FullRotation)
    {
        Billboard bb = target.GetComponent<Billboard>();
        if (bb == null)
        {
            bb = target.AddComponent<Billboard>();
            bb._mode = mode;
        }
        return bb;
    }
}
