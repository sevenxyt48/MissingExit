using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 플레이어가 가까이에서 지정 키를 누르면 onInteract 호출.
/// 라디오 끄기 등 간단한 상호작용에 사용.
/// </summary>
public class InteractOnKey : MonoBehaviour
{
    public string playerTag = "Player";
    public float interactionDistance = 2f;
    public KeyCode key = KeyCode.E;
    public GameObject hint;          // E 키 프롬프트
    public UnityEvent onInteract;

    Transform player;

    void Update()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }
        if (!player) return;

        bool near = Vector3.Distance(player.position, transform.position) <= interactionDistance;
        if (hint) hint.SetActive(near);

        if (near && Input.GetKeyDown(key))
            onInteract?.Invoke();
    }
}
