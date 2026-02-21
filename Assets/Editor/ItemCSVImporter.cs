using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ItemCSVImporter
{
    private const string CsvPath = "Assets/Resources/ItemTable.csv";
    private const string OutputFolder = "Assets/Resources/Items";
    private const string DatabasePath = "Assets/Resources/ItemDatabase.asset";

    // Stat_Modifier 값 → StatType enum 명칭 정규화 (시트 표기명 → 코드 enum명)
    private static readonly Dictionary<string, string> StatTypeAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "DetectionRange", "Range" },
    };

    // CSV 헤더 → ItemData 필드 매핑 (헤더 이름이 다를 경우를 위한 별칭)
    private static readonly Dictionary<string, string[]> FieldAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { "ItemID",        new[] { "ItemID", "ID", "Item_ID" } },
        { "KR_Name",       new[] { "KR_Name", "Name", "이름" } },
        { "Keyword",       new[] { "Keyword", "CombinationKeyword", "키워드", "조합 키워드" } },
        { "Description",   new[] { "Description", "Desc", "설명" } },
        { "Category",      new[] { "Category", "ItemCategory", "카테고리" } },
        { "Rarity",        new[] { "Rarity", "ItemRarity", "등급", "Grade" } },
        { "Priority",      new[] { "Priority", "우선순위" } },
        { "Status_ID",          new[] { "Status_ID", "StatusID", "상태이상" } },
        { "StatusTriggerChance", new[] { "StatusTriggerChance", "TriggerChance", "발동확률" } },
        { "Stat_Modifier", new[] { "Stat_Modifier", "StatType", "Stat", "스탯" } },
        { "Value",         new[] { "Value", "값" } },
        { "PowerScore",    new[] { "PowerScore", "Power", "파워" } },
        { "PartScale",       new[] { "PartScale", "Scale", "파츠크기" } },
        { "PartColor",       new[] { "PartColor", "Color", "파츠색상" } },
        { "TargetBodyPart",  new[] { "TargetBodyPart", "BodyPart", "파츠", "부위" } },
    };

    [MenuItem("Tools/Import Items from CSV")]
    public static void Import()
    {
        string fullPath = Path.Combine(Application.dataPath, "..", CsvPath).Replace("\\", "/");

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[ItemCSVImporter] CSV 파일을 찾을 수 없습니다: {CsvPath}");
            EditorUtility.DisplayDialog("CSV Import", $"CSV 파일이 없습니다:\n{CsvPath}", "OK");
            return;
        }

        string[] lines = File.ReadAllLines(fullPath);
        if (lines.Length < 2)
        {
            Debug.LogWarning("[ItemCSVImporter] CSV가 비어있거나 데이터 행이 없습니다.");
            return;
        }

        // Ensure output folders exist
        EnsureFolder("Assets/Resources");
        EnsureFolder(OutputFolder);

        // Parse header — build column index map with alias support
        string[] headers = ParseCSVLine(lines[0]);
        var columnMap = BuildColumnMap(headers);

        // Log matched columns for debugging
        foreach (var pair in columnMap)
            Debug.Log($"[ItemCSVImporter] 매핑: {pair.Key} → column {pair.Value} (\"{headers[pair.Value]}\")");

        int created = 0;
        int updated = 0;
        int skipped = 0;
        var allItems = new List<ItemData>();

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int row = 1; row < lines.Length; row++)
            {
                string line = lines[row].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] cols = ParseCSVLine(line);
                string itemID = GetValue(cols, columnMap, "ItemID");
                if (string.IsNullOrEmpty(itemID))
                {
                    Debug.LogWarning($"[ItemCSVImporter] Row {row + 1}: ItemID가 비어있어 건너뜁니다.");
                    skipped++;
                    continue;
                }

                string assetPath = $"{OutputFolder}/{itemID}.asset";
                bool isNew = false;

                var item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<ItemData>();
                    isNew = true;
                }

                // --- 기본 정보 ---
                item.ItemID = itemID;
                item.KR_Name = GetValue(cols, columnMap, "KR_Name");
                item.Keyword = GetValue(cols, columnMap, "Keyword");
                item.Description = GetValue(cols, columnMap, "Description");

                // --- 분류 ---
                item.Category = ParseEnum<ItemCategory>(GetValue(cols, columnMap, "Category"));
                item.Rarity = ParseEnum<ItemRarity>(GetValue(cols, columnMap, "Rarity"));

                // --- 로직 ---
                item.Priority = ParseInt(GetValue(cols, columnMap, "Priority"));
                string rawStatusId = GetValue(cols, columnMap, "Status_ID");
                item.Status_ID = (rawStatusId.Equals("NULL", StringComparison.OrdinalIgnoreCase)) ? "" : rawStatusId;
                string chanceStr = GetValue(cols, columnMap, "StatusTriggerChance");
                item.StatusTriggerChance = string.IsNullOrEmpty(chanceStr) ? 1f : ParseFloat(chanceStr);

                // --- 스탯 보너스 (CSV: 단일 Stat_Modifier + Value → StatBonuses 리스트로 변환) ---
                string rawStatType = GetValue(cols, columnMap, "Stat_Modifier");
                if (StatTypeAliases.TryGetValue(rawStatType, out string mappedStatType))
                    rawStatType = mappedStatType;
                StatType statType = ParseEnum<StatType>(rawStatType);
                float statValue = ParseFloat(GetValue(cols, columnMap, "Value"));
                item.StatBonuses = new List<StatEntry>();
                if (statType != StatType.None)
                    item.StatBonuses.Add(new StatEntry { Type = statType, Value = statValue });

                // --- 파워 ---
                int power = ParseInt(GetValue(cols, columnMap, "PowerScore"));
                item.PowerScore = power > 0 ? power : 1;

                // --- 외형 (TargetBodyPart / PartScale / PartColor) ---
                // 컬럼이 없거나 값이 비어있어도 기본값으로 안전하게 처리

                // TargetBodyPart: CSV 컬럼 우선, 없으면 Head 기본값
                string bodyPartStr = GetValue(cols, columnMap, "TargetBodyPart");
                item.TargetBodyPart = string.IsNullOrEmpty(bodyPartStr)
                    ? BodyPart.Head
                    : ParseEnum<BodyPart>(bodyPartStr);

                string scaleStr = GetValue(cols, columnMap, "PartScale");
                string colorStr = GetValue(cols, columnMap, "PartColor");
                item.PartScale = ParseVector2Safe(scaleStr, Vector2.one);
                item.PartColor = ParseColorSafe(colorStr, Color.white);

                // ★ 테스트용 임시 할당 (Form 아이템 시각 확인용) ★
                // CSV에 PartScale/PartColor/TargetBodyPart 컬럼이 없을 때만 적용
                // 실제 CSV 데이터가 준비되면 아래 블록 제거할 것
                if (item.Category == ItemCategory.Form
                    && string.IsNullOrEmpty(scaleStr)
                    && string.IsNullOrEmpty(colorStr)
                    && string.IsNullOrEmpty(bodyPartStr))
                {
                    ApplyTestAppearance(item);
                }

                if (isNew)
                {
                    AssetDatabase.CreateAsset(item, assetPath);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(item);
                    updated++;
                }

                allItems.Add(item);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        // --- ItemDatabase 자동 등록 ---
        RegisterToDatabase(allItems);

        AssetDatabase.Refresh();

        string msg = $"완료 — {created}개 생성, {updated}개 갱신, {skipped}개 건너뜀 (총 {allItems.Count}개)";
        Debug.Log($"[ItemCSVImporter] {msg}");
        EditorUtility.DisplayDialog("CSV Import 완료", msg, "OK");
    }

    private static void RegisterToDatabase(List<ItemData> items)
    {
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
            Debug.Log($"[ItemCSVImporter] ItemDatabase 에셋 생성: {DatabasePath}");
        }

        // Use SerializedObject to write to the private _items field
        var so = new SerializedObject(db);
        var itemsProp = so.FindProperty("_items");
        itemsProp.ClearArray();

        for (int i = 0; i < items.Count; i++)
        {
            itemsProp.InsertArrayElementAtIndex(i);
            itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ItemCSVImporter] ItemDatabase에 {items.Count}개 아이템 등록 완료.");
    }

    // --- Column mapping with alias support ---

    private static Dictionary<string, int> BuildColumnMap(string[] headers)
    {
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
            headerIndex[headers[i].Trim()] = i;

        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in FieldAliases)
        {
            foreach (string alias in field.Value)
            {
                if (headerIndex.TryGetValue(alias, out int idx))
                {
                    columnMap[field.Key] = idx;
                    break;
                }
            }
        }

        return columnMap;
    }

    // --- CSV parsing (handles quoted fields with commas) ---

    private static string[] ParseCSVLine(string line)
    {
        var fields = new List<string>();
        var match = Regex.Match(line,
            @"(?:^|,)(?:""((?:[^""]|"""")*)""|([^,]*))",
            RegexOptions.Compiled);

        while (match.Success)
        {
            string value = match.Groups[1].Success
                ? match.Groups[1].Value.Replace("\"\"", "\"")
                : match.Groups[2].Value;
            fields.Add(value.Trim());
            match = match.NextMatch();
        }

        return fields.ToArray();
    }

    private static string GetValue(string[] cols, Dictionary<string, int> map, string key)
    {
        if (map.TryGetValue(key, out int idx) && idx < cols.Length)
            return cols[idx].Trim();
        return "";
    }

    private static T ParseEnum<T>(string value) where T : struct
    {
        if (string.IsNullOrEmpty(value)) return default;
        if (Enum.TryParse(value, true, out T result)) return result;
        Debug.LogWarning($"[ItemCSVImporter] 알 수 없는 enum 값 '{value}' ({typeof(T).Name}), 기본값 사용.");
        return default;
    }

    private static int ParseInt(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return int.TryParse(value, out int result) ? result : 0;
    }

    private static float ParseFloat(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0f;
        return float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float result) ? result : 0f;
    }

    // ★ 테스트용 임시 외형 할당 — CSV에 실제 데이터가 생기면 제거
    private static void ApplyTestAppearance(ItemData item)
    {
        // Form 아이템은 오른팔(ArmRight)에 붙어야 시각적으로 잘 보임
        item.TargetBodyPart = BodyPart.ArmRight;

        switch (item.Rarity)
        {
            case ItemRarity.Normal:
                item.PartScale = new Vector2(1.5f, 1.5f);
                item.PartColor = new Color(1f, 0.4f, 0.4f);   // 붉은색
                break;
            case ItemRarity.Rare:
                item.PartScale = new Vector2(1.3f, 1.3f);
                item.PartColor = new Color(0.4f, 0.6f, 1f);   // 푸른색
                break;
            case ItemRarity.Epic:
                item.PartScale = new Vector2(1.5f, 1.5f);
                item.PartColor = new Color(1f, 0.6f, 0.2f);   // 주황색
                break;
            case ItemRarity.Legend:
                item.PartScale = new Vector2(2.0f, 2.0f);
                item.PartColor = new Color(1f, 0.85f, 0.1f);  // 황금색
                break;
            default:
                item.PartScale = Vector2.one;
                item.PartColor = Color.white;
                break;
        }
        Debug.Log($"[ItemCSVImporter] 테스트 외형 적용: {item.KR_Name} → {item.TargetBodyPart}, Scale{item.PartScale}, Color{item.PartColor}");
    }

    // "x,y" 또는 "x y" 형식을 Vector2로 파싱. 실패 시 fallback 반환
    private static Vector2 ParseVector2Safe(string value, Vector2 fallback)
    {
        if (string.IsNullOrEmpty(value)) return fallback;
        string[] parts = value.Replace(" ", ",").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2
            && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)
            && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
            return new Vector2(x, y);

        Debug.LogWarning($"[ItemCSVImporter] PartScale 파싱 실패: '{value}' → 기본값 {fallback} 사용.");
        return fallback;
    }

    // "r,g,b" 또는 "r,g,b,a" 형식(0~1 float)을 Color로 파싱. 실패 시 fallback 반환
    private static Color ParseColorSafe(string value, Color fallback)
    {
        if (string.IsNullOrEmpty(value)) return fallback;
        string[] parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3
            && float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r)
            && float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float g)
            && float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float b))
        {
            float a = 1f;
            if (parts.Length >= 4)
                float.TryParse(parts[3].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out a);
            return new Color(r, g, b, a);
        }

        Debug.LogWarning($"[ItemCSVImporter] PartColor 파싱 실패: '{value}' → 기본값 사용.");
        return fallback;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
