using UnityEngine;
using TMPro;

public class ClueIdleWatcher : MonoBehaviour
{
    [Header("활성 조건")]
    public string onlyInRoom = "2-1";
    public float startupGrace = 2f;
    float startTime;

    [Header("근접 판정(충돌 + 힌트 보임)")]
    [Tooltip("단서로 인식할 태그(빈 경우 태그 무시)")]
    public string clueTag = "Clue";
    [Tooltip("단서 레이어(비워두면 전체 검색 후 필터)")]
    public LayerMask clueMask;                 // 전용 Clue 레이어만 체크 권장
    [Tooltip("단서에 ClueItem 컴포넌트가 있어야만 단서로 인정")]
    public bool requireClueComponent = true;
    [Tooltip("단서에 clueTag가 붙어 있어야만 단서로 인정 (빈 태그면 무시)")]
    public bool requireTagMatch = true;

    [Tooltip("플레이어 캡슐 반경에 더할 여유값(충돌 접촉 허용 오차)")]
    public float contactPadding = 0.02f;
    [Tooltip("플레이어에 CharacterController/CapsuleCollider가 없을 때 사용할 대체 구 반경")]
    public float fallbackSphereRadius = 0.45f;

    [Header("힌트 '보임' 기준")]
    [Tooltip("ClueItem.interactPrompt(GameObject)가 활성화되어 있어야 근접으로 인정")]
    public bool requirePromptActive = true;

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

    [Header("루프 사운드(선택)")]
    public bool useLoopSfx = true;
    public AudioClip loopWhileAvoiding;
    [Range(0f, 1f)] public float loopVolume = 0.8f;

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

        if (Time.time - startTime < startupGrace) return false;
        if (gm && gm.CurrentRoomId == "2-4") return false;
        if (!string.IsNullOrEmpty(onlyInRoom) && gm && gm.CurrentRoomId != onlyInRoom) return false;

