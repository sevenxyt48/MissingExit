using UnityEngine;

public class UIFaceCamera : MonoBehaviour
{
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        // 카메라 방향을 기준으로 Y축만 회전 (기울어지지 않게)
        Vector3 toCam = cam.transform.position - transform.position;
        toCam.y = 0f;                     // 위아래 기울어지는 것 방지
        if (toCam.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(-toCam);
        //  ↑ 이미지의 앞면이 카메라를 향하게 (-toCam)
    }
}
