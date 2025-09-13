using UnityEngine;

public class PlayerIdleWatcher : MonoBehaviour
{
    [Header("활성 조건")]
    public string onlyInRoom = "";    // 비우면 항상, 값이 있으면 해당 교실에서만
    public float startupGrace = 2f;   // 시작 유예
    float startTime;

    [Header("정지 감시")]
    public float idleSeconds = 5f;
    [Tooltip("움직임 판정 임계값(미터/초)")]
    public float moveThreshold = 0.05f;
    public float cooldown = 2f;

    [Header("Penalty")]
    public string violationReason = "침묵 규칙 위반";
    [TextArea] public string penaltyText = "네가 멈추는 순간, 진실도 멈춘다.";

    Vector3 lastPos;
    float idleTimer, cd;

    void Start()
    {
        startTime = Time.time;
        lastPos = transform.position;
    }

    bool ActiveNow()
    {
        if (Time.time - startTime < startupGrace) return false;
        var gm = GameManager.Instance;
        if (gm && gm.CurrentRoomId == "2-4") return false;
        if (!string.IsNullOrEmpty(onlyInRoom) && gm && gm.CurrentRoomId != onlyInRoom) return false;
        return true;
    }

    void Update()
    {
        if (!ActiveNow()) { lastPos = transform.position; idleTimer = 0f; return; }

        float speed = (transform.position - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = transform.position;

        if (speed < moveThreshold) idleTimer += Time.deltaTime;
        else idleTimer = 0f;

        if (cd <= 0f && idleTimer >= idleSeconds)
        {
            cd = cooldown;
            PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText);
        }

        if (cd > 0f) cd -= Time.deltaTime;
    }
}
