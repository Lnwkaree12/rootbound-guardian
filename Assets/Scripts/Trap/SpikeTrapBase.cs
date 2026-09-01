using UnityEngine;
using System.Collections;

public class SpikeTrapBase : MonoBehaviour
{
    [Header(" Object หนามที่ต้องการให้ขยับ")]
    [SerializeField] private Transform spikeTransform; // ลาก Object หนามมาใส่ช่องนี้

    [Header("ตำแหน่งการยื่นของหนาม")]
    [SerializeField] private float spikeUpDistance = 1.5f;

    [Header("ความเร็วในการเคลื่อนที่")]
    [SerializeField] private float popUpSpeed = 15f;
    [SerializeField] private float retractSpeed = 2f;

    [Header("ระยะเวลาการรอ (วินาที)")]
    [SerializeField] private float activeTime = 1f;
    [SerializeField] private float cooldownTime = 2f;

    private Vector3 hiddenPosition;
    private Vector3 targetPosition;

    private void Start()
    {
        if (spikeTransform == null)
        {
            Debug.LogError("กรุณาลาก Object หนามมาใส่ในช่อง Spike Transform ด้วยครับ!");
            return;
        }

        // บันทึกตำแหน่งเริ่มต้นของหนาม
        hiddenPosition = spikeTransform.position;
        // คำนวณตำแหน่งที่หนามจะพุ่งขึ้นมา
        targetPosition = hiddenPosition + spikeTransform.up * spikeUpDistance;

        StartCoroutine(SpikeLoop());
    }

    private IEnumerator SpikeLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldownTime);

            // พุ่งขึ้น
            while (Vector3.Distance(spikeTransform.position, targetPosition) > 0.01f)
            {
                spikeTransform.position = Vector3.MoveTowards(spikeTransform.position, targetPosition, popUpSpeed * Time.deltaTime);
                yield return null;
            }
            spikeTransform.position = targetPosition;

            yield return new WaitForSeconds(activeTime);

            // หดลง
            while (Vector3.Distance(spikeTransform.position, hiddenPosition) > 0.01f)
            {
                spikeTransform.position = Vector3.MoveTowards(spikeTransform.position, hiddenPosition, retractSpeed * Time.deltaTime);
                yield return null;
            }
            spikeTransform.position = hiddenPosition;
        }
    }
}