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
    private AudioListener cachedListener;

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

        if (audioSource != null)
        {
            if (popUpSound == null && audioSource.clip != null)
            {
                popUpSound = audioSource.clip;
            }
            // Prioritize trap SFX above ambient torches/lava (priority 128) so PlayOneShot is never culled
            if (audioSource.priority > 64)
            {
                audioSource.priority = 64;
            }
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
        AudioClip clipToPlay = popUpSound != null ? popUpSound : (audioSource != null ? audioSource.clip : null);
        if (clipToPlay == null) return;

        // Only play when within audible distance of the active AudioListener so distant traps don't exhaust real audio voices
        Vector3 listenerPos;
        if (TryGetListenerPosition(out listenerPos))
        {
            float maxDist = audioSource != null ? audioSource.maxDistance : 25f;
            if ((transform.position - listenerPos).sqrMagnitude > maxDist * maxDist)
            {
                return;
            }
        }

        float sfxScale = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        if (audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
        {
            audioSource.PlayOneShot(clipToPlay, sfxScale);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clipToPlay, transform.position, sfxScale);
        }
    }

    private bool TryGetListenerPosition(out Vector3 pos)
    {
        if (cachedListener == null || !cachedListener.enabled || !cachedListener.gameObject.activeInHierarchy)
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            cachedListener = null;
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i].enabled)
                {
                    cachedListener = listeners[i];
                    break;
                }
            }
        }

        if (cachedListener != null)
        {
            pos = cachedListener.transform.position;
            return true;
        }

        if (Camera.main != null)
        {
            pos = Camera.main.transform.position;
            return true;
        }

        pos = Vector3.zero;
        return false;
    }
}