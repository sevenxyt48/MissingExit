using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraRoot;               // 카메라 루트(머리 위치)
    public Transform cameraTransform;          // 실제 Main Camera

    [Header("Look")]
    public float mouseSensitivity = 2.0f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Move")]
    public float walkSpeed = 2.2f;
    public float runSpeed = 3.6f;
    public float crouchSpeed = 1.4f;
    public float gravity = -18f;
    public bool canRun = true;
    public bool canCrouch = true;

    [Header("Crouch")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float standHeight = 1.6f;           // 일어서기 높이
    public float crouchHeight = 1.2f;          // 앉기 높이
    public float heightLerpSpeed = 10f;

    [Header("Start Seated")]
    public bool startSeated = true;            // 시작 시 앉아있음
    public bool allowManualStandUp = true;     // Q 키로 일어날 수 있음
    public KeyCode standUpKey = KeyCode.Q;     // ★ Q로 변경

    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public float bobAmplitudeWalk = 0.03f;
    public float bobAmplitudeRun = 0.05f;
    public float bobFrequencyWalk = 7f;
    public float bobFrequencyRun = 10f;

    [Header("Footsteps")]
    public AudioClip[] footstepClips;
    public float stepIntervalWalk = 2.0f;
    public float stepIntervalRun = 1.2f;
    public float stepVolumeWalk = 0.35f;
    public float stepVolumeRun = 0.5f;

    CharacterController _cc;
    AudioSource _audio;
    float _pitch;
    Vector3 _moveVelocity;
    float _targetHeight;
    float _stepCycle;

    bool _canMove;
    bool _isCrouching;

    // 노트 중 수동 일어서기 금지 토글
    bool _manualStandUpEnabled = true;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _audio = GetComponent<AudioSource>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 시작 상태
        _cc.height = startSeated ? crouchHeight : standHeight;
        _cc.center = new Vector3(0, _cc.height / 2f, 0);
        _targetHeight = _cc.height;

        _canMove = !startSeated; // 앉아서 시작 시 이동/시야 불가

        if (cameraRoot == null)
            Debug.LogError("[FPC] cameraRoot 할당 필요");
        if (cameraTransform == null && cameraRoot != null)
            cameraTransform = cameraRoot.GetComponentInChildren<Camera>()?.transform;
    }

    void Update()
    {
        LookUpdate();
        MoveUpdate();
        HeightUpdate();
        HeadBobUpdate();
        FootstepUpdate();

        // 수동 StandUp: 노트 중에는 비활성화
        if (startSeated && allowManualStandUp && _manualStandUpEnabled && !_canMove && Input.GetKeyDown(standUpKey))
        {
            StandUp();
        }
    }

    // ==== 마우스 시야 ====
    void LookUpdate()
    {
        if (!_canMove) return; // 조작 잠금 시 시야 회전 차단

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        if (cameraRoot != null)
            cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // ==== 이동 ====
    void MoveUpdate()
    {
        if (!_canMove)
        {
            // 잠금 중엔 중력만 최소 적용
            if (_cc.isGrounded && _moveVelocity.y < 0) _moveVelocity.y = -0.1f;
            _moveVelocity.y += gravity * Time.deltaTime * 0.1f;
            _cc.Move(new Vector3(0, _moveVelocity.y, 0) * Time.deltaTime);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = Vector3.ClampMagnitude(new Vector3(h, 0, v), 1f);

        _isCrouching = canCrouch && Input.GetKey(crouchKey);
        bool wantsRun = canRun && Input.GetKey(KeyCode.LeftShift) && !_isCrouching && inputDir.sqrMagnitude > 0.1f;

        float speed = walkSpeed;
        if (wantsRun) speed = runSpeed;
        if (_isCrouching) speed = crouchSpeed;

        Vector3 worldMove = (transform.right * inputDir.x + transform.forward * inputDir.z) * speed;

        if (_cc.isGrounded && _moveVelocity.y < 0) _moveVelocity.y = -2f;
        _moveVelocity.y += gravity * Time.deltaTime;

        Vector3 total = new Vector3(worldMove.x, _moveVelocity.y, worldMove.z);
        _cc.Move(total * Time.deltaTime);
    }

    // ==== 앉기/서기 전환 ====
    void HeightUpdate()
    {
        float target = _isCrouching ? crouchHeight : standHeight;
        if (!_canMove && startSeated) target = crouchHeight;

        _targetHeight = target;

        // CharacterController 보간
        _cc.height = Mathf.Lerp(_cc.height, _targetHeight, Time.deltaTime * heightLerpSpeed);
        _cc.center = new Vector3(0, _cc.height / 2f, 0);

        // 카메라 루트 위치 보간
        if (cameraRoot != null)
        {
            float tHead = Mathf.Lerp(cameraRoot.localPosition.y, _targetHeight - 0.2f, Time.deltaTime * heightLerpSpeed);
            cameraRoot.localPosition = new Vector3(cameraRoot.localPosition.x, tHead, cameraRoot.localPosition.z);
        }
    }

    // ==== 헤드 바운스 ====
    void HeadBobUpdate()
    {
        if (!enableHeadBob || cameraTransform == null || !_cc.isGrounded) return;
        if (!_canMove)
        {
            // 잠금 시엔 정위치로 복귀
            Vector3 local = cameraTransform.localPosition;
            local.y = Mathf.Lerp(local.y, 0f, Time.deltaTime * 6f);
            cameraTransform.localPosition = local;
            return;
        }

        Vector3 horizontalVel = _cc.velocity; horizontalVel.y = 0;
        float speed = horizontalVel.magnitude;

        if (speed < 0.1f)
        {
            Vector3 local = cameraTransform.localPosition;
            local.y = Mathf.Lerp(local.y, 0f, Time.deltaTime * 6f);
            cameraTransform.localPosition = local;
            return;
        }

        bool running = speed > (runSpeed * 0.5f);
        float amp = running ? bobAmplitudeRun : bobAmplitudeWalk;
        float freq = running ? bobFrequencyRun : bobFrequencyWalk;

        float bob = Mathf.Sin(Time.time * freq) * amp;
        Vector3 lp = cameraTransform.localPosition;
        lp.y = bob;
        cameraTransform.localPosition = lp;
    }

    // ==== 발자국 소리 ====
    void FootstepUpdate()
    {
        if (!_cc.isGrounded) return;
        if (!_canMove) return;

        Vector3 vel = _cc.velocity; vel.y = 0;
        float speed = vel.magnitude;

        bool running = speed > (runSpeed * 0.5f);
        float interval = running ? stepIntervalRun : stepIntervalWalk;

        _stepCycle += speed * Time.deltaTime;
        if (_stepCycle > interval)
        {
            PlayFootstep(running);
            _stepCycle = 0f;
        }
    }

    void PlayFootstep(bool running)
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        _audio.volume = running ? stepVolumeRun : stepVolumeWalk;
        _audio.pitch = Random.Range(0.95f, 1.05f);
        _audio.PlayOneShot(clip);
    }

    // ==== 수동 StandUp 호출 ====
    public void StandUp()
    {
        _canMove = true;
        startSeated = false;

        _cc.height = standHeight;
        _cc.center = new Vector3(0, _cc.height / 2f, 0);

        if (cameraRoot != null)
        {
            float cameraY = standHeight - 0.3f;
            cameraRoot.localPosition = new Vector3(
                cameraRoot.localPosition.x,
                cameraY,
                cameraRoot.localPosition.z
            );
        }
    }

    // ==== 외부에서 조작 잠금 ====
    public void SetControlEnabled(bool enabled)
    {
        _canMove = enabled;
        if (!enabled)
        {
            _moveVelocity = Vector3.zero;
            if (cameraTransform != null)
            {
                var lp = cameraTransform.localPosition;
                lp.y = Mathf.Lerp(lp.y, 0f, Time.deltaTime * 8f);
                cameraTransform.localPosition = lp;
            }
        }
    }

    public bool IsControlEnabled => _canMove;

    // ==== 수동 일어서기 허용/차단 ====
    public void SetManualStandUpEnabled(bool enabled)
    {
        _manualStandUpEnabled = enabled;
    }
}
