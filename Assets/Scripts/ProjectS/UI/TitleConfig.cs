using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// 타이틀 화면 아트(교체 지점). 배치 좌표는 원본 PSD(ArtSources/1 title/타이틀화면 - 작업파일.psd, 1920x1080)를 따른다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Title Config", fileName = "TitleConfig")]
    public sealed class TitleConfig : ScriptableObject
    {
        [SerializeField] private Sprite background;
        [SerializeField] private Sprite spaceship;
        [Tooltip("PSD 의 표시 중인 로고 레이어(벡터 스마트 오브젝트)를 추출한 이미지")]
        [SerializeField] private Sprite logo;

        [Header("메뉴 글자 이미지 (흰색, 선택 시 검게 칠함)")]
        [SerializeField] private Sprite menuStart;
        [SerializeField] private Sprite menuLoad;
        [SerializeField] private Sprite menuOption;
        [SerializeField] private Sprite menuExit;

        public Sprite Background => background;
        public Sprite Spaceship => spaceship;
        public Sprite Logo => logo;
        public Sprite MenuStart => menuStart;
        public Sprite MenuLoad => menuLoad;
        public Sprite MenuOption => menuOption;
        public Sprite MenuExit => menuExit;
    }
}
