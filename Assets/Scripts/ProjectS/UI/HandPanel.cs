using System.Collections.Generic;
using ProjectS.Combat;
using ProjectS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 손패 표시(화면 하단)와 카드 클릭 → SelectionController.BeginCard 전달. 덱 더미 수도 표시한다.
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
        private Text pileInfo;
        private readonly Dictionary<CardInstance, Button> buttons = new Dictionary<CardInstance, Button>();
        private CardInstance lastTargeting;

        public static HandPanel Create(BattleController battle, SelectionController selection, PrototypeHud hud)
        {
            var root = UiFactory.CreateStretchedRoot(hud.Canvas.transform, "HandPanel");
            var panel = root.gameObject.AddComponent<HandPanel>();
            panel.battle = battle;
            panel.selection = selection;

            float pad = UiLayout.Padding;
            panel.cardRoot = UiFactory.CreateRect(root.transform, "Cards", new Vector2(0.5f, 0f), new Vector2(0f, pad), new Vector2(10f, CardHeight));
            panel.pileInfo = UiFactory.CreateText(root.transform, "PileInfo", new Vector2(0.5f, 0f), new Vector2(0f, pad + CardHeight + 10f),
                new Vector2(1920f - UiLayout.SideColumnWidth * 2f, 34f), 20, TextAnchor.MiddleCenter);

            battle.Deck.Changed += panel.Rebuild;
            panel.Rebuild();
            return panel;
        }

        private void Rebuild()
        {
            foreach (var b in buttons.Values) Destroy(b.gameObject);
            buttons.Clear();

            var hand = battle.Deck.Hand;
            float total = hand.Count * CardWidth + Mathf.Max(0, hand.Count - 1) * Spacing;
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                float x = -total * 0.5f + i * (CardWidth + Spacing) + CardWidth * 0.5f;
                var button = UiFactory.CreateButton(cardRoot, card.Definition.DisplayName, new Vector2(0.5f, 0f),
                    new Vector2(x - CardWidth * 0.5f, 0f), new Vector2(CardWidth, CardHeight), () => selection.BeginCard(card));
                ((RectTransform)button.transform).pivot = new Vector2(0f, 0f);
                button.GetComponentInChildren<Text>().fontSize = 22;
                UiFactory.SetButtonLabel(button,
                    $"{card.Definition.DisplayName}  [SP {card.Definition.BaseCost}]\n{card.Owner?.DisplayName ?? "공용"}");
                buttons[card] = button;
            }

            pileInfo.text = $"뽑을 카드 {battle.Deck.DrawPileCount}   손패 {hand.Count}   버린 카드 {battle.Deck.DiscardPileCount}" +
                            "   (PROTOTYPE: STS 방식 드로우)";
            lastTargeting = null;
            RefreshColors();
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
