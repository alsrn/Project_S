# Project S — 1스테이지 프로토타입 인계 문서

> 기준 명세: `SRPG_Stage1_Prototype_Agent_Spec.md` (2026-10-28) · 브랜치 `Prototype`
> 기획 수치 출처: `../GameDesigns/GDD_Main/*.xlsx` · 아트 출처: `../ArtSources`

## 1. 작업 결과 요약

편성 → 덱(자동) → 스테이지 입장 → 아군 배치 → 플레이어 턴 ↔ 적 턴 → 승리/패배/나가기 → 결과 화면까지 한 판을 끝까지 플레이할 수 있다.

| 영역 | 내용 |
|---|---|
| 씬 | `Assets/Scenes/Title.unity` (빌드 0번, 타이틀) → `Assets/Scenes/Stage1Prototype.unity` (빌드 1번, 코어 루프). 각각 카메라와 진입점 오브젝트(`TitleFlow` / `StageFlow`)만 있고 나머지는 런타임 생성 |
| 코드 | `Assets/Scripts/ProjectS/` (어셈블리 `ProjectS`). 기존 `Assets/Scripts/GridManager.cs`, `PlayerMover.cs` 는 수정하지 않음 |
| 데이터 | `Assets/Data/` — Stages, Units(캐릭터 7·랩터·Roster), Cards(평타 7 + 랩터 능력), Actions(이동), Rules(전투 규칙), Presentation |
| 아트 | `Assets/Art/` (ArtSources 를 영문 이름으로 복사), 프리팹 `Assets/Prefabs/TileView.prefab`, `UnitView.prefab` |
| 테스트 | `Assets/Tests/EditMode/` 49개 |
| 생성 도구 | 메뉴 **Project S > Setup Stage1 Prototype** — 에셋·프리팹·씬을 만들고 비어 있는 참조만 채운다(이미 있는 값은 덮어쓰지 않음) |

## 2. 실행 방법

- Unity **6000.3.24f1**, `Assets/Scenes/Title.unity` 를 열고 Play (스테이지만 시험할 때는 `Stage1Prototype.unity` 를 바로 Play 해도 된다).
- 타이틀: 마우스 호버/클릭 또는 ↑↓(W/S) + Enter/Space. 게임시작 → 편성부터 코어 루프, 게임종료 → 종료. 불러오기·옵션은 "준비 중" 안내만(PROTOTYPE).
  배치는 원본 PSD(`ArtSources/1 title/타이틀화면 - 작업파일.psd`) 레이어 좌표를 그대로 사용(`TitleScreen`). 로고 `Assets/Art/Title/Logo.png` 는 PSD 의 표시 중 로고 레이어를 추출한 파일.
- 결과 화면: 다시 시작(스테이지 재시작) / 타이틀로.
- 편성: 1스테이지 해금 캐릭터(클레어·리안·에더) 중 1~3명 선택 → 출격. 덱은 출격 캐릭터의 평타 카드 1장씩.
- 배치: 오른쪽 목록에서 아군 선택 → 파란 구역 클릭. 배치된 아군 클릭 = 재선택, 우클릭 = 해제. 전원 배치 시 "전투 시작" 활성.
- 전투:
  - 아군 클릭 → 이동 가능 칸(초록) → 빈 칸 클릭 = 이동 (턴당 1회, SP 1)
  - 손패 카드 클릭 → 사거리(옅은 빨강)·대상 후보(진한 빨강) → 적 클릭 = 사용. 우클릭/Esc = 대상 지정 취소
  - 오른쪽 열: 턴·SP·실행 중 행동·대기열, 일시 정지(P), 나가기(두 번 클릭), 턴 종료
- 승패 시험: `Assets/Data/Rules/BattleRules.asset` 의 `Debug Damage Multiplier` (현재 15).
- 테스트 실행: Window > General > Test Runner > EditMode > Run All.
  배치 모드: `Unity.exe -batchmode -nographics -projectPath <프로젝트> -runTests -testPlatform EditMode -testResults results.xml` (에디터를 닫은 상태)

## 3. 실제 코드 흐름

### 유닛 선택 → 이동

