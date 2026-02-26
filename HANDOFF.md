# RE:MIX PROTO_1 — 작업 인수인계 가이드라인

> 마지막 업데이트: 2026-02-26
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
| 플레이어 부활 + 무적 시스템 | ✅ | `PlayerStats.cs` (Revive, SetInvincible) |
| 무기 공격 (사거리, 크리티컬) | ✅ | `WeaponController.cs` |
| 플레이어 외형 — 스프라이트 교체 + Scale + Color Tint | ✅ | `PlayerAppearance.cs` |
| 아이템 장착 시각 이펙트 (Flash + Shake + ScalePunch + Glitch 동시) | ✅ | `PlayerAppearance.cs` |
| 적 기본 FSM (Chase / Attack / Stun / Die) | ✅ | `EnemyBase.cs` |
| 적 장난감 물리 넉백 + 바운스 | ✅ | `EnemyBase.cs` |
| **캡슐 낙하 스폰 연출** | ✅ | `GachaCapsuleSpawner.cs` |
| **지형 장애물 (NavMesh Carving)** | ✅ | `ObstacleController.cs` |
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

### 2-3. T-M-A 이펙트 파이프라인

아이작 스타일의 수백 가지 아이템 효과를 조합할 수 있는 모듈형 시스템.
`ItemData.Effects` 리스트에 `[SerializeReference]`로 블록을 배치하면 자동 작동.

#### 2-3-1. 코어 인프라
| 항목 | 상태 | 주요 파일 |
|------|------|-----------|
| IItemEffect 인터페이스 + ItemEffectRole enum | ✅ | `IItemEffect.cs` |
| ItemEffectBase (ICD 안전장치, sealed Execute) | ✅ | `ItemEffectBase.cs` |
| ItemEffectContext (파이프라인 데이터 컨테이너) | ✅ | `ItemEffectContext.cs` |
| PlayerEventManager (이벤트 허브 — 방송국) | ✅ | `PlayerEventManager.cs` |
| ItemEffectRunner + ActiveItemPipeline (T→M→A 오케스트레이터) | ✅ | `ItemEffectRunner.cs` |
| ItemEffectVFX (컬러 플래시, 파티클 버스트/상승) | ✅ | `ItemEffectVFX.cs` |

#### 2-3-2. Trigger 모듈 (언제 발동?)
| 클래스 | 발동 조건 | 상태 |
|--------|-----------|------|
| OnAttackTrigger | 플레이어가 공격을 시도할 때 | ✅ |
| OnMeleeHitTrigger | 공격이 적에게 실제로 적중할 때 | ✅ |
| OnTakeDamageTrigger | 플레이어가 피해를 받을 때 | ✅ |
| OnKillTrigger | 적을 처치할 때 | ✅ |
| OnDashTrigger | 대시할 때 | ✅ |
| OnTimerTrigger | 매 N초마다 | ✅ |
| PassiveTrigger | 장착 즉시 (또는 매 1초) | ✅ |
| **OnFatalDamageTrigger** | **HP 0 직전 (사망 가로채기, 횟수 제한)** | ✅ NEW |
| **OnRoomClearTrigger** | **웨이브/방 클리어 시** | ✅ NEW |

#### 2-3-3. Modifier 모듈 (어떻게 변형?)
| 클래스 | 변형 내용 | 상태 |
|--------|-----------|------|
| StatModifier | 데미지 배율, 범위 추가 | ✅ |
| ChanceGateModifier | 확률 실패 시 파이프라인 차단 | ✅ |
| AddTagModifier | 원소 속성 부여 (Fire→ST_BURN 등) | ✅ |
| RadiusModifier | 광역 범위 설정 | ✅ |
| **BounceModifier** | **반사 횟수 + 감쇄율 설정** | ✅ NEW |
| **HomingModifier** | **유도 속성 + 가장 가까운 적 자동 탐지** | ✅ NEW |

#### 2-3-4. Action 모듈 (무엇을 실행?)
| 클래스 | 실행 내용 | 상태 |
|--------|-----------|------|
| DealDamageAction | 단일/광역/자해 피해 | ✅ |
| ApplyStatusAction | StatusEffectManager 브릿지 | ✅ |
| HealSelfAction | 플레이어 체력 회복 | ✅ |
| NullifyDamageAction | 피해 무효화 (즉시 회복 근사) | ✅ |
| **ReviveAction** | **부활 (HP% 회복 + 무적 시간)** | ✅ NEW |
| **SpawnFamiliarAction** | **패밀리어 프리팹 소환 (인스턴스 제한)** | ✅ NEW |
| **DropPickupAction** | **픽업 아이템 N개 랜덤 드롭** | ✅ NEW |

