using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private Canvas _canvas;

    private EnemyBase _enemy;
    private Transform _cameraTransform;
    private bool _initialized;

    public void Init(EnemyBase enemy)
    {
        _enemy = enemy;
        _cameraTransform = Camera.main.transform;

        if (_canvas != null)
            _canvas.enabled = false;

        _enemy.OnDamageTaken += HandleDamageTaken;
        _enemy.OnDeath += HandleDeath;

        _initialized = true;
    }

    private void HandleDamageTaken(float damage)
    {
        if (_canvas != null && !_canvas.enabled)
            _canvas.enabled = true;

        if (_fillImage != null && _enemy != null)
            _fillImage.fillAmount = _enemy.GetHpRatio();
    }

    private void HandleDeath()
    {
        if (_canvas != null)
            _canvas.enabled = false;
    }

    private void LateUpdate()
    {
        if (!_initialized || _cameraTransform == null) return;

        transform.rotation = Quaternion.LookRotation(
            _cameraTransform.forward,
            _cameraTransform.up
        );
    }

    private void OnDestroy()
    {
        if (_enemy != null)
        {
            _enemy.OnDamageTaken -= HandleDamageTaken;
            _enemy.OnDeath -= HandleDeath;
        }
    }
}
