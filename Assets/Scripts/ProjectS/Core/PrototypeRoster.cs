using System.Collections.Generic;
using System.Linq;
using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// PROTOTYPE: 편성 화면의 후보 캐릭터 목록과 편성 규칙. 정식 보유 캐릭터·진행도 시스템이 생기면 대체한다.
    /// 출격 인원 상한과 해금 기준은 스테이지(StageDefinition)에서 가져온다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Prototype Roster", fileName = "PrototypeRoster")]
    public sealed class PrototypeRoster : ScriptableObject
    {
        [SerializeField] private List<UnitDefinition> candidates = new List<UnitDefinition>();

        public IReadOnlyList<UnitDefinition> Candidates => candidates;

        public static bool IsUnlocked(UnitDefinition character, StageDefinition stage) =>
            character.UnlockStage <= stage.StageNumber;

        public bool ValidateParty(IReadOnlyCollection<UnitDefinition> selected, StageDefinition stage, out string reason)
        {
            if (selected == null || selected.Count == 0)
                reason = "최소 1명을 편성해야 합니다";
            else if (selected.Count > stage.PartySize)
                reason = $"최대 {stage.PartySize}명까지 편성할 수 있습니다";
            else if (selected.Distinct().Count() != selected.Count)
                reason = "같은 캐릭터를 중복 편성할 수 없습니다";
            else if (selected.Any(u => !candidates.Contains(u)))
                reason = "후보에 없는 캐릭터입니다";
            else if (selected.Any(u => !IsUnlocked(u, stage)))
                reason = "아직 해금되지 않은 캐릭터입니다";
            else
                reason = null;

            return reason == null;
        }
    }
}
