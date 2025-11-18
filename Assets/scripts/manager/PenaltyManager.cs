using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PenaltyManager : MonoBehaviour
{
    public static PenaltyManager Instance;

    [Header("참조(References)")]
    public Image screenFlash;
    public AudioSource audioSource;
    public TMP_Text popupText;
    public CanvasGroup popupGroup;
    public Camera cameraToShake;

    [Header("플래시(Flash)")]
    public Color flashColor = Color.white;
    [Range(0f, 1f)] public float flashMaxAlpha = 0.55f;
    public float flashDuration = 0.3f;

    [Header("팝업(Popup)")]
    public bool useTypewriter = true;
    public float popupCps = 22f;
    public float popupHold = 1.25f;
    public float popupFade = 0.25f;

    [Header("카메라 흔들림(Shake)")]
    public float shakeDuration = 0.2f;
    public float shakeAmount = 0.05f;

    [Header("사운드(SFX)")]
    public AudioClip defaultPenaltyClip;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("글로벌 쿨다운")]
    [Tooltip("모든 연출 사이 최소 간격(초). 비주얼 스팸 방지용")]
    public float minInterval = 0.8f;

    [Header("Reason별 재발 쿨다운")]
    [Tooltip("같은 규칙(reason)이 다시 위반으로 판정되려면 필요한 최소 시간(초)")]
    public float defaultRepeatCooldown = 4f;

    [System.Serializable] public class ReasonCooldown { public string reasonKey; public float seconds = 4f; }
    [Tooltip("특정 규칙만 재발 쿨다운을 덮어쓰고 싶다면 여기서 지정")]
    public ReasonCooldown[] repeatCooldowns;

    [Header("기타")]
    public bool useUnscaledTime = true;
    public bool debugLog = true;

    float cooldown;
    Coroutine flashCo, popupCo, shakeCo;

    // reason별 쿨다운 테이블
    readonly Dictionary<string, float> reasonBlockUntil = new();

    [System.Serializable]
    public class PenaltyStyle
    {
        public string reasonKey;                // "간섭 금지", "라디오 규칙 위반", ...
        [TextArea] public string messageOverride;
        public AudioClip sfx;
        public Color flashColor = Color.white;
        [Range(0f, 1f)] public float flashAlpha = 0.55f;
        [Range(0f, 3f)] public float intensity = 1f;
        public bool countViolation = true;     // false면 카운트하지 않고 안내만
    }
    [Header("규칙별 스타일(선택)")]
    public PenaltyStyle[] styles;

    float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

    public AudioSource sustainSource;           // Loop 전용
    string sustainingReason;

    // 시작/종료 메서드
    public void StartSustain(string reason, AudioClip clip, float vol = 0.8f)
    {
        if (!sustainSource || !clip) return;
        sustainingReason = reason;
        sustainSource.Stop();
        sustainSource.clip = clip;
        sustainSource.volume = vol;
        sustainSource.loop = true;
        sustainSource.Play();
    }

    public void StopSustain(string reason)
    {
        if (!sustainSource) return;
        if (sustainingReason == reason) { sustainSource.Stop(); sustainSource.clip = null; sustainingReason = null; }
    }

    // 지속 루프 시작(앵커 위치로 재생)
    public void StartSustain(string reason, AudioClip clip, float vol, Transform anchor)
    {
        if (!sustainSource || !clip) return;
        sustainingReason = reason;
        sustainSource.Stop();
        sustainSource.clip = clip;
        sustainSource.volume = vol;
        sustainSource.loop = true;
        if (anchor) sustainSource.transform.position = anchor.position; // 3D 위치 고정
        sustainSource.Play();
    }


    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        if (!cameraToShake) cameraToShake = Camera.main;
        if (popupText && !popupGroup)
        {
            popupGroup = popupText.GetComponent<CanvasGroup>();
            if (!popupGroup) popupGroup = popupText.gameObject.AddComponent<CanvasGroup>();
        }

        SafeResetVisuals();

        if (audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.clip = null;
        }
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (cooldown > 0f) cooldown -= dt;
    }

    float GetRepeatCooldown(string reason)
    {
        if (repeatCooldowns != null)
        {
            for (int i = 0; i < repeatCooldowns.Length; i++)
            {
                var rc = repeatCooldowns[i];
                if (rc != null && rc.reasonKey == reason) return Mathf.Max(0f, rc.seconds);
            }
        }
        return Mathf.Max(0f, defaultRepeatCooldown);
    }

    bool IsBlockedByReasonCooldown(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return false;
        if (reasonBlockUntil.TryGetValue(reason, out var until))
            return Now < until;
        return false;
    }

    void ArmReasonCooldown(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return;
        reasonBlockUntil[reason] = Now + GetRepeatCooldown(reason);
    }

    /// <summary>
    /// 벌칙/안내 재생.
    /// </summary>
    public void ApplyPenalty(
        string reason,
        string message = null,
        AudioClip sfxOverride = null,
        float intensity = 1f,
        bool countViolation = true,
        bool ignoreClueGrace = false,       // ★ 추가
        bool ignoreStartupGrace = false,    // ★ 추가
        bool ignoreRoomEnterGrace = false   // ★ 추가
    )
    {
        // 같은 reason 재발 쿨다운
        if (IsBlockedByReasonCooldown(reason))
        {
            if (debugLog) Debug.Log($"[PenaltyManager] reason cooldown 차단: {reason}");
            return;
        }

        // 글로벌 최소 간격
        if (cooldown > 0f) return;

        // 카운트 방식이면 먼저 GM에 질의
        if (countViolation && GameManager.Instance != null)
        {
            bool accepted = GameManager.Instance.TryReportViolation(
                reason, ignoreClueGrace, ignoreStartupGrace, ignoreRoomEnterGrace);

            if (!accepted)
            {
                SafeResetVisuals();
                if (debugLog) Debug.Log($"[PenaltyManager] 연출 차단: (유예/엔딩 등) — {reason}");
                return;
            }
        }
        else
        {
            if (debugLog) Debug.Log($"[PenaltyManager] 안내 표시(노카운트): {reason}");
        }

        // 쿨다운 무장
        cooldown = minInterval;
        if (countViolation) ArmReasonCooldown(reason);

        // 스타일 매칭
        PenaltyStyle style = null;
        if (!string.IsNullOrEmpty(reason) && styles != null)
        {
            foreach (var s in styles)
                if (s != null && s.reasonKey == reason) { style = s; break; }
        }
        if (style != null)
        {
            if (string.IsNullOrEmpty(message)) message = style.messageOverride;
            if (!sfxOverride) sfxOverride = style.sfx;
            intensity *= style.intensity;
            countViolation = countViolation && style.countViolation;
        }

        // 사운드
        var clip = sfxOverride ? sfxOverride : defaultPenaltyClip;
        if (audioSource && clip) audioSource.PlayOneShot(clip, sfxVolume);

        // 플래시
        Color useColor = (style != null) ? style.flashColor : flashColor;
        float useAlpha = (style != null) ? style.flashAlpha : flashMaxAlpha;
        if (screenFlash)
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(CoFlash(useColor, useAlpha, intensity));
        }

        // 팝업
        if (popupText && !string.IsNullOrEmpty(message))
        {
            if (popupCo != null) StopCoroutine(popupCo);
            popupCo = StartCoroutine(CoPopup(message));
        }

        // 카메라 흔들림
        if (cameraToShake && shakeAmount > 0f && shakeDuration > 0f)
        {
            if (shakeCo != null) StopCoroutine(shakeCo);
            shakeCo = StartCoroutine(CoShake(intensity));
        }
    }

    // --- 안전 리셋(차단/Awake 시 호출) ---
    void SafeResetVisuals()
    {
        if (screenFlash)
        {
            screenFlash.enabled = false;
            var c = flashColor; c.a = 0f; screenFlash.color = c;
        }
        if (popupGroup)
        {
            popupGroup.alpha = 0f;
            popupGroup.gameObject.SetActive(true);
        }
    }

    IEnumerator CoFlash(Color color, float maxAlpha, float intensity)
    {
        screenFlash.enabled = true;
        Color c = color;
        float half = flashDuration * 0.5f;
        float peak = Mathf.Clamp01(maxAlpha * Mathf.Clamp01(intensity));

        float t = 0f;
        while (t < half)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            c.a = Mathf.Lerp(0f, peak, t / half); screenFlash.color = c; yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            c.a = Mathf.Lerp(peak, 0f, t / half); screenFlash.color = c; yield return null;
        }
        screenFlash.enabled = false;
    }

    IEnumerator CoFlash(float intensity) { yield return CoFlash(flashColor, flashMaxAlpha, intensity); }

    IEnumerator CoPopup(string msg)
    {
        popupGroup.gameObject.SetActive(true);

        float t = 0f;
        while (t < popupFade)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            popupGroup.alpha = Mathf.Lerp(0f, 1f, t / popupFade);
            yield return null;
        }
        popupGroup.alpha = 1f;

        if (useTypewriter) yield return StartCoroutine(CoType(msg));
        else popupText.text = msg;

        if (popupHold > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(popupHold);
            else yield return new WaitForSeconds(popupHold);
        }

        t = 0f;
        while (t < popupFade)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            popupGroup.alpha = Mathf.Lerp(1f, 0f, t / popupFade);
            yield return null;
        }
        popupGroup.alpha = 0f;
        popupGroup.gameObject.SetActive(true);
    }

    IEnumerator CoType(string full)
    {
        popupText.text = "";
        float baseDelay = 1f / Mathf.Max(0.001f, popupCps);

        for (int i = 0; i < full.Length; i++)
        {
            popupText.text += full[i];

            float delay = baseDelay;
            char c = full[i];
            if (c == '.' || c == '!' || c == '?') delay += 0.15f;
            else if (c == ',' || c == ';' || c == ':') delay += 0.07f;
            else if (c == '\n') delay += 0.2f;

            if (useUnscaledTime) yield return new WaitForSecondsRealtime(delay);
            else yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator CoShake(float intensity)
    {
        Vector3 origin = cameraToShake.transform.localPosition;
        float t = 0f;
        float amp = shakeAmount * Mathf.Clamp01(intensity);

        while (t < shakeDuration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            cameraToShake.transform.localPosition = origin + Random.insideUnitSphere * amp;
            yield return null;
        }
        cameraToShake.transform.localPosition = origin;
    }
}
