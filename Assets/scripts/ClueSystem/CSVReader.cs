using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class CSVReader
{
    public static List<Dictionary<string, string>> ReadCSV(string filePath)
    {
        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

        TextAsset csvFile = Resources.Load<TextAsset>(filePath);
        if (csvFile == null)
        {
            Debug.LogError("CSV文件未找到: " + filePath);
            return result;
        }

        string[] lines = csvFile.text.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogError("CSV文件格式错误");
            return result;
        }

        // 获取标题行（第一行）
        string[] headers = lines[0].Split(',');

        // 从第二行开始解析数据
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i].Trim())) continue;

            string[] values = lines[i].Split(',');
            Dictionary<string, string> entry = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                entry[headers[j].Trim()] = values[j].Trim();
            }

            result.Add(entry);
        }

        return result;
    }
}