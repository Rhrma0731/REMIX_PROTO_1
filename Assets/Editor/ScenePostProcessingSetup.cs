using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Setup Post Processing &amp; Lighting (Cult Vibe)
///
/// 실행 순서:
///   0. URP 파이프라인 에셋이 Graphics Settings에 없으면 자동 생성 &amp; 연결
///   1. Global Volume + VolumeProfile(Bloom / Vignette / ColorAdjustments) 생성
///   2. Main Camera renderPostProcessing = true
///   3. Directional Light 없으면 던전 청보라 조명 생성
/// </summary>
public static class ScenePostProcessingSetup
{
    private const string ProfileSavePath     = "Assets/Settings/PostProcess_CultVibes.asset";
    private const string URPAssetPath        = "Assets/Settings/UniversalRenderPipelineAsset.asset";
    private const string RendererDataPath    = "Assets/Settings/UniversalRendererData.asset";

    // ──────────────────────────────────────────────────────────────
    // MenuItem
    // ──────────────────────────────────────────────────────────────

    [MenuItem("Tools/Setup Post Processing & Lighting (Cult Vibe)")]
    public static void Setup()
    {
        // 0. URP 파이프라인 에셋이 없으면 생성 → 없으면 이후 단계 모두 무의미
        if (!EnsureURPPipelineAsset())
        {
            Debug.LogError("[PostSetup] URP 파이프라인 에셋 연결 실패. 설정이 중단됩니다.");
            return;
        }

        SetupGlobalVolume();
        SetupCamera();
        SetupDirectionalLight();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PostSetup] 완료 — 씬을 Ctrl+S로 저장하세요.");
    }

    // ──────────────────────────────────────────────────────────────
    // 0. URP 파이프라인 에셋 확인 / 탐색 / 생성
    // ──────────────────────────────────────────────────────────────

    private static bool EnsureURPPipelineAsset()
    {
        // 이미 URP가 활성화돼 있으면 패스
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existingAsset)
        {
            Debug.Log($"[PostSetup] URP 에셋 이미 활성화됨: {AssetDatabase.GetAssetPath(existingAsset)}");
            return true;
        }

        // 프로젝트 내에 이미 만들어진 URP 에셋이 있는지 탐색
        string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var found = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (found != null)
            {
                AssignURPAsset(found);
                Debug.Log($"[PostSetup] 기존 URP 에셋 발견 → Graphics Settings에 연결: {path}");
                return true;
            }
        }

        // 없으면 새로 생성 (사용자 확인 후)
        bool create = EditorUtility.DisplayDialog(
            "URP 파이프라인 에셋 없음",
            "Graphics Settings에 URP 에셋이 없습니다.\n" +
            "새로 생성하여 연결하시겠습니까?\n\n" +
            "저장 경로:\n" + URPAssetPath,
            "생성 및 연결", "취소");

        if (!create) return false;

        Directory.CreateDirectory(Path.GetDirectoryName(URPAssetPath)!);

        // UniversalRendererData → UniversalRenderPipelineAsset 순서로 생성
        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();

        // PostProcessData 연결 — 없으면 Bloom/Vignette 등 모든 효과가 렌더링되지 않음
        var ppDataGuids = AssetDatabase.FindAssets("t:PostProcessData");
        if (ppDataGuids.Length > 0)
        {
            var ppData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                AssetDatabase.GUIDToAssetPath(ppDataGuids[0]));
            if (ppData != null)
                rendererData.postProcessData = ppData;
        }

        AssetDatabase.CreateAsset(rendererData, RendererDataPath);

        var urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
        AssetDatabase.CreateAsset(urpAsset, URPAssetPath);
        AssetDatabase.SaveAssets();

        AssignURPAsset(urpAsset);
        Debug.Log($"[PostSetup] URP 에셋 새로 생성 → Graphics Settings 연결 완료: {URPAssetPath}");
        return true;
    }

    /// <summary>Graphics Settings 전역 + 현재 Quality Level 양쪽에 할당한다.</summary>
    private static void AssignURPAsset(UniversalRenderPipelineAsset asset)
    {
        GraphicsSettings.defaultRenderPipeline = asset;
        QualitySettings.renderPipeline       = asset;
        EditorUtility.SetDirty(asset);
    }

    // ──────────────────────────────────────────────────────────────
    // 1. Global Volume + VolumeProfile
    // ──────────────────────────────────────────────────────────────

    private static void SetupGlobalVolume()
    {
        // 이미 있으면 재사용, 없으면 생성
        GameObject volumeObj = GameObject.Find("Global Volume");
        if (volumeObj == null)
        {
            volumeObj = new GameObject("Global Volume");
            Undo.RegisterCreatedObjectUndo(volumeObj, "Create Global Volume");
        }

        Volume volume = volumeObj.GetComponent<Volume>();
        if (volume == null)
            volume = Undo.AddComponent<Volume>(volumeObj);

        volume.isGlobal = true;
        volume.priority = 1f;

        // ── 프로필 생성 또는 로드 ──────────────────────────────────
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfileSavePath);
        if (profile == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ProfileSavePath)!);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfileSavePath);
        }

        volume.profile = profile;

        // ── Bloom ─────────────────────────────────────────────────
        if (!profile.TryGet<Bloom>(out Bloom bloom))
            bloom = profile.Add<Bloom>(false);

        bloom.active = true;
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.7f);
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(new Color(0.7f, 0.6f, 1.0f)); // 연보랏빛 틴트

        // ── Vignette ──────────────────────────────────────────────
        if (!profile.TryGet<Vignette>(out Vignette vignette))
            vignette = profile.Add<Vignette>(false);

        vignette.active = true;
        vignette.color.Override(new Color(0.04f, 0f, 0.08f));
        vignette.intensity.Override(0.38f);
        vignette.smoothness.Override(0.45f);
        vignette.rounded.Override(true);

        // ── Color Adjustments ─────────────────────────────────────
        if (!profile.TryGet<ColorAdjustments>(out ColorAdjustments ca))
            ca = profile.Add<ColorAdjustments>(false);

        ca.active = true;
        ca.postExposure.Override(-0.4f);
        ca.contrast.Override(18f);
        ca.saturation.Override(-20f);
        ca.colorFilter.Override(new Color(0.78f, 0.83f, 1.0f));

        EditorUtility.SetDirty(profile);
        Debug.Log($"[PostSetup] Global Volume 완료 → {ProfileSavePath}");
    }

    // ──────────────────────────────────────────────────────────────
    // 2. Main Camera — URP Post Processing 활성화
    // ──────────────────────────────────────────────────────────────

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[PostSetup] Main Camera를 찾을 수 없습니다. 태그가 'MainCamera'인지 확인하세요.");
            return;
        }

        UniversalAdditionalCameraData camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null)
            camData = Undo.AddComponent<UniversalAdditionalCameraData>(mainCam.gameObject);

        camData.renderPostProcessing = true;
        EditorUtility.SetDirty(mainCam.gameObject);
        Debug.Log("[PostSetup] Main Camera Post Processing 활성화 완료.");
    }

    // ──────────────────────────────────────────────────────────────
    // 3. Directional Light — 없을 때만 생성
    // ──────────────────────────────────────────────────────────────

    private static void SetupDirectionalLight()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                Debug.Log("[PostSetup] Directional Light 이미 존재 → 생성 건너뜀.");
                return;
            }
        }

        GameObject lightObj = new GameObject("Directional Light");
        Undo.RegisterCreatedObjectUndo(lightObj, "Create Directional Light");

        Light light = Undo.AddComponent<Light>(lightObj);
        light.type      = LightType.Directional;
        light.shadows   = LightShadows.Soft;
        light.intensity = 0.5f;
        light.color     = new Color(0.38f, 0.43f, 0.72f); // 던전 청보라

        lightObj.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

        EditorUtility.SetDirty(lightObj);
        Debug.Log("[PostSetup] Directional Light 생성 완료 (청보라, Intensity 0.5).");
    }

    // ──────────────────────────────────────────────────────────────
    // 별도 MenuItem — Built-in 머티리얼 → URP 일괄 변환
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Built-in RP 셰이더를 쓰는 머티리얼을 URP 호환 셰이더로 일괄 교체한다.
    /// 핑크 오브젝트 수정용. UI/Default 및 Sprites/Default 계열은 건드리지 않는다.
    /// </summary>
    [MenuItem("Tools/Convert Built-in Materials to URP (Fix Pink)")]
    public static void ConvertMaterialsToURP()
    {
        // Built-in → URP 셰이더 대응표
        var shaderMap = new Dictionary<string, string>
        {
            { "Standard",                          "Universal Render Pipeline/Lit" },
            { "Standard (Specular setup)",         "Universal Render Pipeline/Lit" },
            { "Legacy Shaders/Diffuse",            "Universal Render Pipeline/Lit" },
            { "Legacy Shaders/Bumped Diffuse",     "Universal Render Pipeline/Lit" },
            { "Legacy Shaders/Specular",           "Universal Render Pipeline/Lit" },
            { "Legacy Shaders/Bumped Specular",    "Universal Render Pipeline/Lit" },
            { "Unlit/Color",                       "Universal Render Pipeline/Unlit" },
            { "Unlit/Texture",                     "Universal Render Pipeline/Unlit" },
            { "Unlit/Transparent",                 "Universal Render Pipeline/Unlit" },
            { "Unlit/Transparent Cutout",          "Universal Render Pipeline/Unlit" },
            { "Particles/Standard Unlit",          "Universal Render Pipeline/Particles/Unlit" },
            { "Particles/Standard Surface",        "Universal Render Pipeline/Particles/Lit" },
            { "Mobile/Diffuse",                    "Universal Render Pipeline/Simple Lit" },
            { "Mobile/Bumped Diffuse",             "Universal Render Pipeline/Simple Lit" },
        };

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int converted = 0;
        int skipped   = 0;

        foreach (string guid in guids)
        {
            string   path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string shaderName = mat.shader.name;

            // UI, Sprites, TextMeshPro 계열은 건드리지 않는다
            if (shaderName.StartsWith("UI/")        ||
                shaderName.StartsWith("Sprites/")   ||
                shaderName.StartsWith("TextMeshPro") ||
                shaderName.StartsWith("Hidden/"))
            {
                skipped++;
                continue;
            }

            if (!shaderMap.TryGetValue(shaderName, out string urpName)) continue;

            Shader urpShader = Shader.Find(urpName);
            if (urpShader == null)
            {
                Debug.LogWarning($"[MatConvert] URP 셰이더를 찾을 수 없음: {urpName}");
                continue;
            }

            Undo.RecordObject(mat, "Convert to URP");
            mat.shader = urpShader;
            EditorUtility.SetDirty(mat);
            converted++;
            Debug.Log($"[MatConvert] {path}\n  {shaderName} → {urpName}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MatConvert] 완료 — 변환 {converted}개 / 스킵 {skipped}개");
        EditorUtility.DisplayDialog("변환 완료",
            $"변환: {converted}개\n스킵(UI·Sprites): {skipped}개\n\n핑크가 남아있는 오브젝트는\nMaterial > Shader를 수동으로 확인하세요.",
            "확인");
    }
}

