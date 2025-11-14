using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BgmManager : MonoBehaviour
{
    public static BgmManager Instance;

    [System.Serializable]
    public struct RoomBgm
    {
        public string roomId;               // 예: "Hall", "2-1", "2-2", ...
        public AudioClip clip;              // 해당 방 전용 BGM
        [Range(0f, 1f)] public float volume; // 방별 기본 볼륨(0~1)
    }

    [Header("Room → BGM 매핑")]
    public RoomBgm[] roomBgms;

    [Header("페이드")]
    public float fadeSeconds = 1.2f;

    [Header("전역 볼륨/덕킹")]
    [Range(0f, 1f)] public float masterVolume = 1f;   // 전체 BGM 볼륨
    [Range(0f, 1f)] public float duckVolume = 0.35f;  // 단서/팝업 시 낮출 목표
    public float duckLerp = 6f;                       // 덕킹/복귀 속도

    // 내부 상태
    private Dictionary<string, RoomBgm> map;
    private AudioSource a, b;              // 교차 페이드용 2개
    private AudioSource current;           // 현재 출력 소스
    private AudioClip playingClip;
    private float currentRoomVol = 1f;     // 방별 개별 볼륨(0~1)
    private float targetOutVol = 1f;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // 매핑 빌드
        map = new Dictionary<string, RoomBgm>();
        foreach (var x in roomBgms)
        {
            if (!string.IsNullOrEmpty(x.roomId) && x.clip)
                map[x.roomId] = x;
        }

        // 오디오소스 2개 준비
        a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { a, b })
        {
            s.playOnAwake = false;
            s.loop = true;
            s.volume = 0f;
        }
        current = a;
    }

    void Update()
    {
        // 단서/팝업 열림 여부로 덕킹 판단 (GameManager에 IsClueOpen이 있다고 가정)
        var gm = GameManager.Instance;
        bool duck = gm && gm.IsClueOpen;

        // 목표 볼륨 = (덕킹 or 마스터) × 방별 볼륨
        float baseVol = duck ? duckVolume : masterVolume;
        targetOutVol = Mathf.Clamp01(baseVol * currentRoomVol);

        // 현재 소스로 부드럽게 반영
        if (current)
            current.volume = Mathf.MoveTowards(current.volume, targetOutVol, Time.deltaTime * duckLerp);
    }

    /// <summary>방 입장 시 호출: 해당 roomId 전용 BGM으로 크로스페이드.</summary>
    public void PlayForRoom(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return;
        if (!map.TryGetValue(roomId, out var data)) return;

        // 방별 볼륨 갱신(같은 곡이어도 볼륨만 바뀔 수 있음)
        currentRoomVol = Mathf.Clamp01(data.volume);

        // 같은 클립이면 재시작 없이 볼륨만 유지시키고 끝
        if (playingClip == data.clip) return;

        // 다음 소스 준비
        var next = (current == a) ? b : a;
        next.clip = data.clip;
        next.volume = 0f;
        next.Play();

        StopAllCoroutines();
        StartCoroutine(CoCrossfade(current, next));

        current = next;
        playingClip = data.clip;
    }

    private IEnumerator CoCrossfade(AudioSource from, AudioSource to)
    {
        float t = 0f;
        float fromStart = from ? from.volume : 0f;

        // 시작 시점 목표(덕킹 여부와 무관하게 현재 설정 기준으로)
        float startTarget = Mathf.Clamp01(masterVolume * currentRoomVol);

        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeSeconds);

            if (from) from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (to) to.volume = Mathf.Lerp(0f, startTarget, k);
            yield return null;
        }

        if (from) { from.volume = 0f; from.Stop(); }
        if (to) { to.volume = startTarget; }
    }

    /// <summary>(선택) 즉시 정지하고 싶을 때 사용</summary>
    public void StopBgm()
    {
        StopAllCoroutines();
        if (a) { a.Stop(); a.volume = 0f; }
        if (b) { b.Stop(); b.volume = 0f; }
        playingClip = null;
    }
}
