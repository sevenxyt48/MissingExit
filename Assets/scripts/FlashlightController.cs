using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    public Light spot;                    // 카메라에 붙은 Spot Light
    [Tooltip("없으면 자동으로 자식에서 찾음")]
    public Transform swivel;              // 손전등 미세 흔들림용 기준(없으면 this.transform)

    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.L;
    public bool startOn = true;

    [Header("Beam Settings")]
    [Range(10f, 60f)] public float spotAngle = 30f;
    [Range(5f, 30f)] public float range = 15f;
    [Range(0f, 8f)] public float intensity = 3f; // 비물리 광 기준
    public bool castShadows = true;

    [Header("Flicker (공포 연출)")]
    public bool enableFlicker = true;
    [Range(0f, 1f)] public float flickerStrength = 0.15f;
    [Range(0.1f, 10f)] public float flickerSpeed = 4f;

    [Header("Sway (손 흔들림)")]
    public bool enableSway = true;
    [Range(0f, 1f)] public float swayAngle = 0.4f;
    [Range(0.1f, 10f)] public float swaySpeed = 1.2f;

    float baseIntensity;
    float baseSpotAngle;
    Quaternion baseRot;
    bool isOn;

    void Reset()
    {
        spot = GetComponentInChildren<Light>();
        swivel = transform;
    }

    void Awake()
    {
        if (!spot)
        {
            spot = GetComponentInChildren<Light>();
            if (!spot)
            {
                // 자동 생성
                GameObject go = new GameObject("Flashlight_Spot");
                go.transform.SetParent(transform, false);
                spot = go.AddComponent<Light>();
                spot.type = LightType.Spot;
            }
        }
        if (!swivel) swivel = transform;

        // 기본값 적용
        spot.type = LightType.Spot;
        spot.spotAngle = spotAngle;
        spot.range = range;
        spot.intensity = intensity;
        spot.shadows = castShadows ? LightShadows.Hard : LightShadows.None;

        baseIntensity = spot.intensity;
        baseSpotAngle = spot.spotAngle;
        baseRot = swivel.localRotation;

        SetOn(startOn);
    }

    void Update()
    {
        // 토글
        if (Input.GetKeyDown(toggleKey))
            SetOn(!isOn);

        if (!isOn) return;

        // Flicker
        if (enableFlicker)
        {
            float n = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.1234f);
            float delta = (n - 0.5f) * 2f * flickerStrength;
            spot.intensity = baseIntensity * (1f + delta);
        }

        // Sway
        if (enableSway && swivel)
        {
            float t = Time.time * swaySpeed;
            float x = Mathf.Sin(t) * swayAngle;
            float y = Mathf.Cos(t * 0.7f) * swayAngle;
            swivel.localRotation = baseRot * Quaternion.Euler(x, y, 0f);
        }

        // 런타임 슬라이더용 동기화(원하면 고정)
        spot.spotAngle = spotAngle;
        spot.range = range;
    }

    public void SetOn(bool on)
    {
        isOn = on;
        if (spot) spot.enabled = on;
    }
}
