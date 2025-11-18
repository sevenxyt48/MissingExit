using UnityEngine;

/// <summary>
/// 방 안에 오래 머무를 때만 힌트를 보여주는 스크립트.
/// 현재 프로젝트에서는 2-4반에만 사용.
/// 트리거 존에 붙여서 사용.
/// </summary>
public class RoomLingerHint : MonoBehaviour
{
    [Header("기본")]
    public string playerTag = "Player";
    public RoomController room;   // 2-4반 RoomController를 연결

    [Header("타이밍")]
    [Tooltip("플레이어가 존에 들어온 후 첫 힌트를 띄우기까지 대기 시간(초).")]
    public float firstHintAfter = 20f;
    [Tooltip("첫 힌트 이후 반복 힌트를 띄우는 간격(초).")]
    public float repeatEvery = 10f;

    [Header("문구")]
    [TextArea]
    public string hintFirst = "조용하다… 나가보는 게 좋겠다.";
    [TextArea]
    public string hintRepeat = "다음 교실로 이동하세요 (E키로 문 열기)";
    [TextArea]
    public string hintLocked = "이 교실의 단서를 모두 모아야 나갈 수 있다.";

    [Header("색 / 사운드(선택)")]
    public Color colorFirst = new Color32(0x5F, 0x7C, 0xFF, 0xF2);
    public Color colorRepeat = new Color32(0x54, 0xE0, 0xFF, 0xF2);
    public Color colorLocked = new Color32(0xFF, 0xD1, 0x6A, 0xF2);
    public AudioClip sfxFirst;
    public AudioClip sfxRepeat;
    public AudioClip sfxLocked;

    bool inside = false;
    float timer = 0f;
    float nextTime = 0f;
    bool didFirst = false;

    void Awake()
    {
        if (!room)
        {
            room = GetComponentInParent<RoomController>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        inside = true;
        timer = 0f;
        nextTime = firstHintAfter;
        didFirst = false;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = false;
    }

    void Update()
    {
        if (!inside) return;

        // 2-4반에만 동작하도록 제한
        if (room != null && room.roomId != "2-4") return;

        // 단서 화면 열람 직후 유예 시간에는 힌트 출력 안 함
        if (GameManager.Instance && GameManager.Instance.InClueGrace()) return;

        timer += Time.deltaTime;
        if (timer < nextTime) return;

        if (IsAnyDoorLocked())
        {
            // 아직 이 방의 단서를 다 못 모은 상태
            if (!string.IsNullOrEmpty(hintLocked))
            {
                GuidanceToast.Instance?.Show(
                    hintLocked,
                    colorLocked,
                    3.5f,
                    sfxLocked,
                    0.5f
                );
            }
        }
        else
        {
            // 문이 잠겨 있지 않은 상태(= 이 방에서 할 일은 끝난 상태)
            if (!didFirst)
            {
                if (!string.IsNullOrEmpty(hintFirst))
                {
                    GuidanceToast.Instance?.Show(
                        hintFirst,
                        colorFirst,
                        3.5f,
                        sfxFirst,
                        0.5f
                    );
                }
                didFirst = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(hintRepeat))
                {
                    GuidanceToast.Instance?.Show(
                        hintRepeat,
                        colorRepeat,
                        3.5f,
                        sfxRepeat,
                        0.5f
                    );
                }
            }
        }

        nextTime += repeatEvery;
    }

    bool IsAnyDoorLocked()
    {
        if (!room || room.doors == null) return false;

        foreach (var d in room.doors)
        {
            if (d && d.locked) return true;
        }
        return false;
    }
}
