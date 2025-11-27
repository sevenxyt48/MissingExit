using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 키보드 전용 스토리(일기) UI
/// - A / LeftArrow : 이전 페이지
/// - D / RightArrow : 다음 페이지
/// - Q            : 닫기   (ESC는 Pause용)
/// - AD키, Q키 힌트 이미지만 사용 (버튼 없음)
/// - 페이지 이동음 / 닫기음 재생
/// </summary>
public class StoryViewer : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject rootCanvas;
    public TMP_Text titleText;
    public TMP_Text pageText;
    public TMP_Text pageNumberText;

    [Header("타이틀 설정")]
    public string defaultTitle = "주어진 일기장";

    [Header("힌트 이미지 (사진 2개)")]
    public Image moveHintImage;       // AD 키 사진 (HintImg)
    public Image closeHintImage;      // Q 키 사진 (CloseIMG)
    public GameObject hintGroup;

    [Header("사운드")]
    public AudioSource audioSource;   // storySFX (페이지 넘김용)
    public AudioClip moveSfx;         // SFX_confirm
    public AudioClip closeSfx;        // SFX_select

    // 내부 상태
    private string[] pages;
    private int currentPage = 0;
    private bool isOpen = false;
    private System.Action onClosedCallback;

    void Awake()
    {
        if (rootCanvas != null)
            rootCanvas.SetActive(false);
    }

    void Update()
    {
        if (!isOpen) return;

        // 닫기: Q 만 사용 (ESC는 PauseManager 용)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            PlayCloseSound();
            Close();
            return;
        }

        // 다음 페이지: D 또는 →
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnNext();
            return;
        }

        // 이전 페이지: A 또는 ←
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnPrev();
            return;
        }
    }

    /// <summary>
    /// 스토리 화면 열기
    /// </summary>
    public void Open(string[] storyPages, System.Action onClosed = null)
    {
        onClosedCallback = onClosed;

        if (storyPages == null || storyPages.Length == 0)
        {
            onClosedCallback?.Invoke();
            onClosedCallback = null;
            return;
        }

        pages = storyPages;
        currentPage = 0;
        isOpen = true;

        if (GameManager.Instance)
            GameManager.Instance.SetStoryOpen(true);

        if (rootCanvas != null)
            rootCanvas.SetActive(true);

        if (titleText != null)
            titleText.text = defaultTitle;

        ShowCurrentPage();
    }

    private void ShowCurrentPage()
    {
        if (pages == null || pages.Length == 0) return;

        if (pageText)
            pageText.text = pages[currentPage];

        if (pageNumberText)
            pageNumberText.text = $"{currentPage + 1} / {pages.Length}";

        bool multiPages = pages.Length > 1;

        // AD 키 힌트: 페이지가 여러 개일 때만 표시
        if (moveHintImage)
            moveHintImage.gameObject.SetActive(multiPages);

        // Q 키 힌트: 항상 표시
        if (closeHintImage)
            closeHintImage.gameObject.SetActive(true);

        if (hintGroup)
            hintGroup.SetActive(true);
    }

    private void OnNext()
    {
        if (pages == null) return;
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            PlayMoveSound();
            ShowCurrentPage();
        }
    }

    private void OnPrev()
    {
        if (pages == null) return;
        if (currentPage > 0)
        {
            currentPage--;
            PlayMoveSound();
            ShowCurrentPage();
        }
    }

    /// <summary>
    /// Q로 닫기
    /// </summary>
    public void Close()
    {
        isOpen = false;

        if (rootCanvas != null)
            rootCanvas.SetActive(false);

        if (GameManager.Instance)
            GameManager.Instance.SetStoryOpen(false);

        onClosedCallback?.Invoke();
        onClosedCallback = null;
    }

    private void PlayMoveSound()
    {
        if (moveSfx == null) return;

        // 페이지 넘김은 StoryCanvas가 살아 있을 때만 호출되므로
        // 기존 AudioSource로 재생해도 괜찮다.
        if (audioSource)
        {
            audioSource.PlayOneShot(moveSfx);
        }
        else if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(moveSfx, Camera.main.transform.position);
        }
    }

    private void PlayCloseSound()
    {
        if (closeSfx == null) return;

        // Canvas가 곧 비활성화되므로 독립적인 OneShot으로 재생
        if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(closeSfx, Camera.main.transform.position);
        }
        else if (audioSource)
        {
            // 혹시 메인 카메라가 없어도 최소한 시도
            audioSource.PlayOneShot(closeSfx);
        }
    }

    public bool IsOpen() => isOpen;
}
