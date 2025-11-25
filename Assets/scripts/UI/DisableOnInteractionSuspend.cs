// 붙어있는 오브젝트의 Collider / 지정한 스크립트를 Pause(게이트) 동안 자동 비활성화
using UnityEngine;
using System.Collections.Generic;

public class DisableOnInteractionSuspend : MonoBehaviour
{
    [Header("자동으로 끌 Collider들 (비워두면 자식까지 전부 스캔)")]
    public List<Collider> colliders = new List<Collider>();

    [Header("자동으로 끌 스크립트(선택): 예) ClueItem, NoteInteraction, SlidingDoor 등")]
    public List<MonoBehaviour> scripts = new List<MonoBehaviour>();

    // 캐시
    List<bool> colliderEnabled = new List<bool>();
    List<bool> scriptEnabled = new List<bool>();
    bool subscribed = false;

    void Awake()
    {
        if (colliders.Count == 0)
            colliders.AddRange(GetComponentsInChildren<Collider>(true));

        foreach (var c in colliders) colliderEnabled.Add(c ? c.enabled : false);
        foreach (var s in scripts) scriptEnabled.Add(s ? s.enabled : false);
    }

    void OnEnable()
    {
        var gate = GlobalInteractionGate.Instance;
        if (gate != null && !subscribed)
        {
            gate.OnSuspendedChanged += OnGateChanged;
            subscribed = true;
            if (gate.InteractionsSuspended) ApplySuspend(true);
        }
    }

    void OnDisable()
    {
        if (subscribed && GlobalInteractionGate.Instance != null)
        {
            GlobalInteractionGate.Instance.OnSuspendedChanged -= OnGateChanged;
            subscribed = false;
        }
        // 혹시 꺼진 상태로 비활성화 되었다면 원복
        ApplySuspend(false);
    }

    void OnGateChanged(bool suspended) => ApplySuspend(suspended);

    void ApplySuspend(bool suspended)
    {
        // Collider OFF/ON
        for (int i = 0; i < colliders.Count; i++)
        {
            var c = colliders[i];
            if (!c) continue;

            if (suspended)
            {
                // 현상태를 덮어쓰지 않도록 현재값만 유지해두고 끔
                if (colliderEnabled.Count <= i) colliderEnabled.Add(c.enabled);
                c.enabled = false;
            }
            else
            {
                // 원래 상태로 되돌림
                bool orig = (colliderEnabled.Count > i) ? colliderEnabled[i] : true;
                c.enabled = orig;
            }
        }

        // 스크립트 OFF/ON
        for (int i = 0; i < scripts.Count; i++)
        {
            var s = scripts[i];
            if (!s) continue;

            if (suspended)
            {
                if (scriptEnabled.Count <= i) scriptEnabled.Add(s.enabled);
                s.enabled = false;
            }
            else
            {
                bool orig = (scriptEnabled.Count > i) ? scriptEnabled[i] : true;
                s.enabled = orig;
            }
        }
    }
}
