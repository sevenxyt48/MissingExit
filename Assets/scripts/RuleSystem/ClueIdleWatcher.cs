using UnityEngine;

public class ClueIdleWatcher : MonoBehaviour
{
    [Header("활성 조건")]
    public string onlyInRoom = "2-1";
    public float startupGrace = 2f;
    float startTime;

    [Header("단서 근접 판정")]
    public float nearRange = 2.2f;
    public string clueTag = "Clue";
    public LayerMask clueMask;

    [Header("정지 판정(플레이어+카메라 모두)")]
    public float idleSeconds = 5f;
    public float playerSpeedThreshold = 0.05f;
    public float cameraSpeedThreshold = 0.05f;

    [Header("쿨다운")]
    public float cooldown = 2f;
    float cd;

    [Header("Penalty")]
    public string violationReason = "회피 규칙 위반";
    [TextArea] public string penaltyText = "단서를 앞에 두고 멈춰 있었다.";

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

        if (Time.time - startTime < startupGrace) return false;
        if (gm && gm.CurrentRoomId == "2-4") return false;
        if (!string.IsNullOrEmpty(onlyInRoom) && gm && gm.CurrentRoomId != onlyInRoom) return false;
        if (gm && gm.InClueGrace()) return false; // 단서 열람/닫은 직후 유예
        return true;
    }

    bool NearAnyClue()
    {
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

        bool near = NearAnyClue();

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

        if (near && playerStill && camStill) idleTimer += Time.deltaTime;
        else idleTimer = 0f;

        if (cd <= 0f && idleTimer >= idleSeconds)
        {
            cd = cooldown;
            idleTimer = 0f;
            PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText);
        }
        if (cd > 0f) cd -= Time.deltaTime;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, nearRange);
    }
#endif
}
