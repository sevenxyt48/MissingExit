using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("씬 이름")]
    [SerializeField] private string endSceneName = "GameEnd";
    [SerializeField] private string gameplaySceneName = "Main";
    public string GameplaySceneName => gameplaySceneName;

    [Header("단서 진행(전체)")]
    [SerializeField] private int totalClues = 0;
    [SerializeField] private bool autoCountAtStart = true;

    private readonly HashSet<string> collectedClues = new HashSet<string>();
    public int CollectedCount => collectedClues.Count;
    public int TotalClues => totalClues;

    [Header("규칙 위반")]
    [SerializeField] private int violationLimit = 10;
    public int ViolationCount { get; private set; } = 0;

    public float globalStartupGrace = 2f;
    private float sceneStartTime;

    [Header("현재 교실 상태(읽기 전용)")]
    [SerializeField] private string currentRoomId = "";
    public string CurrentRoomId => currentRoomId;

    [Header("단서 열람 유예")]
    public float cluePostCloseGrace = 2f;
    public bool IsClueOpen { get; private set; } = false;
    private float clueGraceUntil = 0f;

    [Header("엔딩 문구(엔딩 화면에서 타이핑)")]
    [TextArea]
    public string goodEndingText =
        "“드디어 모든 진실과 마주한다. 과거의 자신과 피해자의 그림자가 겹치며, 해방감을 느낀다.”";
    [TextArea]
    public string badEndingText =
        "“누군가 회피하면, 진실의 방 문이 닫히고 모든 기억이 재생된다.”";

    public enum EndingType { None, Good, Bad }
    public EndingType LastEnding { get; private set; } = EndingType.None;

    public System.Action<int, int> OnProgressChanged;

    private bool hasEnded = false;

    [Header("엔딩 타이밍")]
    public float goodEndingDelayAfterClose = 1.0f;
    private bool pendingGoodEnding = false;

    private float roomEnterGraceUntil = 0f;

    // ★ Persist 키
    private const string PP_LAST_ENDING = "X_LastEnding__1"; // 1=Good, 2=Bad

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
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
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        sceneStartTime = Time.time;
        currentRoomId = "";
        IsClueOpen = false;
        clueGraceUntil = 0f;
        roomEnterGraceUntil = 0f;

        if (s.name == gameplaySceneName)
        {
            ResetRunState();
            if (autoCountAtStart || totalClues <= 0)
                totalClues = FindObjectsOfType<ClueItem>(true).Length;

            OnProgressChanged?.Invoke(collectedClues.Count, totalClues);
            Debug.Log("[GM] 메인 씬 진입 → 새 게임 상태 초기화 완료");
        }
        else if (s.name == endSceneName)
        {
            Debug.Log($"[GM] 엔딩 씬 진입. LastEnding={LastEnding}");
        }
    }

    private void ResetRunState()
    {
        hasEnded = false;
        pendingGoodEnding = false;
        ViolationCount = 0;
        collectedClues.Clear();
        LastEnding = EndingType.None;
        PlayerPrefs.DeleteKey(PP_LAST_ENDING); // 이전 엔딩 흔적 제거
    }

    public void SetCurrentRoom(string roomId)
    {
        currentRoomId = roomId ?? "";
        Debug.Log($"[GM] 현재 교실: {currentRoomId}");
    }

    public void BeginRoomEnterGrace(float seconds)
    {
        if (seconds <= 0f) return;
        float until = Time.time + seconds;
        if (until > roomEnterGraceUntil) roomEnterGraceUntil = until;
        Debug.Log($"[GM] 입장 유예 시작: {seconds:F1}s (until {roomEnterGraceUntil:F2})");
    }

    public void NotifyClueOpened()
    {
        IsClueOpen = true;
        clueGraceUntil = 0f;
    }
    public void NotifyClueClosed()
    {
        IsClueOpen = false;
        clueGraceUntil = Time.time + cluePostCloseGrace;

        if (pendingGoodEnding && !hasEnded)
        {
            pendingGoodEnding = false;
            Debug.Log("[GM] 단서 UI 닫힘 감지 — 지연 후 좋은 엔딩 진입");
            StartCoroutine(CoTriggerGoodEndingAfterDelay());
        }
    }

    private System.Collections.IEnumerator CoTriggerGoodEndingAfterDelay()
    {
        if (goodEndingDelayAfterClose > 0f)
            yield return new WaitForSecondsRealtime(goodEndingDelayAfterClose);
        TriggerGoodEnding();
    }

    public bool InClueGrace() => IsClueOpen || Time.time < clueGraceUntil;

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
                if (IsClueOpen)
                {
                    pendingGoodEnding = true;
                    Debug.Log("[GM] 모든 단서 수집됨 — 단서 UI 닫힘을 대기(pendingGoodEnding=true)");
                }
                else
                {
                    StartCoroutine(CoTriggerGoodEndingAfterDelay());
                }
            }
        }
    }

    public bool TryReportViolation(string reason = null)
    {
        if (hasEnded) { Debug.Log("[GM] 무시: 이미 엔딩 상태"); return false; }
        if (InClueGrace()) { Debug.Log($"[GM] 무시(단서 열람/유예): {reason}"); return false; }
        if (Time.time < roomEnterGraceUntil) { Debug.Log($"[GM] 무시(입장 유예): {reason}"); return false; }

        float elapsed = Time.time - sceneStartTime;
        if (elapsed < globalStartupGrace)
        {
            Debug.Log($"[GM] 무시(시작 유예 {elapsed:F2}s/{globalStartupGrace}s): {reason}");
            return false;
        }

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

    public void ReportViolation(string reason = null) => TryReportViolation(reason);

    private void TriggerGoodEnding()
    {
        if (hasEnded) return;
        hasEnded = true;
        LastEnding = EndingType.Good;
        PersistLastEnding(LastEnding);
        Debug.Log("[GM] 모든 단서 수집 완료 → 좋은 엔딩");
        SceneManager.LoadScene(endSceneName);
    }

    private void TriggerBadEnding()
    {
        if (hasEnded) return;
        hasEnded = true;
        LastEnding = EndingType.Bad;
        PersistLastEnding(LastEnding);
        Debug.Log("[GM] 규칙 과다 위반 → 나쁜 엔딩");
        SceneManager.LoadScene(endSceneName);
    }

    private void PersistLastEnding(EndingType t)
    {
        PlayerPrefs.SetInt(PP_LAST_ENDING, t == EndingType.Bad ? 2 : 1);
        PlayerPrefs.Save();
    }

    public static EndingType ReadPersistedLastEnding()
    {
        int v = PlayerPrefs.GetInt(PP_LAST_ENDING, 1); // 기본 Good
        return (v == 2) ? EndingType.Bad : EndingType.Good;
    }
}
