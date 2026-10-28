using System;
using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// 플레이어 턴 ↔ 적 턴 전환과 턴 시작/종료 이벤트.
    /// 턴 종료 시점은 ActionScheduler 의 TurnEndReached(앞선 행동 완료 후)에 맞춘다.
    /// 카드 효과 등이 직접 턴을 넘기지 않는다 — 항상 Scheduler 의 턴 종료 요청을 거친다.
    /// </summary>
    public sealed class TurnManager
    {
        private readonly ActionScheduler scheduler;
        private readonly ResourceSystem resources;

        /// <summary>플레이어 턴 기준 턴 번호(1부터). 적 턴은 같은 번호를 공유한다.</summary>
        public int TurnNumber { get; private set; }
        public Team ActiveTeam { get; private set; } = Team.Neutral;

        public event Action<Team, int> TurnStarted;
        public event Action<Team, int> TurnEnded;

        public TurnManager(ActionScheduler scheduler, ResourceSystem resources)
        {
            this.scheduler = scheduler;
            this.resources = resources;
            scheduler.TurnEndReached += OnTurnEndReached;
        }

        public void StartFirstTurn() => StartTurn(Team.Player);

        private void StartTurn(Team team)
        {
            if (scheduler.IsShutdown) return;

            if (team == Team.Player)
            {
                TurnNumber++;
                resources.RefillToMax(); // PROTOTYPE: SP 회복 시점·양 미정
            }

            ActiveTeam = team;
            scheduler.BeginTurn(team);
            Debug.Log($"[Turn] {team} turn {TurnNumber} start");
            TurnStarted?.Invoke(team, TurnNumber);
        }

        private void OnTurnEndReached(Team team)
        {
            if (team != ActiveTeam || scheduler.IsShutdown) return;

            Debug.Log($"[Turn] {team} turn {TurnNumber} end");
            TurnEnded?.Invoke(team, TurnNumber);
            if (scheduler.IsShutdown) return; // 턴 종료 시점 판정(턴 제한 등)으로 전투가 끝난 경우

            StartTurn(team == Team.Player ? Team.Enemy : Team.Player);
        }
    }
}
