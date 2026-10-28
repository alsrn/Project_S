using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// Unit 1기의 화면 표시(프리팹: Assets/Prefabs/UnitView.prefab). Unit.StateChanged 를 구독해 갱신만 하며 상태를 바꾸지 않는다.
    /// - 스프라이트: UnitDefinition 의 방향별 스프라이트(이동 방향에 따라 교체). 없으면 흰 사각형에 유닛 색.
    /// - HP 바, 피격 깜빡임, 칸 단위 슬라이드 이동.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        private const float BodyFill = 0.85f;      // 칸 대비 본체 크기
        private const float SquareFill = 0.7f;     // 스프라이트가 없을 때 사각형 크기
        private const float HpBarHeight = 0.08f;
        private const float SlideTilesPerSecond = 10f;
        private const float FlashSeconds = 0.15f;
        private static readonly Color FlashColor = new Color(1f, 0.45f, 0.45f);

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer hpBack;
        [SerializeField] private Transform hpFillRoot;
        [SerializeField] private SpriteRenderer hpFill;

        private Unit unit;
        private BattlefieldView battlefield;
        private Vector3 targetPosition;
        private bool slideEnabled;
        private int lastHp = int.MaxValue;
        private float flashTimer;
        private Vector2Int facing = Vector2Int.up; // 기획 y+ = 화면 아래(정면)
        private Color baseColor;
        private float hpBarWidth;

        public Unit Unit => unit;

        public static UnitView Create(UnitView prefab, Unit unit, BattlefieldView battlefield)
        {
            var view = Instantiate(prefab, battlefield.transform);
            view.name = $"Unit_{unit.DisplayName}";
            view.unit = unit;
            view.battlefield = battlefield;
            view.facing = unit.Team == Team.Enemy ? Vector2Int.up : Vector2Int.down; // 적은 아래, 아군은 위를 보며 시작
            view.Layout();
            unit.StateChanged += view.OnUnitChanged;
            view.OnUnitChanged(unit);
            return view;
        }

        /// <summary>전투 단계에서 켠다. 배치 단계의 재배치는 즉시 이동으로 보인다.</summary>
        public void EnableSlide() => slideEnabled = true;

        private void Layout()
        {
            float cell = battlefield.CellSize;
            body.sortingOrder = PrototypeVisuals.UnitOrder;
            ApplySprite();

            hpBarWidth = cell * SquareFill;
            float barY = cell * 0.5f - cell * HpBarHeight;
            hpBack.sortingOrder = PrototypeVisuals.HpBarOrder;
            hpBack.color = PrototypeVisuals.HpBarBack;
            hpBack.transform.localPosition = new Vector3(0f, barY, 0f);
            TileView.FitToCell(hpBack, 1f);
            hpBack.transform.localScale = Vector3.Scale(hpBack.transform.localScale, new Vector3(hpBarWidth, cell * HpBarHeight, 1f));

            // 왼쪽 끝 고정으로 줄어들도록 hpFillRoot 가 피벗 역할
            hpFill.sortingOrder = PrototypeVisuals.HpBarOrder + 1;
            hpFill.color = unit.Team == Team.Enemy ? new Color(0.95f, 0.35f, 0.3f) : PrototypeVisuals.HpBarFill;
            TileView.FitToCell(hpFill, 1f);
            hpFill.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            hpFillRoot.localPosition = new Vector3(-hpBarWidth * 0.5f, barY, 0f);
            hpFillRoot.localScale = new Vector3(hpBarWidth, cell * HpBarHeight, 1f);
        }

        private void ApplySprite()
        {
            var def = unit.Definition;
            var sprite = def.GetSprite(facing);
            if (sprite != null)
            {
                body.sprite = sprite;
                baseColor = Color.white;
                FitPreservingAspect(body, battlefield.CellSize * BodyFill);
            }
            else
            {
                // 스프라이트 없음: 프리팹의 흰 사각형을 유닛 색으로
                baseColor = def.Color;
                TileView.FitToCell(body, battlefield.CellSize * SquareFill);
            }

            body.color = flashTimer > 0f ? FlashColor : baseColor;
        }

        private static void FitPreservingAspect(SpriteRenderer renderer, float size)
        {
            var s = renderer.sprite.bounds.size;
            float scale = size / Mathf.Max(s.x, s.y);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                body.color = flashTimer > 0f ? FlashColor : baseColor;
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
                var newTarget = battlefield.CoordToWorld(changed.Coord);
                bool adjacentStep = Vector3.Distance(transform.position, newTarget) <= battlefield.CellSize * 1.01f;

                // 한 칸 이동이면 그 방향을 바라본다(월드 좌표 → 기획 방향: 화면 아래 = y+)
                if (slideEnabled && wasVisible && adjacentStep && newTarget != targetPosition)
                {
                    var delta = newTarget - targetPosition;
                    facing = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                        ? new Vector2Int(delta.x > 0 ? 1 : -1, 0)
                        : new Vector2Int(0, delta.y < 0 ? 1 : -1);
                    ApplySprite();
                }

                targetPosition = newTarget;
                // 처음 등장·배치(재배치 포함)나 한 칸 넘게 떨어진 경우는 즉시 이동
                if (!wasVisible || !adjacentStep || !slideEnabled)
                    transform.position = targetPosition;
            }

            if (changed.Hp < lastHp && lastHp != int.MaxValue) flashTimer = FlashSeconds;
            lastHp = changed.Hp;

            var s = hpFillRoot.localScale;
            s.x = hpBarWidth * Mathf.Clamp01((float)changed.Hp / changed.MaxHp);
            hpFillRoot.localScale = s;
        }

        private void OnDestroy()
        {
            if (unit != null) unit.StateChanged -= OnUnitChanged;
        }
    }
}
