using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// BattleSession 을 프레임마다 진행시키는 MonoBehaviour. 규칙 판단은 하지 않는다.
    /// 일시 정지 중에는 Scheduler 를 진행시키지 않는다.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        private bool enemyTurnPending;

        public BattleSession Session { get; private set; }
        public BattleContext Context => Session.Context;
        public ActionScheduler Scheduler => Session.Scheduler;
        public TurnManager Turns => Session.Turns;
        public PlayerCommands Commands => Session.Commands;
        public ResourceSystem Resources => Session.Resources;
        public DeckSystem Deck => Session.Deck;
        public bool IsPaused { get; set; }

        public static BattleController Create(BattleSession session)
        {
            var controller = new GameObject("BattleController").AddComponent<BattleController>();
            controller.Session = session;
            session.Turns.TurnStarted += controller.OnTurnStarted;
            return controller;
        }

        public void Begin() => Session.Begin();

        /// <summary>강제 종료(승리/패배/나가기). StageFlowController 만 호출한다.</summary>
        public void Shutdown()
        {
            Session.Shutdown();
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
            if (Session != null) Session.Turns.TurnStarted -= OnTurnStarted;
        }
    }
}
