// ClueItem.cs — 힌트(Trigger)와 빛(거리+LOS) 완전 분리
using UnityEngine;
using TMPro;
using cakeslice; // QuickOutline

[RequireComponent(typeof(Collider))]
public class ClueItem : MonoBehaviour
{
    public static System.Action<ClueItem> OnAnyClueCollected;

    [Header("ID (비워두면 오브젝트 이름 사용)")]
    [SerializeField] private string clueID = "";
    [SerializeField] private bool useNameAsId = true;

    [Header("단서 정보")]
    public string clueTitle;
    [TextArea] public string clueContent;
    public Sprite clueImage;
    public AudioClip clueSound;

    [Header("프롬프트(UI)")]
    public GameObject interactPrompt;
    public TMP_Text promptText;
    public KeyCode interactKey = KeyCode.F;

    [Header("빛 효과 (cakeslice/QuickOutline)")]
    [Range(0, 2)] public int outlineColorIndex = 0;
    public bool autoFindOutlinesInChildren = true;
    public bool startOutlineOff = true;

    [Header("빛(윤곽선) 조건")]
    [Tooltip("윤곽선을 켜기 위한 거리(미터) — 힌트 트리거와 독립")]
    public float revealDistance = 3.5f;
    [Tooltip("카메라→단서 사이가 막히면 Off")]
    public bool requireLineOfSight = true;
    [Tooltip("가림 오브젝트 레이어(벽/문/가구 등)")]
    public LayerMask occlusionMask = ~0;

    // 내부
    private bool playerInRange = false;  // ← 오직 힌트/입력용
    private bool collected = false;
    private Collider col;
    private Renderer[] renderersCached;
    private Outline[] outlines;
    private Transform player;     // 플레이어 루트
    private Transform camT;       // 메인 카메라
    private Bounds myBounds;      // 레이 목표점 계산용(메시 합성 중앙)

    private string EffectiveId =>
        (!useNameAsId && !string.IsNullOrWhiteSpace(clueID)) ? clueID : gameObject.name;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (clueID == "CLUE_01") clueID = "";
        if (outlines != null)
            foreach (var ol in outlines) if (ol) ol.color = outlineColorIndex;
    }
#endif

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true; // 힌트용 트리거

        renderersCached = GetComponentsInChildren<Renderer>(true);
        outlines = autoFindOutlinesInChildren
            ? GetComponentsInChildren<Outline>(true)
            : GetComponents<Outline>();

        if (outlines != null)
        {
            foreach (var ol in outlines)
            {
                if (!ol) continue;
                ol.color = outlineColorIndex;
                if (startOutlineOff) ol.enabled = false;
            }
        }

        var cam = Camera.main; camT = cam ? cam.transform : null;
        var p = GameObject.FindGameObjectWithTag("Player"); player = p ? p.transform : null;

        // 바운즈 합성(레이 목표점)
        myBounds = new Bounds(transform.position, Vector3.zero);
        if (renderersCached != null && renderersCached.Length > 0)
        {
            bool first = true;
            foreach (var r in renderersCached)
            {
                if (!r) continue;
                if (first) { myBounds = r.bounds; first = false; }
                else myBounds.Encapsulate(r.bounds);
            }
        }
    }

    private void Start()
    {
        if (interactPrompt) interactPrompt.SetActive(false);
        if (promptText)
        {
            string keyLabel = (interactKey == KeyCode.F) ? "F" : interactKey.ToString();
            promptText.text = $"{keyLabel}키: 조사";
        }
    }

    // ---- 힌트 문구(트리거 전용) ----
    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        playerInRange = true;
        if (interactPrompt) interactPrompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactPrompt) interactPrompt.SetActive(false);
        // ❌ 여기서 윤곽선을 끄지 않습니다! (빛은 거리/시야로 따로 관리)
    }

    private void Update()
    {
        if (collected) return;

        // ✅ 빛 판정은 트리거 여부와 무관하게 '항상' 계산
        UpdateOutlineByDistanceAndLOS();

        // ✅ 상호작용 입력은 트리거 안에서만
        if (playerInRange && Input.GetKeyDown(interactKey))
            Collect();
    }

    // ───────── 빛(윤곽선) 판정 ─────────
    private void UpdateOutlineByDistanceAndLOS()
    {
        bool on = IsWithinRevealDistance() && HasLineOfSight();
        SetOutline(on);
    }

    private bool IsWithinRevealDistance()
    {
        if (!player) return false;
        return Vector3.Distance(player.position, myBounds.center) <= revealDistance;
    }

    private bool HasLineOfSight()
    {
        if (!requireLineOfSight || !camT) return true;

        Vector3 origin = camT.position;
        Vector3 target = myBounds.center;
        Vector3 dir = (target - origin).normalized;
        float max = Vector3.Distance(origin, target);

        // 가림 오브젝트 레이어에 맞춘 레이캐스트
        if (Physics.Raycast(origin, dir, out RaycastHit hit, max, occlusionMask, QueryTriggerInteraction.Ignore))
        {
            // 내 자식이면 통과, 아니면 차단
            return hit.collider && hit.collider.transform.IsChildOf(transform);
        }
        return true; // 아무것도 안 맞으면 시야 확보
    }

    private void SetOutline(bool on)
    {
        if (outlines == null) return;
        foreach (var ol in outlines) if (ol) ol.enabled = on;
    }

    // ───────── 수집 처리 ─────────
    private void Collect()
    {
        if (collected) return;
        collected = true;

        if (interactPrompt) interactPrompt.SetActive(false);
        SetOutline(false);

        if (ClueUIManager.Instance)
            ClueUIManager.Instance.ShowClue(clueTitle, clueContent, clueImage, clueSound);

        GameManager.Instance?.CollectClue(EffectiveId);
        OnAnyClueCollected?.Invoke(this);

        if (col) col.enabled = false;
        if (renderersCached != null)
            foreach (var r in renderersCached) if (r) r.enabled = false;
    }

    [ContextMenu("테스트: 수집 처리 실행")]
    private void __EditorCollectTest()
    {
        if (!Application.isPlaying) return;
        Collect();
    }
}
