using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// 화면 표현용 에셋 묶음(아트 교체 지점). 타일·유닛 프리팹과 공용 스프라이트를 지정한다.
    /// 유닛별 스프라이트는 UnitDefinition 에서 지정한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Presentation Config", fileName = "PresentationConfig")]
    public sealed class PresentationConfig : ScriptableObject
    {
        [Header("프리팹")]
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private UnitView unitPrefab;

        [Header("타일")]
        [Tooltip("일반 타일 스프라이트. 비우면 흰 사각형 + 체크무늬 색")]
        [SerializeField] private Sprite tileSprite;
        [SerializeField] private Color tileTint = Color.white;
        [SerializeField] private Color deploymentTint = new Color(0.55f, 0.7f, 1f);

        [Header("UI")]
        [SerializeField] private Sprite victoryBanner;
        [SerializeField] private Sprite turnEndButton;

        public TileView TilePrefab => tilePrefab;
        public UnitView UnitPrefab => unitPrefab;
        public Sprite TileSprite => tileSprite;
        public Color TileTint => tileTint;
        public Color DeploymentTint => deploymentTint;
        public Sprite VictoryBanner => victoryBanner;
        public Sprite TurnEndButton => turnEndButton;
    }
}
