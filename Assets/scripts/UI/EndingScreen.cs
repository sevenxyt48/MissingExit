using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingScreen : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text endingText;
    public Button restartButton;          // Bad 엔딩에서만 쓸 수 있음 (선택)
    public Image blackBG;                 // 화면 덮는 어두운 배경 (선택)

    [Header("타자기")]
    public bool useTypewriter = true;
    public float cps = 9f;                // characters per second
    public float jitter = 0.02f;
    public float commaPause = 0.15f;
    public float sentencePause = 0.35f;
    public float longPause = 0.6f;

    [Header("텍스트 페이드아웃")]
    public bool fadeOutEndingText = true;
    public float fadeOutDelay = 0.6f;
    public float fadeOutDuration = 1.2f;

    [Header("씬 전환")]
    [Tooltip("엔딩 후 돌아갈 스타트(타이틀) 씬 이름")]
    public string startSceneName = "StartScene";
    [Tooltip("good 엔딩일 때 자동으로 스타트씬 가기 전 기다리는 시간 (초)")]
    public float autoReturnDelayAfterGood = 1.0f;
    [Tooltip("bad 엔딩: 플레이어가 클릭했을 때 이동할 씬 이름 (기본 스타트씬과 같게 써도 됨)")]
    public string restartTargetSceneName = "StartScene";

    [Header("폴백(예비)")]
    [TextArea]
    public string fallbackGoodText =
        "“드디어 모든 진실과 마주한다. 과거의 자신과 피해자의 그림자가 겹치며, 해방감을 느낀다.”";
    [TextArea]
    public string fallbackBadText =
        "“누군가 회피하면, 진실의 방 문이 닫히고 모든 기억이 재생된다.”\n게임 리셋 알림, 공동 책임 강화.";

    // 내부 상태
    private string fullText;
    private bool isBad;
    private bool readyToExit = false;   // Bad 엔딩에서 텍스트가 끝난 뒤에만 true
    private bool loading = false;       // 중복 로드 방지

    void Awake()
    {
        // 재시작 버튼은 Bad 전용으로만 쓸 거라서 초기엔 숨겨둠
        if (restartButton)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(LoadStartScene);
        }
    }

    void Start()
    {
        // 배경 보이게
        if (blackBG) blackBG.enabled = true;

        // 텍스트 초기화
        if (endingText) endingText.text = "";

        // GameManager에서 엔딩 타입/문구 가져오기
        var gm = GameManager.Instance;
        if (gm == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameManager>();
            if (all != null && all.Length > 0) gm = all[0];
        }

        if (gm != null)
        {
            isBad = (gm.LastEnding == GameManager.EndingType.Bad);
            fullText = isBad ? gm.badEndingText : gm.goodEndingText;
            Debug.Log($"[EndingScreen] Source=GM, LastEnding={gm.LastEnding}, UseBad={isBad}");
        }
        else
        {
            var persisted = GameManager.ReadPersistedLastEnding();
            isBad = (persisted == GameManager.EndingType.Bad);
            fullText = isBad ? fallbackBadText : fallbackGoodText;
            Debug.Log($"[EndingScreen] Source=PlayerPrefs, LastEnding={persisted}, UseBad={isBad}");
        }

        // 전체 연출 실행
        StartCoroutine(RunSequence());
    }

    void Update()
    {
        // 나쁜 엔딩(bad) 모드일 때:
        // 텍스트 출력 및 페이드가 다 끝나고 readyToExit = true 된 이후에
        // 아무 키나 마우스 클릭하면 바로 타이틀로 이동
        if (!isBad || !readyToExit || loading) return;

        if (Input.GetMouseButtonDown(0) || Input.anyKeyDown || TouchBegan())
        {
            LoadStartScene();
        }
    }

    bool TouchBegan()
    {
        return (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    IEnumerator RunSequence()
    {
        // 1) 엔딩 텍스트를 타자기 효과로 출력
        yield return StartCoroutine(Typewriter(fullText));

        // 2) 텍스트 페이드아웃 (옵션)
        if (fadeOutEndingText)
            yield return StartCoroutine(FadeOutText(endingText, fadeOutDelay, fadeOutDuration));

        // 3) 분기
        if (isBad)
        {
            // Bad 엔딩:
            // - 바로 씬 전환하지 않고
            // - 플레이어 입력을 기다린다.
            readyToExit = true;

            if (restartButton && !restartButton.gameObject.activeSelf)
                restartButton.gameObject.SetActive(true);
        }
        else
        {
            // Good 엔딩:
            // - 크레딧 없이 자동으로 스타트씬으로 복귀
            if (!string.IsNullOrEmpty(startSceneName))
            {
                if (autoReturnDelayAfterGood > 0f)
                    yield return new WaitForSecondsRealtime(autoReturnDelayAfterGood);

                LoadScene(startSceneName);
            }
        }
    }

    IEnumerator Typewriter(string text)
    {
        if (!useTypewriter || endingText == null)
        {
            if (endingText) endingText.text = text;
            yield break;
        }

        endingText.text = "";
        float baseDelay = 1f / Mathf.Max(0.001f, cps);

        for (int i = 0; i < text.Length; i++)
        {
            // 마우스 클릭 시 전체 스킵
            if (Input.GetMouseButtonDown(0))
            {
                endingText.text = text;
                break;
            }

            char c = text[i];
            endingText.text += c;

            float delay = baseDelay + Random.Range(-jitter, jitter);
            if (delay < 0.01f) delay = 0.01f;

            // 말줄임표 (...) / … 처리
            bool isEllipsis =
                (c == '…') ||
                (c == '.' && i + 2 < text.Length && text[i + 1] == '.' && text[i + 2] == '.');

            if (isEllipsis)
            {
                if (c == '.')
                {
                    endingText.text += "..";
                    i += 2;
                }
                delay += longPause;
            }
            else if (c == '\n' || c == '\r')
            {
                delay += longPause;
            }
            else if (c == '.' || c == '!' || c == '?')
            {
                delay += sentencePause;
            }
            else if (c == ',' || c == ';' || c == ':')
            {
                delay += commaPause;
            }

            yield return new WaitForSecondsRealtime(delay);
        }
    }

    IEnumerator FadeOutText(TMP_Text txt, float delay, float duration)
    {
        if (txt == null) yield break;
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        Color start = txt.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            txt.color = new Color(start.r, start.g, start.b, start.a * k);
            yield return null;
        }
        txt.color = new Color(start.r, start.g, start.b, 0f);
    }

    // Bad 엔딩에서 클릭(또는 재시작 버튼)으로 호출됨
    void LoadStartScene()
    {
        if (loading) return;
        loading = true;
        Time.timeScale = 1f;

        // restartTargetSceneName이 비어있으면 startSceneName으로
        var target = string.IsNullOrEmpty(restartTargetSceneName)
            ? startSceneName
            : restartTargetSceneName;

        Debug.Log($"[EndingScreen] Restart → Load '{target}'");
        LoadScene(target);
    }

    void LoadScene(string name)
    {
        SceneManager.LoadScene(name, LoadSceneMode.Single);
    }
}
