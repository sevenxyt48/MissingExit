using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 2-3반 조장(칠판) 규칙:
/// - InteractOnKey 등에서 Erase()를 호출하면 칠판의 TMP 텍스트를 지우고 즉시 위반 처리.
/// - Renderer 머티리얼 바꾸기 옵션도 겸용.
/// </summary>
public class BlackboardRule : MonoBehaviour
{
    [Header("동작할 방 ID")]
    public string onlyInRoom = "2-3";

    [Header("지울 대상 - TMP 텍스트")]
    public TMP_Text[] texts;                   // TextInBoard3 같은 Text (TMP)
    public bool fadeOut = true;
    public float fadeSeconds = 0.5f;
    [TextArea] public string replaceWith = ""; // ""로 비우기

    [Header("지울 대상 - 머티리얼 교체(선택)")]
    public Renderer[] renderers;               // 칠판 메시에 Renderer가 있다면
    public Material erasedMaterial;

    [Header("Erase 후 On/Off(선택)")]
    public GameObject[] enableOnErase;
    public GameObject[] disableOnErase;

    [Header("Penalty")]
    public string violationReason = "조장 규칙 위반";
    [TextArea] public string penaltyText = "조장 허락 없이 칠판을 지우면 안 된다.";

    bool erased;

    // 편의: 자식에서 자동 채움
    [ContextMenu("Auto Fill (TMP Texts & Renderers)")]
    void AutoFill()
    {
        texts = GetComponentsInChildren<TMP_Text>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    public void Erase()
    {
        if (erased) return;

        var gm = GameManager.Instance;
        if (gm && !string.IsNullOrEmpty(onlyInRoom) && gm.CurrentRoomId != onlyInRoom)
            return; // 방 밖이면 무시(원하면 제거)

        erased = true;

        // 1) TMP 텍스트 지우기
        if (texts != null && texts.Length > 0)
        {
            if (fadeOut) StartCoroutine(CoFadeAndHide(texts, replaceWith, fadeSeconds));
            else
            {
                foreach (var t in texts) if (t) t.text = replaceWith;
            }
        }

        // 2) 칠판 머티리얼 교체(옵션)
        if (erasedMaterial && renderers != null)
        {
            foreach (var r in renderers)
                if (r) r.sharedMaterial = erasedMaterial;
        }

        // 3) 토글
        foreach (var go in disableOnErase) if (go) go.SetActive(false);
        foreach (var go in enableOnErase) if (go) go.SetActive(true);

        // 4) 벌칙
        PenaltyManager.Instance?.ApplyPenalty(violationReason, penaltyText, null, 1f, true);
        Debug.Log("[BlackboardRule] 칠판 지우기 → 규칙 위반");
    }

    System.Collections.IEnumerator CoFadeAndHide(TMP_Text[] list, string after, float dur)
    {
        var cached = new List<(TMP_Text t, Color c)>();
        foreach (var t in list) if (t) cached.Add((t, t.color));

        float tmr = 0f;
        while (tmr < dur)
        {
            tmr += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, (dur <= 0f ? 1f : tmr / dur));
            for (int i = 0; i < cached.Count; i++)
            {
                var c = cached[i].c; c.a = a;
                if (cached[i].t) cached[i].t.color = c;
            }
            yield return null;
        }
        // 마무리: 텍스트 비우고(선택) 비활성화까지
        for (int i = 0; i < cached.Count; i++)
        {
            if (!cached[i].t) continue;
            cached[i].t.text = after;
            var c = cached[i].t.color; c.a = 1f; cached[i].t.color = c;
            cached[i].t.gameObject.SetActive(false);
        }
    }
}
