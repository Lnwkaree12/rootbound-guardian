using UnityEngine;
using System.Collections;

public class AutomaticSmashTrap : MonoBehaviour
{
    [Header("ตำแหน่ง")]
    [SerializeField] private float smashDistance = 5f;

    [Header("ความเร็วและเวลา")]
    [SerializeField] private float smashSpeed = 20f;
    [SerializeField] private float riseSpeed = 2f;
    [SerializeField] private float waitTimeAtBottom = 1f;
    [SerializeField] private float waitTimeAtTop = 2f;

    [Header("ระบบเสียง (Audio)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip smashSound;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private BoxCollider trapCollider;
    private int playersInsideCount = 0; // ตัวนับจำนวนผู้เล่นที่อยู่ในเขตกับดัก

    private void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + Vector3.down * smashDistance;

        trapCollider = GetComponent<BoxCollider>();
        if (trapCollider == null)
        {
            Debug.LogError("[AutomaticSmashTrap] ไม่พบ Box Collider บนตัวกับดัก!");
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        StartCoroutine(TrapLoop());
    }

    // ตรวจจับเมื่อผู้เล่นเดินเข้ามาในเขตกับดัก
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
        {
            playersInsideCount++;
        }
    }

    // ตรวจจับเมื่อผู้เล่นเดินออกจากเขตกับดัก
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
        {
            playersInsideCount = Mathf.Max(0, playersInsideCount - 1);
        }
    }

    private IEnumerator TrapLoop()
    {
        while (true)
        {
            // 1. รอที่จุดบนสุด (ปิด Trigger เพื่อให้บล็อกผู้เล่นตามปกติ)
            if (trapCollider != null) trapCollider.isTrigger = false;
            playersInsideCount = 0; // รีเซ็ตค่า
            yield return new WaitForSeconds(waitTimeAtTop);

            // 2. จังหวะทุบลงมา: เปิด Is Trigger ให้ทะลุผ่านตัวผู้เล่นและทำดาเมจ
            if (trapCollider != null) trapCollider.isTrigger = true;

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, smashSpeed * Time.deltaTime);
                yield return null;
            }

            // บังคับย้ายตำแหน่งให้เป๊ะ แล้วเล่นเสียง
            transform.position = targetPosition;
            PlaySmashSound();

            // 🌟 3. เมื่อถึงพื้น: เช็คว่ามีผู้เล่นอยู่ข้างในไหม
            // ถ้ายังมีผู้เล่นอยู่ ให้รอ (Loop วนไปเรื่อยๆ) จนกว่าผู้เล่นจะเดินออก (playersInsideCount จะเหลือ 0)
            while (playersInsideCount > 0)
            {
                yield return null;
            }

            // 4. เมื่อไม่มีผู้เล่นอยู่ข้างในแล้ว (หรือเดินออกไปแล้ว) ปิด Is Trigger ทันทีให้กลับมาเป็นทึบ
            if (trapCollider != null) trapCollider.isTrigger = false;

            // 5. รอพักตามเวลาที่กำหนดที่พื้น
            yield return new WaitForSeconds(waitTimeAtBottom);

            // 6. ค่อยๆ ลอยกลับขึ้นไป
            while (Vector3.Distance(transform.position, startPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPosition, riseSpeed * Time.deltaTime);
                yield return null;
            }

            // บังคับย้ายกลับจุดเริ่มให้เป๊ะ
            transform.position = startPosition;
        }
    }

    private void PlaySmashSound()
    {
        if (smashSound == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(smashSound);
        }
        else
        {
            AudioSource.PlayClipAtPoint(smashSound, transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 sPos = (Application.isPlaying) ? startPosition : transform.position;
        Vector3 tPos = sPos + Vector3.down * smashDistance;
        Gizmos.DrawLine(sPos, tPos);
        Gizmos.DrawWireSphere(tPos, 0.5f);
    }
}