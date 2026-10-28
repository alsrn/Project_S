using System.Collections.Generic;
using System.Linq;
using ProjectS.Combat;

namespace ProjectS.Core
{
    /// <summary>
    /// 편성 화면 → 전투로 전달되는 결과(불변). 스테이지 입장 시 이 데이터로 아군 유닛을 만든다.
    /// TODO(단계 D): 덱 구성(전용 카드 + 선택한 기본 카드)도 이 객체로 전달한다.
    /// </summary>
    public sealed class PartySelection
    {
        public IReadOnlyList<UnitDefinition> Members { get; }

        public PartySelection(IEnumerable<UnitDefinition> members)
        {
            Members = members.ToList();
        }
    }
}
