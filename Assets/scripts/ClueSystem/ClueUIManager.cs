using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ClueUIManager : MonoBehaviour
{
    public static ClueUIManager Instance;

    [Header("UI 참조")]
    public GameObject cluePanel;
    public TMP_Text titleText;
    public TMP_Text contentText;
    public Image clueImage;
    public AudioSource audioSource;

    [Header("배경 딤 & 클릭 힌트")]
    [Tooltip("단서 열람 중 배경을 어둡게 덮는 전체화면 Image")]
    public Image dimmer;                    // ClueCanvas 하위의 전체화면 Image
    [Tooltip("클릭을 유도하는 아이콘/문구의 CanvasGroup")]
    public CanvasGroup clickHint;           // ClickHint 오브젝트의 CanvasGroup

    [Range(0f, 1f)] public float dimTargetAlpha = 0.55f;
    [Range(0.05f, 1f)] public float dimFadeSeconds = 0.25f;
    [Range(0.05f, 1f)] public float hintFadeSeconds = 0.2f;
    [Tooltip("힌트 아이콘의 위아래 호흡 애니메이션 진폭(비율)")]
    public float hintPulseAmp = 0.08f;
    [Tooltip("힌트 아이콘의 호흡 속도")]
    public float hintPulseSpeed = 3f;
    [Tooltip("타자기 완료 후에만 클릭 힌트를 보여줄지")]
    public bool showClickHintAfterTyping = true;

    [Header("효과")]
    public bool useTypewriter = true;
    [Tooltip("초당 글자 수")]
    public float typewriterSpeed = 16f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool isShowing = false;
    private string currentFullContent = "";
    private Coroutine hintShowCo, hintHideCo, dimCo;

    void Awake()
    {
        if (Instance == null) { Instance = this; InitializeUI(); }
        else { Destroy(gameObject); return; }

        // 항상 맨 위에 뜨게(정렬 보정)
        var canvas = cluePanel ? cluePanel.GetComponentInParent<Canvas>() : GetComponentInChildren<Canvas>(true);
        if (canvas) { canvas.overrideSorting = true; canvas.sortingOrder = 200; }
    }

    void InitializeUI()
    {
        if (cluePanel) cluePanel.SetActive(false);
        if (titleText) titleText.text = "";
        if (contentText) contentText.text = "";
        if (clueImage) clueImage.gameObject.SetActive(false);

        if (dimmer)
        {
            var c = dimmer.color; c.a = 0f; dimmer.color = c;
            dimmer.raycastTarget = true; // 뒤 화면 클릭 차단
        }
        if (clickHint)
        {
            clickHint.alpha = 0f;
            clickHint.gameObject.SetActive(false);
            // Raycast는 보통 끄는 게 좋아서 아이콘 Image의 Raycast Target을 Off로
        }
    }

    public void ShowClue(string title, string content, Sprite image = null, AudioClip sound = null)
    {
        if (cluePanel) cluePanel.SetActive(true);
        isShowing = true;

        if (titleText) titleText.text = title ?? "";
        currentFullContent = content ?? "";

        // 배경 딤 인
        if (dimmer)
        {
            if (dimCo != null) StopCoroutine(dimCo);
            dimCo = StartCoroutine(FadeImageAlpha(dimmer, dimmer.color.a, dimTargetAlpha, dimFadeSeconds));
        }

        // 타자기
        if (useTypewriter && contentText)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeWriter(currentFullContent));
        }
        else
        {
            if (contentText) contentText.text = currentFullContent;
            isTyping = false;
            // 타자기를 사용하지 않더라도 클릭 힌트는 바로(또는 설정에 따라) 노출
            if (clickHint && showClickHintAfterTyping)
            {
                if (hintShowCo != null) StopCoroutine(hintShowCo);
                hintShowCo = StartCoroutine(ShowClickHint());
            }
        }

        // 이미지
        if (clueImage)
        {
            if (image != null) { clueImage.sprite = image; clueImage.gameObject.SetActive(true); }
            else { clueImage.gameObject.SetActive(false); }
        }

        // 사운드
        if (audioSource)
        {
            if (sound != null) { audioSource.clip = sound; audioSource.Play(); }
            else { audioSource.Stop(); }
        }

        // 🔴 열람 시작 통지
        GameManager.Instance?.NotifyClueOpened();
    }

    IEnumerator TypeWriter(string fullText)
    {
        isTyping = true;
        if (contentText) contentText.text = "";

        float tPerChar = 1f / Mathf.Max(0.001f, typewriterSpeed);
        foreach (char c in fullText)
        {
            if (!isShowing) break; // 안전 가드
            if (contentText) contentText.text += c;
            yield return new WaitForSecondsRealtime(tPerChar);
        }

        isTyping = false;
        typingCoroutine = null;

        // 타자기 완료 후 클릭 힌트 노출
        if (clickHint && showClickHintAfterTyping)
        {
            if (hintShowCo != null) StopCoroutine(hintShowCo);
            hintShowCo = StartCoroutine(ShowClickHint());
        }
    }

    void SkipTyping()
    {
        if (!isTyping) return;
        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        isTyping = false;
        if (contentText) contentText.text = currentFullContent;

        // 스킵 시에도 힌트 노출
        if (clickHint && showClickHintAfterTyping)
        {
            if (hintShowCo != null) StopCoroutine(hintShowCo);
            hintShowCo = StartCoroutine(ShowClickHint());
        }
    }

    void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        if (!isShowing) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping) { SkipTyping(); return; }

            // 닫기 전 힌트/딤 정리
            if (clickHint)
            {
                if (hintHideCo != null) StopCoroutine(hintHideCo);
                hintHideCo = StartCoroutine(HideClickHint());
            }
            HideClue(); return;
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (clickHint)
            {
                if (hintHideCo != null) StopCoroutine(hintHideCo);
                hintHideCo = StartCoroutine(HideClickHint());
            }
            HideClue(); return;
        }
    }

    public void HideClue()
    {
        if (!isShowing) return;

        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        isTyping = false;

        if (audioSource && audioSource.isPlaying) audioSource.Stop();
        if (clueImage) clueImage.gameObject.SetActive(false);
        if (titleText) titleText.text = "";
        if (contentText) contentText.text = "";

        // 배경 딤 아웃
        if (dimmer)
        {
            if (dimCo != null) StopCoroutine(dimCo);
            dimCo = StartCoroutine(FadeImageAlpha(dimmer, dimmer.color.a, 0f, dimFadeSeconds));
        }

        if (cluePanel) cluePanel.SetActive(false);
        isShowing = false;

        // 🔴 열람 종료 통지(닫은 뒤 유예가 적용됨)
        GameManager.Instance?.NotifyClueClosed();
    }

    // 외부에서 상태 읽기 원할 때
    public bool IsOpen => isShowing;

    // ──────────────────────────────
    // 보조 코루틴들
    // ──────────────────────────────
    IEnumerator FadeImageAlpha(Image img, float from, float to, float dur)
    {
        if (!img) yield break;
        float t = 0f;
        var col = img.color;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            col.a = Mathf.Lerp(from, to, t / Mathf.Max(0.0001f, dur));
            img.color = col;
            yield return null;
        }
        col.a = to; img.color = col;
    }

    IEnumerator ShowClickHint()
    {
        if (!clickHint) yield break;

        clickHint.gameObject.SetActive(true);

        // 페이드 인
        float t = 0f;
        while (t < hintFadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            clickHint.alpha = Mathf.Lerp(0f, 1f, t / Mathf.Max(0.0001f, hintFadeSeconds));
            yield return null;
        }
        clickHint.alpha = 1f;

        // 호흡 애니메이션(위아래 살짝)
        var rt = clickHint.transform as RectTransform;
        Vector2 basePos = rt.anchoredPosition;
        while (isShowing && !isTyping && clickHint.alpha > 0.99f)
        {
            float y = Mathf.Sin(Time.unscaledTime * hintPulseSpeed) * (rt.sizeDelta.y * hintPulseAmp);
            rt.anchoredPosition = basePos + new Vector2(0f, y);
            yield return null;
        }
        rt.anchoredPosition = basePos;
    }

    IEnumerator HideClickHint()
    {
        if (!clickHint) yield break;

        float a0 = clickHint.alpha;
        float t = 0f;
        while (t < hintFadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            clickHint.alpha = Mathf.Lerp(a0, 0f, t / Mathf.Max(0.0001f, hintFadeSeconds));
            yield return null;
        }
        clickHint.alpha = 0f;
        clickHint.gameObject.SetActive(false);
    }
}
