using ProjectS.Actions;

namespace ProjectS.Combat
{
    /// <summary>
    /// 덱 안의 카드 1장(런타임). 같은 CardDefinition 이 여러 장이어도 각각 별도 인스턴스다.
    /// Owner = 이 카드를 사용하는 캐릭터 유닛(전용 카드). 공용 카드는 null(사용자 없음, 팀 기준으로 판정).
    /// </summary>
    public sealed class CardInstance
    {
        private static int nextId = 1;

        public int Id { get; }
        public CardDefinition Definition { get; }
        public Unit Owner { get; }

        public CardInstance(CardDefinition definition, Unit owner)
        {
            Id = nextId++;
            Definition = definition;
            Owner = owner;
        }

        public override string ToString() => $"{Definition.DisplayName}({Owner?.Definition.DisplayName ?? "공용"})";
    }
}
