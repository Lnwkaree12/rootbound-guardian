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

    private void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + Vector3.down * smashDistance;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        StartCoroutine(TrapLoop());
    }

    private IEnumerator TrapLoop()
    {
        while (true)
        {
            // 1. รอที่จุดบนสุด
            yield return new WaitForSeconds(waitTimeAtTop);

            // 2. ทุบลงมา (แก้ไข: เช็กระยะห่างแทนการใช้ !=)
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, smashSpeed * Time.deltaTime);
                yield return null;
            }

            // บังคับย้ายตำแหน่งให้เป๊ะ แล้วเล่นเสียง
            transform.position = targetPosition;
            PlaySmashSound();

            // 3. เมื่อถึงจุดล่างสุด ให้รอสักพัก
            yield return new WaitForSeconds(waitTimeAtBottom);

            // 4. ค่อยๆ ลอยกลับขึ้นไป (แก้ไข: เช็กระยะห่างแทนการใช้ !=)
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