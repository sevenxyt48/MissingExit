using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ClueUIManager : MonoBehaviour
{
    public static ClueUIManager Instance;

    [Header("UI 참조")]
    public GameObject cluePanel;
    public TMP_Text titleText;
    public TMP_Text contentText;
    public Image clueImage;
    public AudioSource audioSource;

    [Header("효과")]
    public bool useTypewriter = true;
    [Tooltip("초당 글자 수")]
    public float typewriterSpeed = 16f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool isShowing = false;
    private string currentFullContent = "";

    void Awake()
    {
        if (Instance == null) { Instance = this; InitializeUI(); }
        else { Destroy(gameObject); return; }

        // 항상 맨 위에 뜨게(정렬 보정)
        var canvas = cluePanel ? cluePanel.GetComponentInParent<Canvas>() : GetComponentInChildren<Canvas>(true);
        if (canvas) { canvas.overrideSorting = true; canvas.sortingOrder = 200; }
    }

    void InitializeUI()
    {
        if (cluePanel) cluePanel.SetActive(false);
        if (titleText) titleText.text = "";
        if (contentText) contentText.text = "";
        if (clueImage) clueImage.gameObject.SetActive(false);
    }

    public void ShowClue(string title, string content, Sprite image = null, AudioClip sound = null)
    {
        if (cluePanel) cluePanel.SetActive(true);
        isShowing = true;

        if (titleText) titleText.text = title ?? "";
        currentFullContent = content ?? "";

        // 타자기
        if (useTypewriter && contentText)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeWriter(currentFullContent));
        }
        else
        {
            if (contentText) contentText.text = currentFullContent;
            isTyping = false;
        }

        // 이미지
        if (clueImage)
        {
            if (image != null) { clueImage.sprite = image; clueImage.gameObject.SetActive(true); }
            else { clueImage.gameObject.SetActive(false); }
        }

        // 사운드
        if (audioSource)
        {
            if (sound != null) { audioSource.clip = sound; audioSource.Play(); }
            else { audioSource.Stop(); }
        }

        // 🔴 열람 시작 통지
        GameManager.Instance?.NotifyClueOpened();
    }

    IEnumerator TypeWriter(string fullText)
    {
        isTyping = true;
        if (contentText) contentText.text = "";

        float tPerChar = 1f / Mathf.Max(0.001f, typewriterSpeed);
        foreach (char c in fullText)
        {
            if (contentText) contentText.text += c;
            yield return new WaitForSeconds(tPerChar);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    void SkipTyping()
    {
        if (!isTyping) return;
        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        isTyping = false;
        if (contentText) contentText.text = currentFullContent;
    }

    void Update()
    {
        if (!isShowing) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping) { SkipTyping(); return; }
            HideClue(); return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) { HideClue(); return; }
    }

    public void HideClue()
    {
        if (!isShowing) return;

        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        isTyping = false;

        if (audioSource && audioSource.isPlaying) audioSource.Stop();
        if (clueImage) clueImage.gameObject.SetActive(false);
        if (titleText) titleText.text = "";
        if (contentText) contentText.text = "";
        if (cluePanel) cluePanel.SetActive(false);
        isShowing = false;

        // 🔴 열람 종료 통지(닫은 뒤 유예가 적용됨)
        GameManager.Instance?.NotifyClueClosed();
    }

    // 외부에서 상태 읽기 원할 때
    public bool IsOpen => isShowing;
}
