using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingScreen : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text endingText;
    public Button restartButton;          // 있어도 되고, 없어도 됩니다
    public RectTransform creditsRoot;     // Good 전용(선택)
    public Image blackBG;                 // 선택

    [Header("타자기")]
    public bool useTypewriter = true;
    public float cps = 9f;
    public float jitter = 0.02f;
    public float commaPause = 0.15f;
    public float sentencePause = 0.35f;
    public float longPause = 0.6f;

    [Header("텍스트 페이드아웃")]
    public bool fadeOutEndingText = true;
    public float fadeOutDelay = 0.6f;
    public float fadeOutDuration = 1.2f;

    [Header("크레딧 (Good)")]
    public float creditsStartY = -600f;
    public float creditsEndY = 900f;
    public float creditsSpeed = 60f;
    public float creditsDelayAfterText = 0.8f;

    [Header("씬 전환")]
    public string startSceneName = "StartScene";      // Good 엔딩 후 이동
    public float loadDelayAfterCredits = 0.5f;
    public string restartTargetSceneName = "StartScene"; // 🔴 Bad 엔딩: 화면 클릭 시 이동

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

    private void Awake()
    {
        if (restartButton)
        {
            restartButton.gameObject.SetActive(false);          // 시작엔 숨김
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(LoadStartScene);
        }
        if (creditsRoot) creditsRoot.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (blackBG) blackBG.enabled = true;
        if (endingText) endingText.text = "";
        if (creditsRoot)
        {
            creditsRoot.gameObject.SetActive(false);
            creditsRoot.anchoredPosition = new Vector2(creditsRoot.anchoredPosition.x, creditsStartY);
        }

        // GameManager → Resources.FindObjectsOfTypeAll → PlayerPrefs 순서로 판단
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

        StartCoroutine(RunSequence());
    }

    private void Update()
    {
        if (!isBad || !readyToExit || loading) return;

        // 화면 아무 곳이나 클릭(또는 아무 키) → 즉시 StartScene으로
        if (Input.GetMouseButtonDown(0) || Input.anyKeyDown || TouchBegan())
        {
            LoadStartScene();
        }
    }

    private bool TouchBegan()
    {
        return (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    private IEnumerator RunSequence()
    {
        // 1) 텍스트 출력
        yield return StartCoroutine(Typewriter(fullText));

        // 2) 페이드아웃(옵션)
        if (fadeOutEndingText)
            yield return StartCoroutine(FadeOutText(endingText, fadeOutDelay, fadeOutDuration));

        if (isBad)
        {
            // 이제부터 클릭하면 바로 이동 가능
            readyToExit = true;

            // 버튼은 “안내용”으로만 노출(있어도 되고 없어도 됨)
            if (restartButton && !restartButton.gameObject.activeSelf)
                restartButton.gameObject.SetActive(true);
        }
        else
        {
            // Good: 크레딧 → 다음 씬
            if (creditsDelayAfterText > 0f)
                yield return new WaitForSecondsRealtime(creditsDelayAfterText);

            if (creditsRoot)
            {
                creditsRoot.gameObject.SetActive(true);
                yield return StartCoroutine(ScrollCredits());
            }

            if (!string.IsNullOrEmpty(startSceneName))
            {
                if (loadDelayAfterCredits > 0f)
                    yield return new WaitForSecondsRealtime(loadDelayAfterCredits);
                LoadScene(startSceneName);
            }
        }
    }

    private IEnumerator Typewriter(string text)
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
            if (Input.GetMouseButtonDown(0)) // 클릭 시 즉시 완료
            {
                endingText.text = text;
                break;
            }

            char c = text[i];
            endingText.text += c;

            float delay = baseDelay + Random.Range(-jitter, jitter);
            if (delay < 0.01f) delay = 0.01f;

            bool isEllipsis = (c == '…') || (c == '.' && i + 2 < text.Length && text[i + 1] == '.' && text[i + 2] == '.');
            if (isEllipsis)
            {
                if (c == '.') { endingText.text += ".."; i += 2; }
                delay += longPause;
            }
            else if (c == '\n' || c == '\r') delay += longPause;
            else if (c == '.' || c == '!' || c == '?') delay += sentencePause;
            else if (c == ',' || c == ';' || c == ':') delay += commaPause;

            yield return new WaitForSecondsRealtime(delay);
        }
    }

    private IEnumerator FadeOutText(TMP_Text txt, float delay, float duration)
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

    private IEnumerator ScrollCredits()
    {
        if (!creditsRoot) yield break;

        var pos = creditsRoot.anchoredPosition;
        pos.y = creditsStartY;
        creditsRoot.anchoredPosition = pos;

        while (creditsRoot.anchoredPosition.y < creditsEndY)
        {
            creditsRoot.anchoredPosition += Vector2.up * (creditsSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
    }

    // 버튼 클릭 또는 화면 클릭 시 호출
    private void LoadStartScene()
    {
        if (loading) return;
        loading = true;
        Time.timeScale = 1f;

        var target = string.IsNullOrEmpty(restartTargetSceneName) ? "StartScene" : restartTargetSceneName;
        Debug.Log($"[EndingScreen] Restart → Load '{target}'");
        LoadScene(target);
    }

    private void LoadScene(string name)
    {
        SceneManager.LoadScene(name, LoadSceneMode.Single);
    }
}
