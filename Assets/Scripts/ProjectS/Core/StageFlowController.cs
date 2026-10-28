using System.Collections.Generic;
using System.Linq;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectS.Core
{
    /// <summary>
    /// 스테이지 진행 최상위 컨트롤러(씬 진입점). 편성 → 배치 → 전투 → 정산 흐름과 강제 종료를 담당한다.
    /// 단계별 세부 규칙은 각 담당 클래스(PrototypeRoster, DeploymentController, BattleSession, ...)에 위임하고 여기서는 전환만 한다.
    /// 종료(승리/패배/나가기)는 최우선: BattleSession.End 가 진행 중 행동·대기열을 즉시 버린 뒤 결과 화면으로 전환한다.
    /// </summary>
    public sealed class StageFlowController : MonoBehaviour
    {
        [SerializeField] private StageDefinition stage;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private MoveDefinition moveDefinition;
        [SerializeField] private BattleRules battleRules;
        [SerializeField] private PresentationConfig presentation;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;

        private BattlefieldView view;
        private PrototypeHud hud;
        private DeploymentController deployment;
        private DeploymentScreen deploymentScreen;
        private BattleController battle;
        private PartySelection party;
        private readonly List<GameObject> battleUi = new List<GameObject>();

        public StagePhase Phase { get; private set; } = StagePhase.None;
        public Battlefield Battlefield { get; private set; }

        private void Start()
        {
            if (stage == null || roster == null || moveDefinition == null || battleRules == null || presentation == null)
            {
                Debug.LogError("[StageFlow] StageDefinition / PrototypeRoster / MoveDefinition / BattleRules / PresentationConfig 참조가 비어 있습니다. " +
                               "메뉴 Project S > Setup Stage1 Prototype 을 실행하세요.", this);
                return;
            }

            UiFactory.EnsureEventSystem();
            EnterFormation();
        }

        private void EnterFormation()
        {
            SetPhase(StagePhase.Formation);
            FormationScreen.Create(roster, stage, EnterDeployment);
        }

        /// <summary>스테이지 입장: 전장 생성(적 스폰) → 편성 결과로 아군 생성(덱은 전투 시작 시 전달) → 배치 화면.</summary>
        private void EnterDeployment(PartySelection selection)
        {
            party = selection;
            Battlefield = Battlefield.Create(stage);
            LogSpawns();

            List<Unit> allies = party.Members.Select(def => Battlefield.Units.Create(def, Team.Player)).ToList();
            Debug.Log($"[StageFlow] Party: {string.Join(", ", allies)} / Deck: {string.Join(", ", party.Deck.Select(e => e.Card.DisplayName))}");

            view = BattlefieldView.Create(Battlefield, presentation, cellSize);
            hud = PrototypeHud.Create(Battlefield, view);
            FitCamera();

            deployment = new DeploymentController(Battlefield, allies);
            deploymentScreen = DeploymentScreen.Create(deployment, Battlefield, view, hud, TryStartBattle);

            SetPhase(StagePhase.Deployment);
            hud.SetPhase("배치 단계 - 아군 전원을 배치하면 전투를 시작할 수 있습니다");
        }

        /// <summary>Start 버튼 요청. 버튼 활성 여부와 별개로 여기서 조건을 다시 확인한다.</summary>
        private void TryStartBattle()
        {
            if (Phase != StagePhase.Deployment) return;

            if (!deployment.IsComplete)
            {
                hud.ShowMessage("아직 배치하지 않은 아군이 있습니다");
                return;
            }

            Destroy(deploymentScreen.gameObject);
            deploymentScreen = null;

            foreach (var ally in deployment.Allies)
                Debug.Log($"[StageFlow] Deployed {ally} at {ally.Coord}");

            SetPhase(StagePhase.Battle);
            hud.SetPhase("전투\n- 유닛 클릭: 선택\n- 빈 타일: 이동(턴당 1회)\n- 카드 클릭 → 적 클릭: 사용\n- 우클릭·Esc: 취소\n- P: 일시 정지");
            view.EnableMovementAnimation();

            var session = new BattleSession(Battlefield, moveDefinition, battleRules, party.Deck);
            session.Ended += OnBattleEnded;
            battle = BattleController.Create(session);

            var selection = SelectionController.Create(battle, view, hud);
            battleUi.Add(selection.gameObject);
            battleUi.Add(BattlePanel.Create(battle, selection, hud, presentation, ExitStage).gameObject);
            battleUi.Add(HandPanel.Create(battle, selection, hud).gameObject);
            battle.Begin();
        }

        /// <summary>중도 나가기. 진행 중 행동과 대기열을 기다리지 않는다.</summary>
        public void ExitStage()
        {
            if (Phase != StagePhase.Battle) return;
            battle.Session.End(StageOutcome.Exit, "플레이어가 스테이지를 나감");
        }

        /// <summary>전투 종료 처리. 이 시점에 Scheduler 는 이미 정지되어 이후 어떤 행동도 상태를 바꾸지 않는다.</summary>
        private void OnBattleEnded(StageResult result)
        {
            if (Phase == StagePhase.Result) return;
            SetPhase(StagePhase.Result);

            // 전투 입력 UI 제거(결과 화면에서 전투 요청이 나가지 않도록)
            foreach (var go in battleUi)
                if (go != null) Destroy(go);
            battleUi.Clear();
            battle.enabled = false;
            view.ClearHighlights();

            hud.SetPhase($"결과: {result.Outcome}");
            ResultScreen.Create(result, presentation, Restart, Quit);
        }

        private static void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetPhase(StagePhase next)
        {
            Debug.Log($"[StageFlow] Phase {Phase} -> {next}");
            Phase = next;
        }

        private void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (!cam.TryGetComponent(out BattlefieldCamera camController))
                camController = cam.gameObject.AddComponent<BattlefieldCamera>();
            camController.FitTo(view.WorldBounds, hud.Canvas);
        }

        private void LogSpawns()
        {
            Debug.Log($"[StageFlow] {stage.DisplayName}: {Battlefield.Grid.Width}x{Battlefield.Grid.Height} grid created");
            foreach (var unit in Battlefield.Units.All)
                Debug.Log($"[StageFlow] Spawned {unit.DisplayName} ({unit.Team}) at design {unit.Coord} / internal {unit.Coord.ToInternalString()}");
        }
    }
}
