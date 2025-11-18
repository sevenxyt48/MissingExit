using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 교실(반) 단위 진행 관리:
///  - 플레이어가 방에 '들어온 뒤' 모든 문을 닫고 잠금(단서가 1개 이상 있을 때만)
///  - 이 방 안의 단서만 카운트
///  - 전부 수집하면 문 잠금 해제
///  - 입장 직후엔 규칙 유예(enterGraceSeconds) 적용
/// </summary>
public class RoomController : MonoBehaviour
{
    [Header("방 설정")]
    [Tooltip("GameManager.CurrentRoomId 로 기록될 ID (예: 2-1, 2-2, 2-3)")]
    public string roomId = "2-1";

    [Tooltip("플레이어가 RoomZone을 통과하면 문을 닫고 잠급니다.")]
    public bool lockAllDoorsOnEnter = true;

    [TextArea] public string lockMessage = "문이 잠겼다. 이 방의 단서를 모두 모아야 한다.";

    [Tooltip("입장 직후 규칙 위반을 무시하는 유예 시간(초). 0이면 미적용")]
    public float enterGraceSeconds = 3f;

    [Header("참조(자동 채움 가능)")]
    public List<SlidingDoor> doors = new List<SlidingDoor>();
    public List<ClueItem> roomClues = new List<ClueItem>();

    // 진행 상태
    private readonly HashSet<ClueItem> collectedLocal = new HashSet<ClueItem>();
    private int totalLocal = 0;
    private bool entered = false;

    void Awake()
    {
        AutoFillIfEmpty();
        RecountLocalClues();
    }

    void OnEnable() { ClueItem.OnAnyClueCollected += OnAnyClueCollected; }
    void OnDisable() { ClueItem.OnAnyClueCollected -= OnAnyClueCollected; }

    /// <summary>RoomZone에서 호출: 플레이어가 방 내부로 들어왔다.</summary>
    public void OnPlayerEntered()
    {
        if (entered) return; // 이미 내부 상태면 1회만 처리
        entered = true;

        GameManager.Instance?.SetCurrentRoom(roomId);

        // 단서가 1개 이상 있을 때만 잠그기(0개면 잠글 이유가 없음)
        bool shouldLock = lockAllDoorsOnEnter && totalLocal > 0;

        if (shouldLock)
        {
            foreach (var d in doors)
            {
                if (!d) continue;
                d.Close();
                d.SetLocked(true, lockMessage);
            }
            Debug.Log($"[RoomController:{roomId}] 입장 → 문 잠금. {lockMessage}");
        }
        else
        {
            Debug.Log($"[RoomController:{roomId}] 입장 (잠그지 않음: 이 방 단서 {totalLocal}개)");
        }

        // 입장 유예(침묵 등 즉시 위반 방지)
        if (enterGraceSeconds > 0f)
            GameManager.Instance?.BeginRoomEnterGrace(enterGraceSeconds);

        collectedLocal.Clear();
        Debug.Log($"[RoomController:{roomId}] 이 방 단서 수: {totalLocal}");
    }

    private void OnAnyClueCollected(ClueItem item)
    {
        if (!item) return;
        if (!roomClues.Contains(item)) return;     // 이 방 소속만 집계

        if (collectedLocal.Add(item))
        {
            Debug.Log($"[RoomController:{roomId}] 수집 {collectedLocal.Count}/{totalLocal} → {item.name}");
            if (collectedLocal.Count >= totalLocal)
                UnlockAllDoors();
        }
    }

    private void UnlockAllDoors()
    {
        foreach (var d in doors)
        {
            if (!d) continue;
            d.SetLocked(false);
        }
        Debug.Log($"[RoomController:{roomId}] 모든 단서 수집 완료 → 문 잠금 해제");
        // 요구대로 해제 시 별도의 연출/페널티 없음(로그만).
        ProgressionManager.Instance?.MarkRoomCompleted(roomId);
    }

    // ───────────────────────────── 유틸 ─────────────────────────────
    [ContextMenu("Auto Fill (Doors & Clues)")]
    void AutoFillIfEmpty()
    {
        if (doors.Count == 0)
            doors.AddRange(GetComponentsInChildren<SlidingDoor>(true));

        if (roomClues.Count == 0)
            roomClues.AddRange(GetComponentsInChildren<ClueItem>(true));
    }

    [ContextMenu("Recount Local Clues")]
    void RecountLocalClues()
    {
        totalLocal = 0;
        if (roomClues != null)
        {
            foreach (var c in roomClues) if (c) totalLocal++;
        }
    }

    public bool AllLocalCluesCollected()
    {
        return collectedLocal.Count >= totalLocal;
    }

}
