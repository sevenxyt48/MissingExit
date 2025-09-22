using UnityEngine;
using TMPro;
using System.Collections;

public class GuidanceToast : MonoBehaviour
{
    public static GuidanceToast Instance;

    [Header("Refs")]
    public CanvasGroup group;
    public TMP_Text text;
    public AudioSource sfx;          // ★ 유일한 힌트용 AudioSource

    [Header("Style/Timing")]
    public Color defaultColor = new Color(0.43f, 0.66f, 1f, 0.95f);
    public float fade = 0.25f;
    public float defaultSeconds = 3.5f;
    public bool useUnscaledTime = true;
    public AudioClip defaultClip;    // ★ 기본 힌트 사운드(선택)

    Coroutine running;

    void Reset() { group = GetComponent<CanvasGroup>(); text = GetComponentInChildren<TMP_Text>(true); sfx = GetComponent<AudioSource>(); }
    void Awake()
    {
        Instance = this;
        if (!group) group = GetComponent<CanvasGroup>();
        if (!text) text = GetComponentInChildren<TMP_Text>(true);
        if (!sfx) sfx = GetComponent<AudioSource>();
        if (group) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (text) text.color = defaultColor;
    }

    public void Show(string msg, Color? color = null, float? seconds = null, AudioClip clip = null, float volume = 1f)
    {
        if (!group || !text) return;
        text.text = msg;
        text.color = color ?? defaultColor;

        var toPlay = clip ? clip : defaultClip;       // ★ 여기서만 재생
        if (toPlay && sfx) { sfx.volume = volume; sfx.PlayOneShot(toPlay); }

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Co_Show(seconds ?? defaultSeconds));
    }

    IEnumerator Co_Show(float seconds)
    {
        float t = 0; while (t < fade) { t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime); group.alpha = Mathf.Clamp01(t / fade); yield return null; }
        group.alpha = 1f;
        if (seconds > 0f) { if (useUnscaledTime) yield return new WaitForSecondsRealtime(seconds); else yield return new WaitForSeconds(seconds); }
        t = 0; while (t < fade) { t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime); group.alpha = 1f - Mathf.Clamp01(t / fade); yield return null; }
        group.alpha = 0f; running = null;
    }
}
