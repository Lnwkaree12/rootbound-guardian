using UnityEngine;

public class LanternSwing : MonoBehaviour
{
    [Header("Swing Settings")]
    [Tooltip("How fast the lantern swings back and forth.")]
    public float swingSpeed = 2.0f;
    
    [Tooltip("Maximum angle of the swing in degrees.")]
    public float swingAngle = 15.0f;

    private float randomOffset;

    void Start()
    {
        // Add a random offset so that multiple lanterns don't swing in perfect sync
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * swingSpeed + randomOffset) * swingAngle;
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
