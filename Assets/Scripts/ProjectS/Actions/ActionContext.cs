using ProjectS.Combat;

namespace ProjectS.Actions
{
    /// <summary>
    /// 행동의 주체 정보. Actor 는 없을 수 있고(예: 턴 시작 SP 회복), 관계(아군/적) 판정은 항상 Team 기준으로 한다.
    /// Source 는 발생 원인(카드 인스턴스, 패시브, 스테이지 규칙 등).
    /// </summary>
    public readonly struct ActionContext
    {
        public readonly Team Team;
        public readonly Unit Actor;
        public readonly object Source;

        public ActionContext(Team team, Unit actor, object source)
        {
            Team = team;
            Actor = actor;
            Source = source;
        }

        public static ActionContext ForActor(Unit actor, object source = null) => new ActionContext(actor.Team, actor, source);
    }
}
