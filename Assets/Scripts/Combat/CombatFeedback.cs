using System;
using System.Collections;
using UnityEngine;

public class CombatFeedback : MonoBehaviour
{
    public static CombatFeedback Instance { get; private set; }

    [Header("Hit-Stop")]
    [SerializeField] private float _hitStopDuration = 0.05f;
    [SerializeField] private float _critHitStopDuration = 0.1f;

    [Header("Camera Shake — Directional")]
    [SerializeField] private float _shakeIntensity = 0.05f;
    [SerializeField] private float _shakeDuration = 0.1f;
    [SerializeField] private float _directionalBias = 0.7f;
    [SerializeField] private float _shakeFrequency = 40f;

    [Header("White Flash")]
    [SerializeField] private float _flashDuration = 0.08f;
    private static readonly int FLASH_AMOUNT = Shader.PropertyToID("_FlashAmount");

    [Header("Critical Glitch")]
    [SerializeField] private float _glitchDuration = 0.15f;
    [SerializeField] private float _glitchSliceIntensity = 0.06f;
    [SerializeField] private float _glitchFrequency = 60f;
    private static readonly int GLITCH_INTENSITY = Shader.PropertyToID("_GlitchIntensity");

    [Header("Hit Particle (3cm Miniature Scale)")]
    [SerializeField] private ParticleSystem _hitParticlePrefab;
    [SerializeField] private ParticleSystem _critParticlePrefab;
    [SerializeField] private float _particleBaseScale = 0.012f;
    [SerializeField] private float _particleScalePerDamage = 0.001f;

    [Header("Critical Hit")]
    [SerializeField] private float _critChance = 0.2f;
    [SerializeField] private float _critMultiplier = 2f;

    // Sound events — subscribe from AudioManager
    public event Action<Vector3, float> OnHitSoundRequested;
    public event Action<Vector3> OnPartClangRequested;
    public event Action<Vector3, float> OnCriticalHitSoundRequested;

