using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Floor01 텍스처 세트로 머티리얼을 생성하고 씬의 'Floor' 오브젝트에 적용한다.
///
/// 메뉴: Tools/Setup Floor01 Material
///
/// 처리 순서:
///   1. Floor01_N.png 임포트 타입이 NormalMap인지 확인 후 필요시 변경
///   2. URP/Lit (없으면 Standard) 쉐이더로 머티리얼 생성 또는 업데이트
///      - _BaseMap    ← Floor01_B.png
///      - _MetallicGlossMap ← Floor01_M.png
///      - _BumpMap    ← Floor01_N.png
///   3. 현재 씬에서 이름이 'Floor'인 게임오브젝트의 MeshRenderer 머티리얼 교체
/// </summary>
public static class FloorMaterialSetup
{
    private const string TexBasePath     = "Assets/Graphic/Texture/floor/Floor01_B.png";
    private const string TexMetallicPath = "Assets/Graphic/Texture/floor/Floor01_M.png";
    private const string TexNormalPath   = "Assets/Graphic/Texture/floor/Floor01_N.png";
    private const string MaterialPath    = "Assets/Graphic/Materials/Floor01.mat";
    private const string FloorObjectName = "Floor";

    [MenuItem("Tools/Setup Floor01 Material")]
    public static void SetupFloorMaterial()
    {
        // ── 1. Normal Map 임포트 타입 보장 ────────────────────────────────
        EnsureNormalMap(TexNormalPath);

        // ── 2. 텍스처 로드 ───────────────────────────────────────────────
        var texBase     = AssetDatabase.LoadAssetAtPath<Texture2D>(TexBasePath);
        var texMetallic = AssetDatabase.LoadAssetAtPath<Texture2D>(TexMetallicPath);
        var texNormal   = AssetDatabase.LoadAssetAtPath<Texture2D>(TexNormalPath);

        if (texBase == null)
        {
            Debug.LogError($"[FloorSetup] 베이스 텍스처 로드 실패: {TexBasePath}");
            return;
        }
        if (texMetallic == null)
        {
            Debug.LogError($"[FloorSetup] 메탈릭 텍스처 로드 실패: {TexMetallicPath}");
            return;
        }
        if (texNormal == null)
        {
            Debug.LogError($"[FloorSetup] 노멀맵 텍스처 로드 실패: {TexNormalPath}");
            return;
        }

        // ── 3. 쉐이더 결정 (URP/Lit 우선, 없으면 Standard 폴백) ──────────
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        bool isUrp = shader != null;

        if (!isUrp)
        {
            shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("[FloorSetup] URP/Lit 및 Standard 쉐이더를 모두 찾을 수 없음");
                return;
            }
            Debug.LogWarning("[FloorSetup] Universal Render Pipeline/Lit 없음 → Standard 쉐이더로 대체");
        }

        // ── 4. 머티리얼 생성 또는 기존 업데이트 ─────────────────────────
        var  mat   = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        bool isNew = mat == null;

        if (isNew)
            mat = new Material(shader);
        else
            mat.shader = shader;

        if (isUrp)
        {
            // URP Lit 텍스처 슬롯
            mat.SetTexture("_BaseMap", texBase);
            mat.SetTexture("_MetallicGlossMap", texMetallic);
            mat.SetTexture("_BumpMap", texNormal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        else
        {
            // Standard 텍스처 슬롯
            mat.SetTexture("_MainTex", texBase);
            mat.SetTexture("_MetallicGlossMap", texMetallic);
            mat.SetTexture("_BumpMap", texNormal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICGLOSSMAP");
        }

        if (isNew)
            AssetDatabase.CreateAsset(mat, MaterialPath);
        else
            EditorUtility.SetDirty(mat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FloorSetup] 머티리얼 {(isNew ? "생성" : "업데이트")} 완료 ({shader.name}) → {MaterialPath}");

        // ── 5. 씬의 'Floor' 오브젝트에 머티리얼 교체 ───────────────────
        ApplyToFloorObject(mat);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 내부 유틸
    // ─────────────────────────────────────────────────────────────────────

    private static void EnsureNormalMap(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[FloorSetup] TextureImporter 로드 실패: {assetPath}");
            return;
        }

        if (importer.textureType == TextureImporterType.NormalMap)
        {
            Debug.Log($"[FloorSetup] {assetPath} — 이미 Normal Map 타입, 변경 없음");
            return;
        }

        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
        Debug.Log($"[FloorSetup] {assetPath} → Normal Map 타입으로 변경 및 리임포트 완료");
    }

    private static void ApplyToFloorObject(Material mat)
    {
        var scene    = SceneManager.GetActiveScene();
        GameObject floorObj = null;

        foreach (var root in scene.GetRootGameObjects())
        {
            floorObj = FindByName(root.transform, FloorObjectName);
            if (floorObj != null) break;
        }

        if (floorObj == null)
        {
            Debug.LogWarning($"[FloorSetup] 씬에서 '{FloorObjectName}' 게임오브젝트를 찾지 못함 — 머티리얼을 수동으로 교체하세요.");
            return;
        }

        var meshRenderer = floorObj.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogWarning($"[FloorSetup] '{floorObj.name}'에 MeshRenderer가 없음");
            return;
        }

        Undo.RecordObject(meshRenderer, "Apply Floor01 Material");
        meshRenderer.sharedMaterial = mat;
        EditorUtility.SetDirty(meshRenderer);
        Debug.Log($"[FloorSetup] '{floorObj.name}' MeshRenderer 머티리얼 교체 완료 → Floor01.mat");
    }

    /// <summary>parent를 포함한 전체 자식 트리에서 name과 일치하는 첫 번째 오브젝트를 반환.</summary>
    private static GameObject FindByName(Transform parent, string name)
    {
        if (parent.name == name) return parent.gameObject;
        foreach (Transform child in parent)
        {
            var result = FindByName(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
