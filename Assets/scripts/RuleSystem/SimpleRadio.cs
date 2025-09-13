using UnityEngine;

public class SimpleRadio : MonoBehaviour
{
    public AudioSource source;
    public bool randomAutoOn = false;                 // ← 기본값 Off로
    public Vector2 firstOnDelay = new Vector2(2f, 8f);
    public bool IsOn { get; private set; }

    void Awake()
    {
        if (source)
        {
            source.playOnAwake = false;
            source.loop = true;
            if (source.isPlaying) source.Stop();      // ★ 시작 시 강제 정지
        }
        IsOn = false;
    }

    void OnEnable()
    {
        if (source && source.isPlaying) source.Stop(); // ★ 재활성화 때도 정지
        IsOn = false;
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
    }

    public void TurnOff()
    {
        if (!IsOn) return;
        IsOn = false;
        if (source && source.isPlaying) source.Stop();
        Debug.Log("[SimpleRadio] TurnOff");
    }
}
