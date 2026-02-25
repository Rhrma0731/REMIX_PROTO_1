using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// [에디터 전용] T-M-A 기반 근접 아이템 35종을 자동 생성하는 도구.
/// Unity 메뉴: Tools > Generate TMA Items (35)
///
/// 클릭 한 번에 Assets/Resources/Items/TMA/ 폴더에
/// 35개의 ItemData ScriptableObject 에셋이 생성된다.
/// 각 에셋에는 기획서에 명세된 Trigger, Modifier, Action이 미리 조립되어 있다.
///
/// [참고] 미구현 효과(MeleeSweep, SpawnExplosion 등)는
/// 가장 비슷한 기존 구현체로 임시 연결되어 있으며, 주석으로 표시됨.
/// </summary>
public class ItemDataTMAGenerator
{
    private const string OUTPUT_FOLDER = "Assets/Resources/Items/TMA";

    [MenuItem("Tools/Generate TMA Items (35)")]
    public static void Generate()
    {
        // 출력 폴더 보장
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Items");
        EnsureFolder(OUTPUT_FOLDER);

        int count = 0;

        // ══════════════════════════════════════════════════════════
        //  1. [Arm: 출력부] - 무기 형태 및 타격 판정 (12종)
        // ══════════════════════════════════════════════════════════

        count += Create("ARM_01", "대형 파리채", ItemCategory.Form, ItemRarity.Normal, BodyPart.ArmRight,
            "공격이 매우 느려지지만 화면 절반을 휩쓸며 적을 밀쳐냄.",
            "파리채", 2,
            Stats(S(StatType.AttackSpeed, -0.5f), S(StatType.Range, 1.5f), S(StatType.KnockbackForce, 200f)),
            new List<IItemEffect>
            {
                new OnAttackTrigger(),
                new RadiusModifier { Radius = 2.5f },
                // TODO: MeleeSweep → DealDamageAction(AoE)로 임시 대체
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("ARM_02", "전동 드릴", ItemCategory.Form, ItemRarity.Rare, BodyPart.ArmRight,
            "단타 공격이, 누르고 있는 동안 지속적으로 적을 갈아버리는 다단히트로 변경됨.",
            "드릴", 3,
            Stats(S(StatType.MoveSpeed, -2.75f)),
            new List<IItemEffect>
            {
                // TODO: OnAttack(Hold) → OnAttackTrigger로 임시 대체
                new OnAttackTrigger(),
                new StatModifier { DamageMultiplier = 0.3f },
                // TODO: MeleeDrill → DealDamageAction으로 임시 대체
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("ARM_03", "고무줄 요요", ItemCategory.Form, ItemRarity.Normal, BodyPart.ArmRight,
            "근접 주먹 판정이 커서 방향으로 날아갔다가 돌아오는 부메랑 형태가 됨.",
            "요요", 2,
            Stats(S(StatType.AttackDamage, -6f)),
            new List<IItemEffect>
            {
                new OnAttackTrigger(),
                new StatModifier { DamageMultiplier = 0.7f },
                // TODO: FireMeleeHitbox(부메랑) → DealDamageAction으로 임시 대체
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("ARM_04", "불량 가스 토치", ItemCategory.Form, ItemRarity.Rare, BodyPart.ArmRight,
            "근접 무기가 적을 관통하며 화상을 입히는 짧은 화염 방사로 변경됨.",
            "화염", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnAttackTrigger(),
                new AddTagModifier { Tag = "Fire" },
                new RadiusModifier { Radius = 1.5f },
                // TODO: MeleeCone → ApplyStatusAction(ST_BURN)으로 임시 대체
                new ApplyStatusAction { StatusID = "ST_BURN" },
            });

        count += Create("ARM_05", "안마기 모터", ItemCategory.Modifier, ItemRarity.Normal, BodyPart.ArmRight,
            "공격력이 벼룩 수준이 되지만 타격 속도가 미친 듯이 빨라짐.",
            "모터", 2,
            Stats(S(StatType.AttackDamage, -16f), S(StatType.AttackSpeed, 4f)),
            new List<IItemEffect>
            {
                // 패시브 — 스탯 변경은 StatBonuses로 처리, 파이프라인은 형식적
                new PassiveTrigger(),
                new StatModifier { DamageMultiplier = 0.2f },
            });

        count += Create("ARM_06", "장난감 광선검", ItemCategory.Form, ItemRarity.Epic, BodyPart.ArmRight,
            "적의 방어력을 완전히 무시하는 길쭉한 베기 공격을 가함.",
            "광선검", 4,
            Stats(S(StatType.Range, 0.5f)),
            new List<IItemEffect>
            {
                new OnAttackTrigger(),
                // TODO: ArmorPenetration(100%) → StatModifier(1.5x)로 임시 대체
                new StatModifier { DamageMultiplier = 1.5f },
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("ARM_07", "뚫어뻥", ItemCategory.Modifier, ItemRarity.Normal, BodyPart.ArmRight,
            "적을 때리면 적을 내 쪽으로 살짝 끌어당겨 콤보를 이어가게 함.",
            "뚫어뻥", 1,
            Stats(),
            new List<IItemEffect>
            {
                new OnMeleeHitTrigger(),
                // TODO: PullTarget(거리2) → DealDamageAction으로 임시 대체 (풀 효과 미구현)
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("ARM_08", "낡은 프라이팬", ItemCategory.Modifier, ItemRarity.Rare, BodyPart.ArmRight,
            "타격 시 깡깡 소리와 함께 높은 확률로 적을 기절시킴.",
            "프라이팬", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnMeleeHitTrigger(),
                new ChanceGateModifier { Chance = 0.25f },
                new ApplyStatusAction { StatusID = "ST_STUN" },
            });

        count += Create("ARM_09", "테이프 감긴 쇠파이프", ItemCategory.Modifier, ItemRarity.Normal, BodyPart.ArmRight,
            "묵직한 타격감과 함께 적에게 지속 출혈 피해를 입힘.",
            "쇠파이프", 2,
            Stats(),
            new List<IItemEffect>
            {
                new OnMeleeHitTrigger(),
                // TODO: Bleed(출혈) → ST_BURN(화상 도트)으로 임시 대체 (가장 유사한 도트 데미지)
                new ApplyStatusAction { StatusID = "ST_BURN" },
            });

        count += Create("ARM_10", "부러진 골프채", ItemCategory.Modifier, ItemRarity.Rare, BodyPart.ArmRight,
            "체력이 적은 적을 때릴수록 치명타가 터질 확률이 급증함.",
            "골프채", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnMeleeHitTrigger(),
                // TODO: CritChance(적 체력 비례) → StatModifier(1.5x)로 임시 대체
                new StatModifier { DamageMultiplier = 1.5f },
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("ARM_11", "압축 스프링 주먹", ItemCategory.Form, ItemRarity.Epic, BodyPart.ArmRight,
            "공격 키를 모았다 떼면 전방으로 강하게 돌진하며 묵직한 한 방을 꽂음.",
            "스프링", 4,
            Stats(),
            new List<IItemEffect>
            {
                // TODO: OnAttack(Charge) → OnAttackTrigger로 임시 대체
                new OnAttackTrigger(),
                new StatModifier { DamageMultiplier = 3f },
                // TODO: DashForward & MeleeHit → DealDamageAction으로 임시 대체
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("ARM_12", "쌍절곤 (줄넘기)", ItemCategory.Form, ItemRarity.Rare, BodyPart.ArmRight,
            "한 번 공격할 때마다 약간의 시차를 두고 두 번의 타격 판정이 발생함.",
            "쌍절곤", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnAttackTrigger(),
                // TODO: MultiHit(2회) → StatModifier(0.7x per hit)로 임시 대체
                new StatModifier { DamageMultiplier = 0.7f },
                new DealDamageAction { UsePlayerDamage = true },
            });

        // ══════════════════════════════════════════════════════════
        //  2. [Body: 반응부] - 타격 후폭풍 및 엔진 기믹 (12종)
        // ══════════════════════════════════════════════════════════

        count += Create("BODY_01", "고압축 피스톤", ItemCategory.Modifier, ItemRarity.Rare, BodyPart.Body,
            "근접 타격을 성공할 때마다 해당 지점에서 광역 폭발이 일어남.",
            "피스톤", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnMeleeHitTrigger(),
                new RadiusModifier { Radius = 2f },
                // TODO: SpawnExplosion → DealDamageAction(AoE)로 임시 대체
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("BODY_02", "용수철 코일", ItemCategory.Modifier, ItemRarity.Normal, BodyPart.Body,
            "제자리 근접 공격이 전방으로 훅 튀어 나가며 때리는 돌진 베기로 변경됨.",
            "코일", 2,
            Stats(),
            new List<IItemEffect>
            {
                new OnAttackTrigger(),
                // TODO: DashForward(거리3) → StatModifier(Range 보너스)로 임시 대체
                new StatModifier { AreaRadiusAdd = 1.5f },
            });

        count += Create("BODY_03", "망가진 핀볼 범퍼", ItemCategory.Modifier, ItemRarity.Rare, BodyPart.Body,
            "피격 시 적을 강하게 튕겨내며 반사 데미지를 줌.",
            "범퍼", 3,
            Stats(S(StatType.KnockbackForce, 150f)),
            new List<IItemEffect>
            {
                new OnTakeDamageTrigger(),
                new StatModifier { DamageMultiplier = 1.5f },
                new RadiusModifier { Radius = 2f },
                // TODO: DealDamage(Attacker) → DealDamageAction(AoE)로 임시 대체
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("BODY_04", "압력 밥솥 뚜껑", ItemCategory.Modifier, ItemRarity.Epic, BodyPart.Body,
            "피격 시 주변 사방에 적과 나를 가리지 않는 시한폭탄 5개를 흩뿌림.",
            "밥솥", 4,
            Stats(),
            new List<IItemEffect>
            {
                new OnTakeDamageTrigger(),
                new RadiusModifier { Radius = 3f },
                // TODO: SpawnExplosives(5개) → DealDamageAction(AoE)로 임시 대체
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("BODY_05", "정전기 스웨터", ItemCategory.Modifier, ItemRarity.Rare, BodyPart.Body,
            "적을 근접 타격할 때마다 확률적으로 주변 적에게 튕기는 번개를 방출함.",
            "스웨터", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnMeleeHitTrigger(),
                new ChanceGateModifier { Chance = 0.3f },
                new AddTagModifier { Tag = "Electric" },
                // TODO: FireChainLightning → ApplyStatusAction(ST_CHAIN)으로 임시 대체
                new ApplyStatusAction { StatusID = "ST_CHAIN" },
            });

        count += Create("BODY_06", "뽁뽁이 갑옷", ItemCategory.Modifier, ItemRarity.Epic, BodyPart.Body,
            "10초마다 적의 타격을 1회 완전히 무시함.",
            "뽁뽁이", 4,
            Stats(),
            new List<IItemEffect>
            {
                // ICD를 10초로 설정하여 쿨다운 근사 구현
                new OnTakeDamageTrigger { InternalCooldown = 10f },
                new NullifyDamageAction(),
            });

        count += Create("BODY_07", "낡은 선풍기 모터", ItemCategory.Modifier, ItemRarity.Rare, BodyPart.Body,
            "대시한 경로에 적의 이동 속도를 크게 늦추는 소용돌이를 남김.",
            "선풍기", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnDashTrigger(),
                new RadiusModifier { Radius = 3f },
                // TODO: SpawnAura(Tornado) → ApplyStatusAction(ST_SLOW)로 임시 대체
                new ApplyStatusAction { StatusID = "ST_SLOW" },
            });

        count += Create("BODY_08", "굳은 시멘트 포대", ItemCategory.Special, ItemRarity.Epic, BodyPart.Body,
            "가만히 2초간 서 있으면 완벽한 무적 상태의 돌이 됨 (이동/공격 시 해제).",
            "시멘트", 4,
            Stats(),
            new List<IItemEffect>
            {
                // TODO: OnIdle(2초) → OnTimerTrigger(2초)로 임시 대체
                new OnTimerTrigger { Interval = 2f },
                // TODO: Stone form → NullifyDamageAction으로 임시 대체
                new NullifyDamageAction(),
            });

        count += Create("BODY_09", "누액 건전지", ItemCategory.Modifier, ItemRarity.Rare, BodyPart.Body,
            "걸어 다니는 궤적을 따라 적에게 지속 피해를 주는 독 장판을 흘림.",
            "건전지", 3,
            Stats(),
            new List<IItemEffect>
            {
                // TODO: OnMove → OnTimerTrigger(0.5초)로 임시 대체
                new OnTimerTrigger { Interval = 0.5f },
                new AddTagModifier { Tag = "Poison" },
                new RadiusModifier { Radius = 1.5f },
                // TODO: SpawnHazard(PoisonPuddle) → DealDamageAction(AoE)로 임시 대체
                new DealDamageAction { BaseDamage = 3f, UsePlayerDamage = false },
            });

        count += Create("BODY_10", "고장난 토스터", ItemCategory.Modifier, ItemRarity.Rare, BodyPart.Body,
            "대시할 때마다 불길을 남겨 적에게 화상을 입힘.",
            "토스터", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnDashTrigger(),
                new AddTagModifier { Tag = "Fire" },
                new RadiusModifier { Radius = 2f },
                // TODO: SpawnHazard(FireWall) → ApplyStatusAction(ST_BURN)으로 임시 대체
                new ApplyStatusAction { StatusID = "ST_BURN" },
            });

        count += Create("BODY_11", "고무 튜브", ItemCategory.Special, ItemRarity.Normal, BodyPart.Body,
            "벽을 향해 대시하면 튕겨 나오며 대시 쿨타임이 즉시 초기화됨.",
            "튜브", 2,
            Stats(),
            new List<IItemEffect>
            {
                // TODO: OnDashHitWall → OnDashTrigger로 임시 대체
                new OnDashTrigger(),
                // TODO: Bounce & ResetDashCooldown → HealSelfAction으로 임시 대체
                new HealSelfAction { HealAmount = 3f },
            });

        count += Create("BODY_12", "장난감 벌집", ItemCategory.Modifier, ItemRarity.Legend, BodyPart.Body,
            "피격 시 적을 끝까지 쫓아가 자폭하는 유도 벌(드론) 3마리를 방출함.",
            "벌집", 5,
            Stats(),
            new List<IItemEffect>
            {
                new OnTakeDamageTrigger(),
                new RadiusModifier { Radius = 5f },
                // TODO: SpawnDrone(3기) → DealDamageAction(AoE)로 임시 대체
                new DealDamageAction { UsePlayerDamage = true },
            });

        // ══════════════════════════════════════════════════════════
        //  3. [Head: 제어부] - 전투 리듬, 타이머, 유틸리티 (11종)
        // ══════════════════════════════════════════════════════════

        count += Create("HEAD_01", "누전된 헤드폰", ItemCategory.Special, ItemRarity.Normal, BodyPart.Head,
            "3초마다 자신에게 1 데미지를 입힘. (피격 시 발동하는 Body 아이템들과 강력한 시너지)",
            "헤드폰", 2,
            Stats(),
            new List<IItemEffect>
            {
                new OnTimerTrigger { Interval = 3f },
                new DealDamageAction { BaseDamage = 1f, UsePlayerDamage = false, TargetSelf = true },
            });

        count += Create("HEAD_02", "카운터 센서", ItemCategory.Special, ItemRarity.Legend, BodyPart.Head,
            "정확한 타이밍에 적을 때리면 피해를 씹고 3배의 카운터 데미지를 먹임(패링).",
            "카운터", 5,
            Stats(),
            new List<IItemEffect>
            {
                // TODO: OnTakeDamage(0.2초 내 공격 시) → OnTakeDamageTrigger로 임시 대체
                new OnTakeDamageTrigger(),
                new StatModifier { DamageMultiplier = 3f },
                new NullifyDamageAction(),
                // 카운터 공격: 주변 광역 피해
                new RadiusModifier { Radius = 2f },
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("HEAD_03", "장난감 메트로놈", ItemCategory.Special, ItemRarity.Rare, BodyPart.Head,
            "근접 공격을 3번 연속으로 맞추면 3번째 타격은 무조건 치명타가 터짐.",
            "메트로놈", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnMeleeHitTrigger(),
                // TODO: Stack(3회) → StatModifier(2x)로 임시 대체 (스택 미구현)
                new StatModifier { DamageMultiplier = 2f },
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("HEAD_04", "리사이클 렌즈", ItemCategory.Special, ItemRarity.Epic, BodyPart.Head,
            "맵에 떨어진 아이템들을 10초마다 완전히 다른 무작위 아이템으로 변환함.",
            "렌즈", 4,
            Stats(),
            new List<IItemEffect>
            {
                new OnTimerTrigger { Interval = 10f },
                // TODO: RerollDroppedItems → 고유 메카닉, 현재 빈 파이프라인
                // 향후 RerollAction 구현 필요
            });

        count += Create("HEAD_05", "사이렌 경광등", ItemCategory.Special, ItemRarity.Rare, BodyPart.Head,
            "대시할 때 주변 적들을 강제로 도망치게 만듦.",
            "사이렌", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnDashTrigger(),
                new RadiusModifier { Radius = 4f },
                // TODO: Fear(공포) → ST_STUN(기절)으로 임시 대체 (가장 유사한 행동 불능)
                new ApplyStatusAction { StatusID = "ST_STUN" },
            });

        count += Create("HEAD_06", "양은 냄비", ItemCategory.Special, ItemRarity.Rare, BodyPart.Head,
            "적의 원거리 투사체 공격을 맞을 때 30% 확률로 튕겨내어 적에게 돌려줌.",
            "냄비", 3,
            Stats(),
            new List<IItemEffect>
            {
                // TODO: OnTakeDamage(투사체 한정) → OnTakeDamageTrigger로 임시 대체
                new OnTakeDamageTrigger(),
                new ChanceGateModifier { Chance = 0.3f },
                // TODO: DeflectProjectile → NullifyDamageAction으로 임시 대체
                new NullifyDamageAction(),
            });

        count += Create("HEAD_07", "돋보기 안경", ItemCategory.Special, ItemRarity.Rare, BodyPart.Head,
            "화상 상태인 적을 근접 타격하면 데미지가 2배로 들어감.",
            "돋보기", 3,
            Stats(),
            new List<IItemEffect>
            {
                new OnMeleeHitTrigger(),
                // TODO: Condition(Target is Burning) → StatModifier(2x)로 임시 대체
                new StatModifier { DamageMultiplier = 2f },
                new DealDamageAction { UsePlayerDamage = true },
            });

        count += Create("HEAD_08", "망가진 나침반", ItemCategory.Special, ItemRarity.Normal, BodyPart.Head,
            "적을 향해 다가갈 때 이동 속도가 폭발적으로 증가함 (추격 특화).",
            "나침반", 2,
            Stats(S(StatType.MoveSpeed, 2.2f)),
            new List<IItemEffect>
            {
                // TODO: Condition(Moving towards Enemy) → PassiveTrigger로 임시 대체
                // 이속 증가는 StatBonuses로 상시 적용 (조건부 미구현)
                new PassiveTrigger { Continuous = true },
            });

        count += Create("HEAD_09", "철제 깔때기", ItemCategory.Special, ItemRarity.Epic, BodyPart.Head,
            "자해 데미지를 0으로 만들지만, 최대 체력이 절반으로 깎임. (자해 시너지 템의 리스크 조절용)",
            "깔때기", 4,
            Stats(S(StatType.MaxHealth, -50f)),
            new List<IItemEffect>
            {
                // TODO: Immunity(SelfDamage) → PassiveTrigger 1회 발동 (면역 미구현)
                // 최대체력 감소는 StatBonuses로 처리
                new PassiveTrigger(),
            });

        count += Create("HEAD_10", "삐에로 코", ItemCategory.Special, ItemRarity.Normal, BodyPart.Head,
            "적을 처치할 때마다 10% 확률로 체력 회복 아이템을 드랍함.",
            "삐에로", 2,
            Stats(),
            new List<IItemEffect>
            {
                new OnKillTrigger(),
                new ChanceGateModifier { Chance = 0.1f },
                // TODO: DropPickup(Health) → HealSelfAction으로 임시 대체
                new HealSelfAction { HealAmount = 10f },
            });

        count += Create("HEAD_11", "구리 선 다발", ItemCategory.Special, ItemRarity.Normal, BodyPart.Head,
            "모든 근접 공격에 감전 속성을 부여하여 기계류 적에게 추가 피해를 줌.",
            "구리선", 2,
            Stats(),
            new List<IItemEffect>
            {
                new OnMeleeHitTrigger(),
                new AddTagModifier { Tag = "Electric" },
                new ApplyStatusAction { StatusID = "ST_STUN" },
            });

        // ══════════════════════════════════════════════════════════
        //  완료
        // ══════════════════════════════════════════════════════════

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ItemDataTMAGenerator] {count}개의 T-M-A 아이템 에셋 생성 완료! → {OUTPUT_FOLDER}");
        EditorUtility.DisplayDialog("TMA 아이템 생성 완료",
            $"{count}개의 아이템이 {OUTPUT_FOLDER}에 생성되었습니다.\n\n" +
            "ItemDatabase에 자동 등록하려면\nTools > Import Items from CSV 또는\n수동으로 ItemDatabase.asset에 추가하세요.",
            "확인");
    }

    // ── 헬퍼 메서드 ─────────────────────────────────────────────

    /// <summary>ItemData 에셋 하나를 생성하고 디스크에 저장한다.</summary>
    private static int Create(
        string id, string krName, ItemCategory category, ItemRarity rarity,
        BodyPart bodyPart, string description, string keyword, int powerScore,
        List<StatEntry> statBonuses, List<IItemEffect> effects)
    {
        string assetPath = $"{OUTPUT_FOLDER}/{id}_{krName}.asset";

        // 이미 존재하면 덮어쓰기
        var existing = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(assetPath);

        var item = ScriptableObject.CreateInstance<ItemData>();
        item.ItemID = id;
        item.KR_Name = krName;
        item.Keyword = keyword;
        item.Description = description;
        item.Category = category;
        item.Rarity = rarity;
        item.TargetBodyPart = bodyPart;
        item.PowerScore = powerScore;
        item.StatusTriggerChance = 1f;
        item.PartScale = Vector2.one;
        item.PartColor = Color.white;
        item.StatBonuses = statBonuses ?? new List<StatEntry>();
        item.Effects = effects ?? new List<IItemEffect>();

        AssetDatabase.CreateAsset(item, assetPath);
        return 1;
    }

    /// <summary>StatEntry 축약 생성</summary>
    private static StatEntry S(StatType type, float value)
    {
        return new StatEntry { Type = type, Value = value };
    }

    /// <summary>StatEntry 리스트 축약 생성</summary>
    private static List<StatEntry> Stats(params StatEntry[] entries)
    {
        return new List<StatEntry>(entries);
    }

    /// <summary>폴더가 없으면 생성한다.</summary>
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int lastSlash = path.LastIndexOf('/');
        string parent = path.Substring(0, lastSlash);
        string folderName = path.Substring(lastSlash + 1);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