        return true;
    }

    void Update()
    {
        if (!ActiveNow())
        {
            idleTimer = 0f;
            lastPlayerPos = transform.position;
            if (camT) lastCamPos = camT.position;
            if (PenaltyManager.Instance) PenaltyManager.Instance.StopSustain(violationReason);
            return;
        }

        var gm = GameManager.Instance;

        // ▶ 새 기준: “플레이어 콜라이더가 단서 콜라이더에 닿아 있고 + interactPrompt가 활성”이어야만 true
        bool near = IsClueContactAndPromptVisible();

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
        bool viewingClueNow = gm && gm.IsClueOpen;

        // 조건 해제 시 루프 SFX 즉시 정지
        if (viewingClueNow || !near || !(playerStill && camStill))
        {
            if (PenaltyManager.Instance) PenaltyManager.Instance.StopSustain(violationReason);
        }

        if (near && !viewingClueNow && playerStill && camStill) idleTimer += Time.deltaTime;
        else idleTimer = 0f;

        //if (debug && gm && (gm.CurrentRoomId == "2-2" || gm.CurrentRoomId == "2-3"))
        //{
        //    Debug.Log($"[Avoid/{gm.CurrentRoomId}] near={near}, stillP={(playerStill ? 1 : 0)}, stillC={(camStill ? 1 : 0)}, viewing={viewingClueNow}, idle={idleTimer:F2}, cd={cd:F2}");
        //}

        if (cd <= 0f && idleTimer >= idleSeconds)
        {
            cd = cooldown;
            idleTimer = 0f;

            // 방 입장 유예만 우회(요구사항)
            bool ignoreClueGrace = false;
            bool ignoreStartupGrace = false;
            bool ignoreRoomEnterGrace = true;

            if (debug && gm) Debug.Log($"[Avoid/{gm.CurrentRoomId}] TRIGGER → {violationReason}");

            PenaltyManager.Instance?.ApplyPenalty(
                violationReason,
                penaltyText,
                null, 1f,
                true,
                ignoreClueGrace,
                ignoreStartupGrace,
                ignoreRoomEnterGrace
            );

            if (useLoopSfx && loopWhileAvoiding && PenaltyManager.Instance)
                PenaltyManager.Instance.StartSustain(violationReason, loopWhileAvoiding, loopVolume, transform);
        }

        if (cd > 0f) cd -= Time.deltaTime;
    }

    // ---------------------------------------------------
    // “접촉 + interactPrompt 활성”로만 근접을 인정
    // ---------------------------------------------------
    bool IsClueContactAndPromptVisible()
    {
        // 1) 플레이어 충돌 볼륨 계산(CharacterController 우선)
        int searchMask = (clueMask.value != 0) ? clueMask.value : Physics.AllLayers;
        Collider[] touched = null;

        var cc = GetComponent<CharacterController>();
        if (cc)
        {
            Vector3 cCenter = transform.TransformPoint(cc.center);
            float halfH = Mathf.Max(0f, (cc.height * 0.5f) - cc.radius);
            Vector3 p1 = cCenter + Vector3.up * halfH;
            Vector3 p2 = cCenter - Vector3.up * halfH;
            float r = cc.radius + contactPadding;
            touched = Physics.OverlapCapsule(p1, p2, r, searchMask, QueryTriggerInteraction.Collide);
        }
        else
        {
            var cap = GetComponent<CapsuleCollider>();
            if (cap)
            {
                Vector3 center = transform.TransformPoint(cap.center);
                float radius = cap.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) + contactPadding;
                float half = Mathf.Max(0f, (cap.height * 0.5f) - cap.radius) * transform.lossyScale.y;

                Vector3 axis = Vector3.up;
                if (cap.direction == 0) axis = transform.right;
                else if (cap.direction == 1) axis = transform.up;
                else axis = transform.forward;

                Vector3 p1 = center + axis.normalized * half;
                Vector3 p2 = center - axis.normalized * half;
                touched = Physics.OverlapCapsule(p1, p2, radius, searchMask, QueryTriggerInteraction.Collide);
            }
            else
            {
                touched = Physics.OverlapSphere(transform.position, fallbackSphereRadius, searchMask, QueryTriggerInteraction.Collide);
            }
        }

        if (touched == null || touched.Length == 0) return false;

        // 2) 접촉한 콜라이더 중 '단서 + 프롬프트 활성' 인 대상만 인정
        for (int i = 0; i < touched.Length; i++)
        {
            var col = touched[i];
            var go = col.gameObject;

            // 태그 필터
            if (requireTagMatch && !string.IsNullOrEmpty(clueTag))
            {
                if (!go.CompareTag(clueTag) && !(go.transform.parent && go.transform.parent.CompareTag(clueTag)))
                    continue;
            }

            // 컴포넌트 필터
            ClueItem clue = null;
            if (requireClueComponent)
            {
                if (!go.TryGetComponent<ClueItem>(out clue))
                    clue = go.GetComponentInParent<ClueItem>();
                if (!clue) continue;
            }
            else
            {
                go.TryGetComponent<ClueItem>(out clue);
                if (!clue) clue = go.GetComponentInParent<ClueItem>();
            }

            // 프롬프트(힌트 문구) 활성 상태 확인
            if (requirePromptActive)
            {
                GameObject promptGO = clue ? clue.interactPrompt : null;
                bool visible = promptGO && promptGO.activeInHierarchy;

                // CanvasGroup 0 또는 TMP 비활성도 보이지 않는 것으로 간주
                if (visible)
                {
                    var cg = promptGO.GetComponentInParent<CanvasGroup>();
                    if (cg && cg.alpha <= 0.01f) visible = false;
                    var tmp = promptGO.GetComponentInChildren<TMP_Text>(true);
                    if (tmp && !tmp.enabled) visible = false;
                }

                if (!visible)
                {
                    if (debug) Debug.Log($"[Avoid] touch but prompt not active → {go.name}");
                    continue;
                }
            }

            // 모든 조건 통과 → 근접 인정
            if (debug) Debug.Log($"[Avoid] CONTACT+PROMPT OK → {go.name}");
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 대체 구 반경 시각화
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, fallbackSphereRadius);
    }
#endif
}
