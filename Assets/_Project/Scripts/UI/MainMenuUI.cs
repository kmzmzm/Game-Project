using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arcana.Core;

namespace Arcana.UI
{
    /// <summary>
    /// 메인 메뉴 화면의 UI 패널. UIBase를 상속해 페이드 인/아웃으로 표시·숨김을 처리한다.
    /// </summary>
    public class MainMenuUI : UIBase
    {
        // Inspector에서 연결할 UI 요소
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] Button startButton;
        [SerializeField] Button quitButton;

        protected override void Awake()
        {
            // CanvasGroup 초기화 (UIBase.Awake 호출 필수)
            base.Awake();

            // 씬 진입 시 즉시 알파 0으로 설정해 페이드 인 준비
            canvasGroup.alpha = 0f;
        }

        void Start()
        {
            // 씬 로드 완료 후 페이드 인으로 패널 등장
            Show();
        }

        /// <summary>
        /// 패널을 페이드 인으로 표시한다. OnShow 이벤트가 완료 후 발행된다.
        /// </summary>
        public override void Show()
        {
            base.Show();
        }

        /// <summary>
        /// 패널을 페이드 아웃으로 숨긴다. OnHide 이벤트가 완료 후 발행된다.
        /// </summary>
        public override void Hide()
        {
            base.Hide();
        }

        /// <summary>
        /// 게임 시작 버튼 OnClick에 Inspector에서 연결한다. Hub 씬으로 전환한다.
        /// </summary>
        public void OnStartButtonClick()
        {
            GameManager.Instance.ChangeState(GameState.Hub);
            SceneLoader.Instance.LoadScene(SceneLoader.Hub);
        }

        /// <summary>
        /// 종료 버튼 OnClick에 Inspector에서 연결한다.
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
