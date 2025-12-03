using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class StartGame : MonoBehaviour
{
    [Header("씬 이름")]
    [Tooltip("게임 플레이 씬 이름")]
    public string gameplaySceneName = "GameScene";

    [Header("UI 참조")]
    [Tooltip("화면 전체를 덮는 검은색 페이드 이미지")]
    public Image fadeImage;                  // Canvas/fadeImage
    [Tooltip("배경 일러스트/스크린샷 이미지")]
    public Image backgroundImage;            // Canvas/backgroundImage

    [Tooltip("타이틀 텍스트 (타이핑 효과용)")]
    public TMP_Text titleText;               // TitleGroup/titleText

    [Header("크레딧에서 숨길 오브젝트들")]
    [Tooltip("크레딧 화면에서 숨기고 싶은 오브젝트들을 모두 넣기 (예: TitleGroup, keyHint, Enter 힌트 등)")]
    public GameObject[] hideOnCredits;       // TitleGroup, 키힌트, KeySelect 등

    [Header("메뉴 그룹")]
    [Tooltip("Start / Credits / Quit 버튼이 들어있는 오브젝트")]
    public GameObject mainMenuGroup;         // Canvas/MainMenuGroup
    [Tooltip("크레딧 패널 오브젝트 (Panel_Credits)")]
    public GameObject creditsPanel;          // Canvas/Panel_Credits

    [Header("페이드 / 타이틀 효과")]
    [Tooltip("페이드 인/아웃 속도")]
    public float fadeSpeed = 1f;
    [Tooltip("타이틀 텍스트 전체 문자열")]
    public string fullTitle = "잃어버린 출구";
    [Tooltip("타이틀 한 글자씩 찍히는 속도(초)")]
    public float typingSpeed = 0.1f;

    [Header("배경 BGM")]
    public AudioSource bgmSource;
    public bool playBgmOnStart = true;

    [Header("키보드 메뉴 설정")]
    [Tooltip("메뉴 버튼들을 위에서 아래 순서대로 넣기")]
    public Button[] menuButtons;             // Start, Credits, Quit
    [Tooltip("각 버튼에 붙어 있는 ButtonTextColor (동일한 순서로)")]
    public ButtonTextColor[] menuTextColors; // Start, Credits, Quit
    [Tooltip("처음 선택될 메뉴 인덱스 (0 = 첫 번째 버튼)")]
    public int defaultIndex = 0;

    [Header("크레딧 자동 스크롤 (옵션)")]
    [Tooltip("크레딧 텍스트가 들어 있는 ScrollRect (없으면 비워두기)")]
    public ScrollRect creditsScroll;         // Panel_Credits/CreditBox 의 ScrollRect
    [Tooltip("위→아래로 내려가는 데 걸리는 시간(초)")]
    public float creditsScrollTime = 20f;

    [Header("UI 효과음 (선택사항)")]
    public AudioSource uiAudioSource;
    public AudioClip moveClip;               // 위/아래로 이동할 때
    public AudioClip confirmClip;            // 선택(Enter/버튼 클릭) 할 때

    // 내부 상태
    int currentIndex = 0;
    bool isLoading = false;
    Coroutine typingCoroutine;
    Coroutine creditsScrollRoutine;

    // ===================== 초기화 ======================

    void Start()
    {
        // 페이드 이미지가 있다면 처음에는 완전히 검은색에서 시작
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
            StartCoroutine(FadeIn());
        }

        // 배경 BGM 재생
        if (bgmSource != null && playBgmOnStart && !bgmSource.isPlaying)
            bgmSource.Play();

        // 크레딧 패널은 기본적으로 비활성화
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        // 타이틀 타이핑 효과
        if (titleText != null && !string.IsNullOrEmpty(fullTitle))
        {
            titleText.text = "";
            typingCoroutine = StartCoroutine(TypeTitle());
        }

        // 키보드 메뉴 기본 선택 세팅
        if (menuButtons != null && menuButtons.Length > 0)
        {
            currentIndex = Mathf.Clamp(defaultIndex, 0, menuButtons.Length - 1);
            ApplySelectionVisuals();
        }
    }

    // ================= 타이틀 / 페이드 =================

    IEnumerator FadeIn()
    {
        if (fadeImage == null)
            yield break;

        Color c = fadeImage.color;
        float a = c.a;

        while (a > 0f)
        {
            a -= Time.deltaTime * fadeSpeed;
            a = Mathf.Clamp01(a);
            c.a = a;
            fadeImage.color = c;
            yield return null;
        }
    }

    IEnumerator FadeOutAndLoad()
    {
        if (fadeImage == null)
        {
            SceneManager.LoadScene(gameplaySceneName);
            yield break;
        }

        Color c = fadeImage.color;
        float a = c.a;

        while (a < 1f)
        {
            a += Time.deltaTime * fadeSpeed;
            a = Mathf.Clamp01(a);
            c.a = a;
            fadeImage.color = c;
            yield return null;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    IEnumerator TypeTitle()
    {
        titleText.text = "";

        foreach (char ch in fullTitle)
        {
            titleText.text += ch;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    // ================= 버튼 OnClick 함수 =================

    // Start 버튼
    public void OnClickStartGame()
    {
        if (isLoading) return;
        isLoading = true;

        PlayConfirmSfx();

        // 🔥 새 게임 시작 시 GameManager 전체 상태 초기화
        if (GameManager.Instance != null)
        {
            Debug.Log("[StartGame] ▶ GameManager.ResetGameState 호출");
            GameManager.Instance.ResetGameState();
        }
        else
        {
            Debug.LogWarning("[StartGame] GameManager.Instance == null (StartScene에 GameManager가 없을 수 있음)");
        }

        StartCoroutine(FadeOutAndLoad());
    }

    // Credits 버튼 (크레딧 패널 열기)
    public void OnClickOpenCredits()
    {
        PlayConfirmSfx();  // 크레딧 선택 시에도 선택 사운드 재생

        // 패널 보이기
        if (creditsPanel != null)
            creditsPanel.SetActive(true);

        // 메인 메뉴 숨기기
        if (mainMenuGroup != null)
            mainMenuGroup.SetActive(false);

        // 크레딧에서 숨겨야 할 오브젝트들(타이틀, 힌트, 엔터 힌트 등) 끄기
        if (hideOnCredits != null)
        {
            foreach (var go in hideOnCredits)
            {
                if (go != null) go.SetActive(false);
            }
        }

        // 크레딧 자동 스크롤 시작 (항상 맨 위에서부터)
        if (creditsScroll != null)
        {
            creditsScroll.verticalNormalizedPosition = 1f; // 1 = 맨 위
            if (creditsScrollRoutine != null)
                StopCoroutine(creditsScrollRoutine);
            creditsScrollRoutine = StartCoroutine(AutoScrollCredits());
        }
    }

    // 크레딧 패널에서 돌아가기 버튼 / ESC
    public void OnClickCloseCredits()
    {
        PlayConfirmSfx();  // Back 버튼 / ESC에도 사운드

        // 자동 스크롤 중지
        if (creditsScrollRoutine != null)
        {
            StopCoroutine(creditsScrollRoutine);
            creditsScrollRoutine = null;
        }

        // 패널 숨기기
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        // 메인 메뉴 다시 보이기
        if (mainMenuGroup != null)
            mainMenuGroup.SetActive(true);

        // 크레딧에서 숨겼던 오브젝트들 다시 켜기
        if (hideOnCredits != null)
        {
            foreach (var go in hideOnCredits)
            {
                if (go != null) go.SetActive(true);
            }
        }

        // 돌아왔을 때 선택/색상, EventSystem 포커스 복구
        ApplySelectionVisuals();
    }

    // Quit 버튼
    public void OnClickQuit()
    {
        PlayConfirmSfx();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ================= 키보드 메뉴 처리 =================

    void Update()
    {
        HandleKeyboardMenu();
    }

    void HandleKeyboardMenu()
    {
        if (isLoading) return;

        // 크레딧이 열려 있을 때는 ESC로만 닫기 허용
        if (creditsPanel != null && creditsPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnClickCloseCredits();
            }
            return;    // 메인 메뉴 입력은 모두 무시
        }

        // 메인 메뉴가 비활성화 상태면 입력 무시
        if (mainMenuGroup != null && !mainMenuGroup.activeSelf)
            return;

        if (menuButtons == null || menuButtons.Length == 0)
            return;

        int prevIndex = currentIndex;

        // 위로 이동 (W, ↑)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + menuButtons.Length) % menuButtons.Length;
        }
        // 아래로 이동 (S, ↓)
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % menuButtons.Length;
        }

        // 인덱스가 바뀌었으면 시각 효과 + 이동 효과음
        if (prevIndex != currentIndex)
        {
            PlayMoveSfx();
            ApplySelectionVisuals();
        }

        // Enter / Space 로 현재 선택 항목 실행
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            ActivateCurrentMenu();
        }
    }

    void ApplySelectionVisuals()
    {
        // 1) 텍스트 색 처리
        if (menuTextColors != null)
        {
            for (int i = 0; i < menuTextColors.Length; i++)
            {
                var btc = menuTextColors[i];
                if (btc == null) continue;

                if (i == currentIndex)
                    btc.SetSelectedByKeyboard();
                else
                    btc.SetNormalByKeyboard();
            }
        }

        // 2) EventSystem의 선택 대상도 같이 맞춰주기
        if (menuButtons != null &&
            currentIndex >= 0 && currentIndex < menuButtons.Length)
        {
            var btn = menuButtons[currentIndex];
            if (btn != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
            }
        }
    }

    void ActivateCurrentMenu()
    {
        if (menuButtons == null ||
            currentIndex < 0 || currentIndex >= menuButtons.Length)
            return;

        var btn = menuButtons[currentIndex];
        if (btn != null)
        {
            // 버튼 OnClick() 이벤트 호출
            btn.onClick.Invoke();
        }
    }

    // ================= 크레딧 자동 스크롤 =================

    IEnumerator AutoScrollCredits()
    {
        if (creditsScroll == null)
            yield break;

        float t = 0f;

        // verticalNormalizedPosition:
        // 1 = 맨 위, 0 = 맨 아래
        while (t < creditsScrollTime)
        {
            t += Time.unscaledDeltaTime;   // 타임스케일 영향 X
            float normalized = Mathf.Lerp(1f, 0f, t / creditsScrollTime);
            creditsScroll.verticalNormalizedPosition = normalized;
            yield return null;
        }

        creditsScroll.verticalNormalizedPosition = 0f;
    }

    // ================= SFX =================

    void PlayMoveSfx()
    {
        if (uiAudioSource != null && moveClip != null)
            uiAudioSource.PlayOneShot(moveClip);
    }

    void PlayConfirmSfx()
    {
        if (uiAudioSource != null && confirmClip != null)
            uiAudioSource.PlayOneShot(confirmClip);
    }
}
