using System.Collections.Generic;
using ProjectS.Actions;
using UnityEngine;

namespace ProjectS.Combat
{
    /// <summary>
    /// 유닛 종류의 변하지 않는 설정값(에셋). 현재 HP·좌표 같은 런타임 상태는 Unit 에 둔다.
    /// 수치 출처: GameDesigns/GDD_Main 의 캐릭터·적 기획표.
    /// 기획표의 "NxN" 범위는 PROTOTYPE 해석으로 '거리 N'(격자 칸 수)으로 입력한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Unit Definition", fileName = "UnitDefinition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Unit";
        [SerializeField, Min(1)] private int maxHp = 10;
        [SerializeField, Min(0)] private int attack = 10;
        [SerializeField, Min(0)] private int defense;
        [Tooltip("이동 범위(기획 NxN 의 N). PROTOTYPE: 4방향 경로 칸 수로 해석")]
        [SerializeField, Min(0)] private int moveRange = 3;
        [Tooltip("공격 범위(기획 NxN 의 N). PROTOTYPE: 맨해튼 거리로 해석")]
        [SerializeField, Min(0)] private int attackRange = 1;
        [Tooltip("편성 가능해지는 스테이지 번호(플레이어블 캐릭터만 의미 있음)")]
        [SerializeField, Min(1)] private int unlockStage = 1;
        [Tooltip("스프라이트가 없을 때 사각형 색. 스프라이트가 있으면 무시")]
        [SerializeField] private Color color = Color.white;

        [Header("필드 스프라이트 (비우면 색 사각형)")]
        [Tooltip("정면(화면 아래쪽을 볼 때). 방향별 스프라이트가 없으면 이것을 쓴다")]
        [SerializeField] private Sprite spriteFront;
        [SerializeField] private Sprite spriteBack;
        [SerializeField] private Sprite spriteLeft;
        [SerializeField] private Sprite spriteRight;
        [Tooltip("캐릭터 전용 카드(편성 시 덱에 들어감). PROTOTYPE: 평타 카드만")]
        [SerializeField] private List<CardDefinition> cards = new List<CardDefinition>();

        public string DisplayName => displayName;
        public int MaxHp => maxHp;
        public int Attack => attack;
        public int Defense => defense;
        public int MoveRange => moveRange;
        public int AttackRange => attackRange;
        public int UnlockStage => unlockStage;
        public Color Color => color;
        public IReadOnlyList<CardDefinition> Cards => cards;
        public bool HasSprite => spriteFront != null;

        /// <summary>바라보는 방향(격자 기준 이동 방향)에 맞는 스프라이트. 없으면 정면, 정면도 없으면 null.</summary>
        public Sprite GetSprite(Vector2Int facing)
        {
            Sprite s = null;
            if (facing.y > 0) s = spriteFront;       // 기획 y 증가 = 화면 아래쪽 = 정면
            else if (facing.y < 0) s = spriteBack;
            else if (facing.x < 0) s = spriteLeft;
            else if (facing.x > 0) s = spriteRight;
            return s != null ? s : spriteFront;
        }
    }
}