### 2-4. 아이템 데이터 인프라
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

- [ ] **캡슐/장애물 비주얼 개선** — 현재 GachaCapsule=캡슐 메시, Obstacle=큐브 메시 기본 에셋. 아트 에셋 교체 필요
- [ ] **오픈 파티클 이펙트** — `GachaCapsuleSpawner._openParticlePrefab` 미연결. 파티클 에셋 제작 후 Inspector 연결
- [ ] **ItemData 스프라이트 연결** — `Icon`, `AppearanceSprite`, `PedestalSprite` 모두 null 상태. 스프라이트 에셋 준비 후 연결
- [ ] **TargetBodyPart 기획 확정** — 현재 Form 아이템은 `ArmRight`(테스트값), Modifier 아이템은 `Head`(기본값)
- [ ] **게임 클리어 화면** — `StageManager.OnAllStagesComplete` 이벤트를 구독하는 UI 없음
- [ ] **특수 공격 시스템** — 아래 표 참조

### 미구현 특수 공격 시스템

아이템 시트에 다음 공격 유형이 정의되어 있으나 코드가 없음:

| 아이템 ID | 이름 | 필요한 시스템 | T-M-A 블록으로 가능? |
|-----------|------|---------------|---------------------|
| 103 | 뭉툭한 팔 | 관통(Pierce) 공격 | 투사체 Action 필요 |
| 104 | 접착제 팔 | 적 달라붙기 (ST_BOND 특수) | 신규 Status 필요 |
| 105 | 고무 팔 | 반사 투사체 | ✅ BounceModifier 활용 |
| 203 | 돌멩이 주먹 | 충격파 (OverlapSphere 즉시 피해) | ✅ DealDamageAction(AoE) |
| 204 | 드릴 팔 | 지속 관통 | 투사체 Action 필요 |
| 205 | 집게 팔 | 잡기 + 투척 | 전용 Action 필요 |
| 206 | 유압 팔 | 범위 넓은 후방 넉백 | 넉백 Action 필요 |
| 207 | 독 분사기 | 독 도트 (ST_POISON — 미정의) | ApplyStatusAction + 신규 Status |
| 303 | 레이저 포 | 레이저 빔 (선형 히트스캔) | 히트스캔 Action 필요 |
| 304 | 미사일 발사기 | 유도 미사일 | ✅ HomingModifier 활용 |
| 305 | 냉동 포 | 즉시 동결 (ST_FREEZE — 미정의) | ApplyStatusAction + 신규 Status |
| 401~405 | 전설 아이템들 | 각각 고유 메카닉 필요 | 개별 검토 필요 |

**권장 구현 순서**: 스탯 아이템(102, 201~202, 301~302) 먼저 → 단순 Status 아이템 → 복잡한 공격 메카닉 순

### 다음 단계로 필요한 T-M-A 블록

현재 범용 블록으로 커버되지 않는 영역:

| 필요한 블록 | 용도 | 우선순위 |
|-------------|------|----------|
| **ProjectileAction** | 투사체 발사 (BounceCount/IsHoming을 읽어 반사·유도 처리) | 높음 |
| **KnockbackAction** | 넉백 방향·힘 적용 (유압 팔, 충격파 등) | 중간 |
| **HitscanAction** | 레이저 빔 (Raycast 선형 히트) | 중간 |
| **ST_POISON / ST_FREEZE** | 신규 상태이상 정의 (StatusEffectManager 확장) | 중간 |
| **OnHealthThresholdTrigger** | HP가 N% 이하일 때 발동 (분노/위기 아이템) | 낮음 |

---

## 4. 알려진 버그 / 리스크

| 항목 | 설명 | 심각도 |
|------|------|--------|
| AppearanceSprite 없음 | 모든 아이템 null → 파츠 스프라이트 교체 안 됨 (Scale/Color/이펙트는 작동) | 낮음 |
| 306 ST_STUN vs ST_FREEZE 불일치 | 기획 확인 전까지 ST_STUN으로 처리됨 | 중간 |
| CombatFeedback GlitchRoutine | MissingReferenceException 방어 코드 적용 완료, 재발 시 확인 필요 | 낮음 |
| 캡슐 스폰 — `_openParticlePrefab` null | 파티클 없이 동작은 하지만 착지 연출이 밋밋함 | 낮음 |

