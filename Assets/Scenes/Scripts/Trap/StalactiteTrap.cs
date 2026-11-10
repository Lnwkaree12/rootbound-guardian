using System.Collections;
using UnityEngine;

public class StalactiteTrap : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private string stalactiteID; // ID หินแต่ละก้อน (ห้ามซ้ำกัน)

    [Header("Fall Mode Settings")]
    [SerializeField] private bool useRandomDelay = false;
    [SerializeField] private float minDelay = 0f;
    [SerializeField] private float maxDelay = 1.5f;

    [Header("Fall Speed Settings")]
    [SerializeField] private float initialFallVelocity = 10f;
    [SerializeField] private float extraGravityForce = 20f;

    [Header("Settings")]
    [SerializeField] private float destroyDelay = 3f; // เวลาลบทิ้งสำรองกรณีไม่ชนอะไรเลย

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fallSound;   // เสียงตอนเริ่มร่วง
    [SerializeField] private AudioClip impactSound; // เสียงตอนชนกระทบพื้นหรือผู้เล่น

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    private bool hasFallen = false;
    private bool hasImpacted = false; // ป้องกันการเล่นเสียงชนซ้ำซ้อน
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public string StalactiteID => stalactiteID;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(stalactiteID))
        {
            stalactiteID = gameObject.name + "_" + transform.position.ToString();
        }
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (fallSound != null || impactSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // ตั้งเป็น 3D Sound ตามตำแหน่งหิน
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        ResetStalactiteState(false);
    }

    public void DropStalactite()
    {
        if (hasFallen) return;

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.MarkStalactiteAsFallen(stalactiteID);
        }

        float delayToUse = minDelay;
        if (useRandomDelay)
        {
            delayToUse = Random.Range(minDelay, maxDelay);
        }

        StartCoroutine(DelayedDropRoutine(delayToUse));
    }

    private IEnumerator DelayedDropRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        ExecuteDrop();
    }

    private void ExecuteDrop()
    {
        if (hasFallen) return;
        hasFallen = true;
        hasImpacted = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.down * initialFallVelocity;
        }

        // เล่นเสียงตอนร่วง
        PlaySound(fallSound);

        // ตั้งเวลาลบทิ้งสำรอง (ถ้าหินตกไม่โดนอะไรเลย จะได้ไม่ค้างบนฉาก)
        Destroy(gameObject, destroyDelay);
    }

    private void FixedUpdate()
    {
        if (hasFallen && rb != null && !hasImpacted)
        {
            rb.AddForce(Vector3.down * extraGravityForce, ForceMode.Acceleration);
        }
    }

    // 🌟 ตรวจจับการชนแบบ Collision ปกติ
    private void OnCollisionEnter(Collision collision)
    {
        if (!hasFallen || hasImpacted) return;

        if (IsValidTarget(collision.gameObject))
        {
            HandleImpact();
        }
    }

    // 🌟 ตรวจจับการชนแบบ Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (!hasFallen || hasImpacted) return;

        if (IsValidTarget(other.gameObject))
        {
            HandleImpact();
        }
    }

    // 🔍 ฟังก์ชันตรวจสอบว่าเป็น Player หรือ พื้น (Ground/Floor) หรือไม่
    private bool IsValidTarget(GameObject target)
    {
        // 1. เช็คว่าเป็น Player หรือไม่ (เช็คจาก Tag หรือมี Component PlayerHealth)
        bool isPlayer = target.CompareTag("Player") ||
                        target.transform.root.CompareTag("Player") ||
                        target.GetComponentInParent<PlayerHealth>() != null;

        // 2. เช็คว่าเป็นพื้นหรือไม่ (เช็คจาก Tag "Ground" หรือ "Floor")
        bool isGround = target.CompareTag("Ground") ||
                        target.CompareTag("Floor");

        return isPlayer || isGround;
    }

  private void HandleImpact()
    {
        hasImpacted = true;

        // 🌟 1. ตรวจสอบว่าตอนชน โดนตัวผู้เล่นหรือไม่ ถ้าโดนให้สั่งทำดาเมจตรงนี้เลย!
        // (เราสามารถเช็คผ่าน Collider ที่ชน หรือใช้ OverlapSphere เล็กๆ รอบตัวหินตอนชนก็ได้)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.8f);
        foreach (var hit in hitColliders)
        {
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                // ดึงค่าดาเมจจาก DamageDealer หรือกำหนดค่าตรงนี้ (เช่น 25 หรือ 50 ดาเมจ)
                DamageDealer dealer = GetComponent<DamageDealer>();
                int damage = dealer != null ? 25 : 25; // ปรับแต่งค่าดาเมจตามต้องการ

                playerHealth.TakeDamage(damage);
                Debug.Log($"[StalactiteTrap] หินร่วงชนผู้เล่น ทำดาเมจ: {damage}");
                break;
            }
        }

        // 2. เล่นเสียงกระแทก
        PlaySound(impactSound);

        // 3. ปิดการทำงานของ Rigidbody และ Renderer เพื่อซ่อนตัวทันทีแต่ยังให้เสียงเล่นจบ
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 4. ทำลาย GameObject ทิ้งทันทีหลังเสียงเล่นจบ
        float soundDuration = impactSound != null ? impactSound.length : 0.1f;
        Destroy(gameObject, soundDuration);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void ResetStalactiteState(bool isAlreadyFallen)
    {
        StopAllCoroutines();
        hasFallen = isAlreadyFallen;
        hasImpacted = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (isAlreadyFallen)
        {
            gameObject.SetActive(false);
        }
        else
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            gameObject.SetActive(true);
        }
    }
}