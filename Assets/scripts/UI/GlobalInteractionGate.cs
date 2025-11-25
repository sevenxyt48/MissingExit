// 전역 상호작용 멈추기
using UnityEngine;
using System;

public class GlobalInteractionGate : MonoBehaviour
{
    public static GlobalInteractionGate Instance { get; private set; }

    // 중첩 카운터: 0보다 크면 '잠금' 상태
    private int suspendCount = 0;
    public bool InteractionsSuspended => suspendCount > 0;

    public event Action<bool> OnSuspendedChanged;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void Push(string reason = null)
    {
        int before = suspendCount;
        suspendCount++;
        if (before == 0 && suspendCount == 1) OnSuspendedChanged?.Invoke(true);
    }

    public void Pop(string reason = null)
    {
        int before = suspendCount;
        suspendCount = Mathf.Max(0, suspendCount - 1);
        if (before > 0 && suspendCount == 0) OnSuspendedChanged?.Invoke(false);
    }
}