---

## 5. 핵심 파일 구조

```
Assets/
├── Editor/
│   └── ItemCSVImporter.cs              ← CSV → ItemData 에셋 변환 도구
├── Prefabs/
│   ├── GachaCapsule.prefab             ← 캡슐 낙하 비주얼 (교체 예정)
│   └── Obstacle.prefab                 ← 장애물 (Rigidbody + NavMeshObstacle + ObstacleController)
├── Resources/
│   ├── ItemTable.csv                   ← ✅ 생성됨 (35개 아이템)
│   ├── ItemDatabase.asset              ← ✅ 자동 생성됨
│   └── Items/                          ← ✅ 35개 .asset 파일
├── Scripts/
│   ├── Enemy/
│   │   ├── EnemyBase.cs                ← 적 FSM, 넉백, LaunchFromCapsule, ApplyExternalStun
│   │   └── ObstacleController.cs       ← 장애물 물리 산개 + NavMeshObstacle Carving
│   ├── Item/
│   │   ├── ItemData.cs                 ← ScriptableObject, StatType enum, PartScale/PartColor
│   │   ├── ItemDatabase.cs             ← 전체 아이템 목록 컨테이너
│   │   ├── CoinPickup.cs               ← 코인 자석, CollectionRange 적용
│   │   ├── RewardSlotUI.cs             ← 보상 슬롯 단일 UI
│   │   ├── WeaponNameBuilder.cs        ← Keyword → 무기 이름 조합
│   │   └── Effects/                    ← T-M-A 이펙트 파이프라인
│   │       ├── Core/
│   │       │   ├── IItemEffect.cs          ← 인터페이스 + ItemEffectRole enum
│   │       │   ├── ItemEffectBase.cs       ← 추상 베이스 (ICD 강제)
│   │       │   ├── ItemEffectContext.cs     ← 파이프라인 데이터 컨테이너
│   │       │   ├── PlayerEventManager.cs   ← 이벤트 허브 (싱글턴)
│   │       │   └── ItemEffectRunner.cs     ← 파이프라인 오케스트레이터
│   │       ├── Triggers/
│   │       │   ├── TriggerBase.cs          ← Trigger 추상 베이스
│   │       │   ├── OnAttackTrigger.cs
│   │       │   ├── OnMeleeHitTrigger.cs
│   │       │   ├── OnTakeDamageTrigger.cs
│   │       │   ├── OnKillTrigger.cs
│   │       │   ├── OnDashTrigger.cs
│   │       │   ├── OnTimerTrigger.cs
│   │       │   ├── PassiveTrigger.cs
│   │       │   ├── OnFatalDamageTrigger.cs ← ✅ NEW 사망 가로채기
│   │       │   └── OnRoomClearTrigger.cs   ← ✅ NEW 웨이브 클리어
│   │       ├── Modifiers/
│   │       │   ├── ModifierBase.cs         ← Modifier 추상 베이스
│   │       │   ├── StatModifier.cs
│   │       │   ├── ChanceGateModifier.cs
│   │       │   ├── AddTagModifier.cs
│   │       │   ├── RadiusModifier.cs
│   │       │   ├── BounceModifier.cs       ← ✅ NEW 반사 속성
│   │       │   └── HomingModifier.cs       ← ✅ NEW 유도 속성
│   │       └── Actions/
│   │           ├── ActionBase.cs           ← Action 추상 베이스
│   │           ├── DealDamageAction.cs
│   │           ├── ApplyStatusAction.cs
│   │           ├── HealSelfAction.cs
│   │           ├── NullifyDamageAction.cs
│   │           ├── ReviveAction.cs         ← ✅ NEW 부활
│   │           ├── SpawnFamiliarAction.cs  ← ✅ NEW 패밀리어 소환
│   │           └── DropPickupAction.cs     ← ✅ NEW 픽업 드롭
│   ├── Manager/
│   │   ├── StageManager.cs             ← 스테이지/웨이브/보상 흐름, SpawnViaCapsule()
│   │   ├── GachaCapsuleSpawner.cs      ← 캡슐 낙하 연출 + 적/장애물 산개 전담
│   │   ├── RewardSystemManager.cs      ← 보상 UI 애니메이션 + 아이템 흡수
│   │   ├── StatusEffectManager.cs      ← 6종 상태 이상 적용
│   │   ├── CurrencyManager.cs          ← 코인 수집/관리
│   │   ├── DifficultySelectManager.cs  ← 난이도 선택 UI + ObstacleBaseCount 프로퍼티
│   │   ├── DifficultyData.cs           ← DifficultySettings (ObstacleBaseCount 포함)
│   │   └── GameOverManager.cs          ← 게임 오버 처리
│   ├── Player/
│   │   ├── PlayerStats.cs              ← 스탯, 아이템, 부활, 무적
│   │   ├── PlayerMovement.cs           ← 이동 + 조준 (useGravity=false, FreezePositionY)
│   │   ├── WeaponController.cs         ← 공격, 크리티컬, 사거리
│   │   └── PlayerAppearance.cs         ← 스프라이트 장착, PartScale/Color, 이펙트
│   ├── Combat/
│   │   ├── CombatFeedback.cs           ← 글리치 셰이더, 히트스탑
│   │   ├── PlayerHitFeedback.cs        ← 플레이어 피격 피드백
│   │   └── ItemEffectVFX.cs            ← T-M-A 전용 VFX (컬러 플래시, 파티클)
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

### 캡슐 스폰 흐름
```
StageManager.StageTransitionThenSpawn()
    → _capsuleSpawner.ClearObstacles()     ← 이전 웨이브 장애물 제거
    → SpawnViaCapsule()
        → GachaCapsuleSpawner.SpawnWave()  ← 낙하 연출 코루틴
            → 캡슐 낙하 (SmoothStep, 0.7초)
            → 착지 → Destroy(capsule) → ParticleSystem 재생
            → EnemyBase.LaunchFromCapsule() × N  ← 적 산개
            → ObstacleController.Launch() × N    ← 장애물 산개
            → WaitForSeconds(1.2초)
            → onComplete(spawnedEnemies) 콜백
    → 사망 콜백 등록 (_activeEnemies)
