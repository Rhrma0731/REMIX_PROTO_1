# RE:MIX PROTO_1 — 작업 인수인계 가이드라인

> 마지막 업데이트: 2026-03-01
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
| **인게임 아이템 즉시 장착 디버그 도구** | ✅ | `DebugItemEquipper.cs` (F1~F8) |
| **URP 포스트프로세싱 자동 세팅** | ✅ | `ScenePostProcessingSetup.cs` (Tools 메뉴) |
| **Built-in → URP 머티리얼 일괄 변환** | ✅ | `ScenePostProcessingSetup.cs` (Tools 메뉴) |
| **Global Volume 씬 배치** | ✅ | `PostProcess_CultVibes.asset` — Bloom/Vignette/ColorAdjustments Cult Vibe 프로파일 씬에 직접 배치 |
| **배경 오브젝트 씬 배치** | ✅ | `claude remix.unity` — 배경 환경 오브젝트 추가 |

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
| OnFatalDamageTrigger | HP 0 직전 (사망 가로채기, 횟수 제한) | ✅ |
| OnRoomClearTrigger | 웨이브/방 클리어 시 | ✅ |

#### 2-3-3. Modifier 모듈 (어떻게 변형?)
| 클래스 | 변형 내용 | 상태 |
|--------|-----------|------|
| StatModifier | 데미지 배율, 범위 추가 | ✅ |
| ChanceGateModifier | 확률 실패 시 파이프라인 차단 | ✅ |
| AddTagModifier | 원소 속성 부여 (Fire→ST_BURN 등) | ✅ |
| RadiusModifier | 광역 범위 설정 | ✅ |
| BounceModifier | 반사 횟수 + 감쇄율 설정 | ✅ |
| HomingModifier | 유도 속성 + 가장 가까운 적 자동 탐지 | ✅ |

#### 2-3-4. Action 모듈 (무엇을 실행?)
| 클래스 | 실행 내용 | 상태 |
|--------|-----------|------|
| DealDamageAction | 단일/광역/자해 피해 | ✅ |
| ApplyStatusAction | StatusEffectManager 브릿지 | ✅ |
| HealSelfAction | 플레이어 체력 회복 | ✅ |
| NullifyDamageAction | 피해 무효화 (즉시 회복 근사) | ✅ |
| ReviveAction | 부활 (HP% 회복 + 무적 시간) | ✅ |
| SpawnFamiliarAction | 패밀리어 프리팹 소환 (인스턴스 제한) + Configure() 연결 | ✅ |
| DropPickupAction | 픽업 아이템 N개 랜덤 드롭 | ✅ |
| **BloodOathAction** | **방 클리어 시 HP→1 소모, 비율 비례 공격력/이속 영구 증가** | ✅ |

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
| **아이템 에셋 실제 데이터 35개** | ✅ 임포트 완료 (`Assets/Resources/Items/101~405`) |
| **신규 아이템 501~503** | ✅ `Tools > Create New Items (501-503)` 메뉴로 생성 |
| **패밀리어 아이템 601~610** | ✅ `Tools > Create Familiar Items (601-610)` 메뉴로 생성 |
| **ItemDatabase 자동 로드** | ✅ StageManager Awake에서 Resources.Load로 자동 연결 |
| **PlayerStats.AddDynamicBonus()** | ✅ 런타임 스탯 영구 증가 (스택 가능) |
| **PlayerStats.SetHpDirect()** | ✅ 사망 판정 우회 HP 강제 설정 |

### 2-5. 아이템 ID 범위 체계
| 범위 | 분류 | 상태 |
|------|------|------|
| 101~110 | Form 무기 아이템 | ✅ |
| 201~210 | Body 파츠 아이템 | ✅ |
| 301~310 | Head 파츠 아이템 | ✅ |
| 401~405 | 전설 아이템 | ✅ |
| 501~503 | 특수 신규 (1UP!, 피의 서약, 가짜 피 캡슐) | ✅ 메뉴 생성 |
| 601~610 | 패밀리어 소환 신규 아이템 | ✅ 메뉴 생성 |
| 701~721 | 신규 아이템 (7계열) | ✅ 에셋 파일 생성됨 (미배정, 기획 필요) |

