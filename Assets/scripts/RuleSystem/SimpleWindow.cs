using UnityEngine;
using cakeslice; // QuickOutline

[RequireComponent(typeof(Collider))]
public class SimpleWindow : MonoBehaviour
{
    [Header("움직일 패널(필수)")]
    public Transform panel;
    public Vector3 openOffset = new Vector3(0f, 0f, -0.8f);
    public float moveSpeed = 2.5f;

    [Header("SFX(선택)")]
    public AudioSource sfx;
    public AudioClip openClip, closeClip;

    [Header("Outline (상태 기반 표시)")]
    [Tooltip("창문이 열려있는 동안만 윤곽선을 표시합니다.")]
    public bool showOutlineWhileOpen = true;
    [Tooltip("자식에서 cakeslice.Outline을 자동으로 찾습니다.")]
    public bool autoFindOutlinesInChildren = true;

    public bool IsOpen { get; private set; }

    Vector3 closedLocal, openLocal;
    bool moving;
    Collider col;

    // ─ Outline 관리
    Outline[] _outlines;
    bool _consumed; // 한번 상호작용 이후엔 영구적으로 윤곽선 끔

    void Awake()
    {
        col = GetComponent<Collider>();
        if (col) { col.enabled = true; col.isTrigger = false; }
        if (!panel) panel = transform;

        closedLocal = panel.localPosition;
        openLocal = closedLocal + openOffset;

        if (sfx) { sfx.playOnAwake = false; sfx.loop = false; }

        // 윤곽선 수집
        _outlines = autoFindOutlinesInChildren ? GetComponentsInChildren<Outline>(true)
                                               : GetComponents<Outline>();
        SetOutline(false);
        IsOpen = false;
    }

    void Update()
    {
        if (!moving) return;
        Vector3 target = IsOpen ? openLocal : closedLocal;
        panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, moveSpeed * Time.deltaTime);
        if ((panel.localPosition - target).sqrMagnitude < 0.0001f)
        {
            panel.localPosition = target;
            moving = false;
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        moving = true;
        if (sfx && openClip) sfx.PlayOneShot(openClip);

        if (showOutlineWhileOpen && !_consumed) SetOutline(true);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        moving = true;
        if (sfx && closeClip) sfx.PlayOneShot(closeClip);

        SetOutline(false);
    }

    public void Toggle() { if (IsOpen) Close(); else Open(); }

    // ─ 상호작용 이벤트용: 토글 후 첫 상호작용이면 윤곽선 영구 Off
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
