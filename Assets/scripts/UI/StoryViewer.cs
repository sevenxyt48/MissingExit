using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 전체 화면으로 일기(스토리) 페이지를 보여주는 UI
/// - Open(pages, onClosed) 로 표시
/// - Close() 또는 ESC로 닫기
/// - prev / next 버튼으로 페이지 이동
/// - 첫 페이지면 prev 버튼 숨김, 마지막 페이지면 next 버튼 숨김
/// - 상단에 고정 타이틀(예: "주어진 일기장")
/// </summary>
public class StoryViewer : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject rootCanvas;       // StoryCanvas (전체 패널)
    public TMP_Text titleText;          // 상단 타이틀
    public TMP_Text pageText;           // 본문(일기 내용)
    public TMP_Text pageNumberText;     // "1 / n"
    public Button nextButton;
    public Button prevButton;
    public Button closeButton;

    [Header("타이틀 설정")]
    [Tooltip("스토리 화면 상단에 항상 표시할 제목 (ex. '주어진 일기장')")]
    public string defaultTitle = "주어진 일기장";

    // 내부 상태
    private string[] pages;
    private int currentPage = 0;
    private bool isOpen = false;
    private Action onClosedCallback;

    void Awake()
    {
        // 시작할 땐 안 보이게
        if (rootCanvas != null)
            rootCanvas.SetActive(false);

        // 버튼 이벤트 연결
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        if (prevButton != null) prevButton.onClick.AddListener(OnPrev);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        if (!isOpen) return;

        // ESC로 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    /// <summary>
    /// 스토리 화면 열기
    /// storyPages: 페이지별 본문들
    /// onClosed: 닫을 때 호출할 콜백 (ex. 플레이어 다시 움직이게)
    /// </summary>
    public void Open(string[] storyPages, Action onClosed = null)
    {
        onClosedCallback = onClosed;

        // 내용이 없으면 그냥 종료 콜백만 실행하고 끝
        if (storyPages == null || storyPages.Length == 0)
        {
            onClosedCallback?.Invoke();
            onClosedCallback = null;
            return;
        }

        pages = storyPages;
        currentPage = 0;
        isOpen = true;

        // 커서 보이게 해서 버튼 클릭 가능하게
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // HUD 숨김 등 전역 상태 알림
        if (GameManager.Instance)
            GameManager.Instance.SetStoryOpen(true);

        // 패널 켜기
        if (rootCanvas != null)
            rootCanvas.SetActive(true);

        // 타이틀 세팅
        if (titleText != null)
            titleText.text = defaultTitle;

        ShowCurrentPage();
    }

    private void ShowCurrentPage()
    {
        if (pages == null || pages.Length == 0) return;

        // 본문 텍스트
        if (pageText)
            pageText.text = pages[currentPage];

        // 페이지 인디케이터 "1 / n"
        if (pageNumberText)
            pageNumberText.text = $"{currentPage + 1} / {pages.Length}";

        // prev / next 버튼의 표시 여부 결정
        // 첫 페이지: prev 숨김
        if (prevButton)
            prevButton.gameObject.SetActive(currentPage > 0);

        // 마지막 페이지: next 숨김
        if (nextButton)
            nextButton.gameObject.SetActive(currentPage < pages.Length - 1);
    }

    private void OnNext()
    {
        if (pages == null) return;
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowCurrentPage();
        }
    }

    private void OnPrev()
    {
        if (pages == null) return;
        if (currentPage > 0)
        {
            currentPage--;
            ShowCurrentPage();
        }
    }

    /// <summary>
    /// X버튼, ESC 등으로 닫기
    /// </summary>
    public void Close()
    {
        isOpen = false;

        // 패널 끄기
        if (rootCanvas != null)
            rootCanvas.SetActive(false);

        // 다시 게임 모드 → 마우스 없애기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // HUD 복귀 등 전역 상태 알림
        if (GameManager.Instance)
            GameManager.Instance.SetStoryOpen(false);

        // 플레이어 컨트롤 복구 등 콜백
        onClosedCallback?.Invoke();
        onClosedCallback = null;
    }

    public bool IsOpen() => isOpen;
}