### 2-6. 패밀리어 시스템 (FamiliarController.cs)

| FamiliarMode | 설명 | 상태 |
|--------------|------|------|
| `Orbit` | 플레이어 공전 + 주기 공격 | ✅ 완전 구현 |
| `Homing` | 가장 가까운 적 추격 + 자폭 | ✅ 완전 구현 |
| `MouseFollow` | 마우스 커서 방향 이동 + 틱 데미지 | ⚠ 미구현 (Orbit 폴백) |
| `Symmetric` | 방 중앙 기준 플레이어 대칭 이동 | ⚠ 미구현 (Orbit 폴백) |
| `OrbitalShield` | 공전 + 투사체 차단 | ⚠ 미구현 (Orbit 폴백) |

#### 패밀리어 프리팹 현황 (`Assets/Resources/Familiars/`)
| 프리팹 | 색상 | 모드 | 사용 아이템 | 상태 |
|--------|------|------|-------------|------|
| `Familiar_Bee.prefab` | 노란 원 `#FFD900` | Orbit | 기본 Orbit 패밀리어 | ✅ |
| `Familiar_KamikazeBee.prefab` | 빨간 원 `#FF3300` | Homing | 304번 (벌집) | ✅ |
| `Familiar_FlyToy.prefab` | 올리브 그린 | Orbit | 605번 (똥파리) | ✅ |
| `Familiar_Tamagotchi.prefab` | — | Orbit | 601번 | ❌ 미생성 |
| `Familiar_SpiderToy.prefab` | — | MouseFollow | 602번 | ❌ 미생성 |
| `Familiar_Yoyo.prefab` | — | Symmetric | 603번 | ❌ 미생성 |
| `Familiar_HoloPrism.prefab` | — | OrbitalShield | 604번 | ❌ 미생성 |
| `Familiar_BandageBall.prefab` | — | Orbit | 606번 | ❌ 미생성 |
| `Familiar_GuardKeyring.prefab` | — | Orbit | 607번 | ❌ 미생성 |
| `Familiar_PiggyBank.prefab` | — | Homing | 608번 | ❌ 미생성 |
| `Familiar_BigFan.prefab` | — | OrbitalShield | 609번 | ❌ 미생성 |
| `Familiar_BullDog.prefab` | — | Orbit | 610번 | ❌ 미생성 |

> ❌ 프리팹 없으면 `SpawnFamiliarAction`이 경고 출력 후 소환 건너뜀.
> 임시 테스트: `_familiarResourcePath`를 `Familiars/Familiar_Bee`로 변경 또는 Unity MCP로 프리팹 생성.

---

## 3. 미완성 / 남은 작업

### 우선순위 목록

