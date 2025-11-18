using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class AreaRuleTrigger : MonoBehaviour
{
    public enum Mode { OnEnter, OnExit }

    [Header("활성 조건")]
    public string onlyInRoom = "";
    public float startupGrace = 1f;
    float startTime;

    [Header("트리거")]
    public Mode mode = Mode.OnEnter;
    public string playerTag = "Player";
    public float cooldown = 2f;

    [Header("Penalty")]
    public string violationReason = "영역 규칙 위반";
    [TextArea] public string penaltyText = "돌아갈 수 없다.";

    [Header("추가 동작(UnityEvent)")]
    public UnityEvent onTriggered;

    float cd;

    void Reset() { GetComponent<Collider>().isTrigger = true; }
    void Start() { startTime = Time.time; }

    bool ActiveNow()
    {
        if (Time.time - startTime < startupGrace) return false;
        var gm = GameManager.Instance;
        if (gm && gm.CurrentRoomId == "2-4") return false;
        if (!string.IsNullOrEmpty(onlyInRoom) && gm && gm.CurrentRoomId != onlyInRoom) return false;
        return true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (mode != Mode.OnEnter) return;
        if (!ActiveNow()) return;
        if (cd > 0f) return;
        if (!other.CompareTag(playerTag)) return;
        Fire();
    }

    void OnTriggerExit(Collider other)
    {
        if (mode != Mode.OnExit) return;
        if (!ActiveNow()) return;
        if (cd > 0f) return;
        if (!other.CompareTag(playerTag)) return;
        Fire();
    }

    void Update() { if (cd > 0f) cd -= Time.deltaTime; }

    void Fire()
    {
        cd = cooldown;
        PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText);
        onTriggered?.Invoke();
    }
}
