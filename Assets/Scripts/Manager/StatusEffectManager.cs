using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StatusEffectManager : MonoBehaviour
{
    public static StatusEffectManager Instance { get; private set; }

    [Header("ST_STUN")]
    [SerializeField] private float _stunDuration = 2f;

    [Header("ST_GLITCH")]
    [SerializeField] private float _glitchDuration = 2f;
    [SerializeField] private float _glitchTickInterval = 0.25f;

    [Header("ST_CHAIN")]
    [SerializeField] private float _chainRadius = 3.5f;
    [SerializeField] private int _chainMaxTargets = 3;
    [SerializeField] private float _chainDamageRatio = 0.5f;

    private List<ItemData> _activeStatusItems = new List<ItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterItem(ItemData item)
    {
        if (string.IsNullOrEmpty(item.Status_ID)) return;
        _activeStatusItems.Add(item);
    }

    public void ClearItems()
    {
        _activeStatusItems.Clear();
    }

    public void ApplyEffects(EnemyBase enemy)
    {
        foreach (var item in _activeStatusItems)
        {
            if (item.StatusTriggerChance < 1f && UnityEngine.Random.value >= item.StatusTriggerChance)
                continue;

            switch (item.Status_ID)
            {
                case "ST_BURN":
                    enemy.StartCoroutine(BurnRoutine(enemy));
                    break;
                case "ST_SLOW":
                    enemy.StartCoroutine(SlowRoutine(enemy));
                    break;
                case "ST_STUN":
                    enemy.ApplyExternalStun(_stunDuration);
                    break;
                case "ST_GLITCH":
                    StartCoroutine(GlitchEffectRoutine(enemy));
                    break;
                case "ST_DEATH":
                    enemy.TakeDamage(99999f, Vector3.zero);
                    break;
                case "ST_CHAIN":
                    StartCoroutine(ChainRoutine(enemy));
                    break;
            }
        }
    }

    // --- ST_BURN ---

    private IEnumerator BurnRoutine(EnemyBase enemy)
    {
        float tickDamage = PlayerStats.Instance != null
            ? PlayerStats.Instance.AttackDamage * 0.2f
            : 4f;

        float duration = 3f;
        float interval = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (enemy == null) yield break;
            enemy.TakeDamage(tickDamage, Vector3.zero);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    // --- ST_SLOW ---

    private IEnumerator SlowRoutine(EnemyBase enemy)
    {
        if (enemy == null) yield break;

        NavMeshAgent agent = enemy.GetAgent();
        if (agent == null) yield break;

        float originalSpeed = enemy.GetMoveSpeed();
        agent.speed = originalSpeed * 0.5f;

        yield return new WaitForSeconds(3f);

        if (enemy != null && agent != null)
        {
            agent.speed = originalSpeed;
        }
    }

    // --- ST_GLITCH: 무작위로 속도를 얼리거나 폭주시킴 ---

    private IEnumerator GlitchEffectRoutine(EnemyBase enemy)
    {
        if (enemy == null) yield break;

        NavMeshAgent agent = enemy.GetAgent();
        if (agent == null) yield break;

        float originalSpeed = enemy.GetMoveSpeed();
        float elapsed = 0f;

        while (elapsed < _glitchDuration)
        {
            if (enemy == null) yield break;

            bool freeze = UnityEngine.Random.value < 0.5f;
            agent.speed = freeze
                ? originalSpeed * UnityEngine.Random.Range(0f, 0.2f)
                : originalSpeed * UnityEngine.Random.Range(2f, 4f);

            yield return new WaitForSeconds(_glitchTickInterval);
            elapsed += _glitchTickInterval;
        }

        if (enemy != null && agent != null)
            agent.speed = originalSpeed;
    }

    // --- ST_CHAIN: 주변 적에게 연쇄 피해 ---

    private IEnumerator ChainRoutine(EnemyBase primaryEnemy)
    {
        // 한 프레임 대기 — 주 타격이 먼저 처리되도록
        yield return null;

        if (primaryEnemy == null) yield break;

        Vector3 origin = primaryEnemy.transform.position;
        float chainDamage = PlayerStats.Instance != null
            ? PlayerStats.Instance.AttackDamage * _chainDamageRatio
            : 10f * _chainDamageRatio;

        Collider[] hits = Physics.OverlapSphere(origin, _chainRadius);
        int count = 0;

        foreach (var col in hits)
        {
            if (count >= _chainMaxTargets) break;

            var enemy = col.GetComponent<EnemyBase>();
            if (enemy == null || enemy == primaryEnemy || enemy.IsDead) continue;

            Vector3 dir = (enemy.transform.position - origin).normalized;
            enemy.TakeDamage(chainDamage, dir);
            count++;
        }
    }
}
