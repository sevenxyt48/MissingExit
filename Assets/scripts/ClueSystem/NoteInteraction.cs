using UnityEngine;
using cakeslice; // QuickOutline

public class NoteInteraction : MonoBehaviour
{
    [Header("References")]
    public DisplayRule displayRule;
    public GameObject hintText;
    public FirstPersonController playerController; // 자동 탐색 가능

    [Header("Distances")]
    [Tooltip("F키 상호작용 허용 거리(미터)")]
    public float interactionDistance = 2.5f;
    [Tooltip("빛(윤곽선) 표시 거리(미터). interactionDistance와 다르게 줄 수도 있음")]
    public float lightDistance = 2.5f;

    [Header("Line of Sight (선택)")]
    [Tooltip("카메라→노트 사이에 벽/가구가 있으면 빛을 끕니다.")]
    public bool requireLineOfSight = false;
    public LayerMask occlusionMask = ~0;   // 가림체 레이어 (벽/문/가구 등)

    // 내부 상태
    bool isPlayerNear = false;
    bool hasUsed = false;
    Outline[] outlines;          // 자식 메시들에 붙은 cakeslice.Outline 모음
    Transform camT;

    void Start()
    {
        if (hintText) hintText.SetActive(false);

        // 플레이어 자동 찾기
        if (!playerController)
        {
            playerController = FindObjectOfType<FirstPersonController>();
            if (!playerController) Debug.LogError("FirstPersonController를 찾을 수 없습니다!");
        }

        // 카메라 참조
        if (Camera.main) camT = Camera.main.transform;

        // 자식들에서 Outline 수집 & 초기 OFF
        outlines = GetComponentsInChildren<Outline>(true);
        SetOutline(false);
    }

    void Update()
    {
        if (!playerController) return;

        // 거리 판정
        float dist = Vector3.Distance(transform.position, playerController.transform.position);
        bool wasNear = isPlayerNear;
        isPlayerNear = dist <= interactionDistance;

        // 힌트 토글
        if (isPlayerNear && !wasNear) { if (hintText) hintText.SetActive(true); }
        else if (!isPlayerNear && wasNear) { if (hintText) hintText.SetActive(false); }

        // 빛(윤곽선) 토글: 사용 전 + 거리 + (선택)LOS
        if (!hasUsed)
        {
            bool inLightRange = dist <= lightDistance;
            bool losOK = !requireLineOfSight || HasLineOfSight();
            bool wantLight = inLightRange && losOK;

            // 필요할 때만 스위칭(불필요한 enable 호출 방지)
            ToggleOutlineIfNeeded(wantLight);
        }
        else
        {
            // 이미 사용했으면 항상 꺼둠
            SetOutline(false);
        }

        // 상호작용
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
            InteractWithNote();
    }

    bool HasLineOfSight()
    {
        if (!camT) return true; // 카메라 없으면 LOS 스킵
        Vector3 origin = camT.position;
        Vector3 target = GetWorldCenter();
        Vector3 dir = (target - origin).normalized;
        float max = Vector3.Distance(origin, target);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, max, occlusionMask, QueryTriggerInteraction.Ignore))
        {
            // 내 자식/나 자신을 맞았으면 LOS OK
            return hit.collider && hit.collider.transform.IsChildOf(transform);
        }
        return true; // 아무것도 안 맞으면 가림 없음
    }

    Vector3 GetWorldCenter()
    {
        // 여러 렌더러의 바운즈 중심을 대략 사용
        var rends = GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return transform.position;

        Bounds b = new Bounds(rends[0].bounds.center, Vector3.zero);
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b.center;
    }

    void InteractWithNote()
    {
        hasUsed = true;
        if (hintText) hintText.SetActive(false);

        // 빛 영구 OFF
        SetOutline(false);

        // 규칙/노트 UI 표시
        if (displayRule) displayRule.StartDisplayingRules();
        else Debug.LogError("DisplayRule 참조가 설정되지 않았습니다!");
    }

    void ToggleOutlineIfNeeded(bool wantOn)
    {
        if (outlines == null) return;
        // 첫 개체 기준으로 현재 상태 파악
        bool curOn = false;
        foreach (var ol in outlines) { if (ol) { curOn = ol.enabled; break; } }

        if (curOn != wantOn) SetOutline(wantOn);
    }

    void SetOutline(bool on)
    {
        if (outlines == null) return;
        foreach (var ol in outlines) if (ol) ol.enabled = on;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, lightDistance);
    }
#endif
}
