using UnityEngine;
using cakeslice; // ★ QuickOutline 네임스페이스 (윤곽선 제어용)

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

    // 내부
    Outline[] _outlinesCached;

    void Awake()
    {
        if (source)
        {
            source.playOnAwake = false;
            source.loop = true;
            if (source.isPlaying) source.Stop(); // 시작 시 무음 보장
        }
        IsOn = false;

        // ★ 윤곽선 컴포넌트 수집(자식 포함)
        _outlinesCached = autoFindOutlinesInChildren
            ? GetComponentsInChildren<Outline>(true)
            : GetComponents<Outline>();

        // 시작 상태: 라디오 꺼짐 → 윤곽선도 Off
        SetOutline(false);
    }

    void OnEnable()
    {
        if (source && source.isPlaying) source.Stop();
        IsOn = false;
        SetOutline(false);
    }

    void Start()
    {
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

        // ★ 라디오 켜짐 → 윤곽선 On
        if (showOutlineWhileOn) SetOutline(true);
    }

    public void TurnOff()
    {
        if (!IsOn) return;
        IsOn = false;
        if (source && source.isPlaying) source.Stop();

        // 라디오를 끄는 즉시 지속 사운드도 정지
        PenaltyManager.Instance?.StopSustain(stopSustainReason);

        Debug.Log("[SimpleRadio] TurnOff");

        // ★ 라디오 꺼짐 → 윤곽선 Off
        SetOutline(false);
    }

    // ─────────────────────────────
    // 윤곽선 일괄 제어
    // ─────────────────────────────
    void SetOutline(bool on)
    {
        if (_outlinesCached == null) return;
        foreach (var ol in _outlinesCached)
            if (ol) ol.enabled = on;
    }
}
