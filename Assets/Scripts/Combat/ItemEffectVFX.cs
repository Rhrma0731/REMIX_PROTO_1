using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// T-M-A 아이템 이펙트 전용 VFX 매니저.
///
/// 기존 CombatFeedback(흰색 플래시)과 차별화된 컬러 기반 시각 피드백을 제공한다.
/// - 데미지/상태이상: 적 스프라이트에 컬러 플래시 + 파티클 버스트
/// - 회복: 초록 상승 파티클
/// - 방어: 시안 방어 버스트
///
/// [씬 배치] GameManagers 또는 Player 루트에 컴포넌트 추가.
/// Instance가 없으면 자동 생성된다 (lazy singleton).
/// </summary>
public class ItemEffectVFX : MonoBehaviour
{
    public static ItemEffectVFX Instance { get; private set; }

    [Header("Flash")]
    [SerializeField] private float _flashDuration = 0.12f;

    [Header("Particle (3cm Miniature Scale)")]
    [SerializeField] private float _particleBaseSize = 0.008f;
    [SerializeField] private float _particleSpeed = 0.5f;
    [SerializeField] private float _particleLifetime = 0.25f;

    // Shader property IDs (캐시)
    private static readonly int FLASH_AMOUNT = Shader.PropertyToID("_FlashAmount");
    private static readonly int FLASH_COLOR = Shader.PropertyToID("_FlashColor");

    // 상태이상별 컬러 맵
    private static readonly Dictionary<string, Color> STATUS_COLORS = new Dictionary<string, Color>
    {
        { "ST_BURN",   new Color(1.0f, 0.4f, 0.1f) },
        { "ST_SLOW",   new Color(0.3f, 0.7f, 1.0f) },
        { "ST_STUN",   new Color(1.0f, 0.9f, 0.2f) },
        { "ST_GLITCH", new Color(0.8f, 0.2f, 1.0f) },
        { "ST_DEATH",  new Color(0.8f, 0.0f, 0.0f) },
        { "ST_CHAIN",  new Color(0.2f, 0.6f, 1.0f) },
    };

