using System.Collections;
using UnityEngine;

namespace Arcana.Core
{
    /// <summary>
    /// _Boot 씬 진입 시 게임 초기화 흐름을 제어하는 스크립트. 매니저 준비 확인 후 메인 메뉴로 전환한다.
    /// </summary>
    public class BootInitializer : MonoBehaviour
    {
        // 메인 메뉴 전환 전 대기 시간 (초)
        const float DelayBeforeMainMenu = 1f;

        void Awake()
        {
            // 같은 GameObject의 다른 컴포넌트들은 Awake 순서가 컴포넌트 배치 순서를 따름
            // BootInitializer는 마지막에 배치되므로 이 시점에 매니저 인스턴스가 모두 준비되어 있어야 함
            ValidateManagers();
        }

        void Start()
        {
            // Awake에서 SaveManager가 Load()를 호출하지만,
            // Start에서 GameState를 Boot로 명시 후 전환 흐름 시작
            GameManager.Instance.ChangeState(GameState.Boot);

            StartCoroutine(TransitionToMainMenu());
        }

        /// <summary>
        /// 필수 매니저 인스턴스가 모두 준비됐는지 검증한다. 누락 시 에러 로그 출력.
        /// </summary>
        void ValidateManagers()
        {
            if (GameManager.Instance == null)
                Debug.LogError("[BootInitializer] GameManager 인스턴스가 없습니다.");

            if (SceneLoader.Instance == null)
                Debug.LogError("[BootInitializer] SceneLoader 인스턴스가 없습니다.");

            if (SaveManager.Instance == null)
                Debug.LogError("[BootInitializer] SaveManager 인스턴스가 없습니다.");
        }

        /// <summary>
        /// 지정 시간 대기 후 게임 상태를 MainMenu로 전환하고 씬을 로드한다.
        /// </summary>
        IEnumerator TransitionToMainMenu()
        {
            // DelayBeforeMainMenu 초 대기 (로고 표시 등 부팅 연출 시간)
            yield return new WaitForSeconds(DelayBeforeMainMenu);

            // 상태 변경 → 씬 전환 순서 보장
            GameManager.Instance.ChangeState(GameState.MainMenu);
            SceneLoader.Instance.LoadScene(SceneLoader.MainMenu);
        }
    }
}
