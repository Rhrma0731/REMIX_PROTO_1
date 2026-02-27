using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [개발 전용] 키 입력으로 특정 ItemID를 즉시 장착하는 디버그 도구.
///
/// 씬의 아무 GameObject에 붙인 뒤 Inspector에서 장착할 ItemID 목록을 입력한다.
/// - F1~F8 : _itemIDs[0]~[7] 순서대로 즉시 장착
///
/// [씬 배치]
///   DebugItemEquipper (이름은 자유)
///   └─ DebugItemEquipper.cs 컴포넌트
///      └─ _itemIDs : ["304", "101", ...] 입력
///
/// [IMPORTANT] 배포 전 이 GameObject를 비활성화하거나 스크립트를 제거할 것.
/// </summary>
public class DebugItemEquipper : MonoBehaviour
{
    [Header("장착할 ItemID 목록 (F1=첫 번째, F2=두 번째 ...)")]
    [SerializeField] private string[] _itemIDs = { "304" };

    [Tooltip("장착 시 콘솔 로그 출력 여부")]
    [SerializeField] private bool _verbose = true;

    private static readonly Key[] FKEYS =
    {
        Key.F1, Key.F2, Key.F3, Key.F4,
        Key.F5, Key.F6, Key.F7, Key.F8,
    };

    private PlayerAppearance _appearance;
    private ItemDatabase     _database;

    private void Start()
    {
        // PlayerAppearance 탐색
        _appearance = FindFirstObjectByType<PlayerAppearance>();
        if (_appearance == null)
            Debug.LogWarning("[DebugItemEquipper] PlayerAppearance 없음 — 플레이어가 씬에 있는지 확인");

        // ItemDatabase 로드
        _database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (_database == null)
            Debug.LogWarning("[DebugItemEquipper] ItemDatabase.asset 없음 — Assets/Resources/ItemDatabase.asset 확인");

        if (_verbose)
        {
            Debug.Log("[DebugItemEquipper] 활성화 완료. F1~F8 로 아이템 즉시 장착:");
            for (int i = 0; i < _itemIDs.Length && i < FKEYS.Length; i++)
                Debug.Log($"  {FKEYS[i]} → ItemID [{_itemIDs[i]}]");
        }
    }

    private void Update()
    {
        for (int i = 0; i < _itemIDs.Length && i < FKEYS.Length; i++)
        {
            if (Keyboard.current != null && Keyboard.current[FKEYS[i]].wasPressedThisFrame)
            {
                EquipByID(_itemIDs[i]);
                break;
            }
        }
    }

    private void EquipByID(string itemID)
    {
        if (_appearance == null || _database == null)
        {
            Debug.LogError("[DebugItemEquipper] PlayerAppearance 또는 ItemDatabase 없음");
            return;
        }

        ItemData item = _database.GetByID(itemID);
        if (item == null)
        {
            Debug.LogError($"[DebugItemEquipper] ItemID '{itemID}' 를 ItemDatabase에서 찾을 수 없음");
            return;
        }

        _appearance.EquipItem(item);

        if (_verbose)
            Debug.Log($"[DebugItemEquipper] ✅ 장착 완료: [{itemID}] {item.KR_Name}");
    }
}
