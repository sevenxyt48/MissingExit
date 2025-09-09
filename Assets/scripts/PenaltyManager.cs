using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PenaltyManager : MonoBehaviour
{
    [Header("References")]
    public Image screenFlash;       // UI 이미지로 화면 깜빡임
    public AudioSource penaltySound;

    [Header("Settings")]
    public float flashDuration = 0.3f;

    public void ApplyPenalty(string reason)
    {
        Debug.Log("[PenaltyManager] 벌칙 적용: " + reason);

        if (screenFlash != null)
            StartCoroutine(FlashScreen());

        if (penaltySound != null)
            penaltySound.Play();
    }

    private IEnumerator FlashScreen()
    {
        screenFlash.enabled = true;
        yield return new WaitForSeconds(flashDuration);
        screenFlash.enabled = false;
    }
}
