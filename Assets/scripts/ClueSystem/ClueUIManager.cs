using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueUIManager : MonoBehaviour
{
    public static ClueUIManager Instance;

    public GameObject cluePanel;
    public TMP_Text titleText;
    public TMP_Text contentText;
    public Image clueImage;
    public AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 确保开始时面板隐藏
        if (cluePanel != null)
        {
            cluePanel.SetActive(false);
            Debug.Log("初始化时隐藏线索面板");
        }
    }

    private void Update()
    {
        // 按F或ESC关闭线索面板
        if (cluePanel.activeSelf && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)))
        {
            HideClue();
        }
    }

    public void ShowClue(string clueID)
    {
        Debug.Log($"显示线索: {clueID}");

        ClueData data = ClueDatabase.Instance.GetClueByID(clueID);
        if (data == null)
        {
            Debug.LogError($"未找到线索数据: {clueID}");
            return;
        }

        titleText.text = data.title;
        contentText.text = data.content;

        if (data.clueImage != null)
        {
            clueImage.sprite = data.clueImage;
            clueImage.gameObject.SetActive(true);
        }
        else
        {
            clueImage.gameObject.SetActive(false);
        }

        // 播放音效
        if (data.clueSound != null && audioSource != null)
        {
            audioSource.clip = data.clueSound;
            audioSource.Play();
        }

        if (cluePanel != null)
        {
            cluePanel.SetActive(true);
            Debug.Log("线索面板已激活");
        }
    }

    public void HideClue()
    {
        if (cluePanel != null)
        {
            cluePanel.SetActive(false);
            Debug.Log("隐藏线索面板");
        }

        // 停止音效
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}