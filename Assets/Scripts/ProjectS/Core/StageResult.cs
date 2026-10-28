using ProjectS.Combat;

namespace ProjectS.Core
{
    public enum StageOutcome
    {
        None,
        Victory,
        Defeat,
        Exit, // 중도 나가기
    }

    public readonly struct StageResult
    {
        public readonly StageOutcome Outcome;
        public readonly string Reason;
        public readonly int TurnNumber;

        public StageResult(StageOutcome outcome, string reason, int turnNumber)
        {
            Outcome = outcome;
            Reason = reason;
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// 스테이지 종료 조건 판정(상태를 바꾸지 않음).
    /// - 아군 전멸 → 패배 (적과 동시 전멸 시에도 패배 우선: PROTOTYPE 결정)
    /// - 적 전멸 → 승리
    /// - 턴 제한: 플레이어 턴 N(=TurnLimit) 이 끝났는데 적이 남아 있으면 패배 (GDD '10턴 이내 클리어' 근거, PROTOTYPE 해석)
    /// TODO: 스테이지 특수 트리거(미정)
    /// </summary>
    public static class StageRuleEvaluator
    {
        public static StageOutcome EvaluateUnits(Battlefield battlefield, out string reason)
        {
            if (!battlefield.Units.HasAlive(Team.Player))
            {
                reason = "아군 전멸";
                return StageOutcome.Defeat;
            }

            if (!battlefield.Units.HasAlive(Team.Enemy))
            {
                reason = "적 전멸";
                return StageOutcome.Victory;
            }

            reason = null;
            return StageOutcome.None;
        }

        public static bool IsTurnLimitReached(StageDefinition stage, Team endedTeam, int turnNumber) =>
            stage.TurnLimit > 0 && endedTeam == Team.Player && turnNumber >= stage.TurnLimit;
    }
}
