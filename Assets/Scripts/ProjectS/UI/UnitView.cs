using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// Unit 1기의 화면 표시(사각형 + HP 바). Unit.StateChanged 를 구독해 갱신만 하며 상태를 바꾸지 않는다.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        private const float BodyScale = 0.7f;
        private const float HpBarHeight = 0.1f;
        private const float SlideTilesPerSecond = 10f;

        private Unit unit;
        private const float FlashSeconds = 0.15f;

        private Vector3 targetPosition;
        private bool slideEnabled;
        private int lastHp = int.MaxValue;
        private float flashTimer;
        private BattlefieldView battlefield;
        private SpriteRenderer body;
        private Transform hpFill;

        public Unit Unit => unit;

        /// <summary>전투 단계에서 켠다. 배치 단계의 재배치는 즉시 이동으로 보인다.</summary>
        public void EnableSlide() => slideEnabled = true;

        public static UnitView Create(Unit unit, BattlefieldView battlefield)
        {
            var go = new GameObject($"Unit_{unit.DisplayName}");
            go.transform.SetParent(battlefield.transform, false);

            var view = go.AddComponent<UnitView>();
            view.unit = unit;
            view.battlefield = battlefield;
            view.Build();
            unit.StateChanged += view.OnUnitChanged;
            view.OnUnitChanged(unit);
            return view;
        }

        private void Build()
        {
            float cell = battlefield.CellSize;
            body = PrototypeVisuals.CreateSquare("Body", transform, unit.Definition.Color, PrototypeVisuals.UnitOrder);
            body.transform.localScale = Vector3.one * cell * BodyScale;

            float barY = cell * (BodyScale * 0.5f + 0.08f);
            var back = PrototypeVisuals.CreateSquare("HpBack", transform, PrototypeVisuals.HpBarBack, PrototypeVisuals.HpBarOrder);
            back.transform.localPosition = new Vector3(0f, barY, 0f);
            back.transform.localScale = new Vector3(cell * BodyScale, cell * HpBarHeight, 1f);

            // 왼쪽 끝 고정으로 줄어들도록 피벗 역할의 부모를 둔다.
            var fillRoot = new GameObject("HpFillRoot").transform;
            fillRoot.SetParent(transform, false);
            fillRoot.localPosition = new Vector3(-cell * BodyScale * 0.5f, barY, 0f);
            var fill = PrototypeVisuals.CreateSquare("HpFill", fillRoot, PrototypeVisuals.HpBarFill, PrototypeVisuals.HpBarOrder + 1);
            fill.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            fillRoot.localScale = new Vector3(cell * BodyScale, cell * HpBarHeight, 1f);
            hpFill = fillRoot;
        }

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                body.color = flashTimer > 0f ? Color.white : unit.Definition.Color;
            }

            // 모델은 한 칸씩 이동하고, 화면은 그 칸을 향해 미끄러지듯 따라간다(순간 이동처럼 보이지 않게).
            transform.position = Vector3.MoveTowards(transform.position, targetPosition,
                battlefield.CellSize * SlideTilesPerSecond * Time.deltaTime);
        }

        private void OnUnitChanged(Unit changed)
        {
            bool wasVisible = gameObject.activeSelf;
            gameObject.SetActive(changed.IsOnGrid && changed.IsAlive);
            if (changed.IsOnGrid)
            {
                targetPosition = battlefield.CoordToWorld(changed.Coord);
                // 처음 등장·배치(재배치 포함)나 한 칸 넘게 떨어진 경우는 즉시 이동
                bool adjacentStep = Vector3.Distance(transform.position, targetPosition) <= battlefield.CellSize * 1.01f;
                if (!wasVisible || !adjacentStep || !slideEnabled)
                    transform.position = targetPosition;
            }

            // 피격 표시: HP 가 줄면 잠깐 흰색으로 깜빡임
            if (changed.Hp < lastHp) flashTimer = FlashSeconds;
            lastHp = changed.Hp;

            var s = hpFill.localScale;
            s.x = battlefield.CellSize * BodyScale * Mathf.Clamp01((float)changed.Hp / changed.MaxHp);
            hpFill.localScale = s;
        }

        private void OnDestroy()
        {
            if (unit != null) unit.StateChanged -= OnUnitChanged;
        }
    }
}
