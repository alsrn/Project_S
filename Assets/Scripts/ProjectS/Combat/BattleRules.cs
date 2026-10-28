using System;
using UnityEngine;

namespace ProjectS.Combat
{
    public enum DefenseFormula
    {
        Subtract,         // 피해 = 위력 - 방어
        PercentReduction, // 피해 = 위력 × (1 - 방어/100)
        Ratio,            // 피해 = 위력 × 100 / (100 + 방어)
    }

    /// <summary>
    /// 전투 공통 규칙 수치(에셋). 피해 공식과 덱 운용 규칙을 코드에 박지 않고 여기서 바꾼다.
    /// PROTOTYPE: 피해 공식 = 단순 차감(기획 협의), 덱 = Slay the Spire 방식(턴 시작 드로우, 턴 종료 손패 버림).
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Battle Rules", fileName = "BattleRules")]
    public sealed class BattleRules : ScriptableObject
    {
        [Header("피해")]
        [SerializeField] private DefenseFormula defenseFormula = DefenseFormula.Subtract;
        [SerializeField, Min(0)] private int minDamage = 1;
        [Tooltip("테스트용 최종 피해 배율. 기획 수치로는 10턴 안에 클리어가 어려워 승리 흐름을 시험할 때 올린다. 기본 1")]
        [SerializeField, Min(0f)] private float debugDamageMultiplier = 1f;

        [Header("덱 (Slay the Spire 방식)")]
        [SerializeField, Min(0)] private int drawPerTurn = 5;
        [SerializeField, Min(1)] private int handLimit = 10;

        public int DrawPerTurn => drawPerTurn;
        public int HandLimit => handLimit;

        /// <summary>최종 피해량 계산의 유일한 지점.</summary>
        public int CalculateDamage(int power, int defense, bool ignoreDefense)
        {
            float raw = ignoreDefense ? power : ApplyDefense(power, Mathf.Max(0, defense));
            int damage = Mathf.RoundToInt(raw * debugDamageMultiplier);
            return Mathf.Max(minDamage, damage);
        }

        private float ApplyDefense(int power, int defense)
        {
            switch (defenseFormula)
            {
                case DefenseFormula.Subtract: return power - defense;
                case DefenseFormula.PercentReduction: return power * Mathf.Max(0f, 1f - defense / 100f);
                case DefenseFormula.Ratio: return power * 100f / (100f + defense);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
