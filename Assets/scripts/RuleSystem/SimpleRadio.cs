using UnityEngine;

public class SimpleRadio : MonoBehaviour
{
    public AudioSource source;
    public bool randomAutoOn = false;
    public Vector2 firstOnDelay = new Vector2(2f, 8f);
    public bool IsOn { get; private set; }

    // 라디오를 끌 때 어떤 이유의 지속음을 끌지(PenaltyManager와 키 일치)
    public string stopSustainReason = "라디오 규칙 위반";

    void Awake()
    {
        if (source)
        {
            source.playOnAwake = false;
            source.loop = true;
            if (source.isPlaying) source.Stop();   // 시작 시 무음 보장
        }
        IsOn = false;
    }

    void OnEnable()
    {
        if (source && source.isPlaying) source.Stop();
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

        // 라디오를 끄는 즉시 지속 사운드도 정지
        PenaltyManager.Instance?.StopSustain(stopSustainReason);

        Debug.Log("[SimpleRadio] TurnOff");
    }
}
