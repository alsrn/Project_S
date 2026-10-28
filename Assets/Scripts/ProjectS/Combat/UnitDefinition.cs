using UnityEngine;

namespace ProjectS.Combat
{
    /// <summary>
    /// 유닛 종류의 변하지 않는 설정값(에셋). 현재 HP·좌표 같은 런타임 상태는 Unit 에 둔다.
    /// PROTOTYPE: 수치는 모두 임시값이다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Unit Definition", fileName = "UnitDefinition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Unit";
        [SerializeField, Min(1)] private int maxHp = 10;
        [SerializeField] private Color color = Color.white;

        public string DisplayName => displayName;
        public int MaxHp => maxHp;
        public Color Color => color;
    }
}
