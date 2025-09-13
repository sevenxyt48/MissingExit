using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DisplayRule : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("규칙 패널(켜고/끄는 대상)")]
    public GameObject ruleCanvas;
    [Tooltip("규칙 본문을 표시할 TMP_Text")]
    public TMP_Text ruleText;

    [Header("Player")]
    [Tooltip("없으면 자동 탐색됨")]
    public FirstPersonController playerController;

    [Header("Typing Settings")]
    [Tooltip("글자당 지연(초)")]
    public float typingSpeed = 0.035f;

    [Header("Rules (Inspector에서 직접 입력)")]
    [TextArea(2, 6)] public string[] rules;       // <- 배열이 비면 RuleText 초기값을 사용

    [Header("Options")]
    [Tooltip("씬 시작 시 자동으로 규칙을 표시")]
    public bool showOnStart = false;
    [Tooltip("RuleText의 초기 텍스트를 페이지로 쪼갤 때 사용할 토큰. 비워두면 '빈 줄' 기준으로 분할")]
    public string pageDelimiter = "<page>";

    // --- runtime ----
    private readonly List<string> _pages = new List<string>();
    private int _pageIndex = 0;
    private bool _isDisplaying = false;
    private bool _isTyping = false;
    private Coroutine _typingCo;
    private string _ruleTextInitial = "";

    void Awake()
    {
        if (ruleText != null)
        {
            // Inspector에 미리 적어둔 텍스트 백업 (rules 배열이 비었을 때 사용)
            _ruleTextInitial = ruleText.text ?? "";
            ruleText.text = ""; // 시작 시 빈 화면
        }
    }

    void Start()
    {
        if (ruleCanvas != null) ruleCanvas.SetActive(false);

        if (playerController == null)
            playerController = FindObjectOfType<FirstPersonController>();

        BuildPages(); // rules/RuleText에서 페이지 구성

        if (showOnStart)
            StartDisplayingRules();
    }

    /// <summary>
    /// rules 배열이 비어있으면 ruleText 초기값을 사용해 페이지 구성.
    /// - pageDelimiter 가 비어있지 않으면 해당 토큰으로 분할
    /// - 아니면 '빈 줄(한 줄 이상 공백)'을 기준으로 분할
    /// - 아무 기준도 없으면 전체를 1페이지로 사용
    /// </summary>
    private void BuildPages()
    {
        _pages.Clear();

        // 1) Inspector 배열 사용
        if (rules != null && rules.Length > 0)
        {
            foreach (var r in rules)
            {
                var s = (r ?? "").Trim();
                if (!string.IsNullOrEmpty(s))
                    _pages.Add(s);
            }
        }

        // 2) 배열이 비어있다면 RuleText 초기값 사용
        if (_pages.Count == 0)
        {
            var src = (_ruleTextInitial ?? "").Replace("\r\n", "\n").Trim();

            if (!string.IsNullOrEmpty(src))
            {
                if (!string.IsNullOrEmpty(pageDelimiter) && src.Contains(pageDelimiter))
                {
                    var parts = src.Split(new string[] { pageDelimiter }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        var s = p.Trim();
                        if (!string.IsNullOrEmpty(s)) _pages.Add(s);
                    }
                }
                else if (src.Contains("\n\n")) // 빈 줄 기준
                {
                    // 연속된 빈 줄로 분할
                    var parts = SplitByBlankLine(src);
                    foreach (var p in parts)
                    {
                        var s = p.Trim();
                        if (!string.IsNullOrEmpty(s)) _pages.Add(s);
                    }
                }
                else
                {
                    _pages.Add(src); // 한 페이지
                }
            }
        }

        // 3) 여전히 비어있다면 안전 문구
        if (_pages.Count == 0)
            _pages.Add("규칙이 설정되지 않았습니다. (Inspector의 rules 배열 또는 RuleText 초기 텍스트를 채워주세요)");
    }

    /// <summary>연속된 빈 줄로 문자열을 분할</summary>
    private static List<string> SplitByBlankLine(string text)
    {
        var list = new List<string>();
        var lines = text.Split('\n');
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // 완전히 비거나 공백만 있는 줄이면 '페이지 경계'로 간주
            if (string.IsNullOrWhiteSpace(line))
            {
                if (sb.Length > 0)
                {
                    list.Add(sb.ToString());
                    sb.Length = 0;
                }
            }
            else
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line);
            }
        }
        if (sb.Length > 0) list.Add(sb.ToString());
        return list;
    }

    public void StartDisplayingRules()
    {
        if (_isDisplaying)
        {
            // 재호출 시 처음부터 다시
            StopTypingIfAny();
            _pageIndex = 0;
        }

        if (ruleCanvas != null) ruleCanvas.SetActive(true);
        if (playerController != null) playerController.SetControlEnabled(false);

        _isDisplaying = true;

        ShowPage(_pageIndex);
    }

    private void ShowPage(int index)
    {
        if (index < 0 || index >= _pages.Count) return;

        StopTypingIfAny();
        _typingCo = StartCoroutine(TypeRoutine(_pages[index]));
    }

    void Update()
    {
        if (!_isDisplaying) return;

        // 좌클릭 또는 스페이스: 다음/스킵
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (_isTyping)
            {
                // 타이핑 중 → 즉시 완성
                StopTypingIfAny();
                ruleText.text = _pages[_pageIndex];
                _isTyping = false;
            }
            else
            {
                // 다음 페이지
                _pageIndex++;
                if (_pageIndex < _pages.Count)
                {
                    ShowPage(_pageIndex);
                }
                else
                {
                    EndRules();
                }
            }
        }

        // ESC: 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
            EndRules();
    }

    IEnumerator TypeRoutine(string content)
    {
        _isTyping = true;
        if (ruleText != null) ruleText.text = "";

        foreach (char c in content)
        {
            if (ruleText != null) ruleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        _isTyping = false;
    }

    private void StopTypingIfAny()
    {
        if (_typingCo != null)
        {
            StopCoroutine(_typingCo);
            _typingCo = null;
        }
    }

    public void EndRules()
    {
        StopTypingIfAny();

        if (ruleCanvas != null) ruleCanvas.SetActive(false);
        if (playerController != null) playerController.SetControlEnabled(true);

        _isDisplaying = false;
        _isTyping = false;
        _pageIndex = 0;
    }
}
