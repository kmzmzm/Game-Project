using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arcana.Core;

namespace Arcana.UI
{
    /// <summary>
    /// 메인 메뉴 화면의 UI 요소를 제어하는 스크립트. 타이틀 표시 및 씬 전환·종료 버튼을 관리한다.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        // Inspector에서 연결할 UI 레퍼런스
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] Button startButton;
        [SerializeField] Button quitButton;

        void Start()
        {
            // 버튼 클릭 이벤트 등록
            startButton.onClick.AddListener(OnStartClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        void OnDestroy()
        {
            // 씬 언로드 시 리스너 해제 — 메모리 누수 방지
            startButton.onClick.RemoveListener(OnStartClicked);
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        /// <summary>
        /// 게임 시작 버튼 클릭 시 Hub 씬으로 전환한다.
        /// </summary>
        void OnStartClicked()
        {
            GameManager.Instance.ChangeState(GameState.Hub);
            SceneLoader.Instance.LoadScene(SceneLoader.Hub);
        }

        /// <summary>
        /// 종료 버튼 클릭 시 애플리케이션을 종료한다. 에디터에서는 플레이 모드를 중단한다.
        /// </summary>
        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
