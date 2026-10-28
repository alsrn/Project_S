using System.Collections;

namespace ProjectS.Actions
{
    /// <summary>
    /// 행동 실행용 시간. ActionScheduler.Tick 이 매 프레임 DeltaTime 을 채운다.
    /// 일시 정지 중에는 Tick 이 호출되지 않으므로 실행도 멈춘다. Time.timeScale 에 의존하지 않는다.
    /// </summary>
    public sealed class ExecutionClock
    {
        public float DeltaTime { get; internal set; }

        /// <summary>실행 코드에서 `yield return clock.Wait(0.2f)` 로 사용.</summary>
        public IEnumerator Wait(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += DeltaTime;
            }
        }
    }
}
