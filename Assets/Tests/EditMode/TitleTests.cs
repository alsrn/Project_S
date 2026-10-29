using System.Linq;
using NUnit.Framework;
using ProjectS.Core;
using ProjectS.EditorTools;
using ProjectS.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectS.Tests
{
    /// <summary>타이틀 → 코어 루프 진입 구성 검사.</summary>
    public class TitleTests
    {
        [Test]
        public void BuildSettings_TitleFirst_ThenStage1()
        {
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            Assert.GreaterOrEqual(enabled.Count, 2);
            Assert.AreEqual(PrototypeSetup.TitleScenePath, enabled[0], "게임 시작 시 타이틀이 먼저");
            Assert.AreEqual(PrototypeSetup.ScenePath, enabled[1]);

            // SceneNames 상수가 실제 씬 파일 이름과 일치
            Assert.AreEqual(SceneNames.Title, System.IO.Path.GetFileNameWithoutExtension(PrototypeSetup.TitleScenePath));
            Assert.AreEqual(SceneNames.Stage1, System.IO.Path.GetFileNameWithoutExtension(PrototypeSetup.ScenePath));
        }

        [Test]
        public void TitleConfig_HasAllArt()
        {
            var config = AssetDatabase.LoadAssetAtPath<TitleConfig>(PrototypeSetup.TitleConfigPath);
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.Background);
            Assert.IsNotNull(config.Spaceship);
            Assert.IsNotNull(config.Logo);
            Assert.IsNotNull(config.MenuStart);
            Assert.IsNotNull(config.MenuLoad);
            Assert.IsNotNull(config.MenuOption);
            Assert.IsNotNull(config.MenuExit);
        }

        [Test]
        public void TitleScreen_BuildsMenuAtPsdPositions()
        {
            var rootsBefore = SceneManager.GetActiveScene().GetRootGameObjects().ToList();
            var config = AssetDatabase.LoadAssetAtPath<TitleConfig>(PrototypeSetup.TitleConfigPath);
            int started = 0;
            var menu = new[]
            {
                new TitleScreen.MenuEntry { Sprite = config.MenuStart, FallbackLabel = "게임시작", OnSelect = () => started++ },
                new TitleScreen.MenuEntry { Sprite = config.MenuLoad, FallbackLabel = "불러오기" },
                new TitleScreen.MenuEntry { Sprite = config.MenuOption, FallbackLabel = "옵션" },
                new TitleScreen.MenuEntry { Sprite = config.MenuExit, FallbackLabel = "게임종료" },
            };

            try
            {
                var screen = TitleScreen.Create(config, menu, "version_test");
                var items = screen.GetComponentsInChildren<TitleMenuItem>();
                Assert.AreEqual(4, items.Length);

                // 첫 항목(게임시작) 선택 상태: 글자 검정, 나머지 흰색
                var texts = screen.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Text_")).ToList();
                Assert.AreEqual(Color.black, texts[0].color);
                Assert.AreEqual(Color.white, texts[1].color);

                // 호버 → 선택 이동, 클릭 → 해당 항목 동작
                items[3].Hovered();
                Assert.AreEqual(Color.black, texts[3].color);
                Assert.AreEqual(Color.white, texts[0].color);
                items[0].Clicked();
                Assert.AreEqual(1, started);

                // PSD 좌표: 로고 (191,206), 게임시작 글자 (222,536)
                var logo = (RectTransform)screen.transform.Find("Frame/Logo");
                Assert.AreEqual(new Vector2(191, -206), logo.anchoredPosition);
                Assert.AreEqual(new Vector2(222, -536), texts[0].rectTransform.anchoredPosition);
            }
            finally
            {
                foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (!rootsBefore.Contains(go)) Object.DestroyImmediate(go);
            }
        }
    }
}
