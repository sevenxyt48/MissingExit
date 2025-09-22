using UnityEngine;

public class RuleManager : MonoBehaviour
{
    [Header("References")]
    public GameObject ruleCanvas;                  // 규칙 UI Canvas
    public FirstPersonController playerController; // 플레이어 컨트롤러

    [Header("Settings")]
    public KeyCode standUpKey = KeyCode.F;         // 규칙 확인 후 일어서기 키

    private bool hasStoodUp = false;               // 한 번만 일어서기 처리

    void Start()
    {
        // 규칙 UI 표시
        if (ruleCanvas != null)
            ruleCanvas.SetActive(true);
        else
            Debug.LogWarning("[RuleManager] ruleCanvas 할당 필요");

        // 플레이어 이동 잠금
        if (playerController != null)
            playerController.SetControlEnabled(false);
        else
            Debug.LogWarning("[RuleManager] playerController 할당 필요");
    }

    void Update()
    {
        if (!hasStoodUp && Input.GetKeyDown(standUpKey))
        {
            StandUp();
        }
    }

    private void StandUp()
    {
        // 규칙 UI 숨기기
        if (ruleCanvas != null)
            ruleCanvas.SetActive(false);

        // 플레이어 일어서기 + 이동 가능
        if (playerController != null)
            playerController.StandUp();

        hasStoodUp = true;
    }

    // 외부에서 강제로 StandUp 호출 가능
    public void ForceStandUp()
    {
        if (!hasStoodUp)
            StandUp();
    }
}
