using ProjectS.Actions;
using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// 전투 시스템 생성·연결과 프레임 진행 담당(MonoBehaviour). 규칙 판단은 하지 않는다.
    /// - 플레이어 요청: Commands(PlayerCommands)
    /// - 순서/실행: Scheduler,  턴: Turns,  비용: Resources,  대상 판정: Context.Targets
    /// 일시 정지 중에는 Scheduler 를 진행시키지 않는다.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        private bool enemyTurnPending;

        public BattleContext Context { get; private set; }
        public ActionScheduler Scheduler { get; private set; }
        public TurnManager Turns { get; private set; }
        public PlayerCommands Commands { get; private set; }
        public ResourceSystem Resources => Context.Resources;
        public bool IsPaused { get; set; }

        public static BattleController Create(Battlefield battlefield, MoveDefinition moveDefinition)
        {
            var controller = new GameObject("BattleController").AddComponent<BattleController>();
            controller.Context = new BattleContext(battlefield, new ResourceSystem(battlefield.Stage.MaxSp));
            controller.Scheduler = new ActionScheduler(controller.Context);
            controller.Turns = new TurnManager(controller.Scheduler, controller.Context.Resources);
            controller.Commands = new PlayerCommands(controller.Context, controller.Scheduler, moveDefinition);
            controller.Turns.TurnStarted += controller.OnTurnStarted;
            return controller;
        }

        public void Begin() => Turns.StartFirstTurn();

        /// <summary>강제 종료(승리/패배/나가기). StageFlowController 만 호출한다.</summary>
        public void Shutdown()
        {
            Scheduler.Shutdown();
            enabled = false;
        }

        private void Update()
        {
            if (IsPaused || Scheduler.IsShutdown) return;

            Scheduler.Tick(Time.deltaTime);

            if (enemyTurnPending && Scheduler.IsIdle)
            {
                // TODO(단계 E): 적 AI. 현재는 적 턴을 바로 넘긴다.
                enemyTurnPending = false;
                Scheduler.TryRequestEndTurn(Team.Enemy, out _);
            }
        }

        private void OnTurnStarted(Team team, int turn)
        {
            // 재진입을 피하려고 적 턴 처리는 다음 Update 에서 시작한다.
            enemyTurnPending = team == Team.Enemy;
        }

        private void OnDestroy()
        {
            if (Turns != null) Turns.TurnStarted -= OnTurnStarted;
        }
    }
}
