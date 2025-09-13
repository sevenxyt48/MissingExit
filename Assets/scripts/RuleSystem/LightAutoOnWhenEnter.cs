using UnityEngine;
using System.Collections;

public class LightAutoOnWhenEnter : MonoBehaviour
{
    [Header("동작할 방 ID")]
    public string roomId = "2-2";

    [Header("대상 전등")]
    public SimpleLamp lamp;                    // Rule_2-2_light의 SimpleLamp

    [Header("입장 후 켜질 랜덤 지연")]
    public Vector2 delayRange = new Vector2(2f, 8f);

    [Tooltip("켜지기 전에 방을 나가면 예약을 취소할지")]
    public bool cancelIfLeftRoom = true;

    bool fired;
    Coroutine co;

    void Reset()
    {
        if (!lamp) lamp = GetComponentInChildren<SimpleLamp>(true);
    }

    void OnDisable()
    {
        if (co != null) StopCoroutine(co);
        co = null;
        fired = false;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (!fired && gm && gm.CurrentRoomId == roomId)
        {
            fired = true;
            co = StartCoroutine(CoTurnOnAfterDelay());
        }
    }

    IEnumerator CoTurnOnAfterDelay()
    {
        float wait = Random.Range(delayRange.x, delayRange.y);
        float t = 0f;
        while (t < wait)
        {
            if (cancelIfLeftRoom)
            {
                var gm = GameManager.Instance;
                if (!gm || gm.CurrentRoomId != roomId) { fired = false; yield break; }
            }
            t += Time.unscaledDeltaTime;   // 입장 직후 연출 유예와도 잘 맞음
            yield return null;
        }

        if (lamp) lamp.TurnOn();
        enabled = false; // 한 번만 실행
    }
}
