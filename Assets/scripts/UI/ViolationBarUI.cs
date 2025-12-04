using UnityEngine;
using UnityEngine.UI;

public class ViolationBarUI : MonoBehaviour
{
    public static ViolationBarUI Instance;   // 🔴 외부에서 접근할 수 있는 인스턴스

    [Header("UI References")]
    public Image fullImage;       // 붉은 바 (HP-F) : Filled/Horizontal/Left 로 설정
    public Image backgroundImage; // 회색 바 (HP_n) : Simple, 100%
    public Text countText;        // 선택: "x / limit" 표시

    [Header("애니메이션")]
    public bool smooth = true;
    public float lerpSpeed = 8f;  // 수치 변화 부드럽게

    int cachedLimit = 10;
    float currentFill = 1f;

    void Awake()
    {
        // 싱글톤 등록
        Instance = this;

        if (fullImage)
        {
            fullImage.type = Image.Type.Filled;
            fullImage.fillMethod = Image.FillMethod.Horizontal;
            fullImage.fillOrigin = (int)Image.OriginHorizontal.Left; // 왼쪽에서 오른쪽으로 차오름
            fullImage.fillAmount = 1f;
        }
        if (backgroundImage)
        {
            backgroundImage.type = Image.Type.Simple; // 항상 꽉 찬 빈 바
        }

        cachedLimit = GetLimitFromGM();
        currentFill = 1f;
        ApplyFill(1f, true);

        // 시작 텍스트(선택)
        if (countText)
            countText.text = $"0 / {cachedLimit}";
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (!gm) return;

        int count = gm.ViolationCount;
        cachedLimit = GetLimitFromGM();

        // 남은 비율 = 1 - (위반/한도)
        float targetFill = Mathf.Clamp01(1f - (float)count / Mathf.Max(1, cachedLimit));

        // 부드럽게/즉시
        float dt = (Time.unscaledDeltaTime > 0 ? Time.unscaledDeltaTime : Time.deltaTime);
        currentFill = smooth
            ? Mathf.Lerp(currentFill, targetFill, dt * lerpSpeed)
            : targetFill;

        ApplyFill(currentFill, false);

        if (countText) countText.text = $"{count} / {cachedLimit}";
    }

    void ApplyFill(float fill, bool force)
    {
        if (fullImage && (force || !Mathf.Approximately(fullImage.fillAmount, fill)))
            fullImage.fillAmount = fill;
    }

    int GetLimitFromGM()
    {
        if (GameManager.Instance == null) return cachedLimit;

        var f = typeof(GameManager).GetField(
            "violationLimit",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        return (f != null) ? (int)f.GetValue(GameManager.Instance) : cachedLimit;
    }

    // 🔴 게임 재시작 시 체력바를 즉시 초기 상태로 돌리는 함수
    public void ResetImmediate()
    {
        cachedLimit = GetLimitFromGM();
        currentFill = 1f;
        ApplyFill(1f, true);

        if (countText)
            countText.text = $"0 / {cachedLimit}";
    }

    // (선택) 벌칙 때 살짝 번쩍이게 하고 싶으면 PenaltyManager에서 호출
    public void Nudge(float amount = 0.07f)
    {
        if (!fullImage) return;
        // 간단한 크기 펄스 등 연출은 나중에 추가 가능
    }
}
