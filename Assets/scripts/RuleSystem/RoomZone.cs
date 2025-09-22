using UnityEngine;
using System.Collections;

/// <summary>
/// 방 내부 트리거. 플레이어가 이 존에 들어오면:
/// - GameManager.CurrentRoomId 설정
/// - (유예 후) RoomController.OnPlayerEntered() 호출
///   └ 짧게 훑고 나갔을 때 잠금되지 않도록 '입장 확정 시간' 사용 가능
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomZone : MonoBehaviour
{
    public string roomId = "2-1";
    public string playerTag = "Player";
    public RoomController controller; // 같은 오브젝트나 부모/자식에 붙인 RoomController

    [Header("입장 확정 옵션")]
    [Tooltip("true면 enterCommitSeconds 동안 계속 머무를 때만 입장으로 인정")]
    public bool useEnterCommit = true;
    [Tooltip("이 시간이 지나도록 Zone 안에 있어야 '입장'으로 인정")]
    [Range(0f, 2f)] public float enterCommitSeconds = 0.35f;

    Collider zoneCol;
    Coroutine commitCo;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col)
        {
            col.isTrigger = true;
        }
    }

    void Awake()
    {
        if (!controller) controller = GetComponentInParent<RoomController>();
        zoneCol = GetComponent<Collider>();
        if (zoneCol) zoneCol.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (!useEnterCommit)
        {
            CommitEnter();
        }
        else
        {
            if (commitCo != null) StopCoroutine(commitCo);
            commitCo = StartCoroutine(CommitAfterStay(other));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 존을 벗어나면 대기 취소
        if (commitCo != null)
        {
            StopCoroutine(commitCo);
            commitCo = null;
        }
    }

    IEnumerator CommitAfterStay(Collider playerCol)
    {
        float t = 0f;
        // 플레이어가 계속 Zone과 겹치는지 확인
        while (t < enterCommitSeconds)
        {
            if (!playerCol || !zoneCol) yield break;

            // 간단한 교차 검사: 완전히 벗어나면 취소
            if (!zoneCol.bounds.Intersects(playerCol.bounds))
                yield break;

            t += Time.deltaTime;
            yield return null;
        }

        CommitEnter();
    }

    void CommitEnter()
    {
        GameManager.Instance?.SetCurrentRoom(roomId);
        if (controller) controller.OnPlayerEntered();
    }
}
