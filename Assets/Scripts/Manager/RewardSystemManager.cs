using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardSystemManager : MonoBehaviour
{
    public static RewardSystemManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform _chainPopupRoot;
    [SerializeField] private List<RewardSlotUI> _rewardSlots;

    [Header("Chain Drop Animation")]
    [SerializeField] private float _chainDropDuration = 0.4f;
    [SerializeField] private float _chainHiddenY = 600f;
    [SerializeField] private float _chainVisibleY = 0f;

    [Header("World Spawn")]
    [SerializeField] private Transform _pedestalPoint;
    [SerializeField] private GameObject _itemWorldPrefab;

    [Header("Bond Thread Absorption")]
    [SerializeField] private float _threadDuration = 0.6f;
    [SerializeField] private LineRenderer _bondThreadLine;
    [SerializeField] private int _threadResolution = 20;

    [Header("Feedback")]
    [SerializeField] private float _screenShakeIntensity = 0.08f;
    [SerializeField] private float _screenShakeDuration = 0.15f;

    // Events
    public event Action<ItemData> OnItemAbsorbed;
    public event Action OnRewardSequenceComplete;
    public event Action OnScreenShakeRequested;

    private PlayerAppearance _playerAppearance;
    private Transform _playerTransform;
    private Camera _mainCamera;
    private bool _isActive;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _mainCamera = Camera.main;
        HidePopupImmediate();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _playerAppearance = playerObj.GetComponent<PlayerAppearance>();
        }
    }

    // --- Public API ---

    /// <summary>
    /// Called when a combat encounter ends. Provide exactly 3 item choices.
    /// </summary>
    public void ShowRewards(List<ItemData> choices)
    {
        if (_isActive || choices == null || choices.Count < 3) return;

        _isActive = true;
        Time.timeScale = 0f;

        for (int i = 0; i < 3; i++)
        {
            _rewardSlots[i].Setup(choices[i], OnSlotSelected);
        }

        StartCoroutine(AnimateChainDrop());
    }

    // --- Chain Popup Animation ---

    private void HidePopupImmediate()
    {
        if (_chainPopupRoot == null) return;

        _chainPopupRoot.anchoredPosition = new Vector2(
            _chainPopupRoot.anchoredPosition.x,
            _chainHiddenY
        );
        _chainPopupRoot.gameObject.SetActive(false);
    }

    private IEnumerator AnimateChainDrop()
    {
        _chainPopupRoot.gameObject.SetActive(true);

        float elapsed = 0f;
        Vector2 startPos = new Vector2(_chainPopupRoot.anchoredPosition.x, _chainHiddenY);
        Vector2 endPos = new Vector2(_chainPopupRoot.anchoredPosition.x, _chainVisibleY);

        while (elapsed < _chainDropDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _chainDropDuration);
            float eased = EaseOutBounce(t);

            _chainPopupRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            yield return null;
        }

        _chainPopupRoot.anchoredPosition = endPos;
    }

    private IEnumerator AnimateChainRetract()
    {
        float elapsed = 0f;
        Vector2 startPos = _chainPopupRoot.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, _chainHiddenY);
        float duration = _chainDropDuration * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _chainPopupRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, t * t);
            yield return null;
        }

        _chainPopupRoot.gameObject.SetActive(false);
    }

    // --- Selection Flow ---

    private void OnSlotSelected(ItemData selectedItem)
    {
        StartCoroutine(SelectionSequence(selectedItem));
    }

    private IEnumerator SelectionSequence(ItemData item)
    {
        // 1. Retract chain UI
        yield return StartCoroutine(AnimateChainRetract());

        Time.timeScale = 1f;

        // 2. Spawn item on pedestal in world space
        GameObject worldItem = Instantiate(_itemWorldPrefab, _pedestalPoint.position, Quaternion.identity);
        SpriteRenderer itemSprite = worldItem.GetComponent<SpriteRenderer>();
        if (itemSprite != null)
        {
            itemSprite.sprite = item.PedestalSprite != null ? item.PedestalSprite : item.Icon;
        }

        // Billboard pedestal item
        ApplyBillboard(worldItem.transform);

        yield return new WaitForSeconds(0.3f);

        // 3. Bond Thread absorption toward player
        yield return StartCoroutine(AbsorbWithBondThread(worldItem.transform, item));

        // 4. Apply to player
        _playerAppearance.EquipItem(item);

        // 5. Feedback
        OnScreenShakeRequested?.Invoke();
        OnItemAbsorbed?.Invoke(item);

        _isActive = false;
        OnRewardSequenceComplete?.Invoke();
    }

    // --- Bond Thread Absorption ---

    private IEnumerator AbsorbWithBondThread(Transform itemTransform, ItemData item)
    {
        if (_bondThreadLine != null)
        {
            _bondThreadLine.positionCount = _threadResolution;
            _bondThreadLine.enabled = true;
        }

        Vector3 startPos = itemTransform.position;
        float elapsed = 0f;

        while (elapsed < _threadDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _threadDuration);
            float eased = t * t * t; // EaseInCubic — accelerates into player

            Vector3 currentPos = Vector3.Lerp(startPos, _playerTransform.position, eased);
            itemTransform.position = currentPos;

            // Scale down as it approaches
            float scale = 1f - eased * 0.7f;
            itemTransform.localScale = Vector3.one * scale;

            // Update bond thread curve
            UpdateBondThread(startPos, currentPos);

            // Keep billboard
            ApplyBillboard(itemTransform);

            yield return null;
        }

        if (_bondThreadLine != null)
        {
            _bondThreadLine.enabled = false;
        }

        Destroy(itemTransform.gameObject);
    }

    private void UpdateBondThread(Vector3 origin, Vector3 target)
    {
        if (_bondThreadLine == null) return;

        for (int i = 0; i < _threadResolution; i++)
        {
            float t = (float)i / (_threadResolution - 1);
            Vector3 point = Vector3.Lerp(origin, target, t);

            // Sag curve: thread droops slightly in the middle
            float sag = Mathf.Sin(t * Mathf.PI) * 0.15f;
            point.y -= sag;

            _bondThreadLine.SetPosition(i, point);
        }
    }

    // --- Billboard ---

    private void ApplyBillboard(Transform target)
    {
        Transform cam = _mainCamera.transform;
        target.rotation = Quaternion.LookRotation(cam.forward, cam.up);
    }

    // --- Easing ---

    private static float EaseOutBounce(float t)
    {
        if (t < 1f / 2.75f)
            return 7.5625f * t * t;
        if (t < 2f / 2.75f)
            return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
        if (t < 2.5f / 2.75f)
            return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
        return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
    }
}
