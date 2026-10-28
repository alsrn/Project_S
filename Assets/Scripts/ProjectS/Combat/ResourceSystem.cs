using System;
using ProjectS.Actions;
using UnityEngine;

namespace ProjectS.Combat
{
    /// <summary>
    /// 행동 자원(SP)의 유일한 관리 지점: 비용 계산 · 차감(예약) · 반환 · 회복.
    /// 이동과 카드가 같은 SP 를 쓴다. UI 나 효과 코드는 SP 를 직접 바꾸지 않고 이 클래스를 호출한다.
    /// PROTOTYPE: SP 는 플레이어 팀 공용 풀 하나. 적 팀 행동은 비용 0.
    /// </summary>
    public sealed class ResourceSystem
    {
        public int Sp { get; private set; }
        public int MaxSp { get; }

        public event Action Changed;

        public ResourceSystem(int maxSp)
        {
            MaxSp = maxSp;
        }

        /// <summary>
        /// 이 행동을 지금 확정할 때 필요한 비용. TODO: 패시브/버프/맵 기믹 비용 보정은 여기서 합산한다.
        /// </summary>
        public int GetCost(ActionDefinition definition, ActionContext context) =>
            context.Team == Team.Player ? definition.BaseCost : 0;

        public bool CanAfford(int cost) => cost <= Sp;

        /// <summary>비용 예약(선차감). 부족하면 아무것도 바꾸지 않고 false.</summary>
        public bool TrySpend(int cost)
        {
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (!CanAfford(cost)) return false;
            if (cost == 0) return true;

            Sp -= cost;
            Changed?.Invoke();
            return true;
        }

        /// <summary>예약했던 비용을 그대로 돌려준다. PROTOTYPE: 최대치를 넘더라도 예약 당시 금액을 그대로 반환.</summary>
        public void Refund(int amount)
        {
            if (amount <= 0) return;
            Sp += amount;
            Changed?.Invoke();
        }

        /// <summary>효과에 의한 회복(예: "sp 회복" 카드). 최대치를 넘지 않는다.</summary>
        public void Gain(int amount)
        {
            if (amount <= 0) return;
            Sp = Mathf.Min(MaxSp, Sp + amount);
            Changed?.Invoke();
        }

        /// <summary>PROTOTYPE: 플레이어 턴 시작 시 SP 를 최대치로 채운다(기획 미정).</summary>
        public void RefillToMax()
        {
            Sp = MaxSp;
            Changed?.Invoke();
        }
    }
}