```

### ObstacleController 착지 후 NavMesh Carving
- `Awake()`: `carving=true`, `enabled=false` (물리 정지, carving 비활성)
- `Launch()`: Rigidbody 활성화 → AddForce → `SettleRoutine()` 시작
- `SettleRoutine()` 완료 후: isKinematic=true, `navObstacle.enabled=true` → NavMesh에 구멍 뚫림
- **웨이브 클리어 시 `ClearObstacles()`로 전부 Destroy → 다음 웨이브에 재생성**

### 난이도별 장애물 수
| 난이도 | ObstacleBaseCount |
|--------|-------------------|
| 쉬움 | 2 |
| 보통 | 4 |
| 어려움 | 6 |
`DifficultySelectManager.ObstacleBaseCount` 프로퍼티로 읽음.

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

### T-M-A 파이프라인 실행 흐름
```
PlayerAppearance.EquipItem() → ItemEffectRunner.RegisterItem(item)
    → ActiveItemPipeline 생성 → Effects를 Role별 분류
    → Trigger.SetPipelineCallback() + Initialize()
    → Trigger가 PlayerEventManager 이벤트 구독

(게임 중)
PlayerEventManager.BroadcastXxx()
    → Trigger.HandleXxx() → FireTrigger(context.Clone())
    → ActiveItemPipeline.ExecutePipeline()
        → PipelineDepth 체크 (MAX=3, 무한 루프 방지)
        → Modifier 순서대로 context 변형
        → Action 순서대로 실행
```

### 부활 시스템 (OnFatalDamage)
```
PlayerStats.TakeDamage() → HP ≤ 0
    → BroadcastFatalDamage() (사망 판정 전!)
    → OnFatalDamageTrigger 발동 → ReviveAction.Revive(HP%) + SetInvincible()
    → PlayerStats: HP > 0이면 _isDead 설정 안 함 (사망 취소)
