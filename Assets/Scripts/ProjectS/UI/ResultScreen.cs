using System;
using ProjectS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 정산(결과) 화면: 승리 / 패배 / 나가기를 구분해 표시하고, 다시 시작·종료만 제공한다.
    /// PROTOTYPE: 보상·진행도 저장·스테이지 선택 복귀는 미정이라 구현하지 않음.
    /// </summary>
    public sealed class ResultScreen : MonoBehaviour
    {
        public static ResultScreen Create(StageResult result, PresentationConfig config, Action onRestart, Action onQuit)
        {
            var canvas = UiFactory.CreateCanvas("ResultScreen", 20);
            var screen = canvas.gameObject.AddComponent<ResultScreen>();
            var t = canvas.transform;

            UiFactory.CreateFullscreenPanel(t, "Dim", new Color(0f, 0f, 0f, 0.78f));

            if (result.Outcome == StageOutcome.Victory && config.VictoryBanner != null)
            {
                var banner = UiFactory.CreateRect(t, "VictoryBanner", new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(637f, 313f));
                var image = banner.gameObject.AddComponent<Image>();
                image.sprite = config.VictoryBanner;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else
            {
                var title = UiFactory.CreateText(t, "Title", new Vector2(0.5f, 0.5f), new Vector2(0f, 170f),
                    new Vector2(1200f, 160f), 96, TextAnchor.MiddleCenter);
                title.text = Title(result.Outcome);
                title.color = result.Outcome == StageOutcome.Defeat ? new Color(1f, 0.45f, 0.4f) : new Color(0.85f, 0.85f, 0.9f);
            }

            var detail = UiFactory.CreateText(t, "Detail", new Vector2(0.5f, 0.5f), new Vector2(0f, -30f),
                new Vector2(1200f, 100f), 30, TextAnchor.MiddleCenter);
            detail.text = $"{result.Reason}\n진행 턴: {result.TurnNumber}";

            UiFactory.CreateButton(t, "다시 시작", new Vector2(0.5f, 0.5f), new Vector2(-170f, -180f), new Vector2(300f, 80f), onRestart);
            UiFactory.CreateButton(t, "종료", new Vector2(0.5f, 0.5f), new Vector2(170f, -180f), new Vector2(300f, 80f), onQuit);
            return screen;
        }

        private static string Title(StageOutcome outcome)
        {
            switch (outcome)
            {
                case StageOutcome.Victory: return "승리";
                case StageOutcome.Defeat: return "패배";
                case StageOutcome.Exit: return "스테이지 나감";
                default: return "-";
            }
        }
    }
}
