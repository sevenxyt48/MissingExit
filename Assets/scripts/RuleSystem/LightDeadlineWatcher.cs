// LightDeadlineWatcher.cs (교체)
using UnityEngine;

public class LightDeadlineWatcher : MonoBehaviour
{
    [Header("활성 조건")]
    public string onlyInRoom = "2-2";
    public float startupGrace = 2f;

    [Header("전등")]
    public SimpleLamp lamp;            // ← 강타입
    public float deadlineSeconds = 3f; // 켜진 뒤 N초 안에 꺼야 함
    public float repeatWhileOnInterval = 0f;

    [Header("Penalty")]
    public string violationReason = "전등 규칙 위반";
    [TextArea] public string penaltyText = "불을 즉시 꺼라.";

    float enableAt, openAt, nextRepeatAt;
    bool prevOn, violatedThisOn;

    void OnValidate() { if (!lamp) lamp = GetComponentInChildren<SimpleLamp>(true); }
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
        if (!lamp || !Active()) return;

        bool on = lamp.IsOn;
        if (on && !prevOn) { openAt = Time.time; violatedThisOn = false; nextRepeatAt = 0f; }

        if (on)
        {
            if (!violatedThisOn && Time.time - openAt >= Mathf.Max(0.01f, deadlineSeconds))
            {
                PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText);
                violatedThisOn = true;
                if (repeatWhileOnInterval > 0f) nextRepeatAt = Time.time + repeatWhileOnInterval;
            }
            else if (violatedThisOn && repeatWhileOnInterval > 0f && Time.time >= nextRepeatAt)
            {
                PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText);
                nextRepeatAt = Time.time + repeatWhileOnInterval;
            }
        }
        else { openAt = 0f; violatedThisOn = false; nextRepeatAt = 0f; }

        prevOn = on;
    }
}
