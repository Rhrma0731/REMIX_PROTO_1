using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAppearance : MonoBehaviour
{
    public class BodyPartSlot
    {
        public BodyPart Part;
        public SpriteRenderer Renderer;
        public Sprite OriginalSprite;
        public int BaseSortingOrder;
        public Vector3 OriginalScale;
        public Color OriginalColor;
    }

    [Header("Bond Overlay")]
    [SerializeField] private string _overlaySortingLayer = "Player";

    [Header("Bond Impact Effect")]
    [SerializeField] private float _impactScalePunch = 1.4f;
    [SerializeField] private float _impactDuration = 0.15f;

    [Header("Flash Effect")]
    [Tooltip("흰색 플래시가 targetColor로 돌아오는 데 걸리는 시간(초)")]
    [SerializeField] private float _flashDuration = 0.1f;

    [Header("Shake Effect")]
    [Tooltip("흔들림 최대 강도 (localPosition 오프셋)")]
    [SerializeField] private float _shakeMagnitude = 0.12f;
    [Tooltip("흔들림이 완전히 감쇠될 때까지 걸리는 시간(초)")]
    [SerializeField] private float _shakeDuration = 0.35f;

    [Header("Glitch Effect")]
    [SerializeField] private float _glitchDuration = 0.15f;
    [SerializeField] private float _glitchIntensity = 0.6f;
    [SerializeField] private float _glitchJitterAmount = 0.03f;

    // Events
    public event Action<BodyPart, ItemData> OnPartChanged;
    public event Action OnBondAttachSoundRequested;
    public event Action<string> OnWeaponNameChanged;

    private Dictionary<BodyPart, BodyPartSlot> _slotMap;
    private Dictionary<BodyPart, ItemData> _equippedItems;
    private List<ItemData> _allEquippedItems = new List<ItemData>();
    private Vector3 _originalRootScale;

    [Header("Weapon Reference")]
    [SerializeField] private WeaponController _weaponController;

    // Auto-discovery mapping: child object name → BodyPart enum
    private static readonly Dictionary<string, BodyPart> NAME_TO_PART = new Dictionary<string, BodyPart>
    {
        { "Head", BodyPart.Head },
        { "Body", BodyPart.Body },
        { "ArmLeft", BodyPart.ArmLeft },
        { "ArmRight", BodyPart.ArmRight },
        { "Arm", BodyPart.ArmRight }, // fallback: single "Arm" maps to ArmRight
        { "Legs", BodyPart.Legs },
    };

    private void Awake()
    {
        _originalRootScale = transform.localScale;
        _slotMap = new Dictionary<BodyPart, BodyPartSlot>();
        _equippedItems = new Dictionary<BodyPart, ItemData>();

        AutoDiscoverSlots();
    }

    private void AutoDiscoverSlots()
    {
        foreach (var pair in NAME_TO_PART)
        {
            Transform child = transform.Find(pair.Key);
            if (child == null) continue;

            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            // Skip if this BodyPart is already registered (e.g., "Arm" fallback when "ArmRight" already found)
            if (_slotMap.ContainsKey(pair.Value)) continue;

            var slot = new BodyPartSlot
            {
                Part = pair.Value,
                Renderer = sr,
                OriginalSprite = sr.sprite,
                BaseSortingOrder = sr.sortingOrder,
                OriginalScale = sr.transform.localScale,
                OriginalColor = sr.color,
            };

            _slotMap[pair.Value] = slot;

            // Ensure every body part sprite has Billboard component
            Billboard.Ensure(sr.gameObject);
        }
    }

    /// <summary>
    /// Equip item: swap sprite, adjust sorting, play bond impact + glitch, apply stats.
    /// </summary>
    public void EquipItem(ItemData item)
    {
        if (!_slotMap.TryGetValue(item.TargetBodyPart, out BodyPartSlot slot)) return;

        // Track all equipped items
        _allEquippedItems.Add(item);

        // Swap sprite — AppearanceSprite가 없으면 원본 유지 (Scale/Color 효과는 계속 적용됨)
        if (item.AppearanceSprite != null)
        {
            slot.Renderer.sprite = item.AppearanceSprite;
            slot.Renderer.sortingLayerName = _overlaySortingLayer;
            slot.Renderer.sortingOrder = slot.BaseSortingOrder + item.SortingOrderOffset;
        }

        // Apply part scale (PartScale (1,1) = no change)
        slot.Renderer.transform.localScale = new Vector3(
            slot.OriginalScale.x * item.PartScale.x,
            slot.OriginalScale.y * item.PartScale.y,
            slot.OriginalScale.z);

        // Apply player root scale — Collider 피격범위도 함께 축소됨
        if (!Mathf.Approximately(item.PlayerRootScale, 1f))
            transform.localScale *= item.PlayerRootScale;

        // Apply color tint (White = no change)
        slot.Renderer.color = item.PartColor;

        // Ensure billboard on new sprite
        Billboard.Ensure(slot.Renderer.gameObject);

        _equippedItems[item.TargetBodyPart] = item;

        // Apply stat bonuses
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyItem(item);
        }

        // Form item: change weapon sprite and hit radius
        if (item.Category == ItemCategory.Form && _weaponController != null)
        {
            if (item.FormWeaponSprite != null)
                _weaponController.WeaponSpriteRenderer.sprite = item.FormWeaponSprite;
            if (item.FormHitRadius > 0f)
                _weaponController.HitRadius = item.FormHitRadius;
        }

        // Status effect registration
        if (!string.IsNullOrEmpty(item.Status_ID))
        {
            StatusEffectManager.Instance?.RegisterItem(item);
        }

        // T-M-A 이펙트 파이프라인 등록
        ItemEffectRunner.Instance?.RegisterItem(item);

        // Update weapon name
        string weaponName = WeaponNameBuilder.BuildName(_allEquippedItems);
        OnWeaponNameChanged?.Invoke(weaponName);

        // Bond attach impact + glitch
        StartCoroutine(BondImpactRoutine(slot));
        OnBondAttachSoundRequested?.Invoke();
        OnPartChanged?.Invoke(item.TargetBodyPart, item);
    }

    public void ResetPart(BodyPart part)
    {
        if (!_slotMap.TryGetValue(part, out BodyPartSlot slot)) return;

        slot.Renderer.sprite = slot.OriginalSprite;
        slot.Renderer.sortingOrder = slot.BaseSortingOrder;
        slot.Renderer.transform.localScale = slot.OriginalScale;
        slot.Renderer.color = slot.OriginalColor;

        _equippedItems.Remove(part);
    }

    public void ResetAllParts()
    {
        foreach (var pair in _slotMap)
        {
            var slot = pair.Value;
            slot.Renderer.sprite = slot.OriginalSprite;
            slot.Renderer.sortingOrder = slot.BaseSortingOrder;
            slot.Renderer.transform.localScale = slot.OriginalScale;
            slot.Renderer.color = slot.OriginalColor;
        }
        _equippedItems.Clear();
        _allEquippedItems.Clear();

        transform.localScale = _originalRootScale;

        // T-M-A 이펙트 정리
        ItemEffectRunner.Instance?.ClearAll();
    }

    public bool HasEquipped(BodyPart part) => _equippedItems.ContainsKey(part);

    public ItemData GetEquipped(BodyPart part)
    {
        _equippedItems.TryGetValue(part, out ItemData item);
        return item;
    }

    public BodyPartSlot GetSlot(BodyPart part)
    {
        _slotMap.TryGetValue(part, out BodyPartSlot slot);
        return slot;
    }

    // --- Bond Impact: Flash + Shake + Scale Punch + Glitch (모두 동시 진행) ---

    private IEnumerator BondImpactRoutine(BodyPartSlot slot)
    {
        Transform t = slot.Renderer.transform;
        Vector3 baseScale = t.localScale;        // EquipItem에서 PartScale 적용 후의 스케일
        Color targetColor = slot.Renderer.color; // EquipItem에서 PartColor 적용 후의 색상
        Vector3 basePos = t.localPosition;
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        // 글리치 즉시 활성화
        slot.Renderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_GlitchIntensity", _glitchIntensity);
        slot.Renderer.SetPropertyBlock(mpb);

        float totalDuration = Mathf.Max(_impactDuration, _flashDuration, _shakeDuration, _glitchDuration);
        float elapsed = 0f;
        float halfImpact = _impactDuration * 0.5f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            // ── Flash: 흰색 → targetColor 선형 보간 ───────────────────────
            slot.Renderer.color = elapsed < _flashDuration
                ? Color.Lerp(Color.white, targetColor, elapsed / _flashDuration)
                : targetColor;

            // ── Scale Punch: 커졌다가 EaseOutElastic으로 복귀 ────────────
            if (elapsed < _impactDuration)
            {
                if (elapsed < halfImpact)
                {
                    float p = elapsed / halfImpact;
                    t.localScale = baseScale * Mathf.Lerp(1f, _impactScalePunch, p);
                }
                else
                {
                    float p = (elapsed - halfImpact) / halfImpact;
                    t.localScale = baseScale * Mathf.Lerp(_impactScalePunch, 1f, EaseOutElastic(p));
                }
            }
            else
            {
                t.localScale = baseScale;
            }

            // ── Shake: sqrt 감쇠 곡선으로 흔들림이 빠르게 약해짐 ─────────
            if (elapsed < _shakeDuration)
            {
                float decay = Mathf.Sqrt(1f - elapsed / _shakeDuration); // sqrt = 초반에 강하고 후반에 부드럽게 감쇠
                float amt = _shakeMagnitude * decay;
                float sx = (UnityEngine.Random.value * 2f - 1f) * amt;
                float sy = (UnityEngine.Random.value * 2f - 1f) * amt;
                t.localPosition = basePos + new Vector3(sx, sy, 0f);
            }
            else
            {
                t.localPosition = basePos;
            }

            // ── Glitch: 수평 지터 + 강도 선형 페이드 ────────────────────
            if (elapsed < _glitchDuration)
            {
                float jitterX = (UnityEngine.Random.value * 2f - 1f) * _glitchJitterAmount;
                t.localPosition += new Vector3(jitterX, 0f, 0f);

                float fade = 1f - elapsed / _glitchDuration;
                slot.Renderer.GetPropertyBlock(mpb);
                mpb.SetFloat("_GlitchIntensity", _glitchIntensity * fade);
                slot.Renderer.SetPropertyBlock(mpb);
            }

            yield return null;
        }

        // ── 클린업: 모든 상태 확실히 복원 ──────────────────────────────
        t.localScale = baseScale;
        t.localPosition = basePos;
        slot.Renderer.color = targetColor;
        slot.Renderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_GlitchIntensity", 0f);
        slot.Renderer.SetPropertyBlock(mpb);
    }

    private static float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        float p = 0.3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
    }
}
