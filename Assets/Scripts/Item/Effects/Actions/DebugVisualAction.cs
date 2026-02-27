using System;
using System.Collections;
using System.Text;
using UnityEngine;

/// <summary>
/// [Action] T-M-A 파이프라인 디버그 전용 시각화 액션.
///
/// 파이프라인 끝에서 실행되어 다음 세 가지를 동시에 수행한다:
///   1. 콘솔에 Trigger명 + 모든 Modifier 수치 상세 출력
///   2. 플레이어 머리 위에 텍스트 팝업 (위로 부유 후 페이드 아웃)
///   3. 플레이어 위치에 커다란 구체를 1초간 표시
///
/// [사용법] ItemData.Effects 리스트의 맨 끝에 추가.
///          ChanceGateModifier 등과 조합 시 DamageMultiplier == 0이면 실행되지 않음.
/// </summary>
[Serializable]
public class DebugVisualAction : ActionBase
{
    [Tooltip("머리 위에 표시할 텍스트")]
    [SerializeField] private string _popupText = "T-M-A 발동!";

    [Tooltip("구체 및 텍스트 표시 시간 (초)")]
    [SerializeField] private float _displayDuration = 1f;

    [Tooltip("구체 크기 (유니티 단위)")]
    [SerializeField] private float _sphereSize = 0.4f;

    [Tooltip("구체 색상")]
    [SerializeField] private Color _sphereColor = Color.red;

    [Tooltip("텍스트의 플레이어 머리 위 오프셋 높이")]
    [SerializeField] private float _textHeightOffset = 1.5f;

    protected override void OnExecute(ItemEffectContext context)
    {
        // 1. 콘솔 로그 (항상 출력)
        LogPipelineState(context);

        Transform playerTransform = context.Player != null ? context.Player.transform : null;
        if (playerTransform == null) return;

        // ItemEffectVFX의 MonoBehaviour를 빌려서 코루틴 실행
        var vfx = ItemEffectVFX.EnsureInstance();

        // 2. 텍스트 팝업
        vfx.StartCoroutine(TextPopupRoutine(playerTransform));

        // 3. 빨간 구체
        SpawnDebugSphere(playerTransform.position);
    }

    // ── 콘솔 로그 ────────────────────────────────────────────────

    private void LogPipelineState(ItemEffectContext context)
    {
        string triggerName = string.IsNullOrEmpty(context.TriggerName)
            ? "(알 수 없음)"
            : context.TriggerName;

        string itemName = context.SourceItem != null ? context.SourceItem.KR_Name : "(없음)";

        var sb = new StringBuilder();
        sb.AppendLine($"[TMA Debug] Trigger: {triggerName} | 아이템: {itemName}");
        sb.AppendLine("  전달받은 Modifier 수치:");
        sb.AppendLine($"    DamageMultiplier : {context.DamageMultiplier:F2}x");
        sb.AppendLine($"    Damage           : {context.Damage:F1}");
        sb.AppendLine($"    AreaRadius       : {context.AreaRadius:F2}");
        sb.AppendLine($"    ElementTag       : {(string.IsNullOrEmpty(context.ElementTag) ? "-" : context.ElementTag)}");
        sb.AppendLine($"    StatusID         : {(string.IsNullOrEmpty(context.StatusID) ? "-" : context.StatusID)}");
        sb.AppendLine($"    StatusChance     : {context.StatusChance:P0}");
        sb.AppendLine($"    BounceCount      : {context.BounceCount}");
        sb.AppendLine($"    BounceDecay      : {context.BounceDecay:F2}");
        sb.AppendLine($"    IsHoming         : {context.IsHoming}");
        sb.AppendLine($"    HomingStrength   : {context.HomingStrength:F1}");
        sb.Append($"    PipelineDepth    : {context.PipelineDepth}");

        Debug.Log(sb.ToString(), context.Player);
    }

    // ── 텍스트 팝업 ──────────────────────────────────────────────

    private IEnumerator TextPopupRoutine(Transform playerTransform)
    {
        // 텍스트 오브젝트 생성
        var go = new GameObject("TMA_DebugText");
        go.transform.position = playerTransform.position + Vector3.up * _textHeightOffset;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = _popupText;
        tm.fontSize = 64;
        tm.characterSize = 0.05f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.yellow;
        tm.fontStyle = FontStyle.Bold;

        Vector3 startPos = go.transform.position;
        float elapsed = 0f;

        while (elapsed < _displayDuration)
        {
            if (go == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / _displayDuration;

            // 위로 부유
            go.transform.position = startPos + Vector3.up * (0.6f * t);

            // 카메라를 향해 회전 (빌보드)
            if (Camera.main != null)
                go.transform.rotation = Camera.main.transform.rotation;

            // 후반부에 페이드 아웃 (t > 0.5 부터)
            Color c = tm.color;
            c.a = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
            tm.color = c;

            yield return null;
        }

        if (go != null) UnityEngine.Object.Destroy(go);
    }

    // ── 디버그 구체 ──────────────────────────────────────────────

    private void SpawnDebugSphere(Vector3 position)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "TMA_DebugSphere";
        sphere.transform.position = position + Vector3.up * 0.5f;
        sphere.transform.localScale = Vector3.one * _sphereSize;

        // 물리 간섭 방지 — 콜라이더 제거
        UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());

        // 눈에 확 띄는 빨간색 + Emission 머티리얼
        Renderer rend = sphere.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = _sphereColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", _sphereColor * 2f);
            rend.material = mat;
        }

        UnityEngine.Object.Destroy(sphere, _displayDuration);
    }
}
