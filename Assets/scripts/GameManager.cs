using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private List<string> collectedClues = new List<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void CollectClue(string clueID)
    {
        if (!collectedClues.Contains(clueID))
        {
            collectedClues.Add(clueID);
            Debug.Log("收集线索: " + clueID + "，总数: " + collectedClues.Count);
        }
    }

    public int GetCollectedCount()
    {
        return collectedClues.Count;
    }
}
