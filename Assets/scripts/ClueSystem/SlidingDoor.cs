using UnityEngine;

/// <summary>
/// 미닫이 문(입/출구 공용) - 두 방향 힌트(밖/안) 지원.
/// - 밖/안 각각 별도 힌트 UI를 보여줌. 어느 쪽이든 가깝다면 E로 토글.
/// - outside/inside 앵커가 없으면 힌트 오브젝트 위치를 사용(둘 다 없으면 문 Transform 기준).
/// - '밖에서 닫기 금지', '안에 있을 때만 자동 닫힘' 옵션 유지.
/// - 열기/닫기 시작음, 이동 루프, 멈춤음 지원.
/// </summary>
public class SlidingDoor : MonoBehaviour
{
    [Header("문 패널 / 동작")]
    public Transform doorPanel;
    public Vector3 openOffset = new Vector3(-2f, 0f, 0f);
    public float openSpeed = 5f;
    public float interactionDistance = 2f;
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("잠금")]
    public bool locked = false;
    [TextArea] public string lockedText = "문이 잠겨 있다.";

    [Header("방 단서 기준 개방(선택)")]
    public bool requireRoomCluesToOpen = true;
    [TextArea] public string notAllRoomCluesText = "아직 이 방에서 해야 할 일이 남아 있다.";
    public RoomController roomController;

    [Header("자동 닫힘(선택)")]
    public bool autoCloseWhenFar = false;
    public float autoCloseDelay = 1.5f;
    [Tooltip("플레이어가 '방 안'일 때만 자동 닫힘 허용")]
    public bool autoCloseRequiresInside = true;
    [Tooltip("플레이어가 '방 안'이 아니면 수동 닫기(E) 금지")]
    public bool onlyCloseWhenInside = true;

    [Header("UI - Hint (양쪽)")]
    public GameObject hintOutside;
    public GameObject hintInside;
    [Tooltip("호환용: 단일 힌트(없어도 됨)")]
    public GameObject hintText;
    [Tooltip("가까움 판정 기준점(없으면 해당 힌트 오브젝트 위치 사용)")]
    public Transform outsideAnchor;
    public Transform insideAnchor;

    [Header("Audio (선택)")]
    public AudioSource sfx;
    public AudioSource loopSource;
    public AudioClip openStartClip;
    public AudioClip closeStartClip;
    public AudioClip slidingLoopClip;
    public AudioClip stopClip;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    [Range(0f, 1f)] public float loopVolume = 0.7f;

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
        if (col) { col.enabled = true; col.isTrigger = false; }
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

        doorCol = GetComponent<Collider>();
        if (!doorCol && doorPanel) doorCol = doorPanel.GetComponent<Collider>();
        if (doorCol) { doorCol.enabled = true; doorCol.isTrigger = isOpen; }

        if (!roomController) roomController = GetComponentInParent<RoomController>();

        // 힌트 초기 감춤
        if (hintOutside) hintOutside.SetActive(false);
        if (hintInside) hintInside.SetActive(false);
        if (hintText) hintText.SetActive(false);

        CachePlayer();
        SetupAudio();
    }

    void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;
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

                if (doorCol) { doorCol.enabled = true; doorCol.isTrigger = isOpen; }
                StopMoveSfx();
            }
        }

        // 가까움 판정(양쪽)
        bool nearOutside = IsNear(outsideAnchor, hintOutside);
        bool nearInside = IsNear(insideAnchor, hintInside);
        bool nearEither = nearOutside || nearInside || IsNearDoorFallback();

        // 힌트 표시(움직이는 중이면 모두 숨김)
        if (!isMoving)
        {
            if (hintOutside) hintOutside.SetActive(nearOutside && !locked);
            if (hintInside) hintInside.SetActive(nearInside && !locked);

            // 둘 다 세팅 안 했을 때(호환용)
            if (!hintOutside && !hintInside && hintText)
                hintText.SetActive(nearEither && !locked);
        }
        else
        {
            if (hintOutside && hintOutside.activeSelf) hintOutside.SetActive(false);
            if (hintInside && hintInside.activeSelf) hintInside.SetActive(false);
            if (hintText && hintText.activeSelf) hintText.SetActive(false);
        }

        // 입력
        if (nearEither && Input.GetKeyDown(interactKey))
            TryToggle();

        // 자동 닫힘
        if (autoCloseWhenFar && isOpen)
        {
            bool allowAutoClose = !autoCloseRequiresInside || PlayerIsInsideRoom();
            if (allowAutoClose)
            {
                if (!nearEither)
                {
                    leaveTimer = (leaveTimer < 0f) ? 0f : leaveTimer + Time.deltaTime;
                    if (leaveTimer >= autoCloseDelay) Close();
                }
                else leaveTimer = -1f;
            }
            else leaveTimer = -1f;
        }
    }

    bool IsNear(Transform anchor, GameObject hintObj)
    {
        Transform refT = anchor;
        if (!refT && hintObj) refT = hintObj.transform;
        if (!refT) refT = transform;

        return Vector3.Distance(playerObj.transform.position, refT.position) <= interactionDistance;
    }

    bool IsNearDoorFallback()
    {
        // 양쪽 힌트가 없을 때를 위한 보조 판정
        return (!hintOutside && !hintInside) &&
               Vector3.Distance(playerObj.transform.position, transform.position) <= interactionDistance;
    }

    bool PlayerIsInsideRoom()
    {
        if (!roomController || GameManager.Instance == null) return false;
        return GameManager.Instance.CurrentRoomId == roomController.roomId;
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
        if (onlyCloseWhenInside && !PlayerIsInsideRoom()) return;
        StartMove(false);
        PlayStartSfx(false);
    }

    public void Toggle() => TryToggle();

    public void SetLocked(bool value, string messageOverride = null)
    {
        locked = value;
        if (!string.IsNullOrEmpty(messageOverride)) lockedText = messageOverride;
        if (locked) Close();
    }

    // ─────────────── 내부 로직 ───────────────
    void TryToggle()
    {
        if (isMoving) return;

        if (locked)
        {
            string msg = (requireRoomCluesToOpen && roomController != null) ? notAllRoomCluesText : lockedText;
            ShowReason("간섭 금지", msg);
            return;
        }

        if (isOpen)
        {
            if (onlyCloseWhenInside && !PlayerIsInsideRoom()) return;
            Close();
        }
        else
        {
            Open();
        }
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
            doorCol.isTrigger = open; // 열릴 때는 통과
        }

        // 힌트는 이동 중 숨김
        if (hintOutside && hintOutside.activeSelf) hintOutside.SetActive(false);
        if (hintInside && hintInside.activeSelf) hintInside.SetActive(false);
        if (hintText && hintText.activeSelf) hintText.SetActive(false);

        // 루프 사운드
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
            sfx.spatialBlend = 1f;
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
        if (sfx)
        {
            var clip = opening ? openStartClip : closeStartClip;
            if (clip) sfx.PlayOneShot(clip, sfxVolume);
        }
    }

    void StopMoveSfx()
    {
        if (loopSource && loopSource.isPlaying) loopSource.Stop();
        if (sfx && stopClip) sfx.PlayOneShot(stopClip, sfxVolume);
    }
}
