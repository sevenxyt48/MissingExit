// RadioAutoOnWhenEnter.cs
using UnityEngine;
using System.Collections;

public class RadioAutoOnWhenEnter : MonoBehaviour
{
    public string roomId = "2-1";
    public SimpleRadio radio;
    public Vector2 delayRange = new Vector2(2f, 8f);
    bool fired;

    void Reset()
    {
        if (!radio) radio = GetComponentInChildren<SimpleRadio>(true);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (!fired && gm && gm.CurrentRoomId == roomId)
        {
            fired = true;
            StartCoroutine(CoTurnOn());
        }
    }

    IEnumerator CoTurnOn()
    {
        float d = Random.Range(delayRange.x, delayRange.y);
        yield return new WaitForSecondsRealtime(d);
        if (radio) radio.TurnOn();
        enabled = false; // 한 번만
    }
}
