using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    [Tooltip("Minimum light intensity during flicker.")]
    public float minIntensity = 1.6f;
    
    [Tooltip("Maximum light intensity during flicker.")]
    public float maxIntensity = 2.4f;
    
    [Tooltip("How fast the light intensity changes.")]
    public float flickerSpeed = 0.08f;

    [Header("Position Jitter (Optional)")]
    [Tooltip("If true, the light position will jitter slightly to simulate a moving flame.")]
    public bool jitterPosition = true;
    
    [Tooltip("Maximum distance the light can jitter from its base position.")]
    public float jitterRange = 0.05f;

    private Light pointLight;
    private float targetIntensity;
    private float lastIntensity;
    private float timer;
    private Vector3 basePosition;

    void Start()
    {
        pointLight = GetComponent<Light>();
        basePosition = transform.localPosition;
        
        if (pointLight != null)
        {
            lastIntensity = pointLight.intensity;
            targetIntensity = lastIntensity;
        }
    }

    void Update()
    {
        if (pointLight == null) return;

        // 1. Intensity Flickering (smooth interpolation)
        timer += Time.deltaTime;
        if (timer >= flickerSpeed)
        {
            targetIntensity = Random.Range(minIntensity, maxIntensity);
            lastIntensity = pointLight.intensity;
            timer = 0f;
        }

        pointLight.intensity = Mathf.Lerp(lastIntensity, targetIntensity, timer / flickerSpeed);

        // 2. Position Jittering
        if (jitterPosition)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-jitterRange, jitterRange),
                Random.Range(-jitterRange, jitterRange),
                Random.Range(-jitterRange, jitterRange)
            );
            transform.localPosition = Vector3.Lerp(transform.localPosition, basePosition + randomOffset, Time.deltaTime * 5f);
        }
    }
}
