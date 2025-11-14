using UnityEngine;

/// <summary>
/// 교실 문을 나갈 때(복도 쪽 트리거에서) 스토리 일기 화면을 띄우는 트리거
/// - 플레이어가 해당 방에서 나갈 때 한 번만 동작
/// - 스토리Viewer 열기 전에 storyPages 유효성 검사
/// - 만약 스토리를 못 띄우면 플레이어 컨트롤을 건드리지 않음 (멈추는 버그 방지)
/// - ★ exit_trigger에 닿는 순간, GameManager.CurrentRoomId 를 ""(복도)로 변경
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomExitStoryTrigger : MonoBehaviour
{
    [Header("이 트리거가 속한 교실 ID (예: 2-1)")]
    public string roomId = "2-1";

    [Header("플레이어 태그")]
    public string playerTag = "Player";

    [Header("스토리 데이터 (페이지별 텍스트)")]
    [TextArea(3, 6)]
    public string[] storyPages;

    [Header("UI / 플레이어 참조")]
    public StoryViewer storyViewer;          // StoryCanvas에 붙은 StoryViewer
    public FirstPersonController player;     // 플레이어 컨트롤 잠그고 풀려고

    private bool alreadyShown = false;       // 중복 실행 방지

    void Reset()
    {
        // Collider를 trigger로 강제
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    /// <summary>
    /// 플레이어가 교실 출구 트리거에 "닿는 순간" 호출.
    /// 여기서 방을 떠났다고 판정하고 CurrentRoomId 를 비운다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // ★ 여기서부터는 "복도"로 나간 것으로 간주 → 방 규칙은 꺼져야 함
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetCurrentRoom(string.Empty); // "" = 복도 / 어느 방도 아님
            Debug.Log($"[ExitTrigger] {roomId} 출구 도달 → CurrentRoomId를 빈 값으로 설정 (복도)");
        }

        // 스토리를 한 번 띄운 뒤에는 다시 안 띄운다.
        if (alreadyShown) return;
        alreadyShown = true;

        // 스토리 준비가 안 되어 있으면 그냥 경고만 띄우고 끝
        bool canShowStory =
            (storyViewer != null) &&
            (storyPages != null && storyPages.Length > 0);

        if (!canShowStory)
        {
            Debug.LogWarning($"[ExitTrigger] {roomId} 스토리 준비 안됨 (storyViewer 또는 storyPages가 비었음).");
            return;
        }

        // 플레이어 입력 잠금
        if (player != null)
            player.SetControlEnabled(false);

        // 스토리 열기
        storyViewer.Open(storyPages, onClosed: () =>
        {
            // 스토리 닫힌 뒤 플레이어 다시 움직일 수 있게
            if (player != null)
                player.SetControlEnabled(true);

            // ★ 이 방의 출구 스토리를 끝까지 읽었다고 GameManager에 보고
            //    (좋은 엔딩 조건: 2-1/2-2/2-3 출구 스토리 + 전체 단서 수집)
            if (GameManager.Instance != null && !string.IsNullOrEmpty(roomId))
            {
                GameManager.Instance.NotifyExitStoryFinished(roomId);
            }
        });
    }
}