    private Camera _mainCamera;
    private Transform _cameraTransform;
    private Vector3 _cameraBaseLocalPos;
    private Coroutine _hitStopCoroutine;
    private Coroutine _shakeCoroutine;
    private float _savedTimeScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _mainCamera = Camera.main;
        _cameraTransform = _mainCamera.transform;
        _cameraBaseLocalPos = _cameraTransform.localPosition;
    }

    // --- Main Entry Points ---

    public HitResult ProcessHit(float baseDamage)
    {
        bool isCrit = UnityEngine.Random.value < _critChance;
        float finalDamage = isCrit ? baseDamage * _critMultiplier : baseDamage;

        return new HitResult
        {
            Damage = finalDamage,
            IsCritical = isCrit
        };
    }

    public void PlayHitFeedback(EnemyBase enemy, Vector3 hitPoint, Vector3 hitDirection, HitResult result)
    {
        // 1. Hit-Stop
        float stopDuration = result.IsCritical ? _critHitStopDuration : _hitStopDuration;
        PlayHitStop(stopDuration);

        // 2. Directional Camera Shake
        float shakeMult = result.IsCritical ? 2.5f : Mathf.Clamp(result.Damage / 30f, 0.5f, 1.5f);
        PlayDirectionalShake(hitDirection, shakeMult);

        // 3. White Flash
        PlayWhiteFlash(enemy);

        // 4. Critical Glitch Distortion
        if (result.IsCritical)
        {
            PlayCriticalGlitch(enemy);
        }

        // 5. Hit Particles
        SpawnHitParticle(hitPoint, hitDirection, result);

        // 6. Sound Events
        float intensity = Mathf.Clamp01(result.Damage / 50f);
        OnHitSoundRequested?.Invoke(hitPoint, intensity);
        OnPartClangRequested?.Invoke(hitPoint);

        if (result.IsCritical)
        {
            OnCriticalHitSoundRequested?.Invoke(hitPoint, intensity);
        }
    }

    // --- 1. Hit-Stop ---

    private void PlayHitStop(float duration)
    {
        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
            // 이미 정지 중이라면 _savedTimeScale을 갱신하지 않고 유지합니다.
        }
        else
        {
            // 처음 정지하는 경우에만 현재의 정상적인 TimeScale을 저장합니다.
            _savedTimeScale = Time.timeScale;
        }

        _hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        if (Time.timeScale > 0.001f)
        {
            _savedTimeScale = Time.timeScale;
        }
        Time.timeScale = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = _savedTimeScale;
        _hitStopCoroutine = null;
    }

    // --- 2. Directional Camera Shake ---

    public void PlayShake(float intensityMult)
    {
        PlayDirectionalShake(UnityEngine.Random.onUnitSphere, intensityMult);
    }

    private void PlayDirectionalShake(Vector3 hitDirection, float intensityMult)
    {
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = StartCoroutine(DirectionalShakeRoutine(hitDirection, intensityMult));
    }

    private IEnumerator DirectionalShakeRoutine(Vector3 hitDirection, float intensityMult)
    {
        float elapsed = 0f;
        float intensity = _shakeIntensity * intensityMult;

        // Project hit direction onto camera's local XY plane
        Vector3 camRight = _cameraTransform.right;
        Vector3 camUp = _cameraTransform.up;
        float biasX = Vector3.Dot(hitDirection, camRight);
        float biasY = Vector3.Dot(hitDirection, camUp);
        Vector2 bias = new Vector2(biasX, biasY).normalized;

        while (elapsed < _shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / _shakeDuration;

            // Sharp decay for snappy feel
            float decay = (1f - t) * (1f - t);

            float seed = elapsed * _shakeFrequency;
            float noiseX = Mathf.PerlinNoise(seed, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, seed) * 2f - 1f;

            // Blend random noise with directional bias
            float offsetX = Mathf.Lerp(noiseX, bias.x, _directionalBias) * intensity * decay;
            float offsetY = Mathf.Lerp(noiseY, bias.y, _directionalBias) * intensity * decay;

            _cameraTransform.localPosition = _cameraBaseLocalPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        _cameraTransform.localPosition = _cameraBaseLocalPos;
        _shakeCoroutine = null;
    }

    // --- 3. White Flash ---

    private void PlayWhiteFlash(EnemyBase enemy)
    {
        SpriteRenderer sr = enemy.GetSpriteRenderer();
        if (sr == null) return;

        StartCoroutine(FlashRoutine(sr));
    }

    private IEnumerator FlashRoutine(SpriteRenderer sr)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(FLASH_AMOUNT, 1f);
        sr.SetPropertyBlock(mpb);

        float elapsed = 0f;
        while (elapsed < _flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out
        float fadeElapsed = 0f;
        float fadeDuration = _flashDuration * 0.5f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fadeElapsed / fadeDuration);

            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(FLASH_AMOUNT, 1f - t);
            sr.SetPropertyBlock(mpb);

            yield return null;
        }

        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(FLASH_AMOUNT, 0f);
        sr.SetPropertyBlock(mpb);
    }

    // --- 4. Critical Glitch Distortion ---

    private void PlayCriticalGlitch(EnemyBase enemy)
    {
        SpriteRenderer sr = enemy.GetSpriteRenderer();
        if (sr == null) return;

        StartCoroutine(GlitchRoutine(sr));
    }

    private IEnumerator GlitchRoutine(SpriteRenderer sr)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        float elapsed = 0f;

        while (elapsed < _glitchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / _glitchDuration;

            // Pulsating glitch — random intensity spikes that decay
            float pulse = Mathf.Abs(Mathf.Sin(elapsed * _glitchFrequency));
            float decay = 1f - t;
            float glitchValue = pulse * _glitchSliceIntensity * decay;

            // Random horizontal slice offset for distortion feel
            float sliceOffset = (Mathf.PerlinNoise(elapsed * 80f, 0f) * 2f - 1f) * glitchValue;

            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(GLITCH_INTENSITY, glitchValue);
            sr.SetPropertyBlock(mpb);

            // Physical jitter — sprite shakes in local space
            sr.transform.localPosition += new Vector3(sliceOffset, 0f, 0f);

            yield return null;

            // Reset position each frame
            sr.transform.localPosition -= new Vector3(sliceOffset, 0f, 0f);
        }

        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(GLITCH_INTENSITY, 0f);
        sr.SetPropertyBlock(mpb);
    }

    // --- 5. Hit Particles (Miniature Scale) ---

    private void SpawnHitParticle(Vector3 position, Vector3 direction, HitResult result)
    {
        ParticleSystem prefab = result.IsCritical && _critParticlePrefab != null
            ? _critParticlePrefab
            : _hitParticlePrefab;

        if (prefab == null) return;

        Quaternion rotation = Quaternion.LookRotation(direction);
        ParticleSystem ps = Instantiate(prefab, position, rotation);

        float scale = _particleBaseScale + _particleScalePerDamage * result.Damage;
        int burstCount = result.IsCritical ? 18 : 8;

        var main = ps.main;
        main.startSizeMultiplier = scale;
        main.startSpeedMultiplier = scale * (result.IsCritical ? 80f : 45f);
        main.startLifetimeMultiplier = 0.12f;

        // Dense, tiny, sharp sparks — 3cm figurine debris
        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)burstCount)
        });

        var shape = ps.shape;
        shape.angle = result.IsCritical ? 35f : 25f;

        ps.Play();
        Destroy(ps.gameObject, 0.3f);
    }
}

public struct HitResult
{
    public float Damage;
    public bool IsCritical;
}
