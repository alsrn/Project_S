using System;
using System.Collections.Generic;
using System.Linq;
using ProjectS.Combat;
using ProjectS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// PROTOTYPE 편성 화면(최소 UI): 후보 캐릭터를 골라 PartySelection 을 만들어 넘긴다.
    /// 편성 규칙 판정은 PrototypeRoster 가 하고, 이 화면은 입력과 표시만 한다.
    /// TODO(단계 D): 덱 구성 슬롯.
    /// </summary>
    public sealed class FormationScreen : MonoBehaviour
    {
        private PrototypeRoster roster;
        private StageDefinition stage;
        private Action<PartySelection> onConfirm;
        private readonly List<UnitDefinition> selected = new List<UnitDefinition>();
        private readonly Dictionary<UnitDefinition, Button> buttons = new Dictionary<UnitDefinition, Button>();
        private Text status;

        public static FormationScreen Create(PrototypeRoster roster, StageDefinition stage, Action<PartySelection> onConfirm)
        {
            var canvas = UiFactory.CreateCanvas("FormationScreen", 10);
            var screen = canvas.gameObject.AddComponent<FormationScreen>();
            screen.roster = roster;
            screen.stage = stage;
            screen.onConfirm = onConfirm;
            screen.Build(canvas.transform);
            return screen;
        }

        private void Build(Transform root)
        {
            UiFactory.CreateFullscreenPanel(root, "Backdrop", PrototypeVisuals.PanelBackground);

            var title = UiFactory.CreateText(root, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -60f),
                new Vector2(1200f, 60f), 40, TextAnchor.MiddleCenter);
            title.text = "편성 (PROTOTYPE)";

            var hint = UiFactory.CreateText(root, "Hint", new Vector2(0.5f, 1f), new Vector2(0f, -120f),
                new Vector2(1200f, 40f), 24, TextAnchor.MiddleCenter);
            hint.text = $"{stage.DisplayName} - 출격할 캐릭터를 선택하세요 (1~{stage.PartySize}명).  " +
                        "PROTOTYPE 덱: 출격 캐릭터의 평타 카드 각 1장 (공용 카드 선택 미구현)";

            const float buttonHeight = 60f;
            const float spacing = 10f;
            float startY = -180f;
            for (int i = 0; i < roster.Candidates.Count; i++)
            {
                var def = roster.Candidates[i];
                if (def == null) continue;

                var button = UiFactory.CreateButton(root, def.DisplayName, new Vector2(0.5f, 1f),
                    new Vector2(0f, startY - i * (buttonHeight + spacing)), new Vector2(760f, buttonHeight), () => Toggle(def));
                button.interactable = PrototypeRoster.IsUnlocked(def, stage);
                buttons[def] = button;
            }

            status = UiFactory.CreateText(root, "Status", new Vector2(0.5f, 0f), new Vector2(0f, 150f),
                new Vector2(1200f, 40f), 26, TextAnchor.MiddleCenter);

            UiFactory.CreateButton(root, "출격", new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(300f, 70f), Confirm);

            Refresh(null);
        }

        private void Toggle(UnitDefinition def)
        {
            if (selected.Remove(def))
            {
                Refresh(null);
                return;
            }

            if (selected.Count >= stage.PartySize)
            {
                Refresh($"최대 {stage.PartySize}명까지 편성할 수 있습니다");
                return;
            }

            selected.Add(def);
            Refresh(null);
        }

        private void Confirm()
        {
            if (!roster.ValidateParty(selected, stage, out var reason))
            {
                Refresh(reason);
                return;
            }

            var party = PartySelection.WithCharacterCards(selected);
            Destroy(gameObject);
            onConfirm(party);
        }

        private void Refresh(string error)
        {
            foreach (var pair in buttons)
            {
                var def = pair.Key;
                int order = selected.IndexOf(def);
                bool isSelected = order >= 0;
                string stats = $"HP {def.MaxHp}  공 {def.Attack}  방 {def.Defense}  이동 {def.MoveRange}  사거리 {def.AttackRange}";
                string label = !PrototypeRoster.IsUnlocked(def, stage)
                    ? $"{def.DisplayName}   ({def.UnlockStage}스테이지 해금)"
                    : $"{(isSelected ? $"{order + 1}. " : "")}{def.DisplayName}   {stats}";
                UiFactory.SetButtonColor(pair.Value, isSelected ? PrototypeVisuals.ButtonSelected : PrototypeVisuals.ButtonNormal);
                UiFactory.SetButtonLabel(pair.Value, label);
            }

            status.color = error != null ? new Color(1f, 0.5f, 0.4f) : Color.white;
            string deck = string.Join(", ", selected.SelectMany(d => d.Cards.Where(c => c != null).Select(c => $"{c.DisplayName}(SP {c.BaseCost})")));
            status.text = error ?? $"선택 {selected.Count} / {stage.PartySize}     덱: {(deck.Length > 0 ? deck : "없음")}";
        }
    }
}
