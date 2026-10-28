using System;
using System.Linq;
using ProjectS.Combat;
using ProjectS.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 전투 정보 표시와 메뉴 입력. 표시와 요청 전달만 한다.
    /// 오른쪽 열: 턴·SP·실행 중 행동·대기열 목록 / 일시 정지(P) / 나가기(2번 클릭 확인) / 턴 종료.
    /// 왼쪽 열 아래: 선택 유닛 정보.
    /// 일시 정지·나가기는 전투 행동 상태(이동 중, 적 턴 등)와 무관하게 언제나 가능하다.
    /// </summary>
    public sealed class BattlePanel : MonoBehaviour
    {
        private const int MaxQueueLines = 8;
        private const float ExitConfirmSeconds = 3f;

        private BattleController battle;
        private SelectionController selection;
        private PrototypeHud hud;
        private Action onExit;
        private Text status;
        private Text unitInfo;
        private Button endTurnButton;
        private Button pauseButton;
        private Button exitButton;
        private float exitConfirmUntil = -1f;

        public static BattlePanel Create(BattleController battle, SelectionController selection, PrototypeHud hud,
            PresentationConfig config, Action onExit)
        {
            var root = UiFactory.CreateStretchedRoot(hud.Canvas.transform, "BattlePanel");
            var panel = root.gameObject.AddComponent<BattlePanel>();
            panel.battle = battle;
            panel.selection = selection;
            panel.hud = hud;
            panel.onExit = onExit;
            panel.Build(config);
            return panel;
        }

        private void Build(PresentationConfig config)
        {
            var t = transform;
            float pad = UiLayout.Padding;
            float w = UiLayout.ColumnContentWidth;

            var box = UiFactory.CreatePanel(t, "StatusBox", new Vector2(1f, 1f), new Vector2(-pad, -pad),
                new Vector2(w, 470f), PrototypeVisuals.PanelBackground);
            status = UiFactory.CreateText(box.transform, "Status", new Vector2(0f, 1f), new Vector2(14f, -12f),
                new Vector2(w - 28f, 450f), 22, TextAnchor.UpperLeft);

            float halfW = (w - 10f) * 0.5f;
            pauseButton = UiFactory.CreateButton(t, "일시 정지 (P)", new Vector2(1f, 1f), new Vector2(-pad - halfW - 10f, -pad - 480f),
                new Vector2(halfW, 56f), TogglePause);
            exitButton = UiFactory.CreateButton(t, "나가기", new Vector2(1f, 1f), new Vector2(-pad, -pad - 480f),
                new Vector2(halfW, 56f), OnExitClicked);

            unitInfo = UiFactory.CreateText(t, "UnitInfo", new Vector2(0f, 0f), new Vector2(pad, 110f),
                new Vector2(w, 200f), 21, TextAnchor.LowerLeft);

            endTurnButton = UiFactory.CreateButton(t, "턴 종료", new Vector2(1f, 0f), new Vector2(-pad, pad),
                new Vector2(w, 110f), OnEndTurnClicked);
            if (config.TurnEndButton != null)
            {
                // 아트 버튼: 이미지에 글자가 포함되어 있으므로 라벨을 숨기고 원래 색으로 표시
                var image = (Image)endTurnButton.targetGraphic;
                image.sprite = config.TurnEndButton;
                image.preserveAspect = true;
                endTurnButton.GetComponentInChildren<Text>().enabled = false;
                var colors = endTurnButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.85f, 0.95f, 1f);
                colors.pressedColor = new Color(0.7f, 0.7f, 0.75f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.6f);
                endTurnButton.colors = colors;
            }
        }

        private void OnEndTurnClicked()
        {
            if (!battle.Commands.RequestEndTurn(out var reason))
                hud.ShowMessage(reason);
            else if (!battle.Scheduler.IsIdle)
                hud.ShowMessage("턴 종료 예약됨 - 앞선 행동이 끝나면 적 턴으로 넘어갑니다");
        }

        private void TogglePause()
        {
            battle.IsPaused = !battle.IsPaused;
            UiFactory.SetButtonLabel(pauseButton, battle.IsPaused ? "재개 (P)" : "일시 정지 (P)");
            hud.ShowMessage(battle.IsPaused ? "일시 정지 - 행동 진행이 멈췄습니다" : "재개");
        }

        private void OnExitClicked()
        {
            if (Time.unscaledTime <= exitConfirmUntil)
            {
                onExit();
                return;
            }

            exitConfirmUntil = Time.unscaledTime + ExitConfirmSeconds;
            hud.ShowMessage("한 번 더 누르면 스테이지를 나갑니다 (진행 중 행동과 대기열은 버려집니다)");
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.pKey.wasPressedThisFrame) TogglePause();

            UiFactory.SetButtonLabel(exitButton, Time.unscaledTime <= exitConfirmUntil ? "정말 나가기?" : "나가기");

            var scheduler = battle.Scheduler;
            var turns = battle.Turns;
            var stage = battle.Context.Battlefield.Stage;
            string turnLimit = stage.TurnLimit > 0 ? $" / {stage.TurnLimit}" : "";
            string team = turns.ActiveTeam == Team.Player ? "플레이어 턴" : turns.ActiveTeam == Team.Enemy ? "적 턴" : "-";
            string current = scheduler.Current != null ? scheduler.Current.ToString() : "없음";

            var queue = scheduler.QueueLabels;
            string queueText = queue.Count == 0 ? "  (비어 있음)"
                : string.Join("\n", queue.Take(MaxQueueLines).Select((label, i) => $"  {i + 1}. {label}"))
                  + (queue.Count > MaxQueueLines ? $"\n  … 외 {queue.Count - MaxQueueLines}개" : "");

            status.text = $"<b>턴 {turns.TurnNumber}{turnLimit}  {team}</b>" + (battle.IsPaused ? "  <color=#ffcc44>[일시 정지]</color>" : "") + "\n" +
                          $"SP {battle.Resources.Sp} / {battle.Resources.MaxSp}\n\n" +
                          $"실행 중: {current}\n" +
                          $"대기열 {queue.Count}:\n{queueText}";

            // 카드 대상 지정 중에는 턴 종료 비활성(§1.2)
            endTurnButton.interactable = !selection.IsTargeting
                && scheduler.GetAdmission(Team.Player, out _) != Admission.Reject;

            var u = selection.Selected;
            if (u == null)
            {
                unitInfo.text = "";
                return;
            }

            string moveState = u.Team == Team.Player
                ? (battle.Commands.CanMoveThisTurn(u, out _) ? "이동 가능" : "이번 턴 이동 완료")
                : "";
            unitInfo.text = $"<b>선택: {u.DisplayName}</b> ({u.Team})\n위치 {u.Coord}  HP {u.Hp}/{u.MaxHp}\n" +
                            $"공 {u.Attack}  방 {u.Defense}  이동 {u.MoveRange}  사거리 {u.AttackRange}\n{moveState}";
        }
    }
}
