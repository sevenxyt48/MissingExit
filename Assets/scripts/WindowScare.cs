using UnityEngine;
using System.Collections;

public class WindowScare : MonoBehaviour
{
    public enum TriggerMode { Gaze, Distance, GazeAndDistance }

    [Header("References")]
    [Tooltip("플레이어 카메라(필수)")]
    public Transform playerCamera;              // Player/CameraRoot/MainCamera
    [Tooltip("시선/거리 판정 기준점(비우면 자기 자신)")]
    public Transform lookTarget;                // 창 중앙 Empty 권장
    [Tooltip("나타날 오브젝트(처음엔 꺼둠)")]
    public GameObject shadowObject;            // 실루엣/메쉬
    [Tooltip("효과음 재생용 3D AudioSource")]
    public AudioSource sfx;
    [Tooltip("스케어 사운드")]
    public AudioClip scareClip;

    [Header("Trigger")]
    public TriggerMode triggerMode = TriggerMode.Gaze;

    [Tooltip("시선 각도 임계값(작을수록 정면)")]
    [Range(1f, 60f)] public float appearAngle = 10f;
    [Tooltip("발동 전, 해당 각도로 최소 유지해야 하는 시간")]
    public float minLookSeconds = 0.12f;
    [Tooltip("시선 판정에도 거리 제한을 추가하고 싶을 때 (0=무제한)")]
    public float maxDistance = 0f;

    [Tooltip("이 반경 안으로 접근하면 발동(Distance 모드)")]
    public float approachRadius = 0f;
    [Tooltip("거리뿐 아니라 카메라도 창을 봐야 함")]
    public bool requireCameraFacing = false;

    [Header("Scare Timing / Style")]
    [Tooltip("그림자를 좌→우로 이동시키기")]
    public bool moveShadow = false;            // false면 '나타났다 사라짐'
    [Tooltip("이동 시작점(창 왼쪽)")]
    public Transform moveStart;
    [Tooltip("이동 끝점(창 오른쪽)")]
    public Transform moveEnd;
    [Tooltip("이동 시간(초)")]
    public float moveDuration = 1.0f;
    [Tooltip("이동 끝난 뒤 잠깐 유지(초)")]
    public float holdAfterMove = 0.1f;

    [Tooltip("이동을 쓰지 않을 때 켜져있는 시간(초)")]
    public float scareDuration = 0.85f;

    [Tooltip("한 번만 발동")]
    public bool oneShot = true;
    [Tooltip("반복 모드일 때 재발동 쿨다운(초)")]
    public float cooldown = 6f;

    [Header("Audio")]
    [Range(0f, 1f)] public float volume = 0.45f;
    [Range(0f, 3f)] public float pitchJitter = 0.04f;

    [Header("Arming / Edge Trigger")]
    [Tooltip("게임 시작 후 이 시간 동안은 발동 금지")]
    public float armDelay = 1.0f;
    [Tooltip("반경 밖 → 안으로 '진입'할 때만 발동")]
    public bool requireRadiusEnter = true;
    [Tooltip("시선이 임계각 밖 → 안으로 '넘어올 때'만 발동")]
    public bool requireLookEdge = true;

    [Header("Debug")]
    public bool enableDebug = true;
    public float debugEvery = 0.25f;

    // ── 내부 상태 ─────────────────────────────────
    float lookTimer = 0f;
    float nextAvailableTime = 0f;
    bool isScaring = false;
    bool consumed = false;
    float debugTick = 0f;

    Transform player;                // playerCamera.parent
    bool armed = false;
    bool wasInsideRadius = false;    // 이전 프레임 반경 내/외
    bool wasLooking = false;         // 이전 프레임 응시 여부

    // shadowObject == this 인 경우(같은 오브젝트)에 대비해서 렌더러만 on/off
    Renderer[] shadowRenderers;
    bool shadowIsSelf = false;

    void Reset()
    {
        lookTarget = transform;
        sfx = GetComponent<AudioSource>();
        if (!sfx) sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false; sfx.loop = false; sfx.spatialBlend = 1f;
    }

