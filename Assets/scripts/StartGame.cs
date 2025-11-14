using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StartGame : MonoBehaviour
{
    [Header("씬 전환")]
    [Tooltip("실제 플레이(메인 게임) 씬 이름.")]
    public string gameplaySceneName = "GameScene";

    [Header("UI 참조")]
    public Image fadeImage;         // 전체 검정 이미지 (Canvas 안 전체 화면 Image)
    public Image backgroundImage;   // 배경
    public TMP_Text titleText;      // 타이틀 글자
    public GameObject titleGroup;   // 타이틀 묶음
    public GameObject mainMenuGroup;// 버튼 묶음 (Start / Credits / Quit)
    public GameObject creditsPanel; // 크레딧 패널 (DimBackground + 텍스트 + Back)

    [Header("타이틀 효과")]
    public string fullTitle = "잃어버린 출구";
    public float typingSpeed = 0.1f;

    [Header("오디오")]
    public AudioSource bgmSource;
    public bool playBgmOnStart = true;

    private Coroutine typingCoroutine;

    void Start()
    {
        // 1) 스타트 화면에서는 커서 보이게 / 잠금 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2) 페이드 이미지 초기화 (투명하게 시작)
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true); // 꺼져있어도 켜기
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
        }

        // 3) 그룹 기본 상태
        if (titleGroup != null) titleGroup.SetActive(true);
        if (mainMenuGroup != null) mainMenuGroup.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // 4) 배경
        if (backgroundImage != null)
            backgroundImage.enabled = true;

        // 5) BGM
        if (bgmSource != null && playBgmOnStart && !bgmSource.isPlaying)
        {
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // 6) 타이틀 타자 효과
        if (titleText != null && !string.IsNullOrEmpty(fullTitle))
        {
            titleText.text = "";
            typingCoroutine = StartCoroutine(TypeTitle());
        }
    }

    IEnumerator TypeTitle()
    {
        if (titleText == null) yield break;

        titleText.text = "";

        foreach (char c in fullTitle)
        {
            titleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    // =====================
    // 버튼 핸들러
    // =====================

    public void OnClickStartGame()
    {
        StartCoroutine(CoStartGame());
    }

    private IEnumerator CoStartGame()
    {
        // 1) 화면 페이드아웃
        if (fadeImage != null)
        {
            Color startColor = fadeImage.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

            float t = 0f;
            float duration = 0.5f;

            if (!fadeImage.gameObject.activeSelf)
                fadeImage.gameObject.SetActive(true);

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                fadeImage.color = Color.Lerp(startColor, endColor, k);
                yield return null;
            }

            fadeImage.color = endColor;
        }

        // 2) 실제 게임 씬 로드
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnClickOpenCredits()
    {
        if (mainMenuGroup != null) mainMenuGroup.SetActive(false);
        if (titleGroup != null) titleGroup.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
        // creditsPanel 안에 DimBackground(반투명 검은색 전체 Image)가 있다면
        // 그게 자동으로 뒤를 덮어서 어두워질 거야. 코드 수정은 불필요.
    }

    public void OnClickCloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainMenuGroup != null) mainMenuGroup.SetActive(true);
        if (titleGroup != null) titleGroup.SetActive(true);
    }

    public void OnClickQuit()
    {
        Debug.Log("[StartGame] Quit Game 요청");
        Application.Quit();
    }
}
