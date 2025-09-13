using UnityEngine;

public class NoteInteraction : MonoBehaviour
{
    [Header("References")]
    public DisplayRule displayRule;
    public GameObject hintText;
    public FirstPersonController playerController; // FirstPersonController 참조

    [Header("Settings")]
    public float interactionDistance = 2.5f;

    private bool isPlayerNear = false;
    private bool hasUsed = false;

    void Start()
    {
        if (hintText != null)
            hintText.SetActive(false);

        // 자동으로 플레이어 컨트롤러 찾기
        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
            if (playerController == null)
            {
                Debug.LogError("FirstPersonController를 찾을 수 없습니다!");
            }
        }
    }

    void Update()
    {
        if (playerController == null || hasUsed) return;

        // 플레이어와의 거리 계산
        float distance = Vector3.Distance(transform.position, playerController.transform.position);
        bool wasPlayerNear = isPlayerNear;
        isPlayerNear = distance <= interactionDistance;

        // 힌트 텍스트 표시/숨김
        if (isPlayerNear && !wasPlayerNear)
        {
            Debug.Log("플레이어가 노트 근처에 접근함");
            if (hintText != null)
                hintText.SetActive(true);
        }
        else if (!isPlayerNear && wasPlayerNear)
        {
            Debug.Log("플레이어가 노트에서 멀어짐");
            if (hintText != null)
                hintText.SetActive(false);
        }

        // 상호작용 입력 처리
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            InteractWithNote();
        }
    }

    void InteractWithNote()
    {
        hasUsed = true;
        Debug.Log("노트와 상호작용 → 규칙 표시 시작");

        if (hintText != null)
            hintText.SetActive(false);

        if (displayRule != null)
            displayRule.StartDisplayingRules();
        else
            Debug.LogError("DisplayRule 참조가 설정되지 않았습니다!");
    }

    // 씬 뷰에서 상호작용 거리 시각화 (디버깅용)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}