    void Awake()
    {
        if (!lookTarget) lookTarget = transform;
        if (playerCamera && playerCamera.parent) player = playerCamera.parent;

        shadowIsSelf = (shadowObject != null && shadowObject == gameObject);
        if (shadowIsSelf)
        {
            shadowRenderers = GetComponentsInChildren<Renderer>(true);
            ShowShadow(false);   // ← 여기! SetShadowVisible(false) → ShowShadow(false) 로 교체
        }
        else if (shadowObject) shadowObject.SetActive(false);

        //if (enableDebug)
        //{
        //    Debug.Log($"[WindowScare] Awake:{name} cam={(playerCamera ? playerCamera.name : "NULL")} " +
        //              $"target={(lookTarget ? lookTarget.name : "NULL")} shadow={(shadowObject ? shadowObject.name : "NULL")} mode={triggerMode}");
        //}
    }


    void OnEnable()
    {
        StartCoroutine(CoArm());
    }

    IEnumerator CoArm()
    {
        armed = false;
        yield return new WaitForSeconds(armDelay);
        UpdatePrevStatesOnce();   // 시작 상태를 기준으로 wasInside/wasLooking 세팅
        armed = true;
    }

    void UpdatePrevStatesOnce()
    {
        if (!playerCamera || !lookTarget) return;

        // 반경 상태
        var p = playerCamera.parent ? playerCamera.parent.position : playerCamera.position;
        if (approachRadius > 0f)
            wasInsideRadius = Vector3.Distance(p, lookTarget.position) <= approachRadius;

        // 시선 상태
        var toTarget = (lookTarget.position - playerCamera.position).normalized;
        wasLooking = Vector3.Dot(playerCamera.forward, toTarget) >= Mathf.Cos(appearAngle * Mathf.Deg2Rad);
    }

    void Update()
    {
        //if (!playerCamera || !lookTarget)
        //{
        //    if (enableDebug) Debug.LogWarning($"[WindowScare] Missing refs cam={playerCamera} target={lookTarget}", this);
        //    return;
        //}
        if (consumed || isScaring) return;

        if (!oneShot && Time.time < nextAvailableTime) { lookTimer = 0f; return; }

        bool condGaze = false, condDist = false;

        // ── Gaze 판정 ──
        if (triggerMode == TriggerMode.Gaze || triggerMode == TriggerMode.GazeAndDistance)
        {
            // 선택적 거리 제한
            if (maxDistance > 0f)
            {
                float d = Vector3.Distance(playerCamera.position, lookTarget.position);
                if (d > maxDistance) { lookTimer = 0f; goto AFTER_GAZE; }
            }

            Vector3 toTarget = (lookTarget.position - playerCamera.position).normalized;
            float dot = Vector3.Dot(playerCamera.forward, toTarget);
            bool looking = dot >= Mathf.Cos(appearAngle * Mathf.Deg2Rad);
            if (looking) lookTimer += Time.deltaTime; else lookTimer = 0f;
            condGaze = lookTimer >= minLookSeconds;

            //if (enableDebug && Time.time >= debugTick)
            //{
            //    float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            //    Debug.Log($"[WindowScare] Gaze angle={angle:0.0} looking={looking} t={lookTimer:0.00}/{minLookSeconds:0.00}");
            //}
        }
    AFTER_GAZE:

        // ── Distance 판정 ──
        if (triggerMode == TriggerMode.Distance || triggerMode == TriggerMode.GazeAndDistance)
        {
            if (!player) player = playerCamera ? playerCamera.parent : null;
            if (player)
            {
                float d = Vector3.Distance(player.position, lookTarget.position);
                bool near = approachRadius > 0f ? (d <= approachRadius) : false;

                if (requireCameraFacing && near)
                {
                    Vector3 toTarget = (lookTarget.position - playerCamera.position).normalized;
                    near = near && (Vector3.Dot(playerCamera.forward, toTarget) >= Mathf.Cos(appearAngle * Mathf.Deg2Rad));
                }
                condDist = near;

                //if (enableDebug && Time.time >= debugTick)
                //    Debug.Log($"[WindowScare] Dist d={d:0.00} thr={approachRadius:0.00} near={condDist}");
            }
        }

        if (enableDebug && Time.time >= debugTick) debugTick = Time.time + debugEvery;

        // ── 최종 발동 조건 ──
        bool shouldScare =
            (triggerMode == TriggerMode.Gaze && condGaze) ||
            (triggerMode == TriggerMode.Distance && condDist) ||
            (triggerMode == TriggerMode.GazeAndDistance && condGaze && condDist);

        // ── 시작 지연(arming) 및 에지 트리거 ──
        if (shouldScare && armed)
        {
            // 반경 에지: 밖→안 진입일 때만
            if ((triggerMode == TriggerMode.Distance || triggerMode == TriggerMode.GazeAndDistance) &&
                requireRadiusEnter && approachRadius > 0f)
            {
                var pos = playerCamera.parent ? playerCamera.parent.position : playerCamera.position;
                bool insideNow = Vector3.Distance(pos, lookTarget.position) <= approachRadius;
                if (!(insideNow && !wasInsideRadius)) shouldScare = false;
                wasInsideRadius = insideNow; // 상태 갱신
            }

            // 시선 에지: 안봄→봄 일 때만
            if ((triggerMode == TriggerMode.Gaze || triggerMode == TriggerMode.GazeAndDistance) &&
                requireLookEdge)
            {
                var toTarget = (lookTarget.position - playerCamera.position).normalized;
                bool lookingNow = Vector3.Dot(playerCamera.forward, toTarget) >= Mathf.Cos(appearAngle * Mathf.Deg2Rad);
                if (!(lookingNow && !wasLooking)) shouldScare = false;
                wasLooking = lookingNow; // 상태 갱신
            }
        }
        else
        {
            // armed 전에는 이전 상태만 갱신
            if (!armed) UpdatePrevStatesOnce();
        }

        if (shouldScare && armed) StartCoroutine(CoScare());
    }

