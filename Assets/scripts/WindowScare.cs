using UnityEngine;
using System.Collections;

public class WindowScare : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;      // Player/CameraRoot/MainCamera
    public Transform lookTarget;        // 보통 this.transform
    public GameObject shadowObject;     // 처음엔 비활성화
    public AudioSource sfx;             // 이 오브젝트의 AudioSource
    public AudioClip scareClip;

    [Header("Trigger Conditions")]
    [Range(1f, 60f)] public float appearAngle = 10f;
    public float minLookSeconds = 0.12f;
    public float maxDistance = 0f;

    [Header("Scare Timing")]
    public float scareDuration = 0.85f;
    public float cooldown = 6f;
    public bool oneShot = true;

    [Header("Audio")]
    [Range(0f, 1f)] public float volume = 0.45f;
    [Range(0f, 3f)] public float pitchJitter = 0.04f;

    // ── Debug ───────────────────────────────────────────
    [Header("Debug")]
    public bool enableDebug = true;
    public float debugEvery = 0.25f; // 로그 간격(초)

    float lookTimer = 0f;
    float nextAvailableTime = 0f;
    bool isScaring = false;
    bool consumed = false;
    float debugTick = 0f;

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
        if (shadowObject) shadowObject.SetActive(false);

        if (enableDebug)
        {
            Debug.Log($"[WindowScare] Awake at {name} | " +
                      $"playerCamera={(playerCamera ? playerCamera.name : "NULL")}, " +
                      $"lookTarget={(lookTarget ? lookTarget.name : "NULL")}, " +
                      $"shadow={(shadowObject ? shadowObject.name : "NULL")}");
        }
    }

    void Update()
    {
        if (!playerCamera || !lookTarget)
        {
            if (enableDebug) Debug.LogWarning($"[WindowScare] Missing refs: playerCamera={playerCamera}, lookTarget={lookTarget}", this);
            return;
        }
        if (consumed || isScaring) return;

        // 쿨다운
        if (!oneShot && Time.time < nextAvailableTime)
        {
            if (enableDebug && Time.time >= debugTick)
            {
                Debug.Log($"[WindowScare] Cooling down… {nextAvailableTime - Time.time:0.00}s left");
                debugTick = Time.time + debugEvery;
            }
            lookTimer = 0f;
            return;
        }

        // 거리 체크
        float dist = (maxDistance > 0f) ? Vector3.Distance(playerCamera.position, lookTarget.position) : 0f;
        if (maxDistance > 0f && dist > maxDistance)
        {
            if (enableDebug && Time.time >= debugTick)
            {
                Debug.Log($"[WindowScare] Too far. dist={dist:0.00} > max={maxDistance:0.00}");
                debugTick = Time.time + debugEvery;
            }
            lookTimer = 0f;
            return;
        }

        // 시선 각도 체크
        Vector3 toTarget = (lookTarget.position - playerCamera.position).normalized;
        float dot = Vector3.Dot(playerCamera.forward, toTarget);
        float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
        bool looking = angle <= appearAngle;

        if (looking) lookTimer += Time.deltaTime; else lookTimer = 0f;

        if (enableDebug && Time.time >= debugTick)
        {
            Debug.Log($"[WindowScare] angle={angle:0.0}° (thr={appearAngle:0.0}°), " +
                      $"looking={looking}, lookTimer={lookTimer:0.00}/{minLookSeconds:0.00}, " +
                      (maxDistance > 0f ? $"dist={dist:0.00} " : "") +
                      $"oneShot={oneShot}, nextAvail={nextAvailableTime:0.00}");
            debugTick = Time.time + debugEvery;
        }

        if (lookTimer >= minLookSeconds) StartCoroutine(CoScare());
    }

    IEnumerator CoScare()
    {
        isScaring = true;
        lookTimer = 0f;

        if (enableDebug) Debug.Log("[WindowScare] SCARE START");

        // 등장
        if (shadowObject)
        {
            shadowObject.SetActive(true);
            if (enableDebug) Debug.Log("[WindowScare] Shadow ON");
        }
        else if (enableDebug) Debug.LogWarning("[WindowScare] Shadow object is NULL", this);

        // 사운드
        if (sfx && scareClip)
        {
            sfx.pitch = 1f + (pitchJitter > 0f ? Random.Range(-pitchJitter, pitchJitter) : 0f);
            sfx.PlayOneShot(scareClip, volume);
            if (enableDebug) Debug.Log($"[WindowScare] PlayOneShot vol={volume:0.00} pitch={sfx.pitch:0.00}");
        }
        else if (enableDebug) Debug.LogWarning($"[WindowScare] No sfx or clip. sfx={(sfx ? "OK" : "NULL")}, clip={(scareClip ? "OK" : "NULL")}");

        yield return new WaitForSeconds(scareDuration);

        if (shadowObject)
        {
            shadowObject.SetActive(false);
            if (enableDebug) Debug.Log("[WindowScare] Shadow OFF");
        }

        if (oneShot)
        {
            consumed = true;
            if (enableDebug) Debug.Log("[WindowScare] One-shot consumed. Disabling Update.");
            enabled = false;
        }
        else
        {
            nextAvailableTime = Time.time + cooldown;
            if (enableDebug) Debug.Log($"[WindowScare] Cooldown set: {cooldown:0.00}s");
        }

        isScaring = false;
        if (enableDebug) Debug.Log("[WindowScare] SCARE END");
    }

#if UNITY_EDITOR
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
    }
#endif
}
