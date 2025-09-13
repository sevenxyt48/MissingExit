using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class ClueItem : MonoBehaviour
{
    // ─────────────────────────────
    // 방 진행을 위한 전역 이벤트(방 단서 카운트)
    // RoomController가 이 이벤트를 받아서 같은 방 단서만 집계합니다.
    // ─────────────────────────────
    public static System.Action<ClueItem> OnAnyClueCollected;

    [Header("ID (비워두면 오브젝트 이름 사용)")]
    [SerializeField] private string clueID = "";   // 비워두면 이름을 사용
    [SerializeField] private bool useNameAsId = true;

    [Header("단서 정보")]
    public string clueTitle;
    [TextArea] public string clueContent;
    public Sprite clueImage;
    public AudioClip clueSound; // 재생은 ClueUIManager가 담당

    [Header("프롬프트(UI)")]
    public GameObject interactPrompt;   // 근처에서만 보이는 UI 오브젝트
    public TMP_Text promptText;         // "F키: 조사" 같은 텍스트
    public KeyCode interactKey = KeyCode.F;

    [Header("수집 후 처리")]
    [Tooltip("수집 후 콜라이더를 끕니다.")]
    public bool disableColliderOnCollect = true;
    [Tooltip("수집 후 렌더러(모델/메시)를 숨깁니다.")]
    public bool hideRendererOnCollect = true;

    // 내부 상태
    private bool playerInRange = false;
    private bool collected = false;
    private Collider col;
    private Renderer[] renderersCached;

    // 유효 ID
    private string EffectiveId =>
        (!useNameAsId && !string.IsNullOrWhiteSpace(clueID)) ? clueID : gameObject.name;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 프리팹 기본값이 남아 있을 때 자동 비움(실수 방지)
        if (clueID == "CLUE_01") clueID = "";
    }
#endif

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true; // 상호작용용 트리거
        renderersCached = GetComponentsInChildren<Renderer>(true);
    }

    private void Start()
    {
        if (interactPrompt) interactPrompt.SetActive(false);
        if (promptText)
        {
            // 키 표시는 취향껏 바꿔도 됩니다.
            string keyLabel = (interactKey == KeyCode.F) ? "F" : interactKey.ToString();
            promptText.text = $"{keyLabel}키: 조사";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (interactPrompt) interactPrompt.SetActive(true);
        // Debug.Log($"[ClueItem] 플레이어 근접: {EffectiveId}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (interactPrompt) interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (collected) return;
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
            Collect();
    }

    // ─────────────────────────────
    // 실제 수집 처리
    // ─────────────────────────────
    private void Collect()
    {
        if (collected) return;
        collected = true;

        if (interactPrompt) interactPrompt.SetActive(false);

        // 1) 단서 UI 표시(타자기 효과/이미지/사운드는 ClueUIManager에서 처리)
        if (ClueUIManager.Instance)
            ClueUIManager.Instance.ShowClue(clueTitle, clueContent, clueImage, clueSound);

        // 2) 전역 진행 보고(좋은 엔딩을 위한 전체 카운트)
        string id = EffectiveId;
        Debug.Log($"[ClueItem] 수집 완료: {id}");
        GameManager.Instance?.CollectClue(id);

        // 3) 방 진행 보고(이 방의 RoomController가 집계)
        OnAnyClueCollected?.Invoke(this);

        // 4) 재상호작용 방지
        if (disableColliderOnCollect && col) col.enabled = false;
        if (hideRendererOnCollect && renderersCached != null)
        {
            foreach (var r in renderersCached) if (r) r.enabled = false;
        }
    }

    // (선택) 에디터에서 바로 테스트하고 싶을 때 사용
    [ContextMenu("테스트: 수집 처리 실행")]
    private void __EditorCollectTest()
    {
        if (!Application.isPlaying) return;
        Collect();
    }
}
