using UnityEngine;

/// <summary>
/// 미닫이 문(입/출구 공용).
/// - RoomController가 입장 시 SetLocked(true), 방 단서 전부 수집 시 SetLocked(false)를 호출하는 구조를 기본으로 합니다.
/// - "전체 단서"가 아니라 "해당 교실(반)의 단서만"으로 잠금/해제를 처리합니다.
/// - requireRoomCluesToOpen 옵션을 켜면, 잠겨 있을 때 안내 문구를 방-단서 기준 메시지로 보여줍니다.
/// - 오디오: 열기/닫기 시작 효과음, 이동 중 루프, 멈춤음 지원.
/// </summary>
public class SlidingDoor : MonoBehaviour
{
    [Header("문 패널 / 동작")]
    [Tooltip("실제로 이동할 패널 Transform (필수)")]
    public Transform doorPanel;
    public Vector3 openOffset = new Vector3(-2f, 0f, 0f);
    public float openSpeed = 5f;
    public float interactionDistance = 2f;
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("잠금")]
    [Tooltip("초기 잠금 여부. 보통은 false로 두고, RoomController가 입장 시점에 잠급니다.")]
    public bool locked = false;
    [TextArea] public string lockedText = "문이 잠겨 있다.";

    [Header("방 단서 기준 개방(선택)")]
    [Tooltip("문을 열려 할 때, 아직 '이 방' 단서가 남아 있으면 안내 메시지를 보여줍니다. 실제 잠금/해제는 RoomController가 담당합니다.")]
    public bool requireRoomCluesToOpen = true;
    [TextArea] public string notAllRoomCluesText = "아직 이 방에서 해야 할 일이 남아 있다.";
    [Tooltip("이 문을 관리하는 RoomController (비우면 부모에서 자동 검색)")]
    public RoomController roomController;

    [Header("자동 닫힘(선택)")]
    public bool autoCloseWhenFar = false;
    public float autoCloseDelay = 1.5f;

    [Header("UI")]
    public GameObject hintText;                 // 'E 키' 안내

    [Header("Audio (선택)")]
    [Tooltip("시작/끝 효과음용 소스 (비우면 doorPanel에 자동 생성)")]
    public AudioSource sfx;
    [Tooltip("이동 중 루프용 소스 (비우면 doorPanel에 자동 생성)")]
    public AudioSource loopSource;
    public AudioClip openStartClip;
    public AudioClip closeStartClip;
    [Tooltip("문이 움직이는 동안 재생할 루프 사운드(선택)")]
    public AudioClip slidingLoopClip;
    [Tooltip("문이 멈출 때 재생할 타격/엔드 사운드(선택)")]
    public AudioClip stopClip;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    [Range(0f, 1f)] public float loopVolume = 0.7f;

    [Header("Simple One-Clip Mode (옵션)")]
    public bool simpleOneClipMode = true;
    public AudioClip toggleClip;
    public Vector2 pitchRange = new Vector2(0.98f, 1.02f); // 약간의 랜덤 피치


    // ──────────────────────────────────────────────────────────────
    Vector3 closedLocalPos, openLocalPos;
    bool isOpen = false;
    bool isMoving = false;
    float leaveTimer = -1f;
    GameObject playerObj;
    Collider doorCol;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col)
        {
            col.enabled = true;
            col.isTrigger = false; // 기본 닫힘 상태 = 고체
        }
    }

    void Awake()
    {
        if (!doorPanel)
        {
            Debug.LogError("[SlidingDoor] doorPanel이 비어 있습니다.", this);
        }
        else
        {
            closedLocalPos = doorPanel.localPosition;
            openLocalPos = closedLocalPos + openOffset;
        }

        // 콜라이더 참조: 부모에 없으면 doorPanel에서 찾아서 사용
        doorCol = GetComponent<Collider>();
        if (!doorCol && doorPanel) doorCol = doorPanel.GetComponent<Collider>();
        if (doorCol)
        {
            doorCol.enabled = true;
            doorCol.isTrigger = isOpen; // 시작 상태에 맞춰 설정
        }

        if (!roomController) roomController = GetComponentInParent<RoomController>();
        if (hintText) hintText.SetActive(false);
        CachePlayer();
        SetupAudio();
    }

    void Update()
    {
        if (!doorPanel) return;
        if (!playerObj) { CachePlayer(); return; }

        // 이동 처리
        if (isMoving)
        {
            Vector3 target = isOpen ? openLocalPos : closedLocalPos;
            doorPanel.localPosition = Vector3.MoveTowards(
                doorPanel.localPosition, target, openSpeed * Time.deltaTime);

            if ((doorPanel.localPosition - target).sqrMagnitude < 0.0001f)
            {
                doorPanel.localPosition = target;
                isMoving = false;

                // 최종 충돌 상태
                if (doorCol)
                {
                    doorCol.enabled = true;
                    doorCol.isTrigger = isOpen;
                }

                StopMoveSfx(); // 이동 종료 사운드
            }
        }

        // 상호작용
        bool near = Vector3.Distance(playerObj.transform.position, transform.position) <= interactionDistance;
        if (hintText) hintText.SetActive(near && !isMoving);

        if (near && Input.GetKeyDown(interactKey))
            TryToggle();

        // 자동 닫힘
        if (autoCloseWhenFar && isOpen)
        {
            if (!near)
            {
                leaveTimer = (leaveTimer < 0f) ? 0f : leaveTimer + Time.deltaTime;
                if (leaveTimer >= autoCloseDelay) Close();
            }
            else leaveTimer = -1f;
        }
    }

    void CachePlayer()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) playerObj = p;
    }

    // ─────────────── 공개 API ───────────────
    public void Open()
    {
        if (isOpen || isMoving) return;
        if (!CanOpen()) return;
        StartMove(true);
        PlayStartSfx(true);
    }

    public void Close()
    {
        if (!isOpen || isMoving) return;
        StartMove(false);
        PlayStartSfx(false);
    }

    public void Toggle() => TryToggle();

    /// <summary>RoomController가 호출하는 잠금 토글.</summary>
    public void SetLocked(bool value, string messageOverride = null)
    {
        locked = value;
        if (!string.IsNullOrEmpty(messageOverride)) lockedText = messageOverride;
        if (locked) Close(); // 잠글 땐 닫아둠
    }

    // ─────────────── 내부 로직 ───────────────
    void TryToggle()
    {
        if (isMoving) return;

        if (locked)
        {
            // 방 단서 기준 안내를 사용할 경우, 그 문구를 우선 노출
            string msg = (requireRoomCluesToOpen && roomController != null) ? notAllRoomCluesText : lockedText;
            ShowReason("간섭 금지", msg);
            return;
        }

        if (!isOpen) Open(); else Close();
    }

    bool CanOpen()
    {
        if (locked)
        {
            string msg = (requireRoomCluesToOpen && roomController != null) ? notAllRoomCluesText : lockedText;
            ShowReason("간섭 금지", msg);
            return false;
        }
        return true;
    }

    void StartMove(bool open)
    {
        isOpen = open;
        isMoving = true;
        leaveTimer = -1f;

        if (doorCol)
        {
            doorCol.enabled = true;
            // 열릴 땐 통과 가능하게
            doorCol.isTrigger = open;
        }

        if (hintText && hintText.activeSelf) hintText.SetActive(false);

        // 루프 사운드 시작(있다면)
        if (loopSource && slidingLoopClip)
        {
            if (!loopSource.isPlaying)
            {
                loopSource.clip = slidingLoopClip;
                loopSource.volume = loopVolume;
                loopSource.Play();
            }
        }
    }

    void ShowReason(string reason, string msg)
    {
        PenaltyManager.Instance?.ApplyPenalty(reason, msg, null, 1f, false);
    }

    // ─────────────── 오디오 유틸 ───────────────
    void SetupAudio()
    {
        if (!doorPanel) return;

        if (!sfx)
        {
            sfx = doorPanel.GetComponent<AudioSource>();
            if (!sfx) sfx = doorPanel.gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 1f; // 3D
            sfx.rolloffMode = AudioRolloffMode.Linear;
            sfx.dopplerLevel = 0f;
            sfx.volume = sfxVolume;
        }

        if (!loopSource && slidingLoopClip != null)
        {
            loopSource = doorPanel.gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.spatialBlend = 1f;
            loopSource.rolloffMode = AudioRolloffMode.Linear;
            loopSource.dopplerLevel = 0f;
            loopSource.volume = loopVolume;
        }
    }

    void PlayStartSfx(bool opening)
    {
        if (!sfx) return;

        if (simpleOneClipMode && toggleClip)
        {
            sfx.pitch = Random.Range(pitchRange.x, pitchRange.y);
            sfx.PlayOneShot(toggleClip, sfxVolume);
            return;
        }

        var clip = opening ? openStartClip : closeStartClip;
        if (clip)
        {
            sfx.pitch = 1f;
            sfx.PlayOneShot(clip, sfxVolume);
        }
    }


    void StopMoveSfx()
    {
        if (loopSource && loopSource.isPlaying) loopSource.Stop();
        if (sfx && stopClip) sfx.PlayOneShot(stopClip, sfxVolume);
    }
}
