using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    public float rotationSpeed = 10f;

    [Header("Lighting")]
    public Light sun;
    public Light moon;

    [Header("Light Properties")]
    public float sunIntensity = 1f;
    public float moonIntensity = 0.3f;
    public Color sunColor = new Color(1f, 0.95f, 0.8f);
    public Color moonColor = new Color(0.7f, 0.8f, 1f);

    [Header("Shadow Settings (Precision Fix)")]
    public float shadowDistance = 50f; // Very tight shadow distance for precision
    public float shadowBias = 0.05f; // Higher bias to prevent acne/floating
    public float shadowNormalBias = 0.8f; // Much higher to ground shadows

    [Header("Stars")]
    public ParticleSystem stars;
    public float starFadeSpeed = 2f;

    [Header("Ambient Light")]
    public float dayAmbient = 1f;
    public float nightAmbient = 0.1f;
    public Color dayAmbientColor = new Color(0.9f, 0.95f, 1f);
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.15f);

    private ParticleSystem.EmissionModule starEmission;
    private float lastShadowUpdateTime = 0f;
    private const float shadowUpdateInterval = 0.016f; // Update shadows at ~60 FPS even if game runs faster

    void Start()
    {
        if (stars != null)
            starEmission = stars.emission;

        InitializeLights();
        OptimizeShadowSettings();
    }

    void InitializeLights()
    {
        // Set up sun
        if (sun != null)
        {
            sun.intensity = sunIntensity;
            sun.color = sunColor;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = shadowBias;
            sun.shadowNormalBias = shadowNormalBias;
        }

        // Set up moon
        if (moon != null)
        {
            moon.intensity = 0f; // Start off
            moon.color = moonColor;
            moon.shadows = LightShadows.None; // No shadows from moon to prevent jitter
        }
    }

    void OptimizeShadowSettings()
    {
        // Optimize global shadow cascade settings for precision
        // No cascades = best precision for close range, no jitter at transitions
        QualitySettings.shadowCascades = 0; // No cascades = best precision, no transition jitter
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh; // Maximum resolution for precision
        QualitySettings.shadowDistance = shadowDistance;
    }

    void Update()
    {
        // 1. Rotate the parent object to simulate day/night cycle
        // Smooth rotation with Time.deltaTime for frame-rate independence
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);

        // 2. Calculate day/night transition
        float currentAngle = transform.eulerAngles.x;
        float nightFactor = CalculateNightFactor(currentAngle);

        // 3. Update light intensities and shadows (throttled to prevent jitter)
        lastShadowUpdateTime += Time.deltaTime;
        if (lastShadowUpdateTime >= shadowUpdateInterval)
        {
            UpdateLights(nightFactor);
            lastShadowUpdateTime = 0f;
        }

        // 4. Update stars
        UpdateStars(nightFactor);

        // 5. Update ambient lighting
        UpdateAmbientLight(nightFactor);
    }

    float CalculateNightFactor(float angle)
    {
        // Smooth transition between day and night
        // 0° = noon (full day), 180° = midnight (full night)
        if (angle <= 180f)
        {
            // Day to dusk (0° to 180°)
            return Mathf.Clamp01(Mathf.InverseLerp(90f, 180f, angle));
        }
        else
        {
            // Night to dawn (180° to 360°)
            return Mathf.Clamp01(1f - Mathf.InverseLerp(180f, 270f, angle));
        }
    }

    void UpdateLights(float nightFactor)
    {
        float currentAngle = transform.eulerAngles.x;

        // Sun: ONLY enable when above the horizon (angles 0-180)
        if (sun != null)
        {
            // Sun is above horizon when angle is between 0-180
            bool sunAboveHorizon = currentAngle >= 0f && currentAngle <= 180f;

            if (sunAboveHorizon)
            {
                // Sun is visible, gradually adjust intensity based on angle
                float sunTargetIntensity = sunIntensity;
                sun.intensity = Mathf.Lerp(sun.intensity, sunTargetIntensity, Time.deltaTime * 2f);
                sun.shadows = LightShadows.Soft;
            }
            else
            {
                // Sun is below horizon - turn OFF completely
                sun.intensity = 0f;
                sun.shadows = LightShadows.None;
            }
        }

        // Moon: ONLY enable when it's above the horizon (not below terrain)
        if (moon != null)
        {
            // Moon should only emit light when angle is between 180-360 (above horizon during night)
            bool moonAboveHorizon = currentAngle > 180f && currentAngle < 360f;

            if (moonAboveHorizon && nightFactor > 0.5f)
            {
                float moonTargetIntensity = moonIntensity;
                moon.intensity = Mathf.Lerp(moon.intensity, moonTargetIntensity, Time.deltaTime * 2f);
            }
            else
            {
                // Turn off moon when below horizon or during day
                moon.intensity = 0f;
            }

            // Keep moon shadows OFF always to prevent upward shadow casting
            moon.shadows = LightShadows.None;
        }
    }

    void UpdateStars(float nightFactor)
    {
        if (stars != null)
        {
            // Stars appear when it's properly dark
            float targetRate = (nightFactor > 0.8f) ? 1000f : 0f;
            float currentRate = starEmission.rateOverTime.constant;
            float newRate = Mathf.Lerp(currentRate, targetRate, Time.deltaTime * starFadeSpeed);
            starEmission.rateOverTime = newRate;
        }
    }

    void UpdateAmbientLight(float nightFactor)
    {
        // Ambient intensity
        RenderSettings.ambientIntensity = Mathf.Lerp(dayAmbient, nightAmbient, nightFactor);

        // Ambient color (optional - makes nights feel more blue)
        RenderSettings.ambientLight = Color.Lerp(dayAmbientColor, nightAmbientColor, nightFactor);
    }

    // Helper method to quickly test day/night states
    public void SetTimeOfDay(float normalizedTime)
    {
        // 0 = dawn, 0.5 = dusk, 1 = next dawn
        float targetAngle = normalizedTime * 360f;
        transform.rotation = Quaternion.Euler(targetAngle, 0, 0);
    }
}