```text
SelectionController.HandleSelectionClick (UI: 빈 타일 클릭)
 → PlayerCommands.RequestMove
     1) ActionScheduler.GetAdmission     — 이동 중/턴 종료 확정/적 턴이면 거절
     2) PlayerCommands.CanMoveThisTurn   — 턴당 이동 횟수 (TurnUsage)
     3) TargetEvaluator.ValidateMove     — 범위·점유·경로(GridPathfinder BFS)
     4) ActionScheduler.TryCommit(MoveActionInstance)
          ResourceSystem.GetCost/TrySpend (선차감) → OnCommitted(이동 횟수 +1) → 대기열 → PumpQueue
 → PumpQueue: MoveActionInstance.Revalidate (현재 상태로 경로 재계산; 실패 시 Abort + SP·횟수 반환)
 → BattleController.Update → BattleSession.Tick → ActionScheduler.Tick
     → MoveActionInstance.Execute: 경로 한 칸마다 GridSystem.Relocate + 대기
 → Unit.StateChanged → UnitView 가 해당 칸으로 슬라이드, 이동 방향으로 스프라이트 전환
```

### 카드 선택 → 사용 → 대기열 → 실행

```text
HandPanel 카드 클릭 → SelectionController.BeginCard (대상 지정 모드, 턴 종료 버튼 비활성)
 → 하이라이트: TargetEvaluator.GetRangeArea / GetCandidates (Validate 와 같은 판정)
 → 적 클릭 → TargetEvaluator.ValidateTarget → PlayerCommands.RequestCard
     1) 손패 확인 2) GetAdmission 3) TargetEvaluator.ValidateCard
     4) ActionScheduler.TryCommit(CardActionInstance)
          SP 선차감 → OnCommitted: DeckSystem.OnCardCommitted (손패 → 버린 더미) → 대기열
 → (다른 행동 실행 중이면 FIFO 대기. 카드 실행 중 새 요청도 대기, 이동 실행 중 새 요청은 거절)
 → 차례가 오면 CardActionInstance.Revalidate (사용자의 현재 위치 기준 사거리·대상 생존)
     실패: Abort → SP 반환 + DeckSystem.OnCardAborted (카드 손패 복원)
 → Execute: 연출 대기 → CardEffect.Apply (DamageEffect → CombatService.DealDamage → BattleRules.CalculateDamage)
 → 사망 시 GridSystem.Remove, CombatService.Died → BattleSession 이 StageRuleEvaluator 로 승패 확인
     → 종료 시 BattleSession.End: Scheduler.Shutdown (진행 중 행동·대기열 폐기) → StageFlowController.OnBattleEnded → ResultScreen
```

### 적 턴

`TurnManager` 가 적 턴 시작 → `EnemyTurnController` 가 Scheduler 가 빌 때마다 적 1기씩 `EnemyAI` 로 판단하고, 플레이어와 같은 `MoveActionInstance` / `CardActionInstance` 를 `TryCommit` 한다. 모든 적이 끝나면 적 턴 종료를 요청한다.

### 클래스 책임 (명세 §3.1 대응)

