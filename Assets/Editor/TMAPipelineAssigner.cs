using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 35개 ItemData에 기획서 기반 T-M-A 이펙트를 일괄 할당하는 에디터 도구.
///
/// 메뉴: Tools > Assign TMA Effects to All Items
///
/// 매핑 규칙 (message.txt 순번 → asset ID):
///   1~10  → 101~110   /   11~20 → 201~210
///   21~30 → 301~310   /   31~35 → 401~405
///
/// [구현 불가 기능 — 현재 블록으로 근사 처리]
///   MeleeSweep/Drill/Cone  → RadiusModifier + DealDamageAction (AoE 근사)
///   PullTarget / DashForward → 생략 (해당 블록 미구현)
///   SpawnExplosion / Hazard  → DealDamageAction(AoE) 근사
///   ArmorPenetration         → StatModifier(damageMultiplier 상승) 근사
///   GuaranteeCrit            → StatModifier(×2배) 근사
///   Fear                     → ST_STUN 근사
///   Bleed / Shock            → ST_BURN / ST_CHAIN 근사
///   OnIdle / OnMove          → OnTimer / OnDash 근사
///   Area ApplyStatus         → TargetEnemy 없을 시 미발동 (OnDash/OnTimer 트리거 한계)
/// </summary>
public static class TMAPipelineAssigner
{
    // 아이템 ID 전체 목록 (공유 상수)
    private static readonly string[] ALL_IDS =
    {
        "101","102","103","104","105","106","107","108","109","110",
        "201","202","203","204","205","206","207","208","209","210",
        "301","302","303","304","305","306","307","308","309","310",
        "401","402","403","404","405",
        "501","502","503",
        "601","602","603","604","605","606","607","608","609","610",
    };

    // ── DebugVisualAction 추가 ────────────────────────────────────────

