using UnityEngine;
using cakeslice; // QuickOutline

public class SimpleLamp : MonoBehaviour
{
    [Header("제어할 Light들 (비워두면 자식에서 자동 검색)")]
    public Light[] lights;

    [Header("사운드 - 원샷(스위치 클릭 등)")]
    public AudioSource sfxSource;      // Loop=Off, PlayOnAwake=Off
    public AudioClip turnOnClip;
    public AudioClip turnOffClip;

    [Header("사운드 - 허밍(지속)")]
    public AudioSource humLoop;        // Loop=On, PlayOnAwake=Off (선택)

    [Header("Outline (상태 기반 표시)")]
    [Tooltip("전등이 켜져 있는 동안만 윤곽선을 표시합니다.")]
    public bool showOutlineWhileOn = true;
    [Tooltip("자식에서 cakeslice.Outline을 자동으로 찾습니다.")]
    public bool autoFindOutlinesInChildren = true;

    public bool IsOn { get; private set; }

    // ─ Outline 관리
    Outline[] _outlines;
    bool _consumed; // 한번 상호작용 이후엔 영구적으로 윤곽선 끔

    void Awake()
    {
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>(true);

        if (sfxSource) { sfxSource.playOnAwake = false; sfxSource.loop = false; }
        if (humLoop) { humLoop.playOnAwake = false; humLoop.loop = true; if (humLoop.isPlaying) humLoop.Stop(); }

        _outlines = autoFindOutlinesInChildren ? GetComponentsInChildren<Outline>(true)
                                               : GetComponents<Outline>();

        ApplyState(false);
        SetOutline(false);
        IsOn = false;
    }

    public void TurnOn()
    {
        if (IsOn) return;
        IsOn = true;
        ApplyState(true);

        if (sfxSource && turnOnClip) sfxSource.PlayOneShot(turnOnClip);
        if (humLoop && !humLoop.isPlaying) humLoop.Play();

        if (showOutlineWhileOn && !_consumed) SetOutline(true);
    }

    public void TurnOff()
    {
        if (!IsOn) return;
        IsOn = false;
        ApplyState(false);

        if (sfxSource && turnOffClip) sfxSource.PlayOneShot(turnOffClip);
        if (humLoop && humLoop.isPlaying) humLoop.Stop();

        SetOutline(false);
    }

    void ApplyState(bool on)
    {
        if (lights != null)
            foreach (var l in lights) if (l) l.enabled = on;
    }

    // ─ 상호작용 이벤트용: 토글 후 첫 상호작용이면 윤곽선 영구 Off
    public void Toggle() { if (IsOn) TurnOff(); else TurnOn(); }

    public void ToggleAndConsume()
    {
        Toggle();
        ConsumeOutline();
    }

    public void ConsumeOutline()
    {
        if (_consumed) return;
        _consumed = true;
        SetOutline(false);
    }

    void SetOutline(bool on)
    {
        if (_outlines == null) return;
        foreach (var ol in _outlines) if (ol) ol.enabled = on;
    }
}