    private static readonly Color DEFAULT_DAMAGE_COLOR = new Color(1.0f, 0.6f, 0.1f); // 주황
    private static readonly Color HEAL_COLOR = new Color(0.2f, 1.0f, 0.3f);           // 초록
    private static readonly Color SHIELD_COLOR = new Color(0.3f, 0.9f, 1.0f);         // 시안
    private static readonly Color REVIVE_COLOR = new Color(1.0f, 0.95f, 0.4f);        // 금색

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>lazy singleton — 씬에 없으면 자동 생성</summary>
    public static ItemEffectVFX EnsureInstance()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("ItemEffectVFX");
        Instance = go.AddComponent<ItemEffectVFX>();
        return Instance;
    }

    // ── Public API ──────────────────────────────────────────────────

    /// <summary>T-M-A 데미지 액션 발동 시 적에게 주황 컬러 플래시 + 파티클</summary>
    public void PlayDamageFlash(EnemyBase enemy)
    {
        PlayDamageFlash(enemy, DEFAULT_DAMAGE_COLOR);
    }

    public void PlayDamageFlash(EnemyBase enemy, Color color)
    {
        if (enemy == null || enemy.IsDead) return;

        SpriteRenderer sr = enemy.GetSpriteRenderer();
        if (sr != null)
            StartCoroutine(ColorFlashRoutine(sr, color));

        SpawnBurstParticle(enemy.transform.position, color, 6);
    }

    /// <summary>T-M-A 상태이상 액션 발동 시 상태별 컬러 플래시</summary>
    public void PlayStatusFlash(EnemyBase enemy, string statusID)
    {
        if (enemy == null || enemy.IsDead) return;

        Color color = GetStatusColor(statusID);

        SpriteRenderer sr = enemy.GetSpriteRenderer();
        if (sr != null)
            StartCoroutine(ColorFlashRoutine(sr, color));

        SpawnBurstParticle(enemy.transform.position, color, 4);
    }

    /// <summary>T-M-A 회복 액션 발동 시 초록 상승 파티클</summary>
    public void PlayHealEffect(Transform target)
    {
        if (target == null) return;
        SpawnRisingParticle(target.position, HEAL_COLOR, 8);
    }

    /// <summary>T-M-A 방어 액션 발동 시 시안 방어 버스트</summary>
    public void PlayShieldEffect(Transform target)
    {
        if (target == null) return;
        SpawnBurstParticle(target.position, SHIELD_COLOR, 10);
    }

    /// <summary>T-M-A 부활 액션 발동 시 금색 상승 버스트</summary>
    public void PlayReviveEffect(Transform target)
    {
        if (target == null) return;
        SpawnRisingParticle(target.position, REVIVE_COLOR, 12);
        SpawnBurstParticle(target.position, REVIVE_COLOR, 8);
    }

    /// <summary>SpawnExplosionAction 발동 시 주황 폭발 버스트.
    /// 반경에 비례하여 파티클 수와 속도를 스케일한다.</summary>
    public void PlayExplosionEffect(Vector3 center, float radius)
    {
        // 반경에 비례한 파티클 수 (최소 8, 최대 30)
        int count = Mathf.Clamp(Mathf.RoundToInt(radius * 6), 8, 30);
        SpawnExplosionParticle(center, DEFAULT_DAMAGE_COLOR, count, radius);
    }

    // ── 컬러 플래시 (SpriteFlash 셰이더 활용) ──────────────────────

    private IEnumerator ColorFlashRoutine(SpriteRenderer sr, Color color)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        // 플래시 시작: 색상 설정 + FlashAmount = 1
        sr.GetPropertyBlock(mpb);
        mpb.SetColor(FLASH_COLOR, color);
        mpb.SetFloat(FLASH_AMOUNT, 1f);
        sr.SetPropertyBlock(mpb);

        // 페이드 아웃
        float elapsed = 0f;
        while (elapsed < _flashDuration)
        {
            if (sr == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _flashDuration);

            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(FLASH_AMOUNT, 1f - t);
            sr.SetPropertyBlock(mpb);

            yield return null;
        }

        // 클린업: 플래시 완전 해제 + 색상 복원 (흰색)
        if (sr == null) yield break;
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(FLASH_AMOUNT, 0f);
        mpb.SetColor(FLASH_COLOR, Color.white);
        sr.SetPropertyBlock(mpb);
    }

    // ── 런타임 파티클 생성 ──────────────────────────────────────────

    /// <summary>전방향 버스트 파티클 (데미지/상태이상/방어용)</summary>
    private void SpawnBurstParticle(Vector3 position, Color color, int count)
    {
        GameObject go = new GameObject("TMA_BurstVFX");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.startSize = _particleBaseSize;
        main.startSpeed = _particleSpeed;
        main.startLifetime = _particleLifetime;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.loop = false;
        main.playOnAwake = false;
        main.gravityModifier = 0.3f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)count)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;

        // 크기 감소 (페이드 아웃 효과)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // 렌더러 설정 — 기본 파티클 머티리얼 사용
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", color);

        ps.Play();
        Destroy(go, _particleLifetime + 0.1f);
    }

    /// <summary>폭발 파티클 — 반경에 맞춰 방사형으로 퍼지는 넓은 버스트</summary>
    private void SpawnExplosionParticle(Vector3 position, Color color, int count, float radius)
    {
        GameObject go = new GameObject("TMA_ExplosionVFX");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.startSize = _particleBaseSize * Mathf.Max(1f, radius * 0.5f);
        main.startSpeed = _particleSpeed * Mathf.Max(1f, radius * 0.8f);
        main.startLifetime = _particleLifetime * 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.loop = false;
        main.playOnAwake = false;
        main.gravityModifier = 0.15f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)count)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.01f, radius * 0.1f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", color);

        ps.Play();
        Destroy(go, _particleLifetime * 1.5f + 0.1f);
    }

    /// <summary>상승 파티클 (회복용 — 아래에서 위로 올라감)</summary>
    private void SpawnRisingParticle(Vector3 position, Color color, int count)
    {
        GameObject go = new GameObject("TMA_RisingVFX");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.startSize = _particleBaseSize * 0.8f;
        main.startSpeed = _particleSpeed * 0.6f;
        main.startLifetime = _particleLifetime * 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.loop = false;
        main.playOnAwake = false;
        main.gravityModifier = -0.2f; // 위로 올라감

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)count)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(-90f, 0f, 0f); // 위를 향함

        // 크기 감소
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // 렌더러
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", color);

        ps.Play();
        Destroy(go, _particleLifetime * 1.5f + 0.1f);
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────

    private static Color GetStatusColor(string statusID)
    {
        if (!string.IsNullOrEmpty(statusID) && STATUS_COLORS.TryGetValue(statusID, out Color color))
            return color;
        return Color.white;
    }
}
