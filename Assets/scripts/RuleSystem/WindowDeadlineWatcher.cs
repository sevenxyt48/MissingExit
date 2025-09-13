// WindowDeadlineWatcher.cs
using UnityEngine;

/// <summary>
/// 창문이 열린 뒤 deadlineSeconds 안에 닫지 않으면 1회 위반.
/// 닫히면 타이머 리셋. repeatWhileOpenInterval>0이면 계속 열린 동안 주기적 경고.
/// </summary>
public class WindowDeadlineWatcher : MonoBehaviour
{
    [Header("활성 조건")]
    public string onlyInRoom = "2-3";
    public float startupGrace = 2f;

    [Header("대상 창문")]
    public SimpleWindow window;
    public float deadlineSeconds = 3f;
    public float repeatWhileOpenInterval = 0f; // 0=한 번만

    [Header("Penalty")]
    public string violationReason = "창문 규칙 위반";
    [TextArea] public string penaltyText = "창문을 즉시 닫아라.";

    float enableAt;
    bool prevOpen, violatedThisOpen;
    float openAt, nextRepeatAt;

    void Awake() { enableAt = Time.time; }

    bool ActiveNow()
    {
        if (Time.time - enableAt < startupGrace) return false;
        var gm = GameManager.Instance;
        if (gm && !string.IsNullOrEmpty(onlyInRoom) && gm.CurrentRoomId != onlyInRoom) return false;
        return true;
    }

    void Update()
    {
        if (!window || !ActiveNow()) return;

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
                PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText); // 카운트 O
                violatedThisOpen = true;
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
            openAt = 0f;
            violatedThisOpen = false;
            nextRepeatAt = 0f;
        }

        prevOpen = open;
    }
}
