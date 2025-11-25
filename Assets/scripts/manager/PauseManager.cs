using UnityEngine;
using UnityEngine.SceneManagement;
// using TMPro;    // 인스펙터에서 TMP_Text를 쓴다면 이 using을 켜도 됨

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("UI")]
    [Tooltip("전체 일시정지 메뉴를 담고 있는 Canvas 루트 오브젝트")]
    public GameObject pauseCanvas;

    [Tooltip("메뉴 항목: '다시 시작' 텍스트")]
    public TMPro.TMP_Text resumeLabel;

    [Tooltip("메뉴 항목: '메뉴로 돌아가기' 텍스트")]
    public TMPro.TMP_Text menuLabel;

    [Header("색상")]
    [Tooltip("선택되지 않은 메뉴 항목 색상")]
    public Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Tooltip("선택된 메뉴 항목 색상")]
    public Color selectedColor = Color.white;

    [Header("씬 이름")]
    [Tooltip("메뉴로 돌아가기 선택 시 로드할 시작 씬 이름")]
    public string startSceneName = "StartScene";

    [Header("플레이어 참조")]
    [Tooltip("플레이어 이동을 잠글 FirstPersonController")]
    public FirstPersonController playerController;

    // 내부 상태
    private bool isPaused = false;
    private int currentIndex = 0;   // 0: 다시 시작, 1: 메뉴로 돌아가기

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);   // 시작 시에는 항상 꺼진 상태

        UpdateMenuVisual();
    }

    void Start()
    {
        // 플레이어 참조가 비어 있으면 자동으로 찾아줌
        if (playerController == null)
            playerController = FindObjectOfType<FirstPersonController>();
    }

    void Update()
    {
        // 1) 엔딩 상태에서는 Pause 자체를 막는다
        if (GameManager.Instance != null &&
            GameManager.Instance.LastEnding != GameManager.EndingType.None)
        {
            return;
        }

        // 2) ESC로 Pause 메뉴 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        // Pause 아닌 상태에서는 여기서 끝
        if (!isPaused) return;

        // ─────────────────────────────
        // 3) 여기부터는 "중지 메뉴가 떠 있을 때"만 처리
        //    → 다른 스크립트들은 IsPaused 보고 입력/로직 차단
        // ─────────────────────────────

        // 위로 이동: W / A / ↑ / ←
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = 1;
            UpdateMenuVisual();
        }
        // 아래로 이동: S / D / ↓ / →
        else if (Input.GetKeyDown(KeyCode.S) ||
                 Input.GetKeyDown(KeyCode.D) ||
                 Input.GetKeyDown(KeyCode.DownArrow) ||
                 Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentIndex++;
            if (currentIndex > 1) currentIndex = 0;
            UpdateMenuVisual();
        }

        // 선택: Enter / E
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ActivateCurrentItem();
        }
    }

    // 선택된/비선택 메뉴 텍스트 색상 업데이트
    void UpdateMenuVisual()
    {
        if (resumeLabel != null)
            resumeLabel.color = (currentIndex == 0) ? selectedColor : normalColor;

        if (menuLabel != null)
            menuLabel.color = (currentIndex == 1) ? selectedColor : normalColor;
    }

    // 현재 선택된 메뉴 실행
    void ActivateCurrentItem()
    {
        if (currentIndex == 0)
        {
            // "다시 시작"
            Resume();
        }
        else if (currentIndex == 1)
        {
            // "메뉴로 돌아가기"
            ReturnToMenu();
        }
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    void Pause()
    {
        isPaused = true;
        currentIndex = 0;
        UpdateMenuVisual();

        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);

        // 게임 시간 정지
        Time.timeScale = 0f;

        // 플레이어 이동/조작 잠금
        if (playerController != null)
            playerController.SetControlEnabled(false);

        // 이 게임은 마우스 UI를 거의 쓰지 않으므로 커서는 계속 잠금 상태 유지
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Resume()
    {
        isPaused = false;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        // 게임 시간 다시 흐르게
        Time.timeScale = 1f;

        // 플레이어 이동/조작 다시 허용
        if (playerController != null)
            playerController.SetControlEnabled(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ReturnToMenu()
    {
        // 씬 전환 전에 Pause 상태 해제 & 시간/조작 원복
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        if (playerController != null)
            playerController.SetControlEnabled(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!string.IsNullOrEmpty(startSceneName))
            SceneManager.LoadScene(startSceneName);
    }

    // 다른 스크립트에서 Pause 상태를 확인할 때 사용하는 프로퍼티
    public bool IsPaused => isPaused;
}
