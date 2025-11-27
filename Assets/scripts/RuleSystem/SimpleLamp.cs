using UnityEngine;
using cakeslice;   // QuickOutline

public class SimpleLamp : MonoBehaviour
{
    [Header("제어할 Light들 (비워두면 자식에서 자동 검색)")]
    public Light[] lights;

    [Header("사운드 - 원샷(스위치 클릭 등)")]
    public AudioSource sfxSource;      // Loop = false, PlayOnAwake = false
    public AudioClip turnOnClip;
    public AudioClip turnOffClip;

    [Header("사운드 - 허밍(지속)")]
    public AudioSource humLoop;        // Loop = true, PlayOnAwake = false

    [Header("빛(외곽선) 설정")]
    [Tooltip("전등이 켜져 있는 동안만 외곽선을 표시할지 여부")]
    public bool showOutlineWhileOn = true;

    [Tooltip("자식 오브젝트들에서 Outline(cakeslice)을 자동으로 찾을지 여부")]
    public bool autoFindOutlinesInChildren = true;

    public bool IsOn { get; private set; }

    // 외곽선 캐시
    private Outline[] _outlines;

    void Awake()
    {
        // Light 자동 수집
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>(true);

        // SFX 설정
        if (sfxSource)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        // 허밍 루프 설정
        if (humLoop)
        {
            humLoop.playOnAwake = false;
            humLoop.loop = true;
            if (humLoop.isPlaying) humLoop.Stop();
        }

        // Outline 수집
        if (autoFindOutlinesInChildren)
            _outlines = GetComponentsInChildren<Outline>(true);
        else
            _outlines = GetComponents<Outline>();

        // 시작은 불 꺼진 상태
        ApplyState(false);
        SetOutline(false);  // ★ 전등 외곽선도 OFF
        IsOn = false;
    }

    public void TurnOn()
    {
        if (IsOn) return;
        IsOn = true;

        ApplyState(true);   // Light 자체 ON

        if (sfxSource && turnOnClip)
            sfxSource.PlayOneShot(turnOnClip);

        if (humLoop && !humLoop.isPlaying)
            humLoop.Play();

        if (showOutlineWhileOn)
        {
            SetOutline(true);
            Debug.Log("[SimpleLamp] Outline ON"); // ★ 전등 외곽선 켜질 때 로그
        }
    }

    public void TurnOff()
    {
        if (!IsOn) return;
        IsOn = false;

        ApplyState(false);  // Light 자체 OFF

        if (sfxSource && turnOffClip)
            sfxSource.PlayOneShot(turnOffClip);

        if (humLoop && humLoop.isPlaying)
            humLoop.Stop();

        SetOutline(false);  // 전등 외곽선 OFF
    }

    // 상호작용용 토글 함수 (원하면 사용)
    public void Toggle()
    {
        if (IsOn) TurnOff();
        else TurnOn();
    }

    // Light 배열에 on/off 적용
    void ApplyState(bool on)
    {
        if (lights == null) return;
        foreach (var l in lights)
        {
            if (!l) continue;
            l.enabled = on;
        }
    }

    // Outline 일괄 ON/OFF
    void SetOutline(bool on)
    {
        if (_outlines == null) return;
        foreach (var ol in _outlines)
        {
            if (!ol) continue;
            ol.enabled = on;
        }
    }
}
