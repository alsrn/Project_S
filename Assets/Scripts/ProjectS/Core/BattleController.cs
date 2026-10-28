using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// BattleSession 을 프레임마다 진행시키는 MonoBehaviour. 규칙 판단은 하지 않는다.
    /// 일시 정지 중에는 Session.Tick 을 호출하지 않으므로 행동 실행·적 AI 가 멈춘다(화면 조작·메뉴는 계속 가능).
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
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
            return controller;
        }

        public void Begin() => Session.Begin();

        private void Update()
        {
            if (!IsPaused) Session.Tick(Time.deltaTime);
        }
    }
}