- [ ] **캡슐/장애물 비주얼 개선** — 현재 GachaCapsule=캡슐 메시, Obstacle=큐브 메시 기본 에셋. 아트 에셋 교체 필요
- [ ] **오픈 파티클 이펙트** — `GachaCapsuleSpawner._openParticlePrefab` 미연결. 파티클 에셋 제작 후 Inspector 연결
- [ ] **ItemData 스프라이트 연결** — `Icon`, `AppearanceSprite`, `PedestalSprite` 모두 null 상태. 스프라이트 에셋 준비 후 연결
- [ ] **TargetBodyPart 기획 확정** — 현재 Form 아이템은 `ArmRight`(테스트값), Modifier 아이템은 `Head`(기본값)
- [ ] **게임 클리어 화면** — `StageManager.OnAllStagesComplete` 이벤트를 구독하는 UI 없음
- [ ] **특수 공격 시스템** — 아래 표 참조
- [ ] **패밀리어 미구현 모드 3종 구현** — MouseFollow / Symmetric / OrbitalShield (현재 Orbit 폴백)
- [ ] **패밀리어 프리팹 9종 제작** — Tamagotchi, SpiderToy, Yoyo, HoloPrism, BandageBall, GuardKeyring, PiggyBank, BigFan, BullDog
- [ ] **601~610 아이템 미구현 기능** — GrowOnKill(610), FriendlyFire(610), EvolutionSystem(606), SplitOnHit(604) 등

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
| 패밀리어 프리팹 9종 미생성 | 601~610 중 FlyToy(605)만 프리팹 존재, 나머지는 소환 불가 | 중간 |
| **Familiar_FlyToy.prefab 스프라이트 미할당** | `SpriteRenderer.sprite = null` — 패밀리어가 소환되지만 보이지 않음. Unity에서 프리팹 열어 Sprite 슬롯에 이미지 할당 필요 | **높음** |
| **DefaultMaterial 핑크** | Unity 기본 DefaultMaterial은 Built-in RP 전용 — URP 전환 후 DefaultMaterial 사용 오브젝트가 핑크로 보임. `Assets/Graphic/M_Default_URP.mat` 생성 후 수동 교체 필요 | 중간 |
| **URP postProcessData 연결 확인** | `UniversalRendererData.asset`의 `postProcessData` GUID가 `41439944...`으로 수동 패치됨. 씬을 재임포트하거나 URP 업그레이드 시 재확인 필요 | 낮음 |

---

## 5. 핵심 파일 구조

