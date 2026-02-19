using System.Collections;
using UnityEngine;

public class PlayerHitFeedback : MonoBehaviour
{
    [Header("Flash Blink")]
    [SerializeField] private float _flashDuration = 0.06f;
    [SerializeField] private int _blinkCount = 3;
    [SerializeField] private float _blinkInterval = 0.06f;

    [Header("Camera Shake")]
    [SerializeField] private float _shakeIntensity = 1.5f;

    private static readonly int FLASH_AMOUNT = Shader.PropertyToID("_FlashAmount");

    private SpriteRenderer[] _spriteRenderers;
    private Coroutine _flashCoroutine;
    private float _previousHp;
    private bool _subscribed;

    private void Awake()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_subscribed || PlayerStats.Instance == null) return;

        _previousHp = PlayerStats.Instance.CurrentHp;
        PlayerStats.Instance.OnHpChanged += OnHpChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || PlayerStats.Instance == null) return;

        PlayerStats.Instance.OnHpChanged -= OnHpChanged;
        _subscribed = false;
    }

    private void OnHpChanged(float current, float max)
    {
        if (current < _previousHp)
        {
            PlayHitEffects();
        }
        _previousHp = current;
    }

    private void PlayHitEffects()
    {
        // Flash blink
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashBlinkRoutine());

        // Camera shake via CombatFeedback
        if (CombatFeedback.Instance != null)
        {
            CombatFeedback.Instance.PlayShake(_shakeIntensity);
        }
    }

    private IEnumerator FlashBlinkRoutine()
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        for (int i = 0; i < _blinkCount; i++)
        {
            SetFlash(mpb, 1f);

            float elapsed = 0f;
            while (elapsed < _flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetFlash(mpb, 0f);

            elapsed = 0f;
            while (elapsed < _blinkInterval)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        SetFlash(mpb, 0f);
        _flashCoroutine = null;
    }

    private void SetFlash(MaterialPropertyBlock mpb, float amount)
    {
        foreach (var sr in _spriteRenderers)

        {

            if (sr == null) continue;

            sr.GetPropertyBlock(mpb);

            mpb.SetFloat(FLASH_AMOUNT, amount);
            
            sr.SetPropertyBlock(mpb);
        }
    }
}

