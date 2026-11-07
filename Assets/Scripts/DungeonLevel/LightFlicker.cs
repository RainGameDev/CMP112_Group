using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    public float amount = 0.18f;
    public float speed = 3f;

    Light myLight;
    float normalIntensity;
    float randomOffset;

    void Awake()
    {
        myLight = GetComponent<Light>();
        normalIntensity = myLight.intensity;
        randomOffset = Random.value * 100f;
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(randomOffset, Time.time * speed) - 0.5f;
        myLight.intensity = normalIntensity * (1f + noise * 2f * amount);
    }
}
