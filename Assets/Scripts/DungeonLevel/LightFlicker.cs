using UnityEngine;

// Makes a light flicker like a candle flame.
// It goes on every candle, chandelier and doorway light in the level.
// Each light uses its own random starting point in the noise, so they don't
// all pulse in sync (which would look fake).
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    // how strong the flicker is: 0.18 means the brightness goes up and down by about 18%
    public float amount = 0.18f;

    // how fast it flickers
    public float speed = 3f;

    Light lightSource;
    float baseIntensity;   // the brightness the light was set to, we flicker around this
    float seed;            // random offset so every light flickers differently

    void Awake()
    {
        lightSource = GetComponent<Light>();
        baseIntensity = lightSource.intensity;
        seed = Random.value * 100f;
    }

    void Update()
    {
        // Perlin noise gives a smooth random value between 0 and 1.
        // Taking away 0.5 makes it go between -0.5 and +0.5,
        // so the light gets both brighter and dimmer than its normal brightness
        float n = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
        lightSource.intensity = baseIntensity * (1f + n * 2f * amount);
    }
}
