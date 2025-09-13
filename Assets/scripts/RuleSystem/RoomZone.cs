using UnityEngine;

/// <summary>
/// 방 내부 트리거. 플레이어가 이 존에 들어오면:
/// - GameManager.CurrentRoomId 설정
/// - RoomController.OnPlayerEntered() 호출
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomZone : MonoBehaviour
{
    public string roomId = "2-1";
    public string playerTag = "Player";
    public RoomController controller; // 같은 오브젝트나 부모/자식에 붙인 RoomController

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        if (!controller) controller = GetComponentInParent<RoomController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        GameManager.Instance?.SetCurrentRoom(roomId);
        if (controller) controller.OnPlayerEntered();
    }
}
