using UnityEngine;

/// <summary>
/// 창문이 열린 뒤 deadlineSeconds 안에 닫지 않으면 위반.
/// 원샷 스팅어는 PenaltyManager 스타일로, (선택) 지속 루프는 여기서 켜고 끈다.
/// </summary>
public class WindowDeadlineWatcher : MonoBehaviour
{
    [Header("활성 조건")]
    public string onlyInRoom = "2-3";
    public float startupGrace = 2f;

    [Header("대상 창문")]
    public SimpleWindow window;               // 강타입
    public float deadlineSeconds = 3f;
    public float repeatWhileOpenInterval = 0f; // 0=세션당 1회만

    [Header("Penalty")]
    public string violationReason = "창문 규칙 위반";
    [TextArea] public string penaltyText = "창문을 즉시 닫아라.";

    [Header("사운드(선택: 창문 닫힐 때까지 유지)")]
    public bool startSustainOnViolation = false;
    public AudioClip sustainLoopClip;         // 바람/초저주파 드론 등
    [Range(0f, 1f)] public float sustainVolume = 0.8f;

    float enableAt;
    bool prevOpen, violatedThisOpen, sustaining;
    float openAt, nextRepeatAt;

    void OnValidate() { if (!window) window = GetComponentInChildren<SimpleWindow>(true); }
    void Awake() { enableAt = Time.time; }

    bool ActiveNow()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return false;

        if (Time.time - enableAt < startupGrace) return false;
        var gm = GameManager.Instance;
        if (gm && !string.IsNullOrEmpty(onlyInRoom) && gm.CurrentRoomId != onlyInRoom) return false;
        return true;
    }

    void Update()
    {
        if (!window) return;

        if (!ActiveNow())
        {
            StopSustainIfNeeded();
            return;
        }

        bool open = window.IsOpen;

        if (open && !prevOpen)
        {
            openAt = Time.time;
            violatedThisOpen = false;
            nextRepeatAt = 0f;
        }

        if (open)
        {
            if (!violatedThisOpen && (Time.time - openAt) >= Mathf.Max(0.01f, deadlineSeconds))
            {
                // 1) 원샷 스팅어/플래시는 스타일에 따라 ApplyPenalty로 재생
                PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText);

                violatedThisOpen = true;

                // 2) (선택) 지속 루프 시작 — 창문 위치로 앵커링
                if (startSustainOnViolation && sustainLoopClip)
                {
                    PenaltyManager.Instance?.StartSustain(violationReason, sustainLoopClip, sustainVolume, window.transform);
                    sustaining = true;
                }

                if (repeatWhileOpenInterval > 0f)
                    nextRepeatAt = Time.time + repeatWhileOpenInterval;
            }
            else if (violatedThisOpen && repeatWhileOpenInterval > 0f && Time.time >= nextRepeatAt)
            {
                PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText);
                nextRepeatAt = Time.time + repeatWhileOpenInterval;
            }
        }
        else
        {
            // 창문이 닫히면 타이머 리셋 + 지속음 정지
            openAt = 0f; violatedThisOpen = false; nextRepeatAt = 0f;
            StopSustainIfNeeded();
        }

        prevOpen = open;
    }

    void OnDisable() { StopSustainIfNeeded(); }

    void StopSustainIfNeeded()
    {
        if (sustaining)
        {
            PenaltyManager.Instance?.StopSustain(violationReason);
            sustaining = false;
        }
    }
}
