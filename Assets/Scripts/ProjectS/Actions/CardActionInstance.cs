using System.Collections;
using System.Collections.Generic;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Targeting;
using UnityEngine;

namespace ProjectS.Actions
{
    /// <summary>
    /// 카드(또는 적 능력) 사용 1회. 실행 직전 현재 상태로 대상을 재검증한 뒤 효과를 적용한다.
    /// 사용자가 대기 중 이동했더라도 새 위치 기준으로 대상이 유효하면 그대로 실행한다(§3.5).
    /// - 플레이어 카드: Card(손패의 카드 인스턴스)가 있고, 확정 시 손패에서 빠지며 Abort 시 복원(DeckSystem).
    /// - 적 능력: Card 가 null. 같은 CardDefinition 데이터와 실행 체계를 쓰되 덱을 거치지 않는다.
    /// </summary>
    public sealed class CardActionInstance : ActionInstance
    {
        /// <summary>손패에서 사용한 카드. 적 능력이면 null.</summary>
        public CardInstance Card { get; }
        private CardDefinition CardDef => (CardDefinition)Definition;

        public CardActionInstance(CardInstance card, ActionContext context, IReadOnlyList<TargetRef> targets)
            : base(card.Definition, context, targets)
        {
            Card = card;
        }

        /// <summary>덱을 거치지 않는 능력 사용(적 공격 등).</summary>
        public CardActionInstance(CardDefinition ability, ActionContext context, IReadOnlyList<TargetRef> targets)
            : base(ability, context, targets)
        {
        }

        public override bool Revalidate(BattleContext battle, out string reason) =>
            battle.Targets.ValidateCard(CardDef, Context, Targets, out reason);

        public override IEnumerator Execute(BattleContext battle, ExecutionClock clock)
        {
            Debug.Log($"[Card] {this} -> {string.Join(", ", Targets)}");
            yield return clock.Wait(CardDef.ExecuteSeconds * 0.4f);

            foreach (var effect in CardDef.Effects)
                effect.Apply(battle, Context, Targets);

            yield return clock.Wait(CardDef.ExecuteSeconds * 0.6f);
        }

        public override void OnCommitted(BattleContext battle)
        {
            if (Card != null) battle.Deck?.OnCardCommitted(Card);
        }

        public override void OnAborted(BattleContext battle)
        {
            if (Card != null) battle.Deck?.OnCardAborted(Card);
        }
    }
}
