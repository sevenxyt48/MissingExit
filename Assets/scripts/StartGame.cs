using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartGame : MonoBehaviour
{
    public Image fadeImage;          // 검은색 Image (Background 위에 덮음)
    public Image backgroundImage;    // 실제 배경 이미지
    public TMP_Text titleText;
    public float fadeSpeed = 1f;
    public AudioSource bgmSource;
    public string fullTitle = "잃어버린 출구";
    public float typingSpeed = 0.1f;

    private bool startGame = false;

    void Start()
    {
        // fadeImage를 완전히 투명하게 설정
        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0);

        // 배경 이미지는 항상 보이게 (알파 = 1)
        if (backgroundImage != null)
            backgroundImage.color = new Color(1, 1, 1, 1);

        // 제목 초기화
        if (titleText != null)
            titleText.text = "";

        // 배경음악
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // 타이핑 시작
        StartCoroutine(TypeTitle());
    }

    IEnumerator TypeTitle()
    {
        foreach (char c in fullTitle)
        {
            titleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void Update()
    {
        // 아무 키 입력 시 시작
        if (!startGame && Input.anyKeyDown)
        {
            startGame = true;
            SceneManager.LoadScene("GameScene"); // 바로 다음 씬 로드
        }
    }
}