using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    private void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // หันหน้า Canvas ไปทิศทางเดียวกับกล้องเสมอ
            transform.rotation = mainCameraTransform.rotation;
        }
    }
}