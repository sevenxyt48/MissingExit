// SimpleWindow.cs
using UnityEngine;

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

    public bool IsOpen { get; private set; }
    Vector3 closedLocal, openLocal;
    bool moving;
    Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
        if (col) { col.enabled = true; col.isTrigger = false; }
        if (!panel) panel = transform; // 최소 안전장치

        closedLocal = panel.localPosition;
        openLocal = closedLocal + openOffset;

        if (sfx) { sfx.playOnAwake = false; sfx.loop = false; }
    }

    void Update()
    {
        if (!moving) return;
        Vector3 target = IsOpen ? openLocal : closedLocal;
        panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, moveSpeed * Time.deltaTime);
        if (Vector3.SqrMagnitude(panel.localPosition - target) < 0.0001f)
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
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        moving = true;
        if (sfx && closeClip) sfx.PlayOneShot(closeClip);
    }

    public void Toggle() { if (IsOpen) Close(); else Open(); }
}
