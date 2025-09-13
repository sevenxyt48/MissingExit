using UnityEngine;

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

    public bool IsOn { get; private set; }

    void Awake()
    {
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>(true);

        if (sfxSource) { sfxSource.playOnAwake = false; sfxSource.loop = false; }
        if (humLoop) { humLoop.playOnAwake = false; humLoop.loop = true; if (humLoop.isPlaying) humLoop.Stop(); }

        ApplyState(false);
        IsOn = false;
    }

    public void TurnOn()
    {
        if (IsOn) return;
        IsOn = true;
        ApplyState(true);

        if (sfxSource && turnOnClip) sfxSource.PlayOneShot(turnOnClip);
        if (humLoop && !humLoop.isPlaying) humLoop.Play();
    }

    public void TurnOff()
    {
        if (!IsOn) return;
        IsOn = false;
        ApplyState(false);

        if (sfxSource && turnOffClip) sfxSource.PlayOneShot(turnOffClip);
        if (humLoop && humLoop.isPlaying) humLoop.Stop();
    }

    void ApplyState(bool on)
    {
        if (lights != null)
            foreach (var l in lights) if (l) l.enabled = on;
    }
}
