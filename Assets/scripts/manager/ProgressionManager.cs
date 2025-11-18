using UnityEngine;
using System;
using System.Collections.Generic;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance;

    private readonly HashSet<string> completedRooms = new HashSet<string>();
    public event Action<string> OnRoomCompleted;   // ex) "2-1"

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public bool IsCompleted(string roomId) => !string.IsNullOrEmpty(roomId) && completedRooms.Contains(roomId);

    public void MarkRoomCompleted(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return;
        if (completedRooms.Add(roomId))
        {
            Debug.Log($"[Progression] Room completed: {roomId}");
            OnRoomCompleted?.Invoke(roomId);
        }
    }
}
