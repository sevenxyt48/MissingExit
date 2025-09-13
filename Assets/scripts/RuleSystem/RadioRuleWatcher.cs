// RadioRuleWatcher.cs (교체)
using UnityEngine;

public class RadioRuleWatcher : MonoBehaviour
{
    [Header("활성 조건")]
    public string onlyInRoom = "2-1";
    public float startupGrace = 0f;

    [Header("라디오")]
    public SimpleRadio radio;          // ← 강타입
    public float graceSeconds = 6f;    // 켜진 채 N초 유지 시 위반

    [Header("Penalty")]
    public string violationReason = "라디오 규칙 위반";
    [TextArea] public string penaltyText = "라디오를 즉시 꺼라.";

    float enableAt;
    bool prevOn;
    float onStart;
    bool violatedThisOn;

    void OnValidate() { if (!radio) radio = GetComponentInChildren<SimpleRadio>(true); }
    void Awake() { enableAt = Time.time; }

    bool Active()
    {
        if (Time.time - enableAt < startupGrace) return false;
        var gm = GameManager.Instance;
        if (gm && !string.IsNullOrEmpty(onlyInRoom) && gm.CurrentRoomId != onlyInRoom) return false;
        return true;
    }

    void Update()
    {
        if (!radio || !Active()) return;

        bool on = radio.IsOn;
        if (on && !prevOn) { onStart = Time.time; violatedThisOn = false; }

        if (on)
        {
            if (!violatedThisOn && Time.time - onStart >= Mathf.Max(0.01f, graceSeconds))
            {
                PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText);
                violatedThisOn = true;
            }
        }
        else { onStart = 0f; violatedThisOn = false; }

        prevOn = on;
    }
}