```
Assets/
├── Editor/
│   ├── ItemCSVImporter.cs              ← CSV → ItemData 에셋 변환 도구
│   ├── ItemDataTMAGenerator.cs         ← T-M-A 블록 에셋 생성 보조 도구
│   ├── TMAPipelineAssigner.cs          ← T-M-A 일괄 할당 + 신규 아이템 생성 도구
│   │                                          메뉴: Tools > Assign TMA Effects to All Items
│   │                                          메뉴: Tools > Create New Items (501-503)
│   │                                          메뉴: Tools > Create Familiar Items (601-610)
│   │                                          메뉴: Tools > TMA Debug > ...
│   ├── FloorMaterialSetup.cs           ← Floor01 텍스처 세트 → 머티리얼 생성 + Floor 오브젝트 적용
│   │                                      메뉴: Tools > Setup Floor01 Material
│   │                                      ① Floor01_N.png NormalMap 타입 자동 설정
│   │                                      ② URP/Lit 머티리얼 생성 (Assets/Graphic/Materials/Floor01.mat)
│   │                                      ③ 씬의 'Floor' MeshRenderer에 머티리얼 교체 (Undo 지원)
│   └── ScenePostProcessingSetup.cs     ← URP 포스트프로세싱 + 라이팅 자동 세팅
│                                          메뉴: Tools > Setup Post Processing & Lighting (Cult Vibe)
│                                          메뉴: Tools > Convert Built-in Materials to URP (Fix Pink)
│                                          ① URP 파이프라인 에셋 확인/생성 및 GraphicsSettings 연결
│                                          ② Global Volume 생성 (Bloom/Vignette/ColorAdjustments)
│                                          ③ Main Camera renderPostProcessing = true
│                                          ④ Directional Light 없을 때만 생성 (청보라, intensity=0.5)
├── MCPForUnity/                        ← [개발 도구] Claude Code ↔ Unity MCP 브릿지 플러그인
├── Screenshots/                        ← 플레이테스트 참고 스크린샷 (5장)
├── Prefabs/
│   ├── GachaCapsule.prefab             ← 캡슐 낙하 비주얼 (교체 예정)
│   ├── Obstacle_A.prefab               ← 장애물 타입 A (Rigidbody + NavMeshObstacle + ObstacleController)
│   ├── Obstacle_B.prefab               ← 장애물 타입 B
│   └── Obstacle_C.prefab               ← 장애물 타입 C
│   [Obstacle.prefab 삭제됨 → 3종으로 분리]
├── Settings/
│   ├── UniversalRenderPipelineAsset.asset  ← URP 파이프라인 에셋 (GraphicsSettings에 연결됨)
│   ├── UniversalRendererData.asset         ← URP 렌더러 데이터 (postProcessData GUID 수동 패치됨)
│   └── PostProcess_CultVibes.asset         ← Global Volume 프로파일 (Bloom/Vignette/ColorAdjustments)
├── Graphic/
│   ├── M_Default_URP.mat                   ← URP/Lit 머티리얼 — DefaultMaterial 대체용 (수동 교체 필요)
│   ├── 3D graphic/                         ← ✅ 신규 3D 그래픽 에셋 (배경용)
│   ├── Golem_Weak_Mob_2_Walk_NE.png        ← ✅ 골렘 몬스터 스프라이트 (Walk NE 방향)
│   ├── monster1.png                        ← ✅ 몬스터 스프라이트 (미배정)
│   └── PROTOTYPE_1.png                     ← ✅ 프로토타입 참고 이미지
├── Resources/
│   ├── Familiars/
│   │   ├── Familiar_Bee.prefab         ← ✅ Orbit 패밀리어 (노란 원, 공전+공격)
│   │   ├── Familiar_KamikazeBee.prefab ← ✅ Homing 패밀리어 (빨간 원, 추격+자폭, 304용)
│   │   └── Familiar_FlyToy.prefab      ← ✅ Orbit 패밀리어 (올리브 그린, 605용)
│   │       [❌ 미생성: Tamagotchi/SpiderToy/Yoyo/HoloPrism/BandageBall
│   │                   GuardKeyring/PiggyBank/BigFan/BullDog]
│   ├── ItemTable.csv                   ← ✅ 생성됨 (35개 아이템)
│   ├── ItemDatabase.asset              ← ✅ 자동 생성됨
│   └── Items/                          ← ✅ 에셋 파일 (101~405, 501~503, 601~610, 701~721)
├── Scripts/
│   ├── Debug/
│   │   └── DebugItemEquipper.cs        ← [개발 전용] F1~F8 키로 아이템 즉시 장착
│   │                                      씬 오브젝트 "DebugItemEquipper"에 부착됨
│   │                                      배포 전 비활성화 필요
│   ├── Enemy/
│   │   ├── EnemyBase.cs                ← 적 FSM, 넉백, LaunchFromCapsule, ApplyExternalStun
│   │   └── ObstacleController.cs       ← 장애물 물리 산개 + NavMeshObstacle Carving
│   ├── Item/
│   │   ├── ItemData.cs                 ← ScriptableObject, StatType enum, PartScale/PartColor
│   │   ├── ItemDatabase.cs             ← 전체 아이템 목록 컨테이너
│   │   ├── FamiliarController.cs       ← 패밀리어 행동 (Orbit/Homing 구현,
│   │   │                                  MouseFollow/Symmetric/OrbitalShield 폴백)
│   │   ├── CoinPickup.cs               ← 코인 자석, CollectionRange 적용
│   │   ├── RewardSlotUI.cs             ← 보상 슬롯 단일 UI
│   │   ├── WeaponNameBuilder.cs        ← Keyword → 무기 이름 조합
│   │   └── Effects/                    ← T-M-A 이펙트 파이프라인
│   │       ├── Core/
│   │       │   ├── IItemEffect.cs
│   │       │   ├── ItemEffectBase.cs
│   │       │   ├── ItemEffectContext.cs
│   │       │   ├── PlayerEventManager.cs
│   │       │   └── ItemEffectRunner.cs
│   │       ├── Triggers/
│   │       │   ├── TriggerBase.cs
│   │       │   ├── OnAttackTrigger.cs
│   │       │   ├── OnMeleeHitTrigger.cs
│   │       │   ├── OnTakeDamageTrigger.cs
│   │       │   ├── OnKillTrigger.cs
│   │       │   ├── OnDashTrigger.cs
│   │       │   ├── OnTimerTrigger.cs
│   │       │   ├── PassiveTrigger.cs
│   │       │   ├── OnFatalDamageTrigger.cs
│   │       │   └── OnRoomClearTrigger.cs
│   │       ├── Modifiers/
│   │       │   ├── ModifierBase.cs
│   │       │   ├── StatModifier.cs
│   │       │   ├── ChanceGateModifier.cs
│   │       │   ├── AddTagModifier.cs
│   │       │   ├── RadiusModifier.cs
│   │       │   ├── BounceModifier.cs
│   │       │   └── HomingModifier.cs
│   │       └── Actions/
│   │           ├── ActionBase.cs
│   │           ├── DealDamageAction.cs
│   │           ├── ApplyStatusAction.cs
│   │           ├── HealSelfAction.cs
│   │           ├── NullifyDamageAction.cs
│   │           ├── ReviveAction.cs
│   │           ├── SpawnFamiliarAction.cs
│   │           ├── DropPickupAction.cs
│   │           └── BloodOathAction.cs  ← 방 클리어 시 HP→1 소모 + 스탯 영구 증가
│   ├── Manager/
│   │   ├── StageManager.cs
│   │   ├── GachaCapsuleSpawner.cs
│   │   ├── RewardSystemManager.cs
│   │   ├── StatusEffectManager.cs
│   │   ├── CurrencyManager.cs
│   │   ├── DifficultySelectManager.cs
│   │   ├── DifficultyData.cs
│   │   └── GameOverManager.cs
│   ├── Player/
│   │   ├── PlayerStats.cs              ← 스탯, 아이템, 부활, 무적,
│   │   │                                  AddDynamicBonus(), SetHpDirect()
│   │   ├── PlayerMovement.cs
│   │   ├── WeaponController.cs
│   │   └── PlayerAppearance.cs
│   ├── Combat/
│   │   ├── CombatFeedback.cs
│   │   ├── PlayerHitFeedback.cs
│   │   └── ItemEffectVFX.cs
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
            → ObstacleController.Launch() × N    ← 장애물 산개 (3종 중 랜덤 선택)
            → WaitUntil(모든 obs.IsSettled) + _settleWait 타임아웃
            → onComplete(spawnedEnemies) 콜백
    → 사망 콜백 등록 (_activeEnemies)
```