// ════════════════════════════════════════════════════════════════════
// [가이드] 2D 빌보드 스프라이트를 라이팅에 반응시키는 방법
// ════════════════════════════════════════════════════════════════════
//
// 이 프로젝트의 적(EnemyBase)은 3D 씬에 배치된 빌보드 SpriteRenderer 입니다.
// 기본 "Sprites/Default" 셰이더는 Unlit이므로 Directional Light에 반응하지 않습니다.
//
// ── 방법 A. 개별 머티리얼 교체 (권장) ────────────────────────────
//   1. Project 창에서 스프라이트가 사용하는 Material 선택
//   2. Shader를 아래 중 하나로 변경:
//      · Universal Render Pipeline/Lit        ← 3D 라이팅 완전 반응
//      · Universal Render Pipeline/Simple Lit ← 가벼운 버전
//   3. Surface Type → Transparent / Blending Mode → Alpha
//      (PNG 알파 채널 유지)
//
// ── 방법 B. 코드 일괄 교체 MenuItem ─────────────────────────────
//
//   [MenuItem("Tools/Convert Sprite Materials to URP Lit")]
//   public static void ConvertSpriteMaterials()
//   {
//       Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
//       if (urpLit == null) { Debug.LogError("URP Lit 셰이더 없음"); return; }
//       string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
//       foreach (string guid in guids)
//       {
//           string path = AssetDatabase.GUIDToAssetPath(guid);
//           Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
//           if (mat != null && mat.shader.name.Contains("Sprite"))
//           {
//               Undo.RecordObject(mat, "Convert to URP Lit");
//               mat.shader = urpLit;
//               mat.SetFloat("_Surface", 1f); // Transparent
//               mat.SetFloat("_Blend",   0f); // Alpha
//               EditorUtility.SetDirty(mat);
//           }
//       }
//       AssetDatabase.SaveAssets();
//   }
// ════════════════════════════════════════════════════════════════════
