using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public Transform doorPanel;
    public Vector3 openOffset = new Vector3(-2f, 0, 0);
    public float openSpeed = 5f;
    public float interactionDistance = 2f;

    [Header("UI References")]
    public GameObject hintText;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;
    private bool isMoving = false;
    private bool playerNearby = false;
    private GameObject playerObject;

    private Collider doorCollider;

    void Start()
    {
        if (doorPanel == null)
        {
            Debug.LogError("[SlidingDoor] doorPanel 未设置！");
            return;
        }

        closedPos = doorPanel.position;
        openPos = closedPos + openOffset;

        // 检查门初始状态
        isOpen = Vector3.Distance(doorPanel.position, openPos) < Vector3.Distance(doorPanel.position, closedPos);
        doorPanel.position = isOpen ? openPos : closedPos;

        // 获取 Collider
        doorCollider = doorPanel.GetComponent<Collider>();
        if (doorCollider == null)
        {
            Debug.LogError("[SlidingDoor] doorPanel 需要 Collider！");
        }
        else
        {
            // 初始状态：开门可穿，关门阻挡
            doorCollider.enabled = true;
            doorCollider.isTrigger = isOpen;
        }

        FindPlayer();

        Debug.Log("[SlidingDoor] Start: Door " + (isOpen ? "Open" : "Closed") + ", Collider enabled=" + doorCollider.enabled + ", isTrigger=" + doorCollider.isTrigger);
    }

    void Update()
    {
        if (doorPanel == null) return;
        if (playerObject == null) { FindPlayer(); return; }

        float distance = Vector3.Distance(transform.position, playerObject.transform.position);
        bool wasNearby = playerNearby;
        playerNearby = distance <= interactionDistance;

        //if (playerNearby != wasNearby)
        //    Debug.Log("[SlidingDoor] 玩家靠近状态变化: " + playerNearby + " 距离=" + distance);

        UpdateHintText();

        if (playerNearby && Input.GetKeyDown(KeyCode.E) && !isMoving)
        {
            ToggleDoor();
        }
    }

    void LateUpdate()
    {
        if (isMoving)
            MoveDoor();
    }

    void FindPlayer()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) playerObject = GameObject.Find("Player");

        if (playerObject != null)
            Debug.Log("[SlidingDoor] 找到 Player: " + playerObject.name);
        else
            Debug.LogWarning("[SlidingDoor] 没有找到 Player！");
    }

    void UpdateHintText()
    {
        if (hintText == null) return;

        if (playerNearby && !isOpen && !isMoving)
        {
            if (!hintText.activeSelf)
            {
                hintText.SetActive(true);
            }
        }
        else
        {
            if (hintText.activeSelf)
                hintText.SetActive(false);
        }
    }

    void MoveDoor()
    {
        Vector3 targetPos = isOpen ? openPos : closedPos;
        doorPanel.position = Vector3.MoveTowards(doorPanel.position, targetPos, Time.deltaTime * openSpeed);

        float dist = Vector3.Distance(doorPanel.position, targetPos);
        if (dist < 0.01f)
        {
            isMoving = false;
            doorPanel.position = targetPos;
            Debug.Log("[SlidingDoor] 门移动完成: " + (isOpen ? "开" : "关"));
        }
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;
        isMoving = true;

        if (doorCollider != null)
        {
            doorCollider.enabled = true;
            doorCollider.isTrigger = isOpen; // 开门可以穿，关门阻挡
        }

        if (hintText != null && hintText.activeSelf)
            hintText.SetActive(false);

        Debug.Log("[SlidingDoor] ToggleDoor 调用: " + (isOpen ? "开" : "关") + ", Collider isTrigger=" + doorCollider.isTrigger);
    }
}
