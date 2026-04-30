using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcana.Core
{
    /// <summary>
    /// 씬 전환을 담당하는 매니저. 씬 이름 상수와 동기/비동기 로드 메서드를 제공한다.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        // 어디서든 접근 가능한 단일 인스턴스
        public static SceneLoader Instance { get; private set; }

        // 씬 이름 문자열을 하드코딩 없이 참조하기 위한 상수
        public const string Boot      = "_Boot";
        public const string MainMenu  = "MainMenu";
        public const string Hub       = "Hub";
        public const string GameScene = "GameScene";

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
        /// 지정한 씬을 동기 방식으로 즉시 로드한다.
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름 (상수 사용 권장)</param>
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 지정한 씬을 비동기로 로드한다. 로딩 중 프레임이 끊기지 않는다.
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름 (상수 사용 권장)</param>
        public void LoadSceneAsync(string sceneName)
        {
            StartCoroutine(LoadAsync(sceneName));
        }

        // 비동기 로드 코루틴 — isDone이 될 때까지 매 프레임 대기
        IEnumerator LoadAsync(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
                yield return null;
        }
    }
}
