using System.Collections.Generic;
using System.Linq;
using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// PROTOTYPE: 편성 화면의 후보 캐릭터 목록과 편성 규칙. 정식 보유 캐릭터·진행도 시스템이 생기면 대체한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Prototype Roster", fileName = "PrototypeRoster")]
    public sealed class PrototypeRoster : ScriptableObject
    {
        [SerializeField] private List<UnitDefinition> candidates = new List<UnitDefinition>();

        [Tooltip("PROTOTYPE: 출격 인원 상한 (기획 미정)")]
        [SerializeField, Min(1)] private int maxPartySize = 4;

        public IReadOnlyList<UnitDefinition> Candidates => candidates;
        public int MaxPartySize => maxPartySize;

        public bool ValidateParty(IReadOnlyCollection<UnitDefinition> selected, out string reason)
        {
            if (selected == null || selected.Count == 0)
                reason = "최소 1명을 편성해야 합니다";
            else if (selected.Count > maxPartySize)
                reason = $"최대 {maxPartySize}명까지 편성할 수 있습니다";
            else if (selected.Distinct().Count() != selected.Count)
                reason = "같은 캐릭터를 중복 편성할 수 없습니다";
            else if (selected.Any(u => !candidates.Contains(u)))
                reason = "후보에 없는 캐릭터입니다";
            else
                reason = null;

            return reason == null;
        }
    }
}
