using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.EditorTools;
using ProjectS.Grid;
using ProjectS.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectS.Tests
{
    /// <summary>
    /// 전투·배치 UI 가 가운데 전장 영역을 가리지 않는지 검사한다(1920x1080 기준 캔버스).
    /// 모든 UI 요소는 좌/우 열, 하단 손패 띠, 상단 메시지 띠 중 하나 안에 있어야 한다.
    /// </summary>
    public class UiLayoutTests
    {
        private HashSet<GameObject> rootsBefore;
        private Battlefield battlefield;
        private BattlefieldView view;
        private PrototypeHud hud;
        private List<Unit> allies;

        [SetUp]
        public void SetUp()
        {
            rootsBefore = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());

            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(PrototypeSetup.Stage1Path);
            var roster = AssetDatabase.LoadAssetAtPath<PrototypeRoster>(PrototypeSetup.RosterPath);
            battlefield = Battlefield.Create(stage);
            allies = roster.Candidates.Where(c => PrototypeRoster.IsUnlocked(c, stage))
                .Select(def => battlefield.Units.Create(def, Team.Player)).ToList();

            view = BattlefieldView.Create(battlefield, 1f);
            hud = PrototypeHud.Create(battlefield, view);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (!rootsBefore.Contains(go)) Object.DestroyImmediate(go);
        }

        [Test]
        public void BattleUi_StaysOutOfBoardArea()
        {
            for (int i = 0; i < allies.Count; i++)
                battlefield.Grid.Place(allies[i], GridCoord.FromDesign(i + 1, 18));

            var party = PartySelection.WithCharacterCards(allies.Select(a => a.Definition));
            var session = new BattleSession(battlefield,
                AssetDatabase.LoadAssetAtPath<MoveDefinition>(PrototypeSetup.MovePath),
                AssetDatabase.LoadAssetAtPath<BattleRules>(PrototypeSetup.RulesPath), party.Deck);
            session.Begin();
            Assert.AreEqual(3, session.Deck.Hand.Count);

            var battle = BattleController.Create(session);
            var selection = SelectionController.Create(battle, view, hud);
            BattlePanel.Create(battle, selection, hud);
            HandPanel.Create(battle, selection, hud);

            AssertNothingOverBoard();
        }

        [Test]
        public void DeploymentUi_StaysOutOfBoardArea()
        {
            var deployment = new DeploymentController(battlefield, allies);
            DeploymentScreen.Create(deployment, battlefield, view, hud, () => { });

            AssertNothingOverBoard();
        }

        private void AssertNothingOverBoard()
        {
            // 화면 해상도와 무관하게 기준 해상도 크기로 배치를 계산
            var canvas = hud.Canvas;
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRt = (RectTransform)canvas.transform;
            canvasRt.localScale = Vector3.one;
            canvasRt.sizeDelta = UiLayout.ReferenceResolution;

            float w = UiLayout.ReferenceResolution.x;
            float h = UiLayout.ReferenceResolution.y;
            const float tolerance = 0.5f;

            var graphics = canvas.GetComponentsInChildren<Graphic>(true)
                .Where(g => g.name != "LeftColumn" && g.name != "RightColumn")
                .ToList();
            Assert.Greater(graphics.Count, 5);

            var offenders = new List<string>();
            var corners = new Vector3[4];
            foreach (var g in graphics)
            {
                g.rectTransform.GetWorldCorners(corners);
                var min = canvasRt.InverseTransformPoint(corners[0]);
                var max = canvasRt.InverseTransformPoint(corners[2]);
                float xMin = min.x - canvasRt.rect.xMin, xMax = max.x - canvasRt.rect.xMin;
                float yMin = min.y - canvasRt.rect.yMin, yMax = max.y - canvasRt.rect.yMin;

                bool inLeft = xMax <= UiLayout.SideColumnWidth + tolerance;
                bool inRight = xMin >= w - UiLayout.SideColumnWidth - tolerance;
                bool inBottom = yMax <= UiLayout.BottomBarHeight + tolerance;
                bool inTop = yMin >= h - UiLayout.TopBarHeight - tolerance;
                bool onScreen = xMin >= -tolerance && xMax <= w + tolerance && yMin >= -tolerance && yMax <= h + tolerance;

                if (!(inLeft || inRight || inBottom || inTop) || !onScreen)
                    offenders.Add($"{Path(g.transform)} x[{xMin:0}..{xMax:0}] y[{yMin:0}..{yMax:0}]");
            }

            Assert.IsEmpty(offenders, "전장 영역을 가리거나 화면 밖에 있는 UI:\n" + string.Join("\n", offenders));
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
