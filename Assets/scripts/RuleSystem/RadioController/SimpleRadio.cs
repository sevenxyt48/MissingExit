using UnityEngine;
using cakeslice; // QuickOutline

public class SimpleRadio : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource source;
    public bool randomAutoOn = false;
    public Vector2 firstOnDelay = new Vector2(2f, 8f);
    public bool IsOn { get; private set; }

    [Header("Light (Outline)")]
    [Tooltip("라디오가 켜져 있는 동안만 윤곽선을 표시합니다.")]
    public bool showOutlineWhileOn = true;
    [Tooltip("자식에서 cakeslice.Outline을 자동으로 찾습니다.")]
    public bool autoFindOutlinesInChildren = true;

    [Header("Penalty Sustain Key")]
    public string stopSustainReason = "라디오 규칙 위반";

    // 내부 캐시
    Outline[] _outlinesCached;

    void Awake()
    {
        // 오디오 초기화
        if (source)
        {
            source.playOnAwake = false;
            source.loop = true;
            if (source.isPlaying) source.Stop();   // 시작 시 무음 보장
        }
        IsOn = false;

        // 윤곽선 캐시 + 강제 OFF
        CacheOutlines();
        SetOutline(false);
    }

    void OnEnable()
    {
        // 혹시 비활성/재활성 되더라도 항상 꺼진 상태로 시작
        CacheOutlines();
        SetOutline(false);
    }

    void CacheOutlines()
    {
        if (autoFindOutlinesInChildren)
            _outlinesCached = GetComponentsInChildren<Outline>(true);
        else
            _outlinesCached = GetComponents<Outline>();
    }

    void Start()
    {
        // 자동 켜짐 옵션이 있으면 일정 시간 뒤 On
        if (randomAutoOn)
            Invoke(nameof(TurnOn), Random.Range(firstOnDelay.x, firstOnDelay.y));
    }

    public void TurnOn()
    {
        if (IsOn) return;
        IsOn = true;

        if (source && !source.isPlaying)
        {
            source.loop = true;
            source.Play();
        }

        Debug.Log("[SimpleRadio] TurnOn");

        // 라디오 켜짐 → 윤곽선 ON
        if (showOutlineWhileOn)
            SetOutline(true);
    }

    public void TurnOff()
    {
        if (!IsOn) return;
        IsOn = false;

        if (source && source.isPlaying)
            source.Stop();

        // 라디오 끄면 지속 사운드도 정지
        PenaltyManager.Instance?.StopSustain(stopSustainReason);

        Debug.Log("[SimpleRadio] TurnOff");

        // 라디오 꺼짐 → 윤곽선 OFF
        SetOutline(false);
    }

    // InteractOnKey 에서 쓰기 좋게 토글 함수 하나 만들어 둠(원하면 안 써도 됨)
    public void Toggle()
    {
        if (IsOn) TurnOff();
        else TurnOn();
    }

    // 윤곽선 일괄 제어
    void SetOutline(bool on)
    {
        if (_outlinesCached == null) return;
        foreach (var ol in _outlinesCached)
            if (ol) ol.enabled = on;
    }
}
