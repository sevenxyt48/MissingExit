// WindowAutoOpenWhenEnter.cs
using UnityEngine;
using System.Collections;

public class WindowAutoOpenWhenEnter : MonoBehaviour
{
    public string roomId = "2-3";
    public SimpleWindow window;
    public Vector2 delayRange = new Vector2(2f, 8f);
    public bool cancelIfLeftRoom = true;

    bool fired;
    Coroutine co;

    void Reset()
    {
        if (!window) window = GetComponentInChildren<SimpleWindow>(true);
    }

    void OnDisable()
    {
        if (co != null) StopCoroutine(co);
        co = null; fired = false;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (!fired && gm && gm.CurrentRoomId == roomId)
        {
            fired = true;
            co = StartCoroutine(CoOpen());
        }
    }

    IEnumerator CoOpen()
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
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (window) window.Open();
        enabled = false; // 한 번만
    }
}
