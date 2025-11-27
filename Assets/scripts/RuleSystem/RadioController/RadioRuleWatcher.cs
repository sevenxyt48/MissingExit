using UnityEngine;

public class RadioRuleWatcher : MonoBehaviour
{
    [Header("활성 조건")]
    public string onlyInRoom = "2-1";
    public float startupGrace = 0f;

    [Header("라디오")]
    public SimpleRadio radio;              // 강타입 참조
    [Tooltip("라디오가 켜진 채 이 시간(초) 유지되면 위반")]
    public float graceSeconds = 6f;

    [Header("지속 사운드(선택)")]
    public bool startSustainOnViolation = true;
    public AudioClip sustainLoopClip;      // 위반 후 계속 들릴 루프
    [Range(0f, 1f)] public float sustainVolume = 0.8f;

    [Header("Penalty")]
    public string violationReason = "라디오 규칙 위반";
    [TextArea] public string penaltyText = "라디오를 즉시 꺼라.";

    float enableAt;
    bool prevOn;
    float onStart;
    bool violatedThisOn;
    bool sustaining;

    void OnValidate()
    {
        if (!radio) radio = GetComponentInChildren<SimpleRadio>(true);
    }

    void Awake()
    {
        enableAt = Time.time;
    }

    bool Active()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return false;

        if (Time.time - enableAt < startupGrace) return false;
        var gm = GameManager.Instance;
        if (gm && !string.IsNullOrEmpty(onlyInRoom) && gm.CurrentRoomId != onlyInRoom) return false;
        return true;
    }

    void Update()
    {
        if (!radio || !Active())
        {
            // 방을 벗어나면 안전하게 지속음을 끈다
            if (sustaining) { PenaltyManager.Instance?.StopSustain(violationReason); sustaining = false; }
            return;
        }

        bool on = radio.IsOn;

        if (on && !prevOn)
        {
            onStart = Time.time;
            violatedThisOn = false;
        }

        if (on)
        {
            if (!violatedThisOn && Time.time - onStart >= Mathf.Max(0.01f, graceSeconds))
            {
                PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText);
                violatedThisOn = true;

                if (startSustainOnViolation && sustainLoopClip)
                {
                    PenaltyManager.Instance?.StartSustain(violationReason, sustainLoopClip, sustainVolume);
                    sustaining = true;
                }
            }
        }
        else
        {
            // 라디오가 꺼지면 지속음을 정지(이중보호: SimpleRadio에서도 정지 호출함)
            if (sustaining) { PenaltyManager.Instance?.StopSustain(violationReason); sustaining = false; }
            onStart = 0f;
            violatedThisOn = false;
        }

        prevOn = on;
    }

    void OnDisable()
    {
        if (sustaining) { PenaltyManager.Instance?.StopSustain(violationReason); sustaining = false; }
    }
}
