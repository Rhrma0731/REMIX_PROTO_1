using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// ItemData 인스펙터 커스텀 에디터.
/// T-M-A 이펙트 파이프라인을 인스펙터에서 직접 추가·삭제·재정렬할 수 있게 한다.
///
/// 색상 구분:
///   파란색 배경 = Trigger  (언제?)
///   노란색 배경 = Modifier (어떻게?)
///   초록색 배경 = Action   (무엇을?)
/// </summary>
[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    // ── 직렬화 프로퍼티 ────────────────────────────────────────────────
    private SerializedProperty _effectsProp;
    private ReorderableList    _effectsList;

    // ── 반사로 수집한 효과 타입 목록 ───────────────────────────────────
    private static List<Type> _triggerTypes;
    private static List<Type> _modifierTypes;
    private static List<Type> _actionTypes;

    // ── 역할별 색상 ────────────────────────────────────────────────────
    private static readonly Color COLOR_TRIGGER  = new Color(0.35f, 0.65f, 1.00f, 0.25f);
    private static readonly Color COLOR_MODIFIER = new Color(1.00f, 0.85f, 0.25f, 0.25f);
    private static readonly Color COLOR_ACTION   = new Color(0.35f, 1.00f, 0.55f, 0.25f);
    private static readonly Color COLOR_HEADER   = new Color(0f, 0f, 0f, 0.08f);

    // ── 접힘 상태 캐시 (인덱스 → 펼침 여부) ───────────────────────────
    private readonly Dictionary<int, bool> _foldouts = new Dictionary<int, bool>();

    // ──────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        _effectsProp = serializedObject.FindProperty("Effects");
        CollectEffectTypes();
        BuildReorderableList();
    }

    // ── 반사로 TriggerBase / ModifierBase / ActionBase 서브클래스 수집 ─

    private static void CollectEffectTypes()
    {
        if (_triggerTypes != null) return;

        _triggerTypes  = new List<Type>();
        _modifierTypes = new List<Type>();
        _actionTypes   = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            IEnumerable<Type> types;
            try { types = assembly.GetTypes(); }
            catch { continue; }

            foreach (var t in types)
            {
                if (t.IsAbstract || t.IsInterface) continue;

                if (IsSubclassOf(t, typeof(TriggerBase)))       _triggerTypes.Add(t);
                else if (IsSubclassOf(t, typeof(ModifierBase))) _modifierTypes.Add(t);
                else if (IsSubclassOf(t, typeof(ActionBase)))   _actionTypes.Add(t);
            }
        }

        _triggerTypes.Sort((a, b)  => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        _modifierTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        _actionTypes.Sort((a, b)   => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
    }

    private static bool IsSubclassOf(Type type, Type baseType)
    {
        var t = type.BaseType;
        while (t != null)
        {
            if (t == baseType) return true;
            t = t.BaseType;
        }
        return false;
    }

    // ── ReorderableList 구성 ───────────────────────────────────────────

    private void BuildReorderableList()
    {
        _effectsList = new ReorderableList(
            serializedObject, _effectsProp,
            draggable: true, displayHeader: true,
            displayAddButton: false, displayRemoveButton: false);

        _effectsList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Effects  (드래그로 순서 변경)", EditorStyles.boldLabel);
        };

        _effectsList.elementHeightCallback = index =>
        {
            if (index >= _effectsProp.arraySize) return 0f;
            var elem = _effectsProp.GetArrayElementAtIndex(index);
            if (elem.managedReferenceValue == null)
                return EditorGUIUtility.singleLineHeight + 6f;

            bool expanded = _foldouts.TryGetValue(index, out bool f) && f;
            float headerH = EditorGUIUtility.singleLineHeight + 8f;
            if (!expanded) return headerH;

            float propsH = MeasurePropertiesHeight(elem);
            return headerH + propsH + 6f;
        };

        _effectsList.drawElementCallback = DrawEffectElement;
    }

    // ── 각 Effect 요소 그리기 ──────────────────────────────────────────

    private void DrawEffectElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (index >= _effectsProp.arraySize) return;
        var elem = _effectsProp.GetArrayElementAtIndex(index);

        rect.y += 2f;
        rect.height -= 4f;

        if (elem.managedReferenceValue == null)
        {
            EditorGUI.LabelField(rect, "(null — 삭제 권장)");
            return;
        }

        var effectObj  = (IItemEffect)elem.managedReferenceValue;
        var role       = effectObj.Role;
        var typeName   = elem.managedReferenceValue.GetType().Name;

        // 역할별 배경색
        Color bgColor = role switch
        {
            ItemEffectRole.Trigger  => COLOR_TRIGGER,
            ItemEffectRole.Modifier => COLOR_MODIFIER,
            ItemEffectRole.Action   => COLOR_ACTION,
            _                       => Color.clear
        };
        EditorGUI.DrawRect(rect, bgColor);

        // ── 헤더 행 ────────────────────────────────────────────────
        float lineH   = EditorGUIUtility.singleLineHeight;
        Rect headerBg = new Rect(rect.x, rect.y, rect.width, lineH + 4f);
        EditorGUI.DrawRect(headerBg, COLOR_HEADER);

        // 접기/펼치기 토글
        bool expanded = _foldouts.TryGetValue(index, out bool fv) && fv;
        Rect foldRect = new Rect(rect.x + 4f, rect.y + 2f, 14f, lineH);
        bool newExpanded = EditorGUI.Toggle(foldRect, expanded, EditorStyles.foldout);
        if (newExpanded != expanded) _foldouts[index] = newExpanded;

        // 역할 뱃지 + 타입 이름
        string badge = role switch
        {
            ItemEffectRole.Trigger  => "[T]",
            ItemEffectRole.Modifier => "[M]",
            ItemEffectRole.Action   => "[A]",
            _                       => "[?]"
        };
        Rect labelRect = new Rect(rect.x + 22f, rect.y + 2f, rect.width - 60f, lineH);
        EditorGUI.LabelField(labelRect, $"{badge}  {typeName}", EditorStyles.boldLabel);

        // 삭제 버튼
        Rect delRect = new Rect(rect.x + rect.width - 22f, rect.y + 2f, 20f, lineH);
        if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
        {
            serializedObject.Update();
            _effectsProp.DeleteArrayElementAtIndex(index);
            _foldouts.Remove(index);
            serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
            return;
        }

        // ── 프로퍼티 필드 (펼침 시) ────────────────────────────────
        if (!newExpanded) return;

        float y = rect.y + lineH + 8f;
        EditorGUI.indentLevel++;

        var child = elem.Copy();
        var end   = elem.GetEndProperty();
        bool enter = true;

        while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
        {
            enter = false;
            float h      = EditorGUI.GetPropertyHeight(child, true);
            Rect fieldR  = new Rect(rect.x + 8f, y, rect.width - 16f, h);
            EditorGUI.PropertyField(fieldR, child, true);
            y += h + 2f;
        }

        EditorGUI.indentLevel--;
    }

    // ── 프로퍼티 높이 계산 (접힘 해제 시 요소 높이 결정용) ─────────────

    private static float MeasurePropertiesHeight(SerializedProperty elem)
    {
        float total = 0f;
        var child   = elem.Copy();
        var end     = elem.GetEndProperty();
        bool enter  = true;

        while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
        {
            enter  = false;
            total += EditorGUI.GetPropertyHeight(child, true) + 2f;
        }
        return total + 4f;
    }

    // ── 인스펙터 전체 그리기 ───────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Effects 필드를 제외한 기본 필드 모두 표시
        DrawPropertiesExcluding(serializedObject, "m_Script", "Effects");

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("T-M-A 이펙트 파이프라인", EditorStyles.boldLabel);
        EditorGUILayout.Space(2f);

        // 리스트
        _effectsList.DoLayoutList();

        EditorGUILayout.Space(4f);

        // 추가 버튼 행
        EditorGUILayout.BeginHorizontal();
        DrawAddButton("＋ Trigger",  _triggerTypes,  COLOR_TRIGGER);
        DrawAddButton("＋ Modifier", _modifierTypes, COLOR_MODIFIER);
        DrawAddButton("＋ Action",   _actionTypes,   COLOR_ACTION);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2f);
        serializedObject.ApplyModifiedProperties();
    }

    // ── 추가 버튼 하나 그리기 ─────────────────────────────────────────

    private void DrawAddButton(string label, List<Type> types, Color color)
    {
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = color + new Color(0.4f, 0.4f, 0.4f, 0.8f);

        if (GUILayout.Button(label, GUILayout.Height(26f)))
            ShowAddMenu(types);

        GUI.backgroundColor = oldColor;
    }

    // ── 타입 선택 GenericMenu ──────────────────────────────────────────

    private void ShowAddMenu(List<Type> types)
    {
        var menu = new GenericMenu();
        foreach (var type in types)
        {
            var captured = type;
            menu.AddItem(new GUIContent(type.Name), false, () => AddEffect(captured));
        }
        menu.ShowAsContext();
    }

    private void AddEffect(Type type)
    {
        serializedObject.Update();
        int newIdx = _effectsProp.arraySize;
        _effectsProp.InsertArrayElementAtIndex(newIdx);
        _effectsProp.GetArrayElementAtIndex(newIdx).managedReferenceValue =
            Activator.CreateInstance(type);
        _foldouts[newIdx] = true; // 새로 추가된 항목은 자동으로 펼침
        serializedObject.ApplyModifiedProperties();
    }
}
