using UnityEngine;

namespace Arcana.Core
{
    /// <summary>
    /// 게임 전체 상태를 관리하는 최상위 매니저. 씬 전반에 걸쳐 단일 인스턴스를 유지한다.
    /// </summary>

    // 게임이 현재 어느 흐름에 있는지를 나타내는 상태 열거형
    public enum GameState
    {
        Boot,       // 앱 시작 및 초기화
        MainMenu,   // 메인 메뉴 화면
        Hub,        // 허브(로비) 화면
        InGame,     // 게임 플레이 중
        Paused,     // 일시 정지
        GameOver    // 게임 오버
    }

    public class GameManager : MonoBehaviour
    {
        // 어디서든 접근 가능한 단일 인스턴스
        public static GameManager Instance { get; private set; }

        // 현재 게임 상태 (외부에서 읽기만 허용)
        public GameState CurrentState { get; private set; }

        void Awake()
        {
            // 이미 인스턴스가 존재하면 중복 오브젝트 제거
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // 씬이 전환되어도 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 게임 상태를 변경한다.
        /// </summary>
        /// <param name="newState">전환할 대상 상태</param>
        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
        }
    }
}
