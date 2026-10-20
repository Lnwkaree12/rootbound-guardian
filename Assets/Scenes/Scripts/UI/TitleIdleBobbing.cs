using UnityEngine;

/// <summary>
/// Adds a gentle breathing and hovering bob animation for 2D sprites in the title screen.
/// </summary>
public class TitleIdleBobbing : MonoBehaviour
{
    [SerializeField] private float bobFrequency = 2.2f;
    [SerializeField] private float bobAmplitude = 0.018f;
    [SerializeField] private float breathScale = 0.014f;

    private Vector3 initialPos;
    private Vector3 initialScale;

    private void Start()
    {
        initialPos = transform.localPosition;
        initialScale = transform.localScale;
    }

    private void Update()
    {
        float t = Time.time * bobFrequency;
        float yOffset = Mathf.Sin(t) * bobAmplitude;
        transform.localPosition = initialPos + new Vector3(0f, yOffset, 0f);

        float breath = 1f + Mathf.Sin(t) * breathScale;
        transform.localScale = new Vector3(initialScale.x * (2f - breath), initialScale.y * breath, initialScale.z);
    }
}
