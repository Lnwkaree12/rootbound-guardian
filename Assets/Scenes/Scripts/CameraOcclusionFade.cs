using UnityEngine;
using System.Collections.Generic;

public class CameraOcclusionFade : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform; // ลากตัวผู้เล่นมาใส่
    [SerializeField] private LayerMask obstacleLayer;   // เลเยอร์ของฉาก/สิ่งกีดขวาง

    [Header("Fade Settings")]
    [SerializeField] private float fadedAlpha = 0.3f;   // ความโปร่งใสเวลาโดนบัง (0 = ใส, 1 = ทึบ)
    [SerializeField] private float fadeSpeed = 5f;      // ความเร็วในการเปลี่ยนความโปร่งใส
    [SerializeField] private float castRadius = 0.4f;   // รัศมีลำแสงสำหรับตรวจจับด้านข้าง

    private List<RendererAndMaterial> currentlyFadedObjects = new List<RendererAndMaterial>();

    private class RendererAndMaterial
    {
        public Renderer renderer;
        public List<Material> materials = new List<Material>();
        public bool isHitThisFrame;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        Vector3 direction = playerTransform.position - transform.position;
        float distance = direction.magnitude;

        // รีเซ็ตสถานะการชนในเฟรมนี้
        foreach (var obj in currentlyFadedObjects)
        {
            obj.isHitThisFrame = false;
        }

        // ใช้ SphereCastAll เพื่อยิงลำแสงแบบมีความกว้าง ครอบคลุมทุกมุมมองด้านข้าง
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, castRadius, direction.normalized, distance, obstacleLayer);

        foreach (var hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                RendererAndMaterial item = currentlyFadedObjects.Find(x => x.renderer == rend);
                if (item == null)
                {
                    item = new RendererAndMaterial();
                    item.renderer = rend;
                    foreach (var mat in rend.materials)
                    {
                        item.materials.Add(mat);
                    }
                    currentlyFadedObjects.Add(item);
                }
                item.isHitThisFrame = true;
            }
        }

        // จัดการปรับค่าความโปร่งใส
        for (int i = currentlyFadedObjects.Count - 1; i >= 0; i--)
        {
            var item = currentlyFadedObjects[i];
            if (item.renderer == null)
            {
                currentlyFadedObjects.RemoveAt(i);
                continue;
            }

            float targetAlpha = item.isHitThisFrame ? fadedAlpha : 1.0f;

            foreach (var mat in item.materials)
            {
                if (mat.HasProperty("_Alpha")) 
                {
                    float currentAlpha = mat.GetFloat("_Alpha");
                    float newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
                    mat.SetFloat("_Alpha", newAlpha);
                }
                else if (mat.HasProperty("_BaseColor")) 
                {
                    Color color = mat.GetColor("_BaseColor");
                    color.a = Mathf.Lerp(color.a, targetAlpha, fadeSpeed * Time.deltaTime);
                    mat.SetColor("_BaseColor", color);
                }
            }

            // ถ้าวัตถุกลับมาทึบแสงปกติแล้ว ให้ลบออกจากรายชื่อ
            if (!item.isHitThisFrame)
            {
                bool isFullyOpaque = true;
                foreach (var mat in item.materials)
                {
                    float currentAlpha = 1f;
                    if (mat.HasProperty("_Alpha"))
                    {
                        currentAlpha = mat.GetFloat("_Alpha");
                    }
                    else if (mat.HasProperty("_BaseColor"))
                    {
                        currentAlpha = mat.GetColor("_BaseColor").a;
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        currentAlpha = mat.GetColor("_Color").a;
                    }

                    if (Mathf.Abs(currentAlpha - 1.0f) > 0.01f)
                    {
                        isFullyOpaque = false;
                        break;
                    }
                }

                if (isFullyOpaque)
                {
                    currentlyFadedObjects.RemoveAt(i);
                }
            }
        }
    }
}