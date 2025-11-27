using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextColor : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public TMP_Text text;             // 버튼 안의 글자
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.8f, 0.8f);
    public Color pressedColor = new Color(1f, 0.4f, 0.4f);

    void Start()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.color = normalColor;
    }

    // ==== 마우스 이벤트 ==== //
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (text != null) text.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (text != null) text.color = normalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (text != null) text.color = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (text != null) text.color = hoverColor;
    }

    // ==== 키보드용 헬퍼 메서드 ==== //
    // StartGame에서 선택된 항목/비선택 항목 색 바꿀 때 사용
    public void SetNormalByKeyboard()
    {
        if (text != null) text.color = normalColor;
    }

    public void SetSelectedByKeyboard()
    {
        if (text != null) text.color = hoverColor;
    }

    public void SetPressedByKeyboard()
    {
        if (text != null) text.color = pressedColor;
    }
}
