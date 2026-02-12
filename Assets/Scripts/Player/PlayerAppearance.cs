using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAppearance : MonoBehaviour
{
    public class BodyPartSlot
    {
        public BodyPart Part;
        public SpriteRenderer Renderer;
        public Sprite OriginalSprite;
        public int BaseSortingOrder;
    }

    [Header("Bond Overlay")]
    [SerializeField] private string _overlaySortingLayer = "Player";

    [Header("Bond Impact Effect")]
    [SerializeField] private float _impactScalePunch = 1.4f;
    [SerializeField] private float _impactDuration = 0.15f;
    [SerializeField] private float _impactShakeDuration = 0.06f;
    [SerializeField] private float _impactShakeIntensity = 0.02f;

    [Header("Glitch Effect")]
    [SerializeField] private float _glitchDuration = 0.15f;
    [SerializeField] private float _glitchIntensity = 0.6f;
    [SerializeField] private float _glitchJitterAmount = 0.03f;

    // Events
    public event Action<BodyPart, ItemData> OnPartChanged;
    public event Action OnBondAttachSoundRequested;

    private Dictionary<BodyPart, BodyPartSlot> _slotMap;
    private Dictionary<BodyPart, ItemData> _equippedItems;

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

        // Swap sprite
        slot.Renderer.sprite = item.AppearanceSprite;

        // "Bonded-on" look: bump sorting order
        slot.Renderer.sortingLayerName = _overlaySortingLayer;
        slot.Renderer.sortingOrder = slot.BaseSortingOrder + item.SortingOrderOffset;

        // Ensure billboard on new sprite
        Billboard.Ensure(slot.Renderer.gameObject);

        _equippedItems[item.TargetBodyPart] = item;

        // Apply stat bonuses
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyItem(item);
        }

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

        _equippedItems.Remove(part);
    }

    public void ResetAllParts()
    {
        foreach (var pair in _slotMap)
        {
            var slot = pair.Value;
            slot.Renderer.sprite = slot.OriginalSprite;
            slot.Renderer.sortingOrder = slot.BaseSortingOrder;
        }
        _equippedItems.Clear();
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

    // --- Bond Impact: scale punch + micro shake + glitch ---

    private IEnumerator BondImpactRoutine(BodyPartSlot slot)
    {
        Transform t = slot.Renderer.transform;
        Vector3 originalScale = t.localScale;
        Vector3 originalPos = t.localPosition;
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        // Phase 1: Scale punch up + glitch start
        float elapsed = 0f;
        float halfDuration = _impactDuration * 0.5f;

        // Activate glitch on the sprite
        slot.Renderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_GlitchIntensity", _glitchIntensity);
        slot.Renderer.SetPropertyBlock(mpb);

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float scale = Mathf.Lerp(1f, _impactScalePunch, progress);
            t.localScale = originalScale * scale;
            yield return null;
        }

        // Phase 2: Scale punch back (EaseOutElastic) + micro shake + glitch jitter
        elapsed = 0f;
        float glitchElapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            glitchElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float scale = Mathf.Lerp(_impactScalePunch, 1f, EaseOutElastic(progress));
            t.localScale = originalScale * scale;

            // Micro shake during settle
            if (elapsed < _impactShakeDuration)
            {
                float shakeX = (UnityEngine.Random.value * 2f - 1f) * _impactShakeIntensity;
                float shakeY = (UnityEngine.Random.value * 2f - 1f) * _impactShakeIntensity;
                t.localPosition = originalPos + new Vector3(shakeX, shakeY, 0f);
            }
            else
            {
                t.localPosition = originalPos;
            }

            yield return null;
        }

        // Phase 3: Glitch jitter (sprite horizontal jitter for remaining glitch duration)
        t.localScale = originalScale;
        t.localPosition = originalPos;

        float remainingGlitch = _glitchDuration - _impactDuration;
        if (remainingGlitch > 0f)
        {
            elapsed = 0f;
            while (elapsed < remainingGlitch)
            {
                elapsed += Time.deltaTime;
                float jitterX = (UnityEngine.Random.value * 2f - 1f) * _glitchJitterAmount;
                t.localPosition = originalPos + new Vector3(jitterX, 0f, 0f);

                // Fade out glitch intensity
                float fade = 1f - Mathf.Clamp01(elapsed / remainingGlitch);
                slot.Renderer.GetPropertyBlock(mpb);
                mpb.SetFloat("_GlitchIntensity", _glitchIntensity * fade);
                slot.Renderer.SetPropertyBlock(mpb);

                yield return null;
            }
        }

        // Ensure clean state
        t.localScale = originalScale;
        t.localPosition = originalPos;

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
