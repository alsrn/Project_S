using System.Collections.Generic;
using System.Linq;
using ProjectS.Actions;
using ProjectS.Combat;

namespace ProjectS.Core
{
    /// <summary>덱 구성 1항목. Owner = 사용할 캐릭터(전용 카드), null 이면 공용 카드.</summary>
    public readonly struct DeckEntry
    {
        public readonly CardDefinition Card;
        public readonly UnitDefinition Owner;

        public DeckEntry(CardDefinition card, UnitDefinition owner)
        {
            Card = card;
            Owner = owner;
        }
    }

    /// <summary>
    /// 편성 화면 → 전투로 전달되는 결과(불변): 출격 캐릭터 + 덱 구성.
    /// PROTOTYPE: 덱 = 출격 캐릭터들의 전용 카드 각 1장(기획 '임시로 전부 1장') + 공용 카드 각 1장. 공용 카드 선택 UI 는 미구현.
    /// </summary>
    public sealed class PartySelection
    {
        public IReadOnlyList<UnitDefinition> Members { get; }
        public IReadOnlyList<DeckEntry> Deck { get; }

        public PartySelection(IEnumerable<UnitDefinition> members, IEnumerable<DeckEntry> deck)
        {
            Members = members.ToList();
            Deck = deck.ToList();
        }

        /// <summary>PROTOTYPE 기본 덱: 각 캐릭터의 전용 카드 1장씩 + 공용 카드 1장씩(사용자 없음).</summary>
        public static PartySelection WithCharacterCards(IEnumerable<UnitDefinition> members,
            IEnumerable<CardDefinition> commonCards = null)
        {
            var list = members.ToList();
            var deck = list.SelectMany(m => m.Cards.Where(c => c != null).Select(c => new DeckEntry(c, m)))
                .Concat((commonCards ?? Enumerable.Empty<CardDefinition>()).Where(c => c != null).Select(c => new DeckEntry(c, null)));
            return new PartySelection(list, deck);
        }
    }
}