    IEnumerator CoScare()
    {
        isScaring = true;
        lookTimer = 0f;
        if (enableDebug) Debug.Log("[WindowScare] SCARE START");

        // 사운드
        if (sfx && scareClip)
        {
            sfx.pitch = 1f + (pitchJitter > 0f ? Random.Range(-pitchJitter, pitchJitter) : 0f);
            sfx.PlayOneShot(scareClip, volume);
        }

        if (moveShadow && (moveStart && moveEnd))
        {
            // 이동형
            ShowShadow(true);
            Transform t = shadowObject ? shadowObject.transform : transform;
            Vector3 p0 = moveStart.position, p1 = moveEnd.position;
            if (t) t.position = p0;

            float tt = 0f;
            while (tt < 1f)
            {
                tt += Time.deltaTime / Mathf.Max(0.0001f, moveDuration);
                float k = Mathf.SmoothStep(0f, 1f, tt);
                if (t) t.position = Vector3.Lerp(p0, p1, k);
                yield return null;
            }
            if (holdAfterMove > 0f) yield return new WaitForSeconds(holdAfterMove);
            ShowShadow(false);
        }
        else
        {
            // 켜짐-유지-꺼짐
            ShowShadow(true);
            yield return new WaitForSeconds(scareDuration);
            ShowShadow(false);
        }

        if (oneShot) { consumed = true; enabled = false; }
        else { nextAvailableTime = Time.time + cooldown; }

        isScaring = false;
        if (enableDebug) Debug.Log("[WindowScare] SCARE END");
    }

    void ShowShadow(bool on)
    {
        if (shadowIsSelf)
        {
            if (shadowRenderers == null) shadowRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in shadowRenderers) if (r) r.enabled = on;
        }
        else if (shadowObject) shadowObject.SetActive(on);
    }

#if UNITY_EDITOR
    [ContextMenu("Force Once")]
    void ForceOnce() { StopAllCoroutines(); StartCoroutine(CoScare()); }

    void OnDrawGizmosSelected()
    {
        if (!lookTarget) lookTarget = transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(lookTarget.position, 0.15f);

        if (playerCamera)
        {
            Vector3 toTarget = (lookTarget.position - playerCamera.position).normalized;
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
            Gizmos.DrawLine(playerCamera.position, playerCamera.position + toTarget * 2f);
        }
        if (moveStart && moveEnd)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(moveStart.position, moveEnd.position);
        }
    }
#endif
}
