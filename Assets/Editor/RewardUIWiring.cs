using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RewardUIWiring
{
    [MenuItem("Tools/Wire Reward UI References")]
    public static void WireReferences()
    {
        int errors = 0;
        var rewardSlots = new List<RewardSlotUI>();

        // --- RewardSlotUI 참조 연결 ---
        // 비활성 오브젝트도 찾기 위해 씬 루트를 통해 탐색
        GameObject canvasGO = FindInactiveByPath("RewardCanvas");
        Transform chainRoot = canvasGO != null ? canvasGO.transform.Find("ChainPopupRoot") : null;

        string[] slotNames = { "RewardSlot_0", "RewardSlot_1", "RewardSlot_2" };

        foreach (string slotName in slotNames)
        {
            string slotPath = $"RewardCanvas/ChainPopupRoot/{slotName}";
            GameObject slotGO = chainRoot != null ? chainRoot.Find(slotName)?.gameObject : null;
            if (slotGO == null) { Debug.LogError($"[RewardUIWiring] 슬롯 없음: {slotPath}"); errors++; continue; }

            RewardSlotUI slot = slotGO.GetComponent<RewardSlotUI>();
            if (slot == null) { Debug.LogError($"[RewardUIWiring] RewardSlotUI 없음: {slotPath}"); errors++; continue; }

            var so = new SerializedObject(slot);
            so.FindProperty("_iconImage").objectReferenceValue         = slotGO.transform.Find("Icon")?.GetComponent<Image>();
            so.FindProperty("_nameText").objectReferenceValue          = slotGO.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_descriptionText").objectReferenceValue   = slotGO.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_rarityFrame").objectReferenceValue       = slotGO.transform.Find("RarityFrame")?.GetComponent<Image>();
            so.FindProperty("_selectButton").objectReferenceValue      = slotGO.transform.Find("SelectButton")?.GetComponent<Button>();
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(slot);
            rewardSlots.Add(slot);
            Debug.Log($"[RewardUIWiring] 연결 완료: {slotPath}");
        }

        // --- RewardSystemManager 참조 연결 ---
        var rsm = Object.FindFirstObjectByType<RewardSystemManager>();
        if (rsm == null) { Debug.LogError("[RewardUIWiring] RewardSystemManager 없음!"); return; }

        var rsmSO = new SerializedObject(rsm);

        // ChainPopupRoot
        if (chainRoot != null)
            rsmSO.FindProperty("_chainPopupRoot").objectReferenceValue = chainRoot.GetComponent<RectTransform>();
        else
            Debug.LogError("[RewardUIWiring] ChainPopupRoot 없음!");

        // RewardSlots 리스트
        SerializedProperty slotsArray = rsmSO.FindProperty("_rewardSlots");
        slotsArray.ClearArray();
        for (int i = 0; i < rewardSlots.Count; i++)
        {
            slotsArray.InsertArrayElementAtIndex(i);
            slotsArray.GetArrayElementAtIndex(i).objectReferenceValue = rewardSlots[i];
        }

        // PedestalPoint (씬에서 탐색)
        GameObject pedestal = GameObject.Find("PedestalPoint");
        if (pedestal != null)
            rsmSO.FindProperty("_pedestalPoint").objectReferenceValue = pedestal.transform;
        else
            Debug.LogWarning("[RewardUIWiring] PedestalPoint 없음 — 직접 연결 필요");

        // ItemWorldPrefab (프로젝트에서 탐색)
        string[] guids = AssetDatabase.FindAssets("ItemWorld t:Prefab");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            rsmSO.FindProperty("_itemWorldPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log($"[RewardUIWiring] ItemWorldPrefab 연결: {path}");
        }
        else
            Debug.LogWarning("[RewardUIWiring] ItemWorld 프리팹 없음 — 직접 연결 필요");

        rsmSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(rsm);

        EditorSceneManager.MarkSceneDirty(rsm.gameObject.scene);

        string msg = errors == 0
            ? "모든 참조 연결 완료!"
            : $"완료 (오류 {errors}개 — 콘솔 확인)";
        Debug.Log($"[RewardUIWiring] {msg}");
    }

    // 비활성 오브젝트 포함 씬 루트에서 이름으로 찾기
    private static GameObject FindInactiveByPath(string rootName)
    {
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == rootName) return root;
        }
        return null;
    }
}
