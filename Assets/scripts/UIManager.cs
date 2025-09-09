using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Ending UI")]
    public GameObject goodEndingUI;
    public GameObject badEndingUI;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 显示结局 UI
    /// </summary>
    public void ShowEnding(bool isGoodEnding)
    {
        if (goodEndingUI != null) goodEndingUI.SetActive(isGoodEnding);
        if (badEndingUI != null) badEndingUI.SetActive(!isGoodEnding);
    }
}
