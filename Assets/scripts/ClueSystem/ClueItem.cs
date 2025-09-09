using UnityEngine;
using TMPro;

public class ClueItem : MonoBehaviour
{
    public string clueID;
    public GameObject interactPrompt;
    public TMP_Text promptText;
    public string promptMessage = "F 키를 눌러 조사하기";

    private bool playerInRange = false;
    private bool collected = false;

    private void Start()
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        if (promptText != null)
        {
            promptText.text = promptMessage;
        }
    }

    private void Update()
    {
        if (playerInRange && !collected && Input.GetKeyDown(KeyCode.F))
        {
            CollectClue();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !collected)
        {
            playerInRange = true;
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(true);
                Debug.Log($"显示交互提示: {clueID}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
                Debug.Log($"隐藏交互提示: {clueID}");
            }
        }
    }

    private void CollectClue()
    {
        collected = true;
        playerInRange = false;

        Debug.Log($"收集线索: {clueID}");

        // 隐藏交互提示
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        // 显示线索UI
        if (ClueUIManager.Instance != null)
        {
            ClueUIManager.Instance.ShowClue(clueID);
        }
        else
        {
            Debug.LogError("ClueUIManager实例为空!");
        }

        // 禁用碰撞器，防止重复交互
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 可选: 隐藏物体渲染
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
    }
}