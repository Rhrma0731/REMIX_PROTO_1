# RE:MIX PROTO_1 — 작업 인수인계 가이드라인

> 마지막 업데이트: 2026-02-21
> 목적: 다음 세션에서 컨텍스트 없이도 즉시 작업을 이어받을 수 있도록 작성된 문서입니다.

---

## 1. 프로젝트 개요

- **장르**: 로그라이크 아레나 슈터 (3D 씬, 2D 스프라이트)
- **플랫폼**: Unity 6 (URP), 씬명 `claude remix.unity`
- **핵심 루프**: 웨이브 클리어 → 아이템 보상 선택 → 다음 스테이지
- **아이템 시스템**: CSV → ScriptableObject(ItemData) → ItemDatabase → PlayerStats/WeaponController 적용

---

## 2. 현재 구현 완료 목록

### 2-1. 코어 시스템
| 시스템 | 상태 | 주요 파일 |
|--------|------|-----------|
| 플레이어 이동 / 조준 | ✅ | `PlayerMovement.cs` |
| 플레이어 스탯 + 아이템 적용 | ✅ | `PlayerStats.cs` |
| 무기 공격 (사거리, 크리티컬) | ✅ | `WeaponController.cs` |
| 플레이어 외형 — 스프라이트 교체 + Scale + Color Tint | ✅ | `PlayerAppearance.cs` |
| 아이템 장착 시각 이펙트 (Flash + Shake + ScalePunch + Glitch 동시) | ✅ | `PlayerAppearance.cs` |
| 적 기본 FSM (Chase / Attack / Stun / Die) | ✅ | `EnemyBase.cs` |
| 적 장난감 물리 넉백 + 바운스 | ✅ | `EnemyBase.cs` |
| 스테이지 / 웨이브 진행 | ✅ | `StageManager.cs` |
| 보상 UI (체인 드롭 애니메이션) | ✅ | `RewardSystemManager.cs`, `RewardSlotUI.cs` |
| 난이도 선택 (Easy/Normal/Hard) | ✅ | `DifficultySelectManager.cs`, `DifficultyData.cs` |
| 코인 드롭 / 자석 흡수 / 수집 범위 | ✅ | `CoinPickup.cs`, `CurrencyManager.cs`, `CoinUI.cs` |
| 피격 피드백 (글리치 셰이더, 카메라 흔들림) | ✅ | `CombatFeedback.cs`, `PlayerHitFeedback.cs` |
| 체력 UI (플레이어 / 적) | ✅ | `PlayerHealthUI.cs`, `EnemyHealthBar.cs` |
| 게임 오버 | ✅ | `GameOverManager.cs` |
| 무기 이름 자동 조합 | ✅ | `WeaponNameBuilder.cs` |

### 2-2. 상태 이상 (StatusEffectManager.cs)
| Status_ID | 효과 | 상태 |
|-----------|------|------|
| ST_BURN | 3초간 0.5초마다 공격력 20% 도트 피해 | ✅ |
| ST_SLOW | 3초간 이동속도 50% 감소 | ✅ |
| ST_STUN | `_stunDuration`(기본 2초) 동안 완전 정지 | ✅ |
| ST_GLITCH | 2초간 0.25초마다 속도를 0~20% 또는 200~400%로 무작위 변경 | ✅ |
| ST_DEATH | 즉사 (99999 피해) — StatusTriggerChance로 확률 제어 | ✅ |
| ST_CHAIN | 주 타격 후 반경 3.5m 내 최대 3개 적에게 공격력 50% 연쇄 피해 | ✅ |

### 2-3. 아이템 데이터 인프라
| 항목 | 상태 |
|------|------|
| ItemData ScriptableObject 구조 | ✅ |
| StatType enum (None/MaxHealth/AttackDamage/AttackSpeed/MoveSpeed/Range/KnockbackForce/CritChance/CritMultiplier/CollectionRange) | ✅ |
| PartScale (Vector2) + PartColor (Color) 필드 — 파츠 크기 및 색상 틴트 | ✅ |
| StatusTriggerChance 필드 (0~1, 기본 1.0) | ✅ |
| ItemDatabase ScriptableObject | ✅ |
| ItemCSVImporter (Tools > Import Items from CSV) | ✅ |
| CSV 별칭 시스템 (DetectionRange→Range, Grade→Rarity, 조합 키워드→Keyword 등) | ✅ |
| **아이템 에셋 실제 데이터** | ✅ 35개 임포트 완료 (`Assets/Resources/Items/`) |
| **ItemDatabase 자동 로드** | ✅ StageManager Awake에서 Resources.Load로 자동 연결 |

---

## 3. 미완성 / 남은 작업

### 우선순위 목록

- [ ] **ItemData 스프라이트 연결** — `Icon`, `AppearanceSprite`, `PedestalSprite` 모두 null 상태. 스프라이트 에셋 준비 후 각 .asset 파일에 수동 연결 (또는 CSV에 경로 컬럼 추가 후 임포터 확장)
- [ ] **TargetBodyPart 기획 확정** — 현재 Form 아이템은 `ArmRight`(테스트값), Modifier 아이템은 `Head`(기본값). 실제 기획서 기준으로 배정 필요
- [ ] **게임 클리어 화면** — `StageManager.OnAllStagesComplete` 이벤트를 구독하는 UI 없음
- [ ] **특수 공격 시스템** — 아래 표 참조

### 미구현 특수 공격 시스템

아이템 시트에 다음 공격 유형이 정의되어 있으나 코드가 없음:

| 아이템 ID | 이름 | 필요한 시스템 |
|-----------|------|---------------|
| 103 | 뭉툭한 팔 | 관통(Pierce) 공격 |
| 104 | 접착제 팔 | 적 달라붙기 (ST_BOND 특수) |
| 105 | 고무 팔 | 반사 투사체 |
| 203 | 돌멩이 주먹 | 충격파 (OverlapSphere 즉시 피해) |
| 204 | 드릴 팔 | 지속 관통 |
| 205 | 집게 팔 | 잡기 + 투척 |
| 206 | 유압 팔 | 범위 넓은 후방 넉백 |
| 207 | 독 분사기 | 독 도트 (ST_POISON — 미정의) |
| 303 | 레이저 포 | 레이저 빔 (선형 히트스캔) |
| 304 | 미사일 발사기 | 유도 미사일 |
| 305 | 냉동 포 | 즉시 동결 (ST_FREEZE — 미정의) |
| 401~405 | 전설 아이템들 | 각각 고유 메카닉 필요 |

**권장 구현 순서**: 스탯 아이템(102, 201~202, 301~302) 먼저 → 단순 Status 아이템 → 복잡한 공격 메카닉 순

---

## 4. 알려진 버그 / 리스크

| 항목 | 설명 | 심각도 |
|------|------|--------|
| AppearanceSprite 없음 | 모든 아이템 null → 파츠 스프라이트 교체 안 됨 (Scale/Color/이펙트는 작동) | 낮음 |
| 306 ST_STUN vs ST_FREEZE 불일치 | 기획 확인 전까지 ST_STUN으로 처리됨 | 중간 |
| CombatFeedback GlitchRoutine | MissingReferenceException 방어 코드 적용 완료, 재발 시 확인 필요 | 낮음 |

---

## 5. 핵심 파일 구조

```
Assets/
├── Editor/
│   └── ItemCSVImporter.cs          ← CSV → ItemData 에셋 변환 도구
├── Resources/
│   ├── ItemTable.csv               ← ✅ 생성됨 (35개 아이템)
│   ├── ItemDatabase.asset          ← ✅ 자동 생성됨
│   └── Items/                      ← ✅ 35개 .asset 파일
├── Scripts/
│   ├── Enemy/
│   │   └── EnemyBase.cs            ← 적 FSM, 넉백, ApplyExternalStun
│   ├── Item/
│   │   ├── ItemData.cs             ← ScriptableObject, StatType enum, PartScale/PartColor
│   │   ├── ItemDatabase.cs         ← 전체 아이템 목록 컨테이너
│   │   ├── CoinPickup.cs           ← 코인 자석, CollectionRange 적용
│   │   ├── RewardSlotUI.cs         ← 보상 슬롯 단일 UI
│   │   └── WeaponNameBuilder.cs    ← Keyword → 무기 이름 조합
│   ├── Manager/
│   │   ├── StageManager.cs         ← 스테이지/웨이브/보상 흐름 제어
│   │   ├── RewardSystemManager.cs  ← 보상 UI 애니메이션 + 아이템 흡수
│   │   ├── StatusEffectManager.cs  ← 6종 상태 이상 적용
│   │   ├── CurrencyManager.cs      ← 코인 수집/관리
│   │   ├── DifficultySelectManager.cs ← 난이도 선택 UI
│   │   ├── DifficultyData.cs       ← DifficultySettings 데이터 클래스
│   │   └── GameOverManager.cs      ← 게임 오버 처리
│   ├── Player/
│   │   ├── PlayerStats.cs          ← 스탯 계산, 아이템 적용
│   │   ├── PlayerMovement.cs       ← 이동 + 조준
│   │   ├── WeaponController.cs     ← 공격, 크리티컬, 사거리
│   │   └── PlayerAppearance.cs     ← 스프라이트 장착, PartScale/Color, 이펙트
│   ├── Combat/
│   │   ├── CombatFeedback.cs       ← 글리치 셰이더, 히트스탑
│   │   └── PlayerHitFeedback.cs    ← 플레이어 피격 피드백
│   └── UI/
│       ├── PlayerHealthUI.cs
│       ├── EnemyHealthBar.cs
│       ├── CoinUI.cs
│       └── DifficultySlotUI.cs
```

---

## 6. 아키텍처 메모 (주의사항)

### StatType 연결 체인
```
ItemData.StatBonuses (List<StatEntry>)
    → PlayerStats.ApplyItem() → _bonusStats 누적
    → PlayerStats.BonusXxx 프로퍼티 → WeaponController / CoinPickup 등에서 읽음
```
새 StatType 추가 시: **ItemData enum → StatBlock 필드 → operator+ → ApplyItem → 접근자 → 사용처** 순으로 모두 수정 필요.

### 외형 시스템 (PlayerAppearance.cs)
- `BodyPartSlot`에 `OriginalScale`, `OriginalColor` 저장 → `ResetPart()`/`ResetAllParts()` 시 복원
- `EquipItem()` 호출 순서: ① AppearanceSprite 교체 (null이면 유지) → ② PartScale 적용 → ③ PartColor 적용 → ④ BondImpactRoutine 시작
- `BondImpactRoutine`: Flash / ScalePunch / Shake / Glitch 4가지 이펙트가 **단일 루프**에서 독립 타이머로 동시 진행

### StatusEffectManager 호출 위치
`WeaponController.cs:171`에서 히트 판정 직후 `StatusEffectManager.Instance?.ApplyEffects(enemy)` 호출. 이 연결이 끊기면 상태 이상이 전혀 발동하지 않음.

### EnemyBase 외부 스턴 (ST_STUN)
`_isExternallyStunned` 플래그가 `EnterState(Chase)`의 `isStopped = false` 실행을 막아줌. 넉백 바운스 복귀 시에도 정상 작동.

### 보상 시스템 타이밍
마지막 웨이브 클리어 → `RewardSystemManager.ShowRewards()` → `Time.timeScale = 0` → 아이템 선택 → `OnRewardSequenceComplete` 이벤트 → `StageManager.OnRewardComplete()` → 다음 스테이지. 중간에 이벤트 구독이 끊기면 게임이 멈춤.

---

## 7. 작업 재개 시 첫 번째 할 일

1. **이 문서 먼저 읽기**
2. `Assets/Scripts/` 전체를 훑어 현재 컴파일 오류 없는지 확인
3. 플레이 테스트: 아레나 진입 → 전투 → 보상 선택 → 파츠 색상/크기 변화 확인
4. 스프라이트 에셋 준비 → `Icon`, `AppearanceSprite` 연결 (우선순위 높음)
5. 특수 공격 시스템 구현 시작 (원하는 아이템 ID 선택)

---

## 8. 완료 이력

| 날짜 | 작업 내용 |
|------|----------|
| 2026-02-21 | StatusEffectManager 씬 배치 확인 (GameManagers 오브젝트) |
| 2026-02-21 | CSV 임포터 수정 (별칭 추가, NULL 처리) 및 35개 아이템 임포트 |
| 2026-02-21 | StageManager _rewardPool → ItemDatabase 자동 로드로 변경 |
| 2026-02-21 | TempStickyHand NavMesh 오류 수정 (위치 SpawnPoint_1으로 이동) |
| 2026-02-21 | ItemData에 PartScale/PartColor 필드 추가 |
| 2026-02-21 | PlayerAppearance 외형 시스템 확장 (Scale/Color 적용, OriginalScale/Color 저장·복원) |
| 2026-02-21 | BondImpactRoutine 재설계 — Flash + Shake + ScalePunch + Glitch 동시 진행 |
| 2026-02-21 | 시각 이펙트 버그 3종 수정 (AppearanceSprite null 가드, TargetBodyPart 기본값 수정, _playerAppearance null 체크) |
| 2026-02-21 | CSV 재임포트 — Form 아이템 8종 ArmRight 배정 확인 |

---

*이 문서는 `REMIX_PROTO_1/HANDOFF.md`에 저장됩니다.*
