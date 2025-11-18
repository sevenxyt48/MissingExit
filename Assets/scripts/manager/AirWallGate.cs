// AirWallGate.cs (일부만 추가/수정)

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AirWallGate : MonoBehaviour
{
    [Header("Requirement")]
    public string requiredCompletedRoomId = "2-1";

    [Header("Colliders")]
    public Collider wallCollider;     // isTrigger = false (공기벽)
    public Collider infoTrigger;      // << 사용 안 해도 됨 (null 가능)

    [Header("Blocked Message")]
    [TextArea]
    public string blockedTextKorean =
        "아직 다음 구역으로 들어갈 수 없습니다. 먼저 2-1반의 단서를 모두 수집하세요.";

    [Header("Toast Mode (InfoTrigger 없이 사용)")]
    public bool useToastMode = true;        // ✅ 이걸 켜면 트리거 없이 토스트로 안내
    [Range(0.5f, 2.5f)] public float hintDistance = 1.2f;   // 벽까지 근접 거리
    [Range(0.5f, 5f)] public float hintCooldown = 1.5f;   // 반복 안내 쿨다운(초)

    bool unlocked = false;
    Transform player;
    Transform cam;           // 플레이어 카메라(시야선 검사용)
    float lastHintTime = -999f;

    void Start()
    {
        if (!wallCollider) wallCollider = GetComponent<Collider>();
        if (wallCollider) wallCollider.isTrigger = false;

        // 진행 상태 반영
        var pm = ProgressionManager.Instance;
        SetUnlocked(pm && pm.IsCompleted(requiredCompletedRoomId));
        if (pm != null) pm.OnRoomCompleted += OnRoomCompleted;

        // 플레이어/카메라 참조 자동 탐색
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go)
        {
            player = go.transform;
            var camObj = Camera.main;
            cam = camObj ? camObj.transform : player; // 없으면 플레이어 위치로 대체
        }
    }

    void OnDestroy()
    {
        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.OnRoomCompleted -= OnRoomCompleted;
    }

    void OnRoomCompleted(string roomId)
    {
        if (roomId == requiredCompletedRoomId)
            SetUnlocked(true);
    }

    void SetUnlocked(bool v)
    {
        unlocked = v;
        if (wallCollider) wallCollider.enabled = !v;  // 열리면 벽 비활성화
        // infoTrigger는 안 써도 됨
    }

    void Update()
    {
        if (!useToastMode || unlocked || !player || !wallCollider) return;

        // 플레이어가 벽에 충분히 가까운지 판단 (콜라이더 경계와의 최근접점)
        Vector3 p = player.position;
        Vector3 closest = wallCollider.ClosestPoint(p);
        float dist = Vector3.Distance(p, closest);
        bool closeEnough = dist <= hintDistance;

        if (!closeEnough) return;

        // 시야선 상에 벽이 실제로 있는지 간단 체크(옵션)
        bool seeWall = true;
        if (cam)
        {
            Vector3 dir = (closest - cam.position).normalized;
            if (Physics.Raycast(cam.position, dir, out RaycastHit hit, hintDistance + 1.0f))
            {
                // 이 레이가 먼저 맞춘 것이 우리 공기벽이면 보인다고 판단
                seeWall = (hit.collider == wallCollider);
            }
        }

        if (!seeWall) return;

        // 쿨다운
        if (Time.time - lastHintTime < hintCooldown) return;
        lastHintTime = Time.time;

        // ✅ 토스트 출력(당신 UI에 맞춰 한 가지 방법 선택)
        // 1) GuidanceToast(토스트 전용 스크립트가 있을 때)
        GuidanceToast gt = FindObjectOfType<GuidanceToast>(true);
        if (gt != null)
        {
            gt.Show(blockedTextKorean, null, 4f);
            return;
        }

        // 2) PenaltyManager로 간단 토스트 대체(벌점 없이 안내만)
        PenaltyManager.Instance?.ApplyPenalty(
            "진행 차단",
            blockedTextKorean,
            null, 1.5f, false
        );
    }

    // (트리거 방식도 남기고 싶다면 그대로 둬도 됨)
    void OnTriggerEnter(Collider other)
    {
        if (!infoTrigger || !other.CompareTag("Player") || unlocked || !useToastMode) return;
        // useToastMode=true면 Update로 처리하므로 여기서는 보통 미사용
    }
}
