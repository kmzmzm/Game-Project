using UnityEngine;
using TMPro;
using Arcana.Core;

namespace Arcana.UI
{
    /// <summary>
    /// 메인 메뉴 화면의 UI 요소를 제어하는 스크립트. 타이틀 표시 및 씬 전환·종료 버튼을 관리한다.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        // Inspector에서 연결할 타이틀 텍스트 레퍼런스
        [SerializeField] TextMeshProUGUI titleText;

        /// <summary>
        /// 게임 시작 버튼의 OnClick 이벤트에 Inspector에서 직접 연결한다.
        /// Hub 씬으로 전환한다.
        /// </summary>
        public void OnStartButtonClick()
        {
            GameManager.Instance.ChangeState(GameState.Hub);
            SceneLoader.Instance.LoadScene(SceneLoader.Hub);
        }

        /// <summary>
        /// 종료 버튼의 OnClick 이벤트에 Inspector에서 직접 연결한다.
        /// 에디터에서는 플레이 모드를 중단하고, 빌드에서는 애플리케이션을 종료한다.
        /// </summary>
        public void OnQuitButtonClick()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
