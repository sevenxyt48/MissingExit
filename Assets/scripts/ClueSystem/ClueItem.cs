using UnityEngine;
using TMPro;
using cakeslice;   // QuickOutline

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

    [Header("수집 후 처리")]
    public bool disableColliderOnCollect = true;
    public bool hideRendererOnCollect = true;

    [Header("빛 효과 (cakeslice / QuickOutline)")]
    [Range(0, 2)] public int outlineColorIndex = 0;
    public bool autoFindOutlinesInChildren = true;
    public bool startOutlineOff = true;

    [Header("빛(시야/거리) 조건")]
    [Tooltip("이 거리 이내일 때만 윤곽선을 켠다(미터).")]
    public float revealDistance = 3.5f;
    [Tooltip("카메라와 단서 사이에 벽/가구가 있으면 윤곽선을 끈다.")]
    public bool requireLineOfSight = true;
    public LayerMask occlusionMask = ~0;

    // 내부 상태
    bool playerInRange = false;
    bool collected = false;

    Collider col;
    Renderer[] renderersCached;
    Outline[] outlines;
    Transform player;
    Transform camT;
    Bounds worldBounds;

    string EffectiveId =>
        (!useNameAsId && !string.IsNullOrWhiteSpace(clueID)) ? clueID : gameObject.name;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (clueID == "CLUE_01") clueID = "";
    }
#endif

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;

        renderersCached = GetComponentsInChildren<Renderer>(true);

        // Outline 찾기 + 초기 설정
        outlines = autoFindOutlinesInChildren
            ? GetComponentsInChildren<Outline>(true)
            : GetComponents<Outline>();

        if (outlines != null)
        {
            foreach (var ol in outlines)
            {
                if (!ol) continue;
                ol.color = outlineColorIndex;
                if (startOutlineOff) ol.enabled = false;   // ★ 시작부터 강제 Off
            }
        }

        // Bounds 계산 (레이 목표점)
        if (renderersCached != null && renderersCached.Length > 0)
        {
            bool first = true;
            foreach (var r in renderersCached)
            {
                if (!r) continue;
                if (first) { worldBounds = r.bounds; first = false; }
                else worldBounds.Encapsulate(r.bounds);
            }
        }
        else
        {
            worldBounds = new Bounds(transform.position, Vector3.one * 0.1f);
        }

        // 플레이어 / 카메라 자동 탐색
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
        if (Camera.main) camT = Camera.main.transform;
    }

    void Start()
    {
        if (interactPrompt) interactPrompt.SetActive(false);
        if (promptText)
        {
            string keyLabel = (interactKey == KeyCode.F) ? "F" : interactKey.ToString();
            promptText.text = $"{keyLabel}키: 조사";
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (interactPrompt) interactPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (interactPrompt) interactPrompt.SetActive(false);
        // 여기서는 Outline을 건들지 않는다(거리/시야 로직이 Update에서 처리)
    }

    void Update()
    {
        if (collected) return;

        // 1) 빛(Outline) 판정: 항상 거리 + LOS 기준으로
        UpdateOutlineByDistanceAndLOS();

        // 2) 상호작용(F키)은 트리거 안에서만
        if (playerInRange && Input.GetKeyDown(interactKey))
            Collect();
    }

    // ───────── Outline 제어 ─────────
    void UpdateOutlineByDistanceAndLOS()
    {
        if (outlines == null || outlines.Length == 0) return;
        if (!player) return;

        bool near = Vector3.Distance(player.position, worldBounds.center) <= revealDistance;
        if (!near)
        {
            SetOutline(false);
            return;
        }

        bool losOK = true;
        if (requireLineOfSight && camT)
        {
            Vector3 origin = camT.position;
            Vector3 target = worldBounds.center;
            Vector3 dir = (target - origin).normalized;
            float max = Vector3.Distance(origin, target);

            if (Physics.Raycast(origin, dir, out RaycastHit hit, max, occlusionMask,
                QueryTriggerInteraction.Ignore))
            {
                // 내가 아닌 다른 것을 맞으면 시야 차단
                if (!hit.collider.transform.IsChildOf(transform))
                    losOK = false;
            }
        }

        SetOutline(near && losOK);
    }

    void SetOutline(bool on)
    {
        if (outlines == null) return;
        foreach (var ol in outlines)
            if (ol) ol.enabled = on;
    }

    // ───────── 실제 수집 ─────────
    void Collect()
    {
        if (collected) return;
        collected = true;

        if (interactPrompt) interactPrompt.SetActive(false);
        SetOutline(false); // 수집 후 영구 Off

        if (ClueUIManager.Instance)
            ClueUIManager.Instance.ShowClue(clueTitle, clueContent, clueImage, clueSound);

        string id = EffectiveId;
        Debug.Log($"[ClueItem] 수집 완료: {id}");
        GameManager.Instance?.CollectClue(id);

        OnAnyClueCollected?.Invoke(this);

        if (disableColliderOnCollect && col) col.enabled = false;
        if (hideRendererOnCollect && renderersCached != null)
        {
            foreach (var r in renderersCached) if (r) r.enabled = false;
        }
    }

    [ContextMenu("테스트: 수집 처리 실행")]
    void __EditorCollectTest()
    {
        if (!Application.isPlaying) return;
        Collect();
    }
}