    /// <summary>모든 아이템 Effects 리스트 끝에 DebugVisualAction 추가 (중복 방지)</summary>
    [MenuItem("Tools/TMA Debug/Add DebugVisualAction to All Items")]
    public static void AddDebugToAll()
    {
        int added = 0, skipped = 0;
        foreach (string id in ALL_IDS)
        {
            string path = $"Assets/Resources/Items/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null) continue;

            // 이미 DebugVisualAction이 있으면 추가 안 함
            bool alreadyHas = item.Effects != null &&
                              item.Effects.Exists(e => e is DebugVisualAction);
            if (alreadyHas) { skipped++; continue; }

            if (item.Effects == null) item.Effects = new System.Collections.Generic.List<IItemEffect>();
            item.Effects.Add(new DebugVisualAction());
            EditorUtility.SetDirty(item);
            added++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[TMA Debug] DebugVisualAction 추가 완료 — 추가 {added}개 / 이미 있음 {skipped}개");
    }

    /// <summary>모든 아이템 Effects 리스트에서 DebugVisualAction 제거</summary>
    [MenuItem("Tools/TMA Debug/Remove DebugVisualAction from All Items")]
    public static void RemoveDebugFromAll()
    {
        int removed = 0;
        foreach (string id in ALL_IDS)
        {
            string path = $"Assets/Resources/Items/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item?.Effects == null) continue;

            int before = item.Effects.Count;
            item.Effects.RemoveAll(e => e is DebugVisualAction);
            if (item.Effects.Count < before)
            {
                EditorUtility.SetDirty(item);
                removed++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[TMA Debug] DebugVisualAction 제거 완료 — {removed}개 아이템에서 제거");
    }

    // ── 기존 전체 할당 ────────────────────────────────────────────────

    [MenuItem("Tools/Assign TMA Effects to All Items")]
    public static void AssignAll()
    {
        int ok = 0, skip = 0;

        foreach (var (id, effects) in BuildTable())
        {
            string path = $"Assets/Resources/Items/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                Debug.LogWarning($"[TMA Assigner] {id}.asset 없음 — 건너뜀");
                skip++;
                continue;
            }

            item.Effects = effects;
            EditorUtility.SetDirty(item);
            ok++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TMA Assigner] 완료 ✅  성공 {ok}개 / 건너뜀 {skip}개");
    }

    // ── 35개 아이템 T-M-A 정의 ──────────────────────────────────────

    private static List<(string id, List<IItemEffect> effects)> BuildTable()
    {
        return new List<(string, List<IItemEffect>)>
        {
            // ── 101~110 : Form 무기 아이템 ─────────────────────────────

            // 101 대형 파리채 — OnAttack + 광역(3m) + AoE 피해 (MeleeSweep 근사)
            ("101", FX(
                T_Attack(),
                M_Radius(3f),
                A_Damage()
            )),

            // 102 전동 드릴 — OnAttack + 약한 데미지 × 2타 (MultiHit 근사)
            ("102", FX(
                T_Attack(),
                M_Stat(damMul: 0.4f),
                A_Damage(),
                A_Damage()
            )),

            // 103 고무줄 요요 — OnAttack + 데미지 30% 감소 (DamageDown 근사)
            ("103", FX(
                T_Attack(),
                M_Stat(damMul: 0.7f),
                A_Damage()
            )),

            // 104 불량 가스 토치 — OnAttack + Fire 태그 → ST_BURN
            ("104", FX(
                T_Attack(),
                M_Tag("Fire"),
                A_Status("ST_BURN", 0.8f)
            )),

            // 105 안마기 모터 — 장착 시 1회 Passive (데미지 배율 설정, 스탯은 StatBonuses에서 처리)
            ("105", FX(
                T_Passive(continuous: false),
                M_Stat(damMul: 0.3f),
                A_Damage()
            )),

            // 106 장난감 광선검 — OnAttack + 데미지 1.5배 (ArmorPenetration 근사)
            ("106", FX(
                T_Attack(),
                M_Stat(damMul: 1.5f),
                A_Damage()
            )),

            // 107 뚫어뻥 — OnMeleeHit + 추가 피해 (Pull 미구현, 추가 타격으로 근사)
            ("107", FX(
                T_MeleeHit(),
                A_Damage()
            )),

            // 108 낡은 프라이팬 — OnMeleeHit + 60% 확률 + ST_STUN
            ("108", FX(
                T_MeleeHit(),
                M_Chance(0.6f),
                A_Status("ST_STUN")
            )),

            // 109 테이프 감긴 쇠파이프 — OnMeleeHit + ST_BURN (출혈 근사)
            ("109", FX(
                T_MeleeHit(),
                A_Status("ST_BURN")
            )),

            // 110 부러진 골프채 — OnMeleeHit + 데미지 2배 (체력 낮을수록 크리 근사)
            ("110", FX(
                T_MeleeHit(),
                M_Stat(damMul: 2f),
                A_Damage()
            )),

            // ── 201~210 : Body 파츠 아이템 ─────────────────────────────

            // 201 압축 스프링 주먹 — OnAttack + 데미지 2.5배 (차지 한방 근사)
            ("201", FX(
                T_Attack(),
                M_Stat(damMul: 2.5f),
                A_Damage()
            )),

            // 202 쌍절곤 — OnAttack + 두 번 타격 (시차 MultiHit 근사)
            ("202", FX(
                T_Attack(),
                A_Damage(),
                A_Damage()
            )),

            // 203 고압축 피스톤 — OnMeleeHit + 광역 2.5m 폭발 (SpawnExplosion 근사)
            ("203", FX(
                T_MeleeHit(),
                M_Radius(2.5f),
                A_Damage()
            )),

            // 204 용수철 코일 — OnAttack + 피해 (DashForward 미구현)
            ("204", FX(
                T_Attack(),
                A_Damage()
            )),

            // 205 망가진 핀볼 범퍼 — OnTakeDamage + 반사 피해 (Knockback 근사)
            ("205", FX(
                T_TakeDamage(),
                A_Damage()
            )),

            // 206 압력 밥솥 뚜껑 — OnTakeDamage + 광역 3m 고정 10피해 (시한폭탄 근사)
            ("206", FX(
                T_TakeDamage(),
                M_Radius(3f),
                A_DamageFlat(10f)
            )),

            // 207 정전기 스웨터 — OnMeleeHit + 40% 확률 + 광역 3.5m 피해 (연쇄 번개 근사)
            ("207", FX(
                T_MeleeHit(),
                M_Chance(0.4f),
                M_Radius(3.5f),
                A_Damage()
            )),

            // 208 뽁뽁이 갑옷 — OnTakeDamage + 피해 무효 (쿨타임 10초)
            ("208", FX(
                T_TakeDamage(),
                A_Nullify(icd: 10f)
            )),

            // 209 낡은 선풍기 모터 — OnDash + 광역 2m + ST_SLOW (소용돌이 근사)
            // ※ OnDash는 TargetEnemy를 설정하지 않아 ApplyStatus 미발동 → 추후 개선 필요
            ("209", FX(
                T_Dash(),
                M_Radius(2f),
                A_Status("ST_SLOW")
            )),

            // 210 굳은 시멘트 포대 — OnTimer(2s) + 피해 무효 (정지 시 무적 근사)
            ("210", FX(
                T_Timer(2f),
                A_Nullify()
            )),

            // ── 301~310 : Head 파츠 아이템 ─────────────────────────────

            // 301 누액 건전지 — OnTimer(1s) + ST_BURN (독 장판 근사)
            // ※ TargetEnemy 미설정 시 미발동 — 추후 개선 필요
            ("301", FX(
                T_Timer(1f),
                A_Status("ST_BURN")
            )),

            // 302 고장난 토스터 — OnDash + Fire 태그 + 광역 2m 피해 (불길 근사)
            ("302", FX(
                T_Dash(),
                M_Tag("Fire"),
                M_Radius(2f),
                A_Damage()
            )),

            // 303 고무 튜브 — OnDash + 바운스 3회(0.8감쇄) + 피해 (벽 반사 근사)
            ("303", FX(
                T_Dash(),
                M_Bounce(count: 3, decay: 0.8f),
                A_Damage()
            )),

            // 304 장난감 벌집 — OnTakeDamage + Homing + 카미카제 패밀리어 3마리 소환
            ("304", FX(
                T_TakeDamage(),
                M_Homing(strength: 180f),
                A_SpawnFamiliar(maxInstances: 3, resourcePath: "Familiars/Familiar_KamikazeBee")
            )),

            // 305 누전된 헤드폰 — OnTimer(3s) + 자해 1 데미지
            ("305", FX(
                T_Timer(3f),
                A_SelfDamage(1f)
            )),

            // 306 카운터 센서 — OnTakeDamage + 데미지 3배 + 반격 피해 (패링 근사)
            ("306", FX(
                T_TakeDamage(),
                M_Stat(damMul: 3f),
                A_Damage()
            )),

            // 307 장난감 메트로놈 — OnMeleeHit + 데미지 1.5배 (3콤보 크리 근사)
            ("307", FX(
                T_MeleeHit(),
                M_Stat(damMul: 1.5f),
                A_Damage()
            )),

            // 308 리사이클 렌즈 — OnTimer(10s) + 광역 5m 피해 (리롤 미구현, AoE 근사)
            ("308", FX(
                T_Timer(10f),
                M_Radius(5f),
                A_Damage()
            )),

            // 309 사이렌 경광등 — OnDash + 광역 4m + ST_STUN (공포 근사)
            // ※ OnDash는 TargetEnemy 미설정 → 추후 개선 필요
            ("309", FX(
                T_Dash(),
                M_Radius(4f),
                A_Status("ST_STUN")
            )),

            // 310 양은 냄비 — OnTakeDamage + 30% 확률 + 피해 무효 (투사체 반사 근사)
            ("310", FX(
                T_TakeDamage(),
                M_Chance(0.3f),
                A_Nullify()
            )),

            // ── 401~405 : 전설 아이템 ───────────────────────────────────

            // 401 돋보기 안경 — OnMeleeHit + 데미지 2배 (화상 적 특화 근사)
            ("401", FX(
                T_MeleeHit(),
                M_Stat(damMul: 2f),
                A_Damage()
            )),

            // 402 망가진 나침반 — Passive(연속) — 조건부 이속 미구현, 매초 소량 회복으로 근사
            ("402", FX(
                T_Passive(continuous: true),
                A_HealSelf(healAmount: 0.5f, percent: false)
            )),

            // 403 철제 깔때기 — OnTakeDamage + 피해 무효 (자해 면역 근사)
            ("403", FX(
                T_TakeDamage(),
                A_Nullify()
            )),

            // 404 삐에로 코 — OnKill + 10% 확률 + DropPickup(1개)
            ("404", FX(
                T_Kill(),
                M_Chance(0.1f),
                A_DropPickup(count: 1)
            )),

            // 405 구리 선 다발 — OnMeleeHit + Electric 태그 + ST_CHAIN (감전 연쇄 근사)
            ("405", FX(
                T_MeleeHit(),
                M_Tag("Electric"),
                A_Status("ST_CHAIN")
            )),
        };
    }

    // ── 304번 아이템 패밀리어 경로 업데이트 ───────────────────────────────

    /// <summary>
    /// 304.asset의 SpawnFamiliarAction._familiarResourcePath를
    /// "Familiars/Familiar_KamikazeBee"로 업데이트한다.
    /// 전체 재할당 없이 304번 아이템만 수정.
    /// </summary>
    [MenuItem("Tools/TMA Debug/Update Item 304 Familiar Path")]
    public static void UpdateItem304FamiliarPath()
    {
        string path = "Assets/Resources/Items/304.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null) { Debug.LogError("[TMA Assigner] 304.asset 없음"); return; }
        if (item.Effects == null) { Debug.LogWarning("[TMA Assigner] 304 Effects null"); return; }

        bool updated = false;
        foreach (var effect in item.Effects)
        {
            if (effect is SpawnFamiliarAction familiar)
            {
                familiar.FamiliarResourcePath = "Familiars/Familiar_KamikazeBee";
                updated = true;
            }
        }

        if (updated)
        {
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
            Debug.Log("[TMA Assigner] 304.asset SpawnFamiliarAction 경로 → Familiars/Familiar_KamikazeBee");
        }
        else
        {
            Debug.LogWarning("[TMA Assigner] 304.asset에서 SpawnFamiliarAction 미발견");
        }
    }

    // ── 신규 아이템 501~503 생성 ───────────────────────────────────────

    /// <summary>
    /// 새 기획서 아이템 3종을 Assets/Resources/Items 에 생성한 뒤 ItemDatabase에 등록한다.
    ///   501 — 100원짜리 동전 (1UP!)
    ///   502 — 피의 서약
    ///   503 — 가짜 피 캡슐
    /// </summary>
    [MenuItem("Tools/Create New Items (501-503)")]
    public static void CreateNewItems()
    {
        // 501 — 100원짜리 동전 (1UP!)
        CreateItem(
            id:          "501",
            krName:      "100원짜리 동전",
            keyword:     "1UP",
            description: "죽는 순간 딱 한 번, 체력을 모두 회복하고 3초간 무적 상태가 된다.",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Rare,
            effects:     FX(
                new OnFatalDamageTrigger { MaxUses = 1 },
                new ReviveAction         { HealPercent = 100f, InvincibleDuration = 3f }
            )
        );

        // 502 — 피의 서약
        CreateItem(
            id:          "502",
            krName:      "피의 서약",
            keyword:     "Blood Oath",
            description: "방 클리어 시 현재 체력을 모두 소모해 1로 만들고, 소모한 체력 비율에 비례하여 공격력과 이동속도가 영구적으로 증가한다.",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Epic,
            effects:     FX(
                new OnRoomClearTrigger(),
                new BloodOathAction { AttackDamageScale = 5f, MoveSpeedScale = 1f }
            )
        );

        // 503 — 가짜 피 캡슐
        CreateItem(
            id:          "503",
            krName:      "가짜 피 캡슐",
            keyword:     "Martyr",
            description: "항상 장착된 것처럼 보이지만, 장착 즉시 공격력이 1 증가한다.",
            category:    ItemCategory.Modifier,
            rarity:      ItemRarity.Normal,
            statBonuses: new List<StatEntry>
            {
                new StatEntry { Type = StatType.AttackDamage, Value = 1f }
            },
            effects: null
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TMA Assigner] 신규 아이템 501~503 생성 완료.");
    }

    // ── 신규 패밀리어 아이템 601~610 생성 ─────────────────────────────────

    /// <summary>
    /// 패밀리어 소환 기반 아이템 10종을 Assets/Resources/Items 에 생성한다.
    ///
    /// [구현 상태 범례]
    ///   ✅ 완전 지원  — 현재 FamiliarController 기능으로 동작
    ///   ⚠ Orbit 근사 — 기획 동작 미구현, 플레이어 공전으로 폴백
    ///   ❌ 미구현     — 별도 FamiliarMode 또는 Action 구현 필요
    ///
    /// 미구현 프리팹(Familiars/Familiar_*)은 현재 없으므로 Resources.Load 경고 발생.
    /// → 프리팹 제작 후 해당 경로에 배치하면 자동 연결됨.
    /// → 임시 테스트는 기존 Familiar_Bee / Familiar_KamikazeBee 경로를 사용할 것.
    /// </summary>
    [MenuItem("Tools/Create Familiar Items (601-610)")]
    public static void CreateFamiliarItems()
    {
        // ── 601 다마고치 펫 (7 Seals) ─────────────────────────────────────
        // ✅ Orbit 기본 소환
        // ⚠ 10초 형태 변경 미구현 (TODO: FamiliarMode.ShapeShift — 주기적 스탯/외형 교체)
        CreateItem(
            id:          "601",
            krName:      "다마고치 펫",
            keyword:     "7 Seals",
            description: "적을 쫓아 부딪히는 픽셀 펫을 소환한다.\n[미구현] 10초마다 형태 변경 (ShapeShift 모드 필요)",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Normal,
            effects: FX(
                T_Passive(continuous: false),
                A_SpawnFamiliar(maxInstances: 1, resourcePath: "Familiars/Familiar_Tamagotchi")
            )
        );

        // ── 602 끈끈이 거미 장난감 (???'s Only Friend) ────────────────────
        // ⚠ Orbit 근사 (마우스 조준점 추적 미구현)
        // ❌ FamiliarMode.MouseFollow — Mouse.current.position → 월드 좌표 변환 필요
        CreateItem(
            id:          "602",
            krName:      "끈끈이 거미 장난감",
            keyword:     "???'s Only Friend",
            description: "마우스 조준점을 따라다니며 적에게 틱 데미지를 주는 거미 패밀리어 소환.\n[미구현] MouseFollow 모드 — 현재 Orbit 폴백",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Normal,
            effects: FX(
                T_Passive(continuous: false),
                A_SpawnFamiliar(maxInstances: 1, resourcePath: "Familiars/Familiar_SpiderToy")
            )
        );

        // ── 603 짝퉁 요요 (Abel) ──────────────────────────────────────────
        // ⚠ Orbit 근사 (플레이어 대칭 이동 미구현)
        // ❌ FamiliarMode.Symmetric — RoomCenter 좌표 + 플레이어 offset 반전 필요
        CreateItem(
            id:          "603",
            krName:      "짝퉁 요요",
            keyword:     "Abel",
            description: "방 중앙 기준으로 플레이어와 완벽히 대칭 이동하는 요요 패밀리어 소환.\n[미구현] Symmetric 모드 — 현재 Orbit 폴백",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Rare,
            effects: FX(
                T_Passive(continuous: false),
                A_SpawnFamiliar(maxInstances: 1, resourcePath: "Familiars/Familiar_Yoyo")
            )
        );

        // ── 604 홀로그램 스티커 (Angelic Prism) ──────────────────────────
        // ✅ Orbit 공전 기본 동작
        // ❌ 타격 시 4갈래 증폭 미구현 (TODO: Action.SplitProjectile — 투사체 분열 액션 필요)
        CreateItem(
            id:          "604",
            krName:      "홀로그램 스티커",
            keyword:     "Angelic Prism",
            description: "캐릭터 주위를 도는 홀로그램 궤도. 타격 판정 시 4갈래 증폭.\n[미구현] SplitProjectile 액션 — 현재 공전+공격만 동작",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Rare,
            effects: FX(
                T_Passive(continuous: false),
                A_SpawnFamiliar(maxInstances: 1, resourcePath: "Familiars/Familiar_HoloPrism")
            )
        );

        // ── 605 똥파리 장난감 (Angry Fly) ────────────────────────────────
        // ⚠ Orbit 근사 (적 중심 공전 미구현 — 플레이어 중심 공전으로 근사)
        // 기본 attackInterval을 짧게 설정해 "지속 피해" 느낌을 줌
        // TODO: EnemyOrbit 모드 — 가장 가까운 적 주변을 맴도는 로직 필요
        CreateItem(
            id:          "605",
            krName:      "똥파리 장난감",
            keyword:     "Angry Fly",
            description: "적의 주위를 맴돌며 지속 피해를 주고 박치기하는 똥파리 소환.\n[미구현] 적 중심 공전 — 현재 플레이어 공전 근사",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Normal,
            effects: FX(
                T_Passive(continuous: false),
                A_SpawnFamiliar(maxInstances: 1, resourcePath: "Familiars/Familiar_FlyToy")
            )
        );

        // ── 606 일회용 밴드 뭉치 (Ball of Bandages) ──────────────────────
        // ✅ Orbit 공전 기본 동작
        // ❌ 4단계 진화 미구현 (TODO: FamiliarEvolution 시스템 — 아이템 재획득 시 업그레이드)
        CreateItem(
            id:          "606",
            krName:      "일회용 밴드 뭉치",
            keyword:     "Ball of Bandages",
            description: "캐릭터 주위를 도는 방어막 겸 타격 펫. 획득 시마다 4단계 진화.\n[미구현] 진화 시스템 — 현재 1단계 고정",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Normal,
            effects: FX(
                T_Passive(continuous: false),
                A_SpawnFamiliar(maxInstances: 1, resourcePath: "Familiars/Familiar_BandageBall")
            )
        );

        // ── 607 수호천사 열쇠고리 (Best Bud) ─────────────────────────────
        // ✅ OnTakeDamage + Orbit 완전 지원
        // 피격 시 소환 → 빠른 공전 속도는 Familiar_GuardKeyring 프리팹에서 orbitSpeed로 조정
        // maxInstances: 3 — 연속 피격 시 최대 3마리까지 누적 소환
        CreateItem(
            id:          "607",
            krName:      "수호천사 열쇠고리",
            keyword:     "Best Bud",
            description: "피격 시 캐릭터 궤도를 아주 빠르게 돌며 데미지를 주는 열쇠고리 소환.",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Rare,
            effects: FX(
                T_TakeDamage(),
                A_SpawnFamiliar(maxInstances: 3, resourcePath: "Familiars/Familiar_GuardKeyring")
            )
        );

        // ── 608 뚱뚱한 돼지 저금통 (Big Chubby) ──────────────────────────
        // ⚠ Homing 근사 (공격 방향 직진 미구현 — Homing 자폭으로 근사)
        // ❌ DirectionalLaunch 모드 — WeaponController.AttackDirection 참조 + 관통 이동 필요
        // 공격 시마다 소환 → maxInstances: 1로 동시 소환 1개 제한
        CreateItem(
            id:          "608",
            krName:      "뚱뚱한 돼지 저금통",
            keyword:     "Big Chubby",
            description: "공격하는 방향으로 무겁게 날아가 적을 관통하는 돼지 저금통 펫 소환.\n[미구현] 공격 방향 직진+관통 — 현재 Homing 자폭 근사",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Rare,
            effects: FX(
                T_Attack(),
                A_SpawnFamiliar(maxInstances: 1, resourcePath: "Familiars/Familiar_PiggyBank")
            )
        );

        // ── 609 여름철 왕부채 (Big Fan) ───────────────────────────────────
        // ⚠ Orbit 근사 (투사체 차단 미구현)
        // ❌ FamiliarMode.OrbitalShield — Collider 트리거 + Projectile 레이어 감지 후 Destroy 필요
        // 느린 공전: 프리팹의 orbitSpeed를 낮게 설정 (권장값: 40~60)
        CreateItem(
            id:          "609",
            krName:      "여름철 왕부채",
            keyword:     "Big Fan",
            description: "캐릭터 궤도를 느리게 돌며 투사체를 막아주는 방패 패밀리어 소환.\n[미구현] OrbitalShield 모드(투사체 차단) — 현재 Orbit 폴백",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Normal,
            effects: FX(
                T_Passive(continuous: false),
                A_SpawnFamiliar(maxInstances: 1, resourcePath: "Familiars/Familiar_BigFan")
            )
        );

        // ── 610 화난 불독 장난감 (Blood Puppy) ───────────────────────────
        // ✅ Orbit 기본 소환
        // ❌ GrowOnKill 미구현 — 킬 시 스케일 증가 + AttackDamage 누적 필요
        // ❌ FriendlyFire 미구현 — 일정 스택 이상 시 플레이어 공격 전환 필요
        CreateItem(
            id:          "610",
            krName:      "화난 불독 장난감",
            keyword:     "Blood Puppy",
            description: "적을 때릴수록 거대해지며 나중엔 플레이어까지 공격하는 불독 펫 소환.\n[미구현] GrowOnKill / FriendlyFire — 현재 Orbit 기본 동작",
            category:    ItemCategory.Special,
            rarity:      ItemRarity.Rare,
            effects: FX(
                T_Passive(continuous: false),
                A_SpawnFamiliar(maxInstances: 1, resourcePath: "Familiars/Familiar_BullDog")
            )
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TMA Assigner] 패밀리어 아이템 601~610 생성 완료 ✅");
    }

    private static void CreateItem(
        string id, string krName, string keyword, string description,
        ItemCategory category, ItemRarity rarity,
        List<IItemEffect> effects,
        List<StatEntry> statBonuses = null)
    {
        string path = $"Assets/Resources/Items/{id}.asset";

        // 이미 존재하면 덮어쓰지 않음
        if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null)
        {
            Debug.LogWarning($"[TMA Assigner] {id}.asset 이미 존재 — 건너뜀");
            return;
        }

        var item = ScriptableObject.CreateInstance<ItemData>();
        item.ItemID      = id;
        item.KR_Name     = krName;
        item.Keyword     = keyword;
        item.Description = description;
        item.Category    = category;
        item.Rarity      = rarity;
        item.PowerScore  = 3;
        item.Effects     = effects ?? new List<IItemEffect>();
        item.StatBonuses = statBonuses ?? new List<StatEntry>();

        AssetDatabase.CreateAsset(item, path);

        // ItemDatabase 자동 등록
        string dbPath = "Assets/Resources/ItemDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
        if (db != null)
        {
            var serializedDb = new SerializedObject(db);
            var listProp = serializedDb.FindProperty("_items");
            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = item;
            serializedDb.ApplyModifiedProperties();
            EditorUtility.SetDirty(db);
            Debug.Log($"[TMA Assigner] {id} → ItemDatabase 등록 완료");
        }
        else
        {
            Debug.LogWarning($"[TMA Assigner] ItemDatabase.asset 없음 — {id} 수동 등록 필요");
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 헬퍼 — 리스트 생성
    // ────────────────────────────────────────────────────────────────

    private static List<IItemEffect> FX(params IItemEffect[] effects)
        => new List<IItemEffect>(effects);

    // ── Trigger 팩토리 ───────────────────────────────────────────────

    private static OnAttackTrigger     T_Attack()       => new OnAttackTrigger();
    private static OnMeleeHitTrigger   T_MeleeHit()     => new OnMeleeHitTrigger();
    private static OnTakeDamageTrigger T_TakeDamage()   => new OnTakeDamageTrigger();
    private static OnKillTrigger       T_Kill()         => new OnKillTrigger();
    private static OnDashTrigger       T_Dash()         => new OnDashTrigger();
    private static OnRoomClearTrigger  T_RoomClear()    => new OnRoomClearTrigger();

    private static OnTimerTrigger T_Timer(float interval)
        => new OnTimerTrigger { Interval = interval };

    private static PassiveTrigger T_Passive(bool continuous)
        => new PassiveTrigger { Continuous = continuous };

    // ── Modifier 팩토리 ──────────────────────────────────────────────

    private static StatModifier      M_Stat(float damMul = 1f, float areaAdd = 0f)
        => new StatModifier { DamageMultiplier = damMul, AreaRadiusAdd = areaAdd };

    private static ChanceGateModifier M_Chance(float chance)
        => new ChanceGateModifier { Chance = chance };

    private static AddTagModifier    M_Tag(string tag)
        => new AddTagModifier { Tag = tag };

    private static RadiusModifier    M_Radius(float radius)
        => new RadiusModifier { Radius = radius };

    private static BounceModifier    M_Bounce(int count, float decay = 1f)
        => new BounceModifier { BounceCount = count, DamageDecayPerBounce = decay };

    private static HomingModifier    M_Homing(float strength)
        => new HomingModifier { HomingStrength = strength };

    // ── Action 팩토리 ────────────────────────────────────────────────

    /// 플레이어 공격력 기반 피해
    private static DealDamageAction A_Damage()
        => new DealDamageAction { UsePlayerDamage = true };

    /// 고정 수치 피해 (플레이어 공격력 무관)
    private static DealDamageAction A_DamageFlat(float amount)
        => new DealDamageAction { BaseDamage = amount, UsePlayerDamage = false };

    /// 자해 피해 (고정 수치)
    private static DealDamageAction A_SelfDamage(float amount)
        => new DealDamageAction { BaseDamage = amount, UsePlayerDamage = false, TargetSelf = true };

    /// 상태이상 적용
    private static ApplyStatusAction A_Status(string statusID, float chance = 1f)
        => new ApplyStatusAction { StatusID = statusID, Chance = chance };

    /// 피해 무효 (icd: 내부 쿨타임 초)
    private static NullifyDamageAction A_Nullify(float icd = 0.1f)
    {
        var a = new NullifyDamageAction();
        a.InternalCooldown = icd;
        return a;
    }

    /// 체력 회복
    private static HealSelfAction A_HealSelf(float healAmount, bool percent)
        => new HealSelfAction { HealAmount = healAmount, PercentOfMax = percent };

    /// 픽업 드롭 (프리팹은 Inspector에서 나중에 연결)
    private static DropPickupAction A_DropPickup(int count)
        => new DropPickupAction { DropCount = count };

    /// 패밀리어 소환
    /// resourcePath: Resources 폴더 기준 경로 (예: "Familiars/Familiar_Bee")
    private static SpawnFamiliarAction A_SpawnFamiliar(int maxInstances, string resourcePath = "Familiars/Familiar_Bee")
        => new SpawnFamiliarAction { MaxInstances = maxInstances, FamiliarResourcePath = resourcePath };

    /// 피의 서약 — HP를 1로 깎고 비율에 비례한 스탯 영구 증가
    private static BloodOathAction A_BloodOath(float atkScale = 5f, float spdScale = 1f)
        => new BloodOathAction { AttackDamageScale = atkScale, MoveSpeedScale = spdScale };
}
