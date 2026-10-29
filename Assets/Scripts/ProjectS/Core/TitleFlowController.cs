using ProjectS.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectS.Core
{
    /// <summary>
    /// 타이틀 씬 진입점. 메뉴 동작만 결정한다: 게임시작 → 스테이지 씬(편성부터 코어 루프),
    /// 게임종료 → 종료. 불러오기·옵션은 저장/설정 시스템이 없어 안내 메시지만(PROTOTYPE).
    /// </summary>
    public sealed class TitleFlowController : MonoBehaviour
    {
        [SerializeField] private TitleConfig config;

        private TitleScreen screen;

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("[Title] TitleConfig 참조가 비어 있습니다. 메뉴 Project S > Setup Stage1 Prototype 을 실행하세요.", this);
                return;
            }

            UiFactory.EnsureEventSystem();
            var menu = new[]
            {
                new TitleScreen.MenuEntry { Sprite = config.MenuStart, FallbackLabel = "게임시작", OnSelect = StartGame },
                new TitleScreen.MenuEntry { Sprite = config.MenuLoad, FallbackLabel = "불러오기", OnSelect = NotAvailable },
                new TitleScreen.MenuEntry { Sprite = config.MenuOption, FallbackLabel = "옵션", OnSelect = NotAvailable },
                new TitleScreen.MenuEntry { Sprite = config.MenuExit, FallbackLabel = "게임종료", OnSelect = QuitGame },
            };
            screen = TitleScreen.Create(config, menu, $"version_{Application.version}");
        }

        private void StartGame()
        {
            screen.Lock();
            SceneManager.LoadScene(SceneNames.Stage1);
        }

        // TODO: 저장/불러오기, 옵션(음량·해상도 등) — 기획 미정
        private void NotAvailable() => screen.ShowMessage("아직 준비 중인 기능입니다 (프로토타입)");

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
