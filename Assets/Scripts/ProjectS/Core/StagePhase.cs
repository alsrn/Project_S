namespace ProjectS.Core
{
    /// <summary>StageFlowController 가 관리하는 스테이지 진행 단계.</summary>
    public enum StagePhase
    {
        None,
        Formation,   // 편성(+덱 구성)
        Deployment,  // 아군 배치
        Battle,      // 플레이어 턴 ↔ 적 턴
        Result,      // 승리 / 패배 / 나가기 결과
    }
}
