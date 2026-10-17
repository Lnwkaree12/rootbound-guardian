using UnityEngine;

/// <summary>
/// Helper MonoBehaviour to synchronize active state between paws on left and right of a button.
/// </summary>
public class PawPairSync : MonoBehaviour
{
    public GameObject leftPaw;
    public GameObject rightPaw;

    private void OnEnable()
    {
        if (leftPaw != null) leftPaw.SetActive(true);
        if (rightPaw != null) rightPaw.SetActive(true);
    }

    private void OnDisable()
    {
        if (leftPaw != null) leftPaw.SetActive(false);
        if (rightPaw != null) rightPaw.SetActive(false);
    }
}
