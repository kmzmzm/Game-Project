using System.Collections;
using UnityEngine;

namespace Arcana.Core
{
    /// <summary>
    /// Time.timeScale을 일시적으로 0으로 만들어 히트스탑 연출을 제공하는 매니저.
    /// </summary>
    public class HitstopManager : MonoBehaviour
    {
        public static HitstopManager Instance { get; private set; }

        // 현재 히트스탑 코루틴 — 중복 실행 방지용
        Coroutine _hitstopCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 지정한 시간(초) 동안 게임을 일시 정지해 히트스탑 효과를 낸다.
        /// 이미 실행 중이면 기존 코루틴을 중단하고 새로 시작한다.
        /// </summary>
        /// <param name="duration">히트스탑 지속 시간 (실제 시간 기준, 초)</param>
        public void DoHitstop(float duration)
        {
            if (_hitstopCoroutine != null)
                StopCoroutine(_hitstopCoroutine);

            _hitstopCoroutine = StartCoroutine(HitstopRoutine(duration));
        }

        // timeScale이 0이므로 WaitForSeconds 대신 WaitForSecondsRealtime 사용
        IEnumerator HitstopRoutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _hitstopCoroutine = null;
        }
    }
}
