using System.Collections.Generic;
using ProjectS.Targeting;
using UnityEngine;

namespace ProjectS.Actions
{
    /// <summary>
    /// 카드 1종의 정의(에셋): 비용(BaseCost), 대상 선택 조건, 효과 목록, 연출 시간.
    /// 새 카드는 이 에셋을 하나 더 만들고 값과 효과를 채우면 된다(카드별 C# 클래스 없음).
    /// 실행 중에는 새 전투 요청을 대기열에 넣는다(§1.2).
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Card Definition", fileName = "CardDefinition")]
    public sealed class CardDefinition : ActionDefinition
    {
        [SerializeField, TextArea] private string description;
        [SerializeField] private List<TargetRequirement> targetRequirements = new List<TargetRequirement>();
        [SerializeReference] private List<CardEffect> effects = new List<CardEffect>();
        [Tooltip("카드 사용 연출 시간(초). 이 동안 다른 요청은 대기열에 쌓인다")]
        [SerializeField, Min(0f)] private float executeSeconds = 1.2f;

        public string Description => description;
        public IReadOnlyList<TargetRequirement> TargetRequirements => targetRequirements;
        public IReadOnlyList<CardEffect> Effects => effects;
        public float ExecuteSeconds => executeSeconds;

        public override WhileExecutingPolicy WhileExecuting => WhileExecutingPolicy.QueueNewRequests;

#if UNITY_EDITOR
        // 인스펙터에서 [SerializeReference] 효과를 추가하기 위한 메뉴(컴포넌트 ⋮ 메뉴)
        [ContextMenu("Add Damage Effect")]
        private void AddDamageEffect()
        {
            UnityEditor.Undo.RecordObject(this, "Add Damage Effect");
            effects.Add(new DamageEffect());
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
