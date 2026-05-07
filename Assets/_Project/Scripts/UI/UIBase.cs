using System;
using System.Collections;
using UnityEngine;

namespace Arcana.UI
{
    /// <summary>
    /// 모든 UI 패널의 베이스 클래스. CanvasGroup 기반 페이드 인/아웃과 Show/Hide 이벤트를 제공한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIBase : MonoBehaviour
    {
        // 페이드 지속 시간 (초) — Inspector에서 패널별로 조정 가능
        [SerializeField] protected float fadeDuration = 0.3f;

        // 페이드 제어에 사용하는 CanvasGroup 컴포넌트
        protected CanvasGroup canvasGroup;

        // 진행 중인 페이드 코루틴 (중복 실행 방지용)
        Coroutine fadeCoroutine;

        /// <summary>Show() 완료 후 발행되는 이벤트.</summary>
        public event Action OnShow;

        /// <summary>Hide() 완료 후 발행되는 이벤트.</summary>
        public event Action OnHide;

        protected virtual void Awake()
        {
            // CanvasGroup이 없으면 자동으로 추가
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// UI 패널을 페이드 인하며 표시한다.
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);

            // 이전 페이드가 실행 중이면 중단 후 새로 시작
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeIn());
        }

        /// <summary>
        /// UI 패널을 페이드 아웃하며 숨긴다. 완료 후 GameObject를 비활성화한다.
        /// </summary>
        public virtual void Hide()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeOut());
        }

        // 알파를 0 → 1로 선형 보간하는 페이드 인 코루틴
        IEnumerator FadeIn()
        {
            // 입력 차단 상태로 시작
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            // 페이드 완료 후 입력 허용
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            OnShow?.Invoke();
        }

        // 알파를 1 → 0으로 선형 보간하는 페이드 아웃 코루틴
        IEnumerator FadeOut()
        {
            // 페이드 아웃 중 입력 차단
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);

            OnHide?.Invoke();
        }
    }
}
