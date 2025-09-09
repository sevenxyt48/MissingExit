using UnityEngine;
using TMPro;
using System.Collections;

public class DisplayRule : MonoBehaviour
{
    [Header("UI References")]
    public GameObject ruleCanvas;
    public TMP_Text ruleText;

    [Header("Player")]
    public FirstPersonController playerController; // 명시적 참조

    [Header("Typing Settings")]
    public float typingSpeed = 0.05f;

    private string[] rules;
    private int currentRule = 0;
    private bool isDisplaying = false;
    private bool isTyping = false;

    void Start()
    {
        if (ruleCanvas != null)
            ruleCanvas.SetActive(false);

        // 자동으로 플레이어 컨트롤러 찾기
        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
        }

        LoadRules();
    }

    void LoadRules()
    {
        TextAsset csv = Resources.Load<TextAsset>("Rules/rules");
        if (csv != null)
        {
            rules = csv.text.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            rules = new string[] { "규칙 파일을 찾을 수 없습니다." };
            Debug.LogError("Rules/rules.csv 파일을 Resources 폴더에서 찾을 수 없습니다!");
        }
    }

    public void StartDisplayingRules()
    {
        if (ruleCanvas != null)
            ruleCanvas.SetActive(true);

        if (playerController != null)
            playerController.SetControlEnabled(false); // 플레이어 이동 잠금

        currentRule = 0;
        isDisplaying = true;
        StartCoroutine(TypeRule(rules[currentRule]));
    }

    void Update()
    {
        if (isDisplaying && Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                // 타이핑 중 클릭하면 즉시 완성
                StopAllCoroutines();
                ruleText.text = rules[currentRule];
                isTyping = false;
            }
            else
            {
                // 다음 규칙으로 진행
                currentRule++;
                if (currentRule < rules.Length)
                {
                    StartCoroutine(TypeRule(rules[currentRule]));
                }
                else
                {
                    EndRules();
                }
            }
        }

        // ESC 키로 규칙 표시 취소
        if (isDisplaying && Input.GetKeyDown(KeyCode.Escape))
        {
            EndRules();
        }
    }

    IEnumerator TypeRule(string line)
    {
        isTyping = true;
        ruleText.text = "";

        foreach (char c in line)
        {
            ruleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    void EndRules()
    {
        if (ruleCanvas != null)
            ruleCanvas.SetActive(false);

        if (playerController != null)
            playerController.SetControlEnabled(true); // 플레이어 이동 복구

        isDisplaying = false;
        currentRule = 0;
    }
}