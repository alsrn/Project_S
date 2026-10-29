using System.Collections.Generic;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Targeting;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 손패 표시(화면 하단)와 카드 클릭 → SelectionController.BeginCard 전달.
    /// 카드 위에 마우스를 올리면 설명(기획 문구 + 효과 요약)을, 아니면 덱 더미 수를 표시한다.
    /// 카드 선택(UI 조작)은 행동 실행 중에도 가능하다. 실제 사용 가능 여부는 요청 시 판정된다.
    /// </summary>
    public sealed class HandPanel : MonoBehaviour
    {
        private const float CardWidth = 210f;
        private const float CardHeight = 110f;
        private const float Spacing = 12f;

        private BattleController battle;
        private SelectionController selection;
        private RectTransform cardRoot;
        private Text info;
        private readonly Dictionary<CardInstance, Button> buttons = new Dictionary<CardInstance, Button>();
        private CardInstance lastTargeting;
        private CardInstance hovered;

        public static HandPanel Create(BattleController battle, SelectionController selection, PrototypeHud hud)
        {
            var root = UiFactory.CreateStretchedRoot(hud.Canvas.transform, "HandPanel");
            var panel = root.gameObject.AddComponent<HandPanel>();
            panel.battle = battle;
            panel.selection = selection;

            float pad = UiLayout.Padding;
            panel.cardRoot = UiFactory.CreateRect(root.transform, "Cards", new Vector2(0.5f, 0f), new Vector2(0f, pad), new Vector2(10f, CardHeight));
            panel.info = UiFactory.CreateText(root.transform, "CardInfo", new Vector2(0.5f, 0f), new Vector2(0f, pad + CardHeight + 6f),
                new Vector2(1920f - UiLayout.SideColumnWidth * 2f - 20f, 60f), 18, TextAnchor.LowerCenter);

            battle.Deck.Changed += panel.Rebuild;
            panel.Rebuild();
            return panel;
        }

        private void Rebuild()
        {
            foreach (var b in buttons.Values) Destroy(b.gameObject);
            buttons.Clear();
            hovered = null;

            var hand = battle.Deck.Hand;
            float total = hand.Count * CardWidth + Mathf.Max(0, hand.Count - 1) * Spacing;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                float x = -total * 0.5f + i * (CardWidth + Spacing) + CardWidth * 0.5f;
                var button = UiFactory.CreateButton(cardRoot, card.Definition.DisplayName, new Vector2(0.5f, 0f),
                    new Vector2(x - CardWidth * 0.5f, 0f), new Vector2(CardWidth, CardHeight), () => selection.BeginCard(card));
                ((RectTransform)button.transform).pivot = new Vector2(0f, 0f);
                button.GetComponentInChildren<Text>().fontSize = 20;
                UiFactory.SetButtonLabel(button,
                    $"{card.Definition.DisplayName}  [SP {card.Definition.BaseCost}]\n{card.Owner?.Definition.DisplayName ?? "공용"} · {TargetLabel(card.Definition)}");

                var relay = button.gameObject.AddComponent<PointerRelay>();
                relay.Entered = () => SetHovered(card);
                relay.Exited = () => { if (hovered == card) SetHovered(null); };
                buttons[card] = button;
            }

            lastTargeting = null;
            RefreshColors();
            RefreshInfo();
        }

        private static string TargetLabel(CardDefinition card)
        {
            if (card.TargetRequirements.Count == 0) return "자신";
            var req = card.TargetRequirements[0];
            string who = req.relation == TargetRelation.Ally ? "아군 1" : req.relation == TargetRelation.Enemy ? "적 1" : "유닛 1";
            return req.rangeSource == RangeSource.Unlimited ? $"{who}(사거리 무제한)" : who;
        }

        private void SetHovered(CardInstance card)
        {
            hovered = card;
            RefreshInfo();
        }

        private void RefreshInfo()
        {
            if (hovered != null)
            {
                var def = hovered.Definition;
                info.text = $"<b>{def.DisplayName}</b> [SP {def.BaseCost}] {def.Description}\n<color=#9fd0ff>{def.DescribeEffects()}</color>";
                return;
            }

            info.text = $"뽑을 카드 {battle.Deck.DrawPileCount}   손패 {battle.Deck.Hand.Count}   버린 카드 {battle.Deck.DiscardPileCount}" +
                        $"   (턴마다 {battle.Session.Rules.DrawPerTurn}장 드로우, 턴 종료 시 손패 버림)";
        }

        private void Update()
        {
            if (selection.TargetingCard != lastTargeting) RefreshColors();
        }

        private void RefreshColors()
        {
            lastTargeting = selection.TargetingCard;
            foreach (var pair in buttons)
            {
                bool usable = pair.Key.Owner == null || pair.Key.Owner.IsAlive;
                pair.Value.interactable = usable;
                UiFactory.SetButtonColor(pair.Value, pair.Key == lastTargeting ? PrototypeVisuals.ButtonSelected : PrototypeVisuals.ButtonNormal);
            }
        }

        private void OnDestroy()
        {
            if (battle != null) battle.Deck.Changed -= Rebuild;
        }
    }
}
