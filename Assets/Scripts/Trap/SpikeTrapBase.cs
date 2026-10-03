using UnityEngine;
using System.Collections;

public class SpikeTrapBase : MonoBehaviour
{
    [Header(" Object หนามที่ต้องการให้ขยับ")]
    [SerializeField] private Transform spikeTransform;

    [Header("ตัวทำดาเมจ / Collider หนาม")]
    [SerializeField] private Collider spikeCollider; // ลาก Box Collider ของตัวหนามมาใส่ หรือใช้ DamageDealer

    [Header("ตำแหน่งการยื่นของหนาม")]
    [SerializeField] private float spikeUpDistance = 1.5f;

    [Header("ความเร็วในการเคลื่อนที่")]
    [SerializeField] private float popUpSpeed = 15f;
    [SerializeField] private float retractSpeed = 2f;

    [Header("ระยะเวลาการรอ (วินาที)")]
    [SerializeField] private float activeTime = 1f;
    [SerializeField] private float cooldownTime = 2f;

    [Header("ระบบเสียง (Audio)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popUpSound;

    private Vector3 hiddenPosition;
    private Vector3 targetPosition;

    private void Start()
    {
        if (spikeTransform == null)
        {
            Debug.LogError("กรุณาลาก Object หนามมาใส่ในช่อง Spike Transform ด้วยครับ!");
            return;
        }

        // หากไม่ได้ลาก Collider มาใส่ ให้ลองดึงจาก spikeTransform อัตโนมัติ
        if (spikeCollider == null)
        {
            spikeCollider = spikeTransform.GetComponent<Collider>();
        }

        // ปิด Collider ไว้ตั้งแต่เริ่มเกม
        if (spikeCollider != null)
        {
            spikeCollider.enabled = false;
        }

        hiddenPosition = spikeTransform.position;
        targetPosition = hiddenPosition + spikeTransform.up * spikeUpDistance;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        StartCoroutine(SpikeLoop());
    }

    private IEnumerator SpikeLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldownTime);

            PlayPopUpSound();

            // 🟢 เปิด Collider ทำดาเมจ เมื่อหนามเริ่มพุ่งขึ้น
            if (spikeCollider != null) spikeCollider.enabled = true;

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

            // 🔴 ปิด Collider เมื่อหนามหดกลับสุดแล้ว
            if (spikeCollider != null) spikeCollider.enabled = false;
        }
    }

    private void PlayPopUpSound()
    {
        if (popUpSound == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(popUpSound);
        }
        else
        {
            AudioSource.PlayClipAtPoint(popUpSound, transform.position);
        }
    }
}