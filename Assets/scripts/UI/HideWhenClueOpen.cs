using UnityEngine;

/// <summary>
/// 단서 UI나 스토리(일기) UI가 열려 있을 때
/// 이 오브젝트를 숨기거나 CanvasGroup으로 페이드 아웃시킨다.
///
/// - ViolationBarUI 루트, GuidanceToast 루트, 상시 힌트 UI 등에 붙이면
///   스토리 화면 동안 자동으로 안 보이게 됨.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HideWhenClueOpen : MonoBehaviour
{
    [Header("동작 방식")]
    [Tooltip("true면 GameObject 자체를 SetActive로 끄고 켠다. false면 CanvasGroup.alpha로 페이드한다.")]
    public bool disableGameObject = false;

    [Tooltip("disableGameObject=false일 때만 사용. 부드럽게 페이드할지 여부.")]
    public bool useFade = true;

    [Tooltip("useFade=true일 때 0→1 또는 1→0 전환 걸리는 시간(초)")]
    public float fadeSeconds = 0.15f;

    private CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (!group && !disableGameObject)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        bool shouldHide = false;

        if (GameManager.Instance)
        {
            // 단서창이 열려 있거나(또는 방금 닫힌 유예 중) => 기존 로직
            // 또는 스토리 화면(일기 뷰어)이 열려 있을 때 => 새로 추가한 로직
            bool clueOpenOrGrace = GameManager.Instance.InClueGrace();
            bool storyOpen = GameManager.Instance.IsStoryOpen;

            shouldHide = clueOpenOrGrace || storyOpen;
        }

        if (disableGameObject)
        {
            // GameObject 자체를 껐다 켜기
            if (gameObject.activeSelf == shouldHide)
                gameObject.SetActive(!shouldHide);
        }
        else
        {
            // CanvasGroup 페이드 방식
            float targetAlpha = shouldHide ? 0f : 1f;

            if (useFade)
            {
                // Time.unscaledDeltaTime을 우선 쓰고, 0이면 deltaTime
                float dt = (Time.unscaledDeltaTime > 0f) ? Time.unscaledDeltaTime : Time.deltaTime;
                float step = (fadeSeconds > 0f) ? dt / fadeSeconds : 1f;
                group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, step);
            }
            else
            {
                group.alpha = targetAlpha;
            }

            group.blocksRaycasts = !shouldHide;
            group.interactable = !shouldHide;
        }
    }
}
