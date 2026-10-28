using System;
using System.Collections.Generic;
using ProjectS.Combat;
using ProjectS.Grid;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// 스테이지마다 달라지는 데이터(맵 크기, 적 스폰, 아군 배치 구역).
    /// 좌표는 기획 문서와 같은 1기반으로 입력하고, 내부 좌표 변환은 GridCoord.FromDesign 에서만 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Stage Definition", fileName = "StageDefinition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [Serializable]
        public struct EnemySpawn
        {
            public UnitDefinition unit;
            [Tooltip("기획 좌표 (1기반)")] public Vector2Int designCoord;

            public GridCoord Coord => GridCoord.FromDesign(designCoord);
        }

        [SerializeField] private string displayName = "Stage";
        [SerializeField, Min(1)] private int stageNumber = 1;
        [Tooltip("출격 인원(기획: 1스테이지 3명). PROTOTYPE: 1명 이상 이 값 이하 허용")]
        [SerializeField, Min(1)] private int partySize = 3;
        [Tooltip("턴 제한(기획: 1스테이지 10턴). 0 = 제한 없음")]
        [SerializeField, Min(0)] private int turnLimit = 10;
        [Tooltip("게임 코스트(최대) = 행동 자원 SP 최대치로 해석 (PROTOTYPE)")]
        [SerializeField, Min(1)] private int maxSp = 20;
        [SerializeField, Min(1)] private int width = 20;
        [SerializeField, Min(1)] private int height = 20;
        [SerializeField] private List<EnemySpawn> enemySpawns = new List<EnemySpawn>();

        [Header("아군 배치 구역 (기획 좌표, 양 끝 포함)")]
        [SerializeField] private Vector2Int deploymentZoneMin = new Vector2Int(1, 18);
        [SerializeField] private Vector2Int deploymentZoneMax = new Vector2Int(20, 20);

        // TODO: 스테이지 특수 종료 트리거(미정). 현재 종료 규칙은 전멸·나가기뿐이다.

        public string DisplayName => displayName;
        public int StageNumber => stageNumber;
        public int PartySize => partySize;
        public int TurnLimit => turnLimit;
        public int MaxSp => maxSp;
        public int Width => width;
        public int Height => height;
        public IReadOnlyList<EnemySpawn> EnemySpawns => enemySpawns;
        public GridCoord DeploymentZoneMin => GridCoord.FromDesign(deploymentZoneMin);
        public GridCoord DeploymentZoneMax => GridCoord.FromDesign(deploymentZoneMax);

        public bool IsInDeploymentZone(GridCoord c)
        {
            var d = c.ToDesign();
            return d.x >= deploymentZoneMin.x && d.x <= deploymentZoneMax.x
                && d.y >= deploymentZoneMin.y && d.y <= deploymentZoneMax.y;
        }
    }
}