```
**핵심**: T-M-A 파이프라인이 동기 실행되므로 BroadcastFatalDamage()가 리턴될 때 이미 HP가 회복된 상태.

### ItemEffectContext 궤적 필드
`BounceCount`, `BounceDecay`, `IsHoming`, `HomingStrength` — Modifier가 설정하고 향후 ProjectileAction 등이 소비. 현재는 필드만 존재하며, 이를 읽는 투사체 Action은 미구현.

---

## 7. 작업 재개 시 첫 번째 할 일

1. **이 문서 먼저 읽기**
2. `Assets/Scripts/` 전체를 훑어 현재 컴파일 오류 없는지 확인
3. 플레이 테스트: 난이도 선택 → 캡슐 낙하 → 적/장애물 산개 → 전투 → 보상 선택 → 파츠 색상/크기 변화 확인
4. `GachaCapsuleSpawner._openParticlePrefab` 파티클 에셋 제작 및 연결
5. 캡슐/장애물 아트 에셋 교체 (현재 기본 Capsule/Cube 메시)
6. 스프라이트 에셋 준비 → `Icon`, `AppearanceSprite` 연결 (우선순위 높음)
7. **T-M-A 블록 테스트**: ItemData .asset에 Effects 리스트 배치 → 인게임 발동 확인
8. **ProjectileAction 구현** — BounceCount/IsHoming을 실제로 소비하는 투사체 Action (최우선)
9. 특수 공격 시스템 구현 시작 (원하는 아이템 ID 선택)

---

## 8. 완료 이력

| 날짜 | 작업 내용 |
|------|----------|
| 2026-02-23 | 캡슐 강하 스폰 시스템 구현 (GachaCapsuleSpawner.cs 신규) |
| 2026-02-23 | 지형 장애물 시스템 구현 (ObstacleController.cs 신규, NavMesh Carving) |
| 2026-02-23 | EnemyBase.LaunchFromCapsule() 추가 — 캡슐 산개 스폰 전용 진입점 |
| 2026-02-23 | DifficultyData/SelectManager에 ObstacleBaseCount 추가 (Easy=2, Normal=4, Hard=6) |
| 2026-02-23 | StageManager SpawnCurrentWave → SpawnViaCapsule() 대체 (폴백 유지) |
| 2026-02-23 | GachaCapsule.prefab / Obstacle.prefab 생성 및 씬 Inspector 연결 완료 |
| 2026-02-23 | PlayerMovement useGravity=false, FreezePositionY 추가 (플로팅 방지) |
| 2026-02-21 | StatusEffectManager 씬 배치 확인 (GameManagers 오브젝트) |
| 2026-02-21 | CSV 임포터 수정 (별칭 추가, NULL 처리) 및 35개 아이템 임포트 |
| 2026-02-21 | StageManager _rewardPool → ItemDatabase 자동 로드로 변경 |
| 2026-02-21 | TempStickyHand NavMesh 오류 수정 (위치 SpawnPoint_1으로 이동) |
| 2026-02-21 | ItemData에 PartScale/PartColor 필드 추가 |
| 2026-02-21 | PlayerAppearance 외형 시스템 확장 (Scale/Color 적용, OriginalScale/Color 저장·복원) |
| 2026-02-21 | BondImpactRoutine 재설계 — Flash + Shake + ScalePunch + Glitch 동시 진행 |
| 2026-02-21 | 시각 이펙트 버그 3종 수정 (AppearanceSprite null 가드, TargetBodyPart 기본값 수정, _playerAppearance null 체크) |
| 2026-02-21 | CSV 재임포트 — Form 아이템 8종 ArmRight 배정 확인 |
| 2026-02-26 | T-M-A 범용 모듈 7종 추가 (OnFatalDamageTrigger, ReviveAction, SpawnFamiliarAction, OnRoomClearTrigger, DropPickupAction, BounceModifier, HomingModifier) |
| 2026-02-26 | PlayerEventManager에 OnFatalDamage, OnRoomClear 이벤트 추가 |
| 2026-02-26 | PlayerStats에 Revive(), SetInvincible(), 무적 체크, Fatal→부활 가로채기 추가 |
| 2026-02-26 | ItemEffectContext에 BounceCount, BounceDecay, IsHoming, HomingStrength 필드 추가 |
| 2026-02-26 | StageManager 웨이브 클리어 시 BroadcastRoomClear() 호출 추가 |
| 2026-02-26 | ItemEffectVFX에 PlayReviveEffect() (금색 파티클) 추가 |

---

*이 문서는 `REMIX_PROTO_1/HANDOFF.md`에 저장됩니다.*