| 명세 | 구현 |
|---|---|
| StageFlowController | `Core/StageFlowController` — 단계 전환, 나가기, 결과 화면 |
| BattleController | `Core/BattleSession`(순수 C# 조립·종료 판정 연결) + `Core/BattleController`(프레임 진행, 일시 정지) |
| TurnManager | `Core/TurnManager` |
| ActionScheduler | `Core/ActionScheduler` (Tick 기반, 코루틴 미사용) |
| TargetEvaluator | `Targeting/TargetEvaluator`, 조건 선언 `Targeting/TargetRequirement` |
| ResourceSystem | `Combat/ResourceSystem` |
| GridSystem | `Grid/GridSystem`, `Grid/GridCoord`(좌표 변환 단일 지점), `Grid/GridPathfinder` |
| UnitRegistry | `Combat/Unit`, `Combat/UnitRegistry` |
| StageRuleEvaluator | `Core/StageResult.cs` 의 `StageRuleEvaluator` |
| 플레이어 요청 입구 | `Core/PlayerCommands` |
| 피해·사망 | `Combat/CombatService`, 공식은 `Combat/BattleRules` |
| 덱 | `Combat/DeckSystem`, `Combat/CardInstance` |
| 적 AI | `Core/EnemyAI`(판단), `Core/EnemyTurnController`(진행) |

## 4. 새 카드 1장 추가하기

1. Project 창 `Assets/Data/Cards` 에서 **Create > Project S > Card Definition**.
2. `Display Name`, `Base Cost`(SP), `Description`, `Execute Seconds`(연출 시간) 입력.
3. `Target Requirements` 에 항목 추가 (예: 적 1명 = Kind `Unit`, Relation `Enemy`, Require Alive 체크, Range Source `ActorAttackRange` 또는 `Fixed`+Fixed Range / `Unlimited`). 0개면 대상 선택 없이 바로 사용.
4. 인스펙터 우측 ⋮ 메뉴에서 효과 추가(목록 순서대로 적용). 각 효과의 `Target` = `ChosenTarget`(고른 대상) 또는 `Self`(사용자).
   - **Add Damage Effect**: `Attack Multiplier`(현재 공격력 배율), `Flat Damage`(고정 피해), `Ignore Defense`
   - **Add Heal Effect**: 고정량 / 최대 체력 % / 현재 체력 %
   - **Add Status Effect**: 라벨, 디버프 여부, 지속 턴, 보정(공격 +, 방어 +, 방어 ×, 이동 +, 가하는 피해 ×, 받는 피해 ×)
   - **Add Remove Debuffs Effect**: 디버프 해제(개수, 0 = 전부)
5. 캐릭터 `Assets/Data/Units/Char_*.asset` 의 `Cards` 목록에 추가하면 편성 시 덱에 들어간다(첫 카드는 평타로 유지 — 적 AI·밸런스 테스트가 사용). 공용 카드는 `PrototypeRoster.asset` 의 `Common Cards`.

예: 살육 = [Status(Self, 공+70, 2턴)] → [Damage ×5]. 맹독탄 = [Status(Chosen, 디버프, 방×0.5, 받는피해×2, 1턴)].
새 효과 종류가 필요하면 `Actions/CardEffect.cs` 에 `CardEffect` 하위 클래스를 하나 추가하고(카드별 클래스 X), `CardDefinition` 의 ContextMenu 에 추가 메뉴를 만든다.

### 구현된 카드 (출처: 카드 디자인 시트)

| 캐릭터 | 카드 |
|---|---|
| 클레어 | 회전 베기(평타), 내려 찍기, 살육 |
| 리안 | 휘두르기(평타), 방패 활성화, 기사의 의지 |
| 에더 | 혈탄 발사(평타), 맹독탄, 혈청 투여 |
| 공용 | 지원 사격 |
| 2스테이지 이후 캐릭터 | 평타만 |
| 미구현 | 광역(포격, 치유의 노래, 치유파동), 대상 없음(sp 회복, 추가 지원), 카드 셔플류(케이티), 화상 지속 피해(니카), 연사(카노) |

## 5. 아트 교체

- 유닛: `UnitDefinition` 의 `Sprite Front/Back/Left/Right` (Front 만 있으면 모든 방향에 사용, 없으면 `Color` 사각형).
- 타일·결과 배너·턴 종료 버튼: `Assets/Data/Presentation/PresentationConfig.asset`.
- 표시 구조: `Assets/Prefabs/TileView.prefab`(Base + Highlight), `UnitView.prefab`(Body + HP 바). 크기는 칸에 맞춰 자동 조정.
- ArtSources 의 새 파일은 `Editor/PrototypeArtSetup.cs` 의 `ArtFiles` 목록에 (원본, 영문 대상) 을 추가하고 Setup 메뉴를 실행.
- 가져왔지만 아직 쓰지 않는 것: `Mob2_*`(2스테이지 적 후보), `Dirt/Stone/Steel` 타일. 미사용: 타이틀·스토리·스테이지 선택 UI, 스탠딩 일러스트.

## 6. 임시 결정 목록 (PROTOTYPE / TODO)

| 항목 | 임시 결정 | 바꾸는 곳 |
|---|---|---|
| 화면 방향 | 기획 격자판처럼 (1,1) 왼쪽 위, y 아래로 증가 | `BattlefieldView.CoordToWorld/TryWorldToCoord` |
| "NxN" 범위 | 거리 N (이동 = 4방향 경로 칸 수, 공격 = 맨해튼 거리) — 기획 협의 | `UnitDefinition`, `TargetEvaluator` |
| SP | 플레이어 팀 공용, 최대 20(스테이지 표 '게임 코스트'), 플레이어 턴 시작 시 최대치로 회복 | `StageDefinition.maxSp`, `ResourceSystem.RefillToMax` |
| 이동 | 1회 SP 1(거리 무관), 유닛별 턴당 1회 | `Move_Default.asset` (`Base Cost`, `Max Moves Per Turn`) |
| 이동 중 통과 | 모든 유닛(아군 포함) 통과 불가 | `TargetEvaluator.IsPassable` |
| 피해 공식 | 단순 차감(위력 − 방어), 최소 1 — 기획 협의 | `BattleRules.asset` (공식 선택 가능) |
| 테스트 배율 | 최종 피해 ×15 (기획 수치로는 10턴 클리어 불가) | `BattleRules.asset` `Debug Damage Multiplier` |
| 평타 배율 | 휘두르기 ×1→×3, 혈탄 발사 ×1→×2.2 (×15 기준 평타 675~750, 두 명이면 랩터 처치) | `Card_Lian_Swing`, `Card_Ader_BloodShot` |
| 덱 | Slay the Spire 방식: 턴 시작 5장 드로우, 턴 종료 손패 버림, 빈 덱은 버린 더미 셔플 — 기획 협의 | `BattleRules.asset`, `DeckSystem` |
| 덱 구성 | 출격 캐릭터 카드 각 1장 + 공용 카드 각 1장 자동(3명이면 10장). 덱 선택 UI 미구현 | `PartySelection.WithCharacterCards`, `PrototypeRoster.commonCards` |
| 드로우 | 턴당 4장(기획 협의 3~4장) — 10장 덱이 2~3턴마다 순환 | `BattleRules.asset` `Draw Per Turn` |
| 상태 효과 지속 | 기획 'N턴' = N 라운드. 적 턴 종료 시 1 감소. 플레이어 턴에 건 '1턴' = 그 턴 + 이어지는 적 턴 | `StatusEffect`, `CombatService.EndRound` |
| 상태 효과 중첩 | 같은 효과도 별개로 중첩 | `ApplyStatusEffect` |
| 방패 활성화 지속 | 기획 미기재 → 1턴 | `Card_Lian_ShieldUp` |
| 혈청 투여 사거리 | 기획 미기재 → 에더 공격 범위(5), 자신 포함 아군 | `Card_Ader_Serum` |
| 기사의 의지 이동 | '5x5' → 리안 이동 4 → 5 (+1) | `Card_Lian_KnightsWill` |
| 테스트 배율 적용 범위 | 피해에만 ×15, 회복·버프 수치에는 미적용 | `BattleRules.CalculateDamage` |
| 카드 사용 상태 | 확정 시 손패→버림, 재검증 실패 시 손패 복원 | `DeckSystem.OnCardCommitted/Aborted` |
| 대상 추적 | Unit 은 ID, Tile 은 좌표를 기억하고 실행 시점에 재조회 | `TargetRef` |
| 출격 인원 | 1명 이상 스테이지 값(3) 이하 | `StageDefinition.partySize` |
| 해금 | `UnitDefinition.unlockStage` ≤ 스테이지 번호만 편성 가능 | 각 캐릭터 에셋 |
| 적 AI | 첫 번째 능력으로 사거리 안 대상 중 HP 최저 공격, 아니면 이동 후 공격 가능한 가장 가까운 칸 / 가장 가까운 아군에 접근 | `EnemyAI` |
| 적 능력 | 랩터 '내려 찍기'(공격력 ×1)만. '돌진' 미구현 | `Card_Enemy_Raptor_Slam` |
| 턴 제한 패배 | 플레이어 턴 10 종료 시 적이 남아 있으면 패배(그 턴의 적 턴은 없음) | `StageRuleEvaluator.IsTurnLimitReached` |
| 동시 전멸 | 패배 우선 | `StageRuleEvaluator.EvaluateUnits` |
| 결과 화면 | 다시 시작(씬 재로드)/종료만. 보상·저장 없음 | `ResultScreen` |
| 카노 정조준 | ×3 단발. 연속 사용·SP 증가 미구현 | `Card_Kano_Aim` |

## 7. 수용 테스트 결과 (명세 §6)

표기: **T** = EditMode 자동 테스트 통과 · **P** = Play 에서 사용자 확인 · **미검증** = 확인 안 됨

| 항목 | 결과 |
|---|---|
| 6.1 맵 20×20, 기획/내부 좌표 일치 | T, P |
| 6.1 적 3기 (1,10)(2,5)(2,15) | T, P |
| 6.1 배치 구역 안 빈 타일에만 배치 | T, P |
| 6.1 전원 배치 시에만 전투 시작 활성 | T(로직), P |
| 6.2 Actor 선택 시 이동 가능 타일 표시 | P |
| 6.2 경로는 연속 인접 타일 | T, P |
| 6.2 이동 불가/점유/범위 밖 거절, 비용 없음 | T |
| 6.2 이동 중 이동·카드·턴 종료 거절 | T, P |
| 6.3 카드 선택 시 대상 표시 | P (사거리 영역 표시는 이번 변경 — 미검증) |
| 6.3 잘못된 대상/자원 부족 시 대기열·비용 없음 | T |
| 6.3 카드 실행 중 요청은 순서대로 대기 | T, P |
| 6.3 등록 시 선차감, 실행 시 재차감 없음 | T |
| 6.3 대상 사망 등으로 무효가 된 예약은 취소·환불 | T |
| 6.3 Actor 가 대기 중 이동해도 대상 유효하면 실행 | T |
| 6.3 예약된 턴 종료는 선행 행동 후, 이후 신규 거절 | T |
| 6.3 대상 지정 중 턴 종료 비활성, 명시적 취소 | 구현됨 · 미검증(Play) |
| 6.4 전멸 시 결과 화면 전환 | T(판정·정지), 결과 화면 표시는 미검증(Play) |
| 6.4 나가기/강제 종료는 행동·대기열을 기다리지 않음 | T |
| 6.4 종료 뒤 지연 실행이 상태를 바꾸지 않음 | T |
| 6.4 일시 정지·카메라·선택이 행동 정책과 분리 | 일시 정지·선택: 구현(일시 정지 Play 미검증). **카메라 팬/줌 미구현(P1)** |
| UI 가 전장을 가리지 않음 | T(1920×1080 기준), P |

## 8. 남은 문제와 위험

- **밸런스**: GDD 수치 + 단순 차감이면 리안(공 25)·에더(공 35)가 랩터(방 30)에 거의 피해를 못 주고, 랩터(공 40)는 클레어(방 45)·리안(방 60)에 최소 피해만 준다. 지금은 테스트 배율 ×15와 평타 배율 조정으로 플레이 가능하게만 맞춘 상태 — 피해 공식/수치는 기획 결정 필요.
- **적 위협 부족**: 위 이유로 아군 전멸 패배는 거의 일어나지 않고, 패배는 주로 턴 제한으로 발생.
- **스킬 카드 밸런스**: 테스트 배율 ×15 에서 내려 찍기(방어 무시 120 → 1800)는 랩터를 한 장으로 처치, 살육·맹독탄 조합은 과도하게 강함. 회복(혈청 +400 등)은 배율을 받지 않아 상대적으로 약함 — 피해 공식 결정 후 재조정 필요.
- **미구현 P1**: 카메라 팬/줌, 카드 사용 연출(투사체·이펙트), 피해 숫자 팝업, 유닛 정보 패널 아트(`5 battle/chr_info.png` 등).
- **미구현 P2**: 광역·타일 대상 효과(효과는 유닛 대상만), 상태 효과 아이콘(지금은 왼쪽 정보 패널에 글자로만 표시), 시야/LOS(켜면 '미지원'으로 실패), 맵 기믹, 2·3스테이지, 대상 추적 정책(§3.5).
- **에디터 배치 모드 부작용**: 명령줄로 Unity 를 실행하면 패키지가 `ProjectSettings.asset` 의 `SENTIS_ANALYTICS_ENABLED` 정의를 지운다 — 커밋하지 말 것.
- **팀원 검토 필요**: 턴 제한 패배 시점(플레이어 턴 10 종료 vs 적 턴 10 종료), 동시 전멸 시 판정, 재검증 실패 시 카드 손패 복원 정책, 이동 비용(거리 무관 1).
