using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("씬 이름")]
    [SerializeField] private string endSceneName = "GameEnd";
    [SerializeField] private string gameplaySceneName = "GameScene";
    public string GameplaySceneName => gameplaySceneName;

    [Header("단서 진행(전체)")]
    [Tooltip("0이면 시작 시 자동 계산(비활성 포함)")]
    [SerializeField] private int totalClues = 0;
    [SerializeField] private bool autoCountAtStart = true;

    private readonly HashSet<string> collectedClues = new HashSet<string>();
    public int CollectedCount => collectedClues.Count;
    public int TotalClues => totalClues;

    [Header("규칙 위반")]
    [Tooltip("이 수치 이상 위반하면 즉시 나쁜 엔딩")]
    [SerializeField] private int violationLimit = 10;   // 기본 10개
    public int ViolationCount { get; private set; } = 0;

    [Tooltip("게임 시작/씬 전환 직후 이 시간 동안은 위반을 무시(초)")]
    public float globalStartupGrace = 2f;
    private float sceneStartTime;

    [Header("현재 교실 상태(읽기 전용)")]
    [Tooltip("RoomZone에서 실행 중 갱신됩니다. 예: 2-1 / 2-2 / 2-3 / 2-4")]
    [SerializeField] private string currentRoomId = "";
    public string CurrentRoomId => currentRoomId;

    [Header("단서 열람 유예")]
    [Tooltip("단서 화면을 닫은 뒤 이 시간 동안 규칙 감시를 일시 정지")]
    public float cluePostCloseGrace = 2f;
    public bool IsClueOpen { get; private set; } = false;
    private float clueGraceUntil = 0f;

    [Header("스토리 열람 유예")]
    [Tooltip("스토리 화면을 닫은 뒤 이 시간 동안 규칙 감시를 일시 정지")]
    public float storyPostCloseGrace = 1.5f;
    public bool IsStoryOpen { get; private set; } = false;
    private float storyGraceUntil = 0f;

    // RoomController.OnPlayerEntered() 에서 BeginRoomEnterGrace로 설정
    private float roomEnterGraceUntil = 0f;

    [Header("엔딩 문구(엔딩 화면에서 타이핑)")]
    [TextArea]
    public string goodEndingText =
        "“드디어 모든 진실과 마주한다. 과거의 자신과 피해자의 그림자가 겹치며, 해방감을 느낀다.”";
    [TextArea]
    public string badEndingText =
        "“누군가 회피하면, 진실의 방 문이 닫히고 모든 기억이 재생된다.”\n게임 리셋 알림, 공동 책임 강화.";

    public enum EndingType { None, Good, Bad }
    public EndingType LastEnding { get; private set; } = EndingType.None;

    public System.Action<int, int> OnProgressChanged;
    private bool hasEnded = false;

    // 방별 스토리 데이터 (현재는 직접 사용 안 해도 됨)
    private readonly Dictionary<string, string[]> roomStories = new Dictionary<string, string[]>();

    // 좋은 엔딩을 위해 "반드시 스토리를 읽어야 하는" 교실 목록
    private static readonly string[] roomsRequiredForGoodEnding = { "2-1", "2-2", "2-3" };

    // 각 교실 출구 스토리를 다 읽었는지 기록
    private readonly HashSet<string> exitStoryFinishedRooms = new HashSet<string>();

    // EndingScreen에서 PlayerPrefs로 마지막 엔딩 복구용
    private const string PP_LAST_ENDING = "X_LastEnding__1";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            InitRoomStories();
            HookProgressionEvents();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        sceneStartTime = Time.time;

        if (autoCountAtStart || totalClues <= 0)
        {
            totalClues = FindObjectsOfType<ClueItem>(true).Length;
            Debug.Log($"[GM] 총 단서 수: {totalClues}");
        }

        if (!Application.CanStreamedLevelBeLoaded(endSceneName))
            Debug.LogError($"[GM] 엔딩 씬 '{endSceneName}' 이(가) Build Settings에 없습니다.");
    }

    private void OnValidate()
    {
        if (violationLimit < 1) violationLimit = 1;
        if (globalStartupGrace < 0f) globalStartupGrace = 0f;
        if (cluePostCloseGrace < 0f) cluePostCloseGrace = 0f;
        if (storyPostCloseGrace < 0f) storyPostCloseGrace = 0f;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        sceneStartTime = Time.time;
        currentRoomId = "";
        IsClueOpen = false;
        clueGraceUntil = 0f;

        IsStoryOpen = false;
        storyGraceUntil = 0f;
        roomEnterGraceUntil = 0f;

        HookProgressionEvents();
        hasEnded = false;

        exitStoryFinishedRooms.Clear();
    }

    public void SetCurrentRoom(string roomId)
    {
        currentRoomId = roomId ?? "";
        Debug.Log($"[GM] 현재 교실: {currentRoomId}");
    }

    // ───────────────── 단서 열람 통지 ─────────────────
    public void NotifyClueOpened()
    {
        IsClueOpen = true;
        clueGraceUntil = 0f;
    }

    public void NotifyClueClosed()
    {
        IsClueOpen = false;
        clueGraceUntil = Time.time + cluePostCloseGrace;
    }

    public bool InClueGrace() => IsClueOpen || Time.time < clueGraceUntil;

    // ───────────────── 스토리 열람 통지(StoryViewer에서 사용) ─────────────────
    public void SetStoryOpen(bool open)
    {
        IsStoryOpen = open;
        if (open)
        {
            storyGraceUntil = 0f;
        }
        else
        {
            if (storyPostCloseGrace > 0f)
                storyGraceUntil = Time.time + storyPostCloseGrace;
        }
    }

    public bool InStoryGrace() => IsStoryOpen || Time.time < storyGraceUntil;

    // ───────────────── 방 입장 유예( RoomController에서 사용 ) ─────────────────
    public void BeginRoomEnterGrace(float seconds)
    {
        if (seconds <= 0f) return;
        float until = Time.time + seconds;
        if (until > roomEnterGraceUntil) roomEnterGraceUntil = until;
        Debug.Log($"[GM] 방 입장 유예 시작: {seconds:F1}s (until={roomEnterGraceUntil:F2})");
    }

    public bool InRoomEnterGrace() => Time.time < roomEnterGraceUntil;

    // ───────────────── 단서 수집 보고 ─────────────────
    public void CollectClue(string clueID)
    {
        if (string.IsNullOrWhiteSpace(clueID)) clueID = "Unnamed_" + (collectedClues.Count + 1);
        if (hasEnded) return;

        if (collectedClues.Add(clueID))
        {
            Debug.Log($"[GM] 수집: {clueID} ({collectedClues.Count}/{totalClues})");
            OnProgressChanged?.Invoke(collectedClues.Count, totalClues);

            if (!hasEnded && totalClues > 0 && collectedClues.Count >= totalClues)
            {
                Debug.Log("[GM] 모든 단서 수집 완료 (아직 좋은 엔딩 아님). 출구 스토리 조건까지 충족해야 함.");
                CheckGoodEndingCondition();
            }
        }
    }

    // RoomExitStoryTrigger에서 호출: 해당 방의 출구 스토리를 끝까지 읽었을 때
    public void NotifyExitStoryFinished(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return;
        if (!exitStoryFinishedRooms.Add(roomId))
        {
            // 이미 처리한 방이면 무시
            return;
        }

        Debug.Log($"[GM] 출구 스토리 열람 완료: {roomId}");
        CheckGoodEndingCondition();
    }

    // "좋은 엔딩으로 넘어가도 되는지" 조건 체크
    private void CheckGoodEndingCondition()
    {
        if (hasEnded) return;

        // 1) 단서를 전부 모았는지
        bool allCluesCollected = (totalClues > 0 && collectedClues.Count >= totalClues);
        if (!allCluesCollected) return;

        // 2) 필수 교실(2-1, 2-2, 2-3)의 출구 스토리를 모두 읽었는지
        foreach (var requiredRoom in roomsRequiredForGoodEnding)
        {
            if (!exitStoryFinishedRooms.Contains(requiredRoom))
            {
                // 아직 읽지 않은 방이 있으면 좋은 엔딩 조건 미충족
                return;
            }
        }

        // 여기까지 통과했다 = 모든 단서 + 2-1/2-2/2-3 출구 스토리까지 확인 완료
        Debug.Log("[GM] 좋은 엔딩 조건 충족: 모든 단서 + 2-1/2-2/2-3 출구 스토리 열람 완료 → 좋은 엔딩 진입.");
        TriggerGoodEnding();
    }

    // ───────────────── 규칙 위반 ─────────────────
    // 기존 호출과의 하위 호환용
    public bool TryReportViolation(string reason = null)
    {
        // 기존 로직을 유지하면서, 새 플래그 버전으로 위임
        return TryReportViolation(reason,
            ignoreClueGrace: false,
            ignoreStartupGrace: false,
            ignoreRoomEnterGrace: false);
    }

    // PenaltyManager(새 버전)에서 사용하는 확장 버전
    public bool TryReportViolation(
        string reason,
        bool ignoreClueGrace,
        bool ignoreStartupGrace,
        bool ignoreRoomEnterGrace)
    {
        if (hasEnded)
        {
            Debug.Log("[GM] 무시: 이미 엔딩 상태");
            return false;
        }

        // 스토리 열람/유예는 항상 우선(플래그 무시)
        if (InStoryGrace())
        {
            Debug.Log($"[GM] 무시(스토리 열람/유예): {reason}");
            return false;
        }

        // 단서 열람 유예
        if (!ignoreClueGrace && InClueGrace())
        {
            Debug.Log($"[GM] 무시(단서 열람/유예): {reason}");
            return false;
        }

        // 글로벌 시작 유예
        float elapsed = Time.time - sceneStartTime;
        if (!ignoreStartupGrace && elapsed < globalStartupGrace)
        {
            Debug.Log($"[GM] 무시(시작 유예 {elapsed:F2}s/{globalStartupGrace}s): {reason}");
            return false;
        }

        // 방 입장 직후 유예(예: 교실 들어와서 바로 침묵 규칙 터지는 것 방지)
        if (!ignoreRoomEnterGrace && InRoomEnterGrace())
        {
            Debug.Log($"[GM] 무시(방 입장 유예): {reason}");
            return false;
        }

        // 2-4(무규칙) 제외
        if (currentRoomId == "2-4")
        {
            Debug.Log($"[GM] 무시(2-4 무규칙): {reason}");
            return false;
        }

        ViolationCount++;
        Debug.Log($"[GM] 규칙 위반 수락: {reason}  → {ViolationCount}/{violationLimit} (room={currentRoomId})");

        if (ViolationCount >= violationLimit)
            TriggerBadEnding();

        return true;
    }

    // 하위 호환
    public void ReportViolation(string reason = null) => TryReportViolation(reason);

    // ───────────────── 엔딩 트리거 ─────────────────
    private void TriggerGoodEnding()
    {
        if (hasEnded) return;
        hasEnded = true;
        LastEnding = EndingType.Good;

        PersistLastEnding(LastEnding);

        SceneManager.LoadScene(endSceneName);
        Debug.Log("[GM] 좋은 엔딩 씬으로 이동.");
    }

    private void TriggerBadEnding()
    {
        if (hasEnded) return;
        hasEnded = true;
        LastEnding = EndingType.Bad;

        PersistLastEnding(LastEnding);

        SceneManager.LoadScene(endSceneName);
        Debug.Log("[GM] 규칙 과다 위반 → 나쁜 엔딩");
    }

    // PlayerPrefs에 마지막 엔딩 기록
    void PersistLastEnding(EndingType t)
    {
        int val = (t == EndingType.Bad) ? 2 : 1;
        PlayerPrefs.SetInt(PP_LAST_ENDING, val);
        PlayerPrefs.Save();
    }

    // EndingScreen에서 사용: GameManager 인스턴스를 못 찾을 때 백업용
    public static EndingType ReadPersistedLastEnding()
    {
        int v = PlayerPrefs.GetInt(PP_LAST_ENDING, 1);
        return (v == 2) ? EndingType.Bad : EndingType.Good;
    }

    // ───────────────── ProgressionManager 연결 (현재는 로그만 남겨도 무방) ─────────────────
    private void HookProgressionEvents()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnRoomCompleted -= OnRoomCompletedFromProgress;
            ProgressionManager.Instance.OnRoomCompleted += OnRoomCompletedFromProgress;
        }
    }

    private void OnRoomCompletedFromProgress(string roomId)
    {
        Debug.Log($"[GM] 방 완료 콜백 수신: {roomId} (출구 스토리는 RoomExitStoryTrigger가 처리).");
        // 필요하다면 여기서 ShowRoomStory(roomId)를 호출할 수도 있지만
        // 현재 프로젝트에서는 exit trigger 기반으로만 스토리를 띄우므로 로그만 남긴다.
    }

    // ───────────────── 방별 스토리 기본값 (현재는 사용 안 해도 됨) ─────────────────
    private void InitRoomStories()
    {
        roomStories.Clear();

        roomStories["2-1"] = new[]
        {
            "2-1반 스토리 1페이지 예시."
        };

        roomStories["2-2"] = new[]
        {
            "2-2반 스토리 1페이지 예시."
        };

        roomStories["2-3"] = new[]
        {
            "2-3반 스토리 1페이지 예시."
        };

        roomStories["2-4"] = new[]
        {
            "2-4반 스토리 1페이지 예시."
        };
    }

    public string[] GetRoomStoryPages(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return null;
        if (roomStories.TryGetValue(roomId, out var pages))
            return pages;
        return null;
    }

    public void ShowRoomStory(string roomId)
    {
        var pages = GetRoomStoryPages(roomId);
        if (pages == null || pages.Length == 0)
        {
            Debug.Log($"[GM] {roomId} 스토리 없음 → 표시하지 않음");
            return;
        }

        var viewer = Object.FindObjectOfType<StoryViewer>(true);
        if (!viewer)
        {
            Debug.LogWarning("[GM] StoryViewer를 찾지 못했습니다. 스토리 표시 불가");
            return;
        }

        SetStoryOpen(true);

        var player = Object.FindObjectOfType<FirstPersonController>();
        if (player != null) player.SetControlEnabled(false);

        viewer.Open(pages, () =>
        {
            SetStoryOpen(false);

            var p = Object.FindObjectOfType<FirstPersonController>();
            if (p != null) p.SetControlEnabled(true);
        });
    }
}
