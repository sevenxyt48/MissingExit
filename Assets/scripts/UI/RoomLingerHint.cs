using UnityEngine;

public class RoomLingerHint : MonoBehaviour
{
    [Header("기본")]
    public string playerTag = "Player";
    public RoomController room;

    [Header("타이밍")]
    public float firstHintAfter = 20f;
    public float repeatEvery = 10f;

    [Header("문구")]
    [TextArea] public string hintFirst = "조용하다… 나가보는 게 좋겠다.";
    [TextArea] public string hintRepeat = "다음 교실로 이동하세요 (E키로 문 열기)";
    [TextArea] public string hintLocked = "이 교실의 단서를 모두 모아야 나갈 수 있다.";

    [Header("색 / 사운드(선택)")]
    public Color colorFirst = new Color32(0x5F, 0x7C, 0xFF, 0xF2);
    public Color colorRepeat = new Color32(0x54, 0xE0, 0xFF, 0xF2);
    public Color colorLocked = new Color32(0xFF, 0xD1, 0x6A, 0xF2);
    public AudioClip sfxFirst;
    public AudioClip sfxRepeat;
    public AudioClip sfxLocked;

    bool inside; float timer; float nextTime; bool didFirst;

    void Awake() { if (!room) room = GetComponentInParent<RoomController>(); }
    void OnTriggerEnter(Collider other) { if (!other.CompareTag(playerTag)) return; inside = true; timer = 0f; nextTime = firstHintAfter; didFirst = false; }
    void OnTriggerExit(Collider other) { if (!other.CompareTag(playerTag)) return; inside = false; }

    void Update()
    {
        if (!inside) return;
        if (GameManager.Instance && GameManager.Instance.InClueGrace()) return;

        timer += Time.deltaTime;
        if (timer < nextTime) return;

        if (IsAnyDoorLocked())
        {
            GuidanceToast.Instance?.Show(hintLocked, colorLocked, 3.5f, sfxLocked, 0.5f);
        }
        else
        {
            if (!didFirst)
            {
                GuidanceToast.Instance?.Show(hintFirst, colorFirst, 3.5f, sfxFirst, 0.5f);
                didFirst = true;
            }
            else
            {
                GuidanceToast.Instance?.Show(hintRepeat, colorRepeat, 3.5f, sfxRepeat, 0.5f);
            }
        }

        nextTime += repeatEvery;
    }

    bool IsAnyDoorLocked()
    {
        if (!room || room.doors == null) return false;
        foreach (var d in room.doors) if (d && d.locked) return true;
        return false;
    }
}