### ObstacleController 착지 후 NavMesh Carving
- `Awake()`: `carving=true`, `enabled=false` (물리 정지, carving 비활성)
- `Launch()`: Rigidbody 활성화 → AddForce → `SettleRoutine()` 시작
- `SettleRoutine()` 완료 후: isKinematic=true, `navObstacle.enabled=true`, `IsSettled=true` → NavMesh에 구멍 뚫림
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

### FamiliarController 모드 구조
```
FamiliarMode enum:
    Orbit          → 구현됨: XZ 공전 + 주기 공격
    Homing         → 구현됨: 적 추격 + 자폭
    MouseFollow    → 미구현, Orbit 폴백 (TODO: Mouse.current.position 월드 변환)
    Symmetric      → 미구현, Orbit 폴백 (TODO: RoomCenter 좌표 + offset 반전)
    OrbitalShield  → 미구현, Orbit 폴백 (TODO: Projectile 레이어 트리거 감지)

SpawnFamiliarAction._familiarResourcePath가 null이거나 프리팹이 없으면
Resources.Load() → null → 경고 로그 출력 후 소환 건너뜀.
```

### DebugItemEquipper 사용법
```
씬 오브젝트 "DebugItemEquipper" → DebugItemEquipper.cs 컴포넌트
    _itemIDs: ["604", "605", "607", ...] — 테스트할 ItemID 배열
    F1 → _itemIDs[0] 즉시 장착 (PlayerAppearance.EquipItem 호출)
    F2 → _itemIDs[1] ... F8 → _itemIDs[7]

주의: New Input System 사용 — Keyboard.current[Key.F1].wasPressedThisFrame
배포 전 GameObject 비활성화 필요
```

---

## 7. 작업 재개 시 첫 번째 할 일

