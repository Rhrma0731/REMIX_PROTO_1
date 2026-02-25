using System;
using UnityEngine;

/// <summary>
/// 플레이어의 모든 상태 변화를 중계하는 중앙 이벤트 허브 ("방송국").
///
/// 아이템의 Trigger들은 이 매니저의 이벤트를 구독(Subscribe)하여
/// 특정 상황에 자신의 T-M-A 파이프라인을 발동시킨다.
///
/// 기존 시스템(WeaponController, PlayerStats 등)은 적절한 시점에
/// BroadcastXxx() 메서드를 호출하여 이벤트를 방송한다.
///
/// [씬 배치] GameManagers 오브젝트 또는 Player 루트에 컴포넌트로 추가할 것.
/// </summary>
[DefaultExecutionOrder(-100)] // 다른 매니저보다 먼저 Awake 실행 — Trigger 구독 보장
public class PlayerEventManager : MonoBehaviour
{
    public static PlayerEventManager Instance { get; private set; }

    // ── 플레이어 행동 이벤트 ──────────────────────────────────────
    /// <summary>플레이어가 공격을 시도할 때</summary>
    public event Action OnAttack;
    /// <summary>적에게 실제 피해를 가한 직후 (enemy, damage)</summary>
    public event Action<EnemyBase, float> OnDealDamage;
    /// <summary>적을 처치했을 때</summary>
    public event Action<EnemyBase> OnKillEnemy;
    /// <summary>플레이어가 피해를 받았을 때 (damage)</summary>
    public event Action<float> OnTakeDamage;
    /// <summary>대시할 때 (현재 미구현 — 향후 PlayerMovement에서 호출)</summary>
    public event Action OnDash;
    /// <summary>회복할 때 (amount)</summary>
    public event Action<float> OnHeal;

    // ── 주기 이벤트 ──────────────────────────────────────────────
    /// <summary>매 1초마다 발동. OnTimerTrigger 등이 구독한다.</summary>
    public event Action OnSecondTick;

    // ── 아이템 이벤트 ────────────────────────────────────────────
    /// <summary>아이템이 장착되었을 때</summary>
    public event Action<ItemData> OnItemEquipped;

    private float _secondTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        // 매초 틱 — Time.deltaTime 사용으로 히트스탑(timeScale=0) 중에는 자연스럽게 정지
        _secondTimer += Time.deltaTime;
        if (_secondTimer >= 1f)
        {
            _secondTimer -= 1f;
            OnSecondTick?.Invoke();
        }
    }

    // ── 외부 시스템에서 호출하는 Broadcast 메서드 ────────────────
    // WeaponController, PlayerStats 등에서 한 줄로 호출하면 된다.

    public void BroadcastAttack() => OnAttack?.Invoke();
    public void BroadcastDealDamage(EnemyBase enemy, float damage) => OnDealDamage?.Invoke(enemy, damage);
    public void BroadcastKillEnemy(EnemyBase enemy) => OnKillEnemy?.Invoke(enemy);
    public void BroadcastTakeDamage(float damage) => OnTakeDamage?.Invoke(damage);
    public void BroadcastDash() => OnDash?.Invoke();
    public void BroadcastHeal(float amount) => OnHeal?.Invoke(amount);
    public void BroadcastItemEquipped(ItemData item) => OnItemEquipped?.Invoke(item);
}
