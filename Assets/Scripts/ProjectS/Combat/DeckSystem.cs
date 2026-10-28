using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectS.Combat
{
    /// <summary>
    /// 뽑을 카드 더미 / 손패 / 버린 카드 더미.
    /// PROTOTYPE(Slay the Spire 방식): 턴 시작 시 N장 드로우, 뽑을 더미가 비면 버린 더미를 섞어 다시 만든다,
    /// 턴 종료 시 손패를 전부 버린다. 소멸(Exhaust) 등은 미구현.
    /// 카드 사용 상태 정책(§3.3)도 여기 한 곳에서만 처리한다: 확정 시 손패→버림, 재검증 실패(Abort) 시 버림→손패 복원.
    /// </summary>
    public sealed class DeckSystem
    {
        private readonly List<CardInstance> drawPile;
        private readonly List<CardInstance> hand = new List<CardInstance>();
        private readonly List<CardInstance> discardPile = new List<CardInstance>();
        private readonly Random random;

        public IReadOnlyList<CardInstance> Hand => hand;
        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;

        public event Action Changed;

        public DeckSystem(IEnumerable<CardInstance> cards, int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
            drawPile = cards.ToList();
            Shuffle(drawPile);
        }

        public bool IsInHand(CardInstance card) => hand.Contains(card);

        public void Draw(int count, int handLimit)
        {
            for (int i = 0; i < count && hand.Count < handLimit; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0) break;
                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    Shuffle(drawPile);
                }

                hand.Add(drawPile[drawPile.Count - 1]);
                drawPile.RemoveAt(drawPile.Count - 1);
            }

            Changed?.Invoke();
        }

        public void DiscardHand()
        {
            discardPile.AddRange(hand);
            hand.Clear();
            Changed?.Invoke();
        }

        /// <summary>카드 사용 확정(비용 예약과 같은 시점). 손패에서 버린 더미로.</summary>
        public void OnCardCommitted(CardInstance card)
        {
            if (!hand.Remove(card))
                throw new InvalidOperationException($"{card} is not in hand");
            discardPile.Add(card);
            Changed?.Invoke();
        }

        /// <summary>예상치 못한 Abort 시 사용 상태 복원. PROTOTYPE 정책.</summary>
        public void OnCardAborted(CardInstance card)
        {
            if (!discardPile.Remove(card)) return;
            hand.Add(card);
            Changed?.Invoke();
        }

        private void Shuffle(List<CardInstance> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
