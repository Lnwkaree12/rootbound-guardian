using UnityEngine;
using System.Collections;

public class AutomaticSmashTrap : MonoBehaviour
{
    [Header("ตำแหน่ง")]
    [SerializeField] private float smashDistance = 5f; // ระยะทางที่หินจะทุบลงไป (จากจุดเริ่ม)

    [Header("ความเร็วและเวลา")]
    [SerializeField] private float smashSpeed = 20f;   // ความเร็วตอนทุบลง (ยิ่งเยอะยิ่งแรง)
    [SerializeField] private float riseSpeed = 2f;    // ความเร็วตอนลอยกลับขึ้นไป (มักจะช้ากว่า)
    [SerializeField] private float waitTimeAtBottom = 1f; // เวลารอที่พื้นก่อนขึ้น
    [SerializeField] private float waitTimeAtTop = 2f;    // เวลารอก่อนเริ่มทุบใหม่

    [Header("ระบบเสียง (Audio)")]
    [SerializeField] private AudioSource audioSource; // ตัวเล่นเสียง
    [SerializeField] private AudioClip smashSound;   // เสียงทุบกระแทกพื้น

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private void Start()
    {
        // บันทึกตำแหน่งเริ่มต้น
        startPosition = transform.position;
        // คำนวณตำแหน่งเป้าหมายด้านล่าง
        targetPosition = startPosition + Vector3.down * smashDistance;

        // ถ้าไม่ได้แนบ AudioSource มา ให้ลองหาใน GameObject ตัวนี้
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // เริ่มทำงาน loop อัตโนมัติ
        StartCoroutine(TrapLoop());
    }

    private IEnumerator TrapLoop()
    {
        // วนลูปทำงานไปเรื่อยๆ ไม่มีวันจบ
        while (true)
        {
            // 1. รอที่จุดบนสุด
            yield return new WaitForSeconds(waitTimeAtTop);

            // 2. ทุบลงมาอย่างรวดเร็ว
            while (transform.position != targetPosition)
            {
                // MoveTowards จะค่อยๆ ย้าย object ไปที่เป้าหมาย
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, smashSpeed * Time.deltaTime);
                yield return null; // รอเฟรมถัดไป
            }

            // เล่นเสียงกระแทกพื้นเมื่อทุบลงมาถึงจุดล่างสุด
            PlaySmashSound();

            // 3. เมื่อถึงจุดล่างสุด ให้รอสักพัก
            yield return new WaitForSeconds(waitTimeAtBottom);

            // 4. ค่อยๆ ลอยกลับขึ้นไป
            while (transform.position != startPosition)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPosition, riseSpeed * Time.deltaTime);
                yield return null; // รอเฟรมถัดไป
            }
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
            // หากไม่มี AudioSource จะเล่นเสียงแบบ 3D ณ ตำแหน่งที่กระแทกพื้น
            AudioSource.PlayClipAtPoint(smashSound, transform.position);
        }
    }

    // วาดเส้น Debug ในหน้า Scene เพื่อให้เห็นระยะทุบ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 sPos = (Application.isPlaying) ? startPosition : transform.position;
        Vector3 tPos = sPos + Vector3.down * smashDistance;
        Gizmos.DrawLine(sPos, tPos);
        Gizmos.DrawWireSphere(tPos, 0.5f);
    }
}