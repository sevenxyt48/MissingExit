using UnityEngine;

public class PlayerIdleWatcher : MonoBehaviour
{
    [Header("활성 조건")]
    public string onlyInRoom = "";          // 비우면 전체 방
    public float startupGrace = 2f;
    float startTime;

    [Header("정지 판정(플레이어+카메라 모두)")]
    public float idleSeconds = 7f;
    public float playerSpeedThreshold = 0.04f;
    public float cameraSpeedThreshold = 0.04f;

    [Header("쿨다운")]
    public float cooldown = 3f;
    float cd;

    [Header("Penalty")]
    public string violationReason = "정지 규칙 위반";
    [TextArea] public string penaltyText = "오래도록 움직이지 않았다.";

    [Header("단서 근접 시 억제(겹침 방지)")]
    public bool suppressIfNearClue = true;  // true면 회피 규칙과 겹치는 구간에서 정지 규칙 억제
    public float nearRange = 2.2f;
    public string clueTag = "Clue";
    public LayerMask clueMask;

    [Header("디버그")]
    public bool debug = true;

    Transform camT;
    Vector3 lastPlayerPos;
    Vector3 lastCamPos;
    float idleTimer;

    void Start()
    {
        startTime = Time.time;
        lastPlayerPos = transform.position;
        var cam = Camera.main; camT = cam ? cam.transform : null;
        if (camT) lastCamPos = camT.position;
    }

    bool ActiveNow()
    {
        var gm = GameManager.Instance;
        bool ok = true;

        if (Time.time - startTime < startupGrace) ok = false;
        if (!string.IsNullOrEmpty(onlyInRoom) && gm && gm.CurrentRoomId != onlyInRoom) ok = false;
        if (gm && gm.CurrentRoomId == "2-4") ok = false; // 2-4 무규칙

        if (debug && gm && gm.CurrentRoomId == "2-2")
            Debug.Log($"[Idle/2-2] ActiveNow={ok}, room={gm.CurrentRoomId}, t={Time.time - startTime:F2}");

        return ok;
    }

    bool NearAnyClue()
    {
        if (!suppressIfNearClue) return false;

        if (clueMask.value != 0)
            return Physics.CheckSphere(transform.position, nearRange, clueMask);

        foreach (var c in Physics.OverlapSphere(transform.position, nearRange))
            if (c.CompareTag(clueTag)) return true;

        return false;
    }

    void Update()
    {
        if (!ActiveNow())
        {
            idleTimer = 0f;
            lastPlayerPos = transform.position;
            if (camT) lastCamPos = camT.position;
            return;
        }

        var gm = GameManager.Instance;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float playerSpeed = (transform.position - lastPlayerPos).magnitude / dt;
        lastPlayerPos = transform.position;

        float camSpeed = 0f;
        if (camT)
        {
            camSpeed = (camT.position - lastCamPos).magnitude / dt;
            lastCamPos = camT.position;
        }

        bool playerStill = playerSpeed < playerSpeedThreshold;
        bool camStill = camSpeed < cameraSpeedThreshold;

        // 단서 근접 시 억제 (회피 규칙과 충돌 방지)
        bool nearClue = NearAnyClue();
        bool suppressedByClue = suppressIfNearClue && nearClue && !(gm && gm.IsClueOpen == true); // 열람 중은 굳이 억제할 필요 X

        if (playerStill && camStill && !suppressedByClue) idleTimer += Time.deltaTime;
        else idleTimer = 0f;

        if (debug && gm && gm.CurrentRoomId == "2-2")
        {
            Debug.Log($"[Idle/2-2] nearClue={nearClue}, suppressed={suppressedByClue}, stillP={(playerStill ? 1 : 0)}, stillC={(camStill ? 1 : 0)}, idle={idleTimer:F2}, cd={cd:F2}");
        }

        if (cd <= 0f && idleTimer >= idleSeconds)
        {
            cd = cooldown;
            idleTimer = 0f;

            bool ignoreClueGrace = false;      // 정지 규칙은 유예 존중
            bool ignoreStartupGrace = false;
            bool ignoreRoomEnterGrace = false;

            if (debug && gm && gm.CurrentRoomId == "2-2")
                Debug.Log($"[Idle/2-2] TRIGGER → {violationReason}");

            PenaltyManager.Instance?.ApplyPenalty(
                violationReason,
                penaltyText,
                null, 1f,
                true,
                ignoreClueGrace,
                ignoreStartupGrace,
                ignoreRoomEnterGrace
            );
        }

        if (cd > 0f) cd -= Time.deltaTime;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!suppressIfNearClue) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, nearRange);
    }
#endif
}
