using ProjectS.UI;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// 스테이지 진행 최상위 컨트롤러(씬 진입점). 편성 → 배치 → 전투 → 정산 흐름과 강제 종료를 담당한다.
    /// 현재(단계 A): StageDefinition 으로 전장을 만들고 적 스폰을 표시·로그한다.
    /// </summary>
    public sealed class StageFlowController : MonoBehaviour
    {
        [SerializeField] private StageDefinition stage;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;

        public Battlefield Battlefield { get; private set; }

        private void Start()
        {
            if (stage == null)
            {
                Debug.LogError("[StageFlow] StageDefinition 이 지정되지 않았습니다.", this);
                return;
            }

            Battlefield = Battlefield.Create(stage);
            LogSpawns();

            var view = BattlefieldView.Create(Battlefield, cellSize);
            var cam = Camera.main;
            if (cam != null)
            {
                if (!cam.TryGetComponent(out BattlefieldCamera camController))
                    camController = cam.gameObject.AddComponent<BattlefieldCamera>();
                camController.FitTo(view.WorldBounds);
            }

            PrototypeHud.Create(Battlefield, view);
        }

        private void LogSpawns()
        {
            Debug.Log($"[StageFlow] {stage.DisplayName}: {Battlefield.Grid.Width}x{Battlefield.Grid.Height} grid created");
            foreach (var unit in Battlefield.Units.All)
                Debug.Log($"[StageFlow] Spawned {unit.DisplayName} ({unit.Team}) at design {unit.Coord} / internal {unit.Coord.ToInternalString()}");
        }
    }
}
