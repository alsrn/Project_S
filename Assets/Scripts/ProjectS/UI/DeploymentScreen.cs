using System;
using System.Collections.Generic;
using System.Linq;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 배치 화면 입력·표시: 아군 목록에서 유닛 선택 → 타일 클릭으로 배치/재배치, 우클릭으로 배치 해제.
    /// 판정은 DeploymentController 가 하고 실패 사유를 HUD 메시지로 보여준다.
    /// Start 버튼은 전원 배치 시에만 활성화되며, 실제 전투 진입 판정은 StageFlowController 가 다시 확인한다.
    /// </summary>
    public sealed class DeploymentScreen : MonoBehaviour
    {
        private DeploymentController deployment;
        private Battlefield battlefield;
        private BattlefieldView view;
        private PrototypeHud hud;
        private Action onStart;

        private readonly Dictionary<Unit, Button> unitButtons = new Dictionary<Unit, Button>();
        private Button startButton;
        private Unit selected;

        public static DeploymentScreen Create(DeploymentController deployment, Battlefield battlefield,
            BattlefieldView view, PrototypeHud hud, Action onStart)
        {
            var root = UiFactory.CreateStretchedRoot(hud.Canvas.transform, "DeploymentScreen");
            var screen = root.gameObject.AddComponent<DeploymentScreen>();
            screen.deployment = deployment;
            screen.battlefield = battlefield;
            screen.view = view;
            screen.hud = hud;
            screen.onStart = onStart;
            screen.Build();
            deployment.Changed += screen.Refresh;
            screen.Select(deployment.Allies.FirstOrDefault());
            return screen;
        }

        private void Build()
        {
            var panelAnchor = new Vector2(1f, 1f);
            const float width = UiLayout.ColumnContentWidth;
            float height = 120f + deployment.Allies.Count * 62f;
            float pad = UiLayout.Padding;
            var panel = UiFactory.CreatePanel(transform, "AllyPanel", panelAnchor, new Vector2(-pad, -pad),
                new Vector2(width, height), PrototypeVisuals.PanelBackground);

            var title = UiFactory.CreateText(panel.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -10f),
                new Vector2(width - 20f, 90f), 22, TextAnchor.UpperLeft);
            title.text = "아군 배치\n유닛 선택 → 파란 구역 클릭\n우클릭: 배치 해제";

            for (int i = 0; i < deployment.Allies.Count; i++)
            {
                var unit = deployment.Allies[i];
                unitButtons[unit] = UiFactory.CreateButton(panel.transform, unit.DisplayName, new Vector2(0.5f, 1f),
                    new Vector2(0f, -110f - i * 62f), new Vector2(width - 30f, 54f), () => Select(unit));
            }

            startButton = UiFactory.CreateButton(transform, "전투 시작", new Vector2(1f, 0f), new Vector2(-pad, pad),
                new Vector2(width, 70f), () => onStart());
        }

        private void Update()
        {
            if (BoardPointer.WasLeftClickOnBoard(view, out var coord))
                HandleLeftClick(coord);
            else if (BoardPointer.WasRightClickOnBoard(view, out coord))
                HandleRightClick(coord);
        }

        private void HandleLeftClick(GridCoord coord)
        {
            var occupant = battlefield.Grid.GetOccupant(coord);
            if (occupant != null && deployment.Allies.Contains(occupant))
            {
                Select(occupant);
                return;
            }

            if (selected == null)
            {
                hud.ShowMessage("배치할 아군을 먼저 선택하세요");
                return;
            }

            if (!deployment.TryDeploy(selected, coord, out var reason))
            {
                hud.ShowMessage(reason);
                return;
            }

            // 편의: 다음 미배치 아군을 자동 선택
            Select(deployment.Allies.FirstOrDefault(u => !u.IsOnGrid) ?? selected);
        }

        private void HandleRightClick(GridCoord coord)
        {
            var occupant = battlefield.Grid.GetOccupant(coord);
            if (occupant == null || !deployment.Allies.Contains(occupant)) return;

            deployment.Undeploy(occupant);
            Select(occupant);
        }

        private void Select(Unit unit)
        {
            selected = unit;
            Refresh();
        }

        private void Refresh()
        {
            foreach (var pair in unitButtons)
            {
                var unit = pair.Key;
                string where = unit.IsOnGrid ? unit.Coord.ToString() : "미배치";
                UiFactory.SetButtonLabel(pair.Value, $"{unit.DisplayName}   {where}");
                UiFactory.SetButtonColor(pair.Value, unit == selected ? PrototypeVisuals.ButtonSelected : PrototypeVisuals.ButtonNormal);
            }

            startButton.interactable = deployment.IsComplete;

            view.ClearHighlights();
            if (selected != null)
            {
                view.AddHighlights(deployment.GetAvailableTiles(), PrototypeVisuals.HighlightAvailable);
                if (selected.IsOnGrid)
                    view.AddHighlights(new[] { selected.Coord }, PrototypeVisuals.HighlightSelected);
            }
        }

        private void OnDestroy()
        {
            if (deployment != null) deployment.Changed -= Refresh;
            if (view != null) view.ClearHighlights();
        }
    }
}