1. **이 문서 먼저 읽기**
2. `Assets/Scripts/` 전체를 훑어 현재 컴파일 오류 없는지 확인
3. 플레이 테스트: 난이도 선택 → 캡슐 낙하 → 적/장애물 산개 → 전투 → 보상 선택 → 파츠 색상/크기 변화 확인
4. **패밀리어 아이템 테스트**: F1~F8로 601~610 장착 → 패밀리어 소환 확인 (605 FlyToy는 동작, 나머지는 프리팹 없어 경고)
5. **패밀리어 프리팹 9종 제작** — 우선순위: 607(GuardKeyring/OnTakeDamage), 608(PiggyBank/OnAttack) 먼저 테스트
6. **패밀리어 미구현 모드 구현** — MouseFollow(602) → Symmetric(603) → OrbitalShield(604, 609) 순서 권장
7. `GachaCapsuleSpawner._openParticlePrefab` 파티클 에셋 제작 및 연결
8. 캡슐/장애물 아트 에셋 교체 (현재 기본 Capsule/Cube 메시)
9. 스프라이트 에셋 준비 → `Icon`, `AppearanceSprite` 연결 (우선순위 높음)
10. **ProjectileAction 구현** — BounceCount/IsHoming을 실제로 소비하는 투사체 Action

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
| 2026-02-27 | git 병합 완료 — 로컬 작업(캡슐/장애물) + 원격 T-M-A 시스템 충돌 없이 통합 |
| 2026-02-27 | Familiar_Bee.prefab / Familiar_KamikazeBee.prefab 생성 (Resources/Familiars/) |
| 2026-02-27 | 304.asset SpawnFamiliarAction._familiarResourcePath = "Familiars/Familiar_KamikazeBee" 연결 |
| 2026-02-27 | FamiliarController.cs 신규 (Orbit/Homing 2모드, Configure(context) 연동, PlayerStats 스케일) |
| 2026-02-27 | SpawnFamiliarAction에 Configure(context) 호출 추가 — IsHoming/TargetEnemy 자동 전달 |
| 2026-02-27 | BloodOathAction.cs 신규 (방 클리어 시 체력→1 + 비율 스탯 영구 증가) |
| 2026-02-27 | PlayerStats.AddDynamicBonus() + SetHpDirect() 추가 |
| 2026-02-27 | TMAPipelineAssigner에 "Create New Items (501-503)" 메뉴 추가 |
| 2026-02-27 | MCPForUnity 플러그인 추가 (`Assets/MCPForUnity/`) — Claude Code ↔ Unity Editor 직접 연동 |
| 2026-02-27 | 플레이테스트 스크린샷 5장 추가 (`Assets/Screenshots/`) — navmesh, 매니저, 시작 화면 등 |
| 2026-02-27 | DebugItemEquipper.cs 신규 — F1~F8 키로 ItemID 즉시 장착 (New Input System 호환) |
| 2026-02-27 | FamiliarController FamiliarMode enum 확장 — MouseFollow / Symmetric / OrbitalShield 추가 (Orbit 폴백, TODO 명시) |
| 2026-02-27 | TMAPipelineAssigner ALL_IDS 확장 (501~503, 601~610 포함) |
| 2026-02-27 | "Create Familiar Items (601-610)" 메뉴 추가 — 10종 패밀리어 아이템 ItemData 생성 |
| 2026-02-27 | Familiar_FlyToy.prefab 생성 (올리브 그린, Orbit 모드, attackInterval=1s, 605번 전용) |
| 2026-02-27 | 605번 소환 버그 수정 — Familiar_FlyToy.prefab 없어 Resources.Load null 반환하던 문제 해결 |
| 2026-02-27 | T-M-A 파이프라인 디버그 로그 추가 — PassiveTrigger 발동 확인, SpawnFamiliarAction 실행/실패/위치 로그 |
| 2026-02-27 | SpawnFamiliarAction Z축 보정 — 스폰 위치 Z값을 playerPos.z 고정으로 수정 (2D 스프라이트 뒤로 밀림 방지) |
| 2026-02-27 | SpawnFamiliarAction SpriteRenderer.sprite null 경고 추가 — Familiar_FlyToy.prefab 스프라이트 미할당 버그 탐지 |
| 2026-02-27 | FloorMaterialSetup.cs 신규 — Floor01 텍스처 3종으로 URP/Lit 머티리얼 생성 및 씬 Floor 오브젝트 자동 적용 |
| 2026-02-27 | Floor01_N.png NormalMap 타입 자동 보장 로직 추가 (FloorMaterialSetup) |
| 2026-02-28 | GachaCapsuleSpawner 장애물 프리팹 선택을 순차(i % N)에서 무작위(Random.Range)로 변경 |
| 2026-02-28 | Obstacle.prefab 삭제 → Obstacle_A / Obstacle_B / Obstacle_C 3종으로 분리 (GachaCapsuleSpawner 다형 스폰) |
| 2026-02-28 | ObstacleController.IsSettled 프로퍼티 추가 — SettleRoutine 완료 시 true 설정 |
| 2026-02-28 | GachaCapsuleSpawner 착지 대기를 고정 WaitForSeconds → WaitUntil(IsSettled) + 타임아웃으로 변경 (불필요한 딜레이 제거) |
| 2026-02-28 | EnemyBase.Awake() — _spriteRenderer null이면 GetComponentInChildren 자동 탐색 추가 (StickyHand 등 Inspector 미연결 해결) |
| 2026-02-28 | EnemyBase ApplyBillboard / UpdateFacingDirection — _spriteRenderer null 가드 추가 |
| 2026-02-28 | ScenePostProcessingSetup.cs 신규 에디터 스크립트 (Tools > Setup Post Processing & Lighting / Convert Built-in Materials to URP) |
| 2026-02-28 | EnsureURPPipelineAsset() — URP 파이프라인 에셋 없을 시 자동 생성 및 GraphicsSettings/QualitySettings 연결 |
| 2026-02-28 | SetupGlobalVolume() — Bloom(보라 틴트)/Vignette(다크 퍼플)/ColorAdjustments(콜드 블루 필터) Cult Vibe 세팅 |
| 2026-02-28 | SetupCamera() — UniversalAdditionalCameraData.renderPostProcessing = true 자동 활성화 |
| 2026-02-28 | SetupDirectionalLight() — Directional Light 없을 때만 생성 (청보라 #6170b8, intensity=0.5) |
| 2026-02-28 | ConvertMaterialsToURP() — Built-in Standard/Unlit/Particle 셰이더 → URP 셰이더 일괄 변환 (UI/Sprites/Hidden 제외) |
| 2026-02-28 | UniversalRendererData.asset — postProcessData GUID 수동 패치 (Bloom/Vignette 렌더링 안 되던 문제 해결) |
| 2026-02-28 | SpawnExplosionAction.cs 신규 — 폭발 Action 모듈 (미구현 스켈레톤, 기획 연동 대기) |
| 2026-03-01 | Global Volume 씬 직접 배치 — PostProcess_CultVibes.asset 프로파일 적용 (Bloom/Vignette/ColorAdjustments) |
| 2026-03-01 | 배경 오브젝트 씬 배치 — claude remix.unity에 배경 환경 오브젝트 추가 |
| 2026-03-01 | 몬스터 스프라이트 추가 — Golem_Weak_Mob_2_Walk_NE.png, monster1.png (Assets/Graphic/) |
| 2026-03-01 | 3D 배경 그래픽 에셋 추가 — Assets/Graphic/3D graphic/ |
| 2026-03-01 | NavMesh-Floor.asset 삭제 — 씬 로컬 폴더(Assets/claude remix/)로 이전 |
| 2026-03-01 | DESIGN_PLAN.txt 추가 — 701~721 아이템 Phase 1 확장 설계 계획서 |

---

*이 문서는 `REMIX_PROTO_1/HANDOFF.md`에 저장됩니다.*
