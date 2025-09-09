using UnityEngine;
using System.Collections.Generic;

public class ClueDatabase : MonoBehaviour
{
    public static ClueDatabase Instance;

    [SerializeField] private string csvFilePath = "Clues/clue_data";
    private Dictionary<string, ClueData> clueDictionary = new Dictionary<string, ClueData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCluesFromCSV();
    }

    private void LoadCluesFromCSV()
    {
        List<Dictionary<string, string>> data = CSVReader.ReadCSV(csvFilePath);

        foreach (Dictionary<string, string> row in data)
        {
            ClueData clue = new ClueData();

            if (row.ContainsKey("clueID")) clue.clueID = row["clueID"];
            if (row.ContainsKey("title")) clue.title = row["title"];
            if (row.ContainsKey("content")) clue.content = row["content"];

            // 加载图片资源
            if (row.ContainsKey("imagePath") && !string.IsNullOrEmpty(row["imagePath"]))
            {
                clue.clueImage = Resources.Load<Sprite>(row["imagePath"]);
                if (clue.clueImage == null)
                {
                    Debug.LogWarning($"Image not found: {row["imagePath"]}");
                }
            }

            // 加载音频资源
            if (row.ContainsKey("soundPath") && !string.IsNullOrEmpty(row["soundPath"]))
            {
                clue.clueSound = Resources.Load<AudioClip>(row["soundPath"]);
                if (clue.clueSound == null)
                {
                    Debug.LogWarning($"Sound not found: {row["soundPath"]}");
                }
            }

            if (!string.IsNullOrEmpty(clue.clueID))
            {
                clueDictionary[clue.clueID] = clue;
                Debug.Log($"Loaded clue: {clue.clueID} - {clue.title}");
            }
        }

        Debug.Log($"Total clues loaded: {clueDictionary.Count}");
    }

    public ClueData GetClueByID(string id)
    {
        if (clueDictionary.TryGetValue(id, out ClueData clue))
        {
            return clue;
        }

        Debug.LogWarning($"Clue ID not found: {id}");
        return null;
    }
}