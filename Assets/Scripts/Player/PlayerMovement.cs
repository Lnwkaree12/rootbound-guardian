using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private AudioClip moveSfx;
    private float currentSpeed;
    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector3 input;
    private Vector3 lastMoveDirection;
    private bool wasMoving;
    public Animator animator;
    public SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        if (moveSfx != null) audioSource.clip = moveSfx;
    }

    void Update()
    {
        HandleInput();
        Animate();
    }

    public void HandleInput()
    {
        // รับค่าแกนนอน (A/D) และแกนตั้ง (W/S)
        input.x = Input.GetAxisRaw("Horizontal");
        input.z = Input.GetAxisRaw("Vertical"); // เก็บค่า W/S ไว้ในแกน Z ของ 3D
        
        if (input.sqrMagnitude > 1) input.Normalize();

        if (input.x > 0) sr.flipX = false;
        else if (input.x < 0) sr.flipX = true;

        if (input != Vector3.zero) lastMoveDirection = input;

        bool isMoving = input != Vector3.zero;
        if (isMoving && !wasMoving)
        {
            if (moveSfx != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else if (!isMoving && wasMoving)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
        wasMoving = isMoving;
    }

    void FixedUpdate()
    {
        // คำนวณความเร็วรวมเพื่อส่งให้ Animator
        currentSpeed = new Vector2(input.x, input.z).magnitude * moveSpeed;

        // ใช้ Rigidbody ควบคุมการเคลื่อนที่ทั้งแกน X และ Z
        // ส่วนแกน Y ให้ใช้ความเร็วเดิมของ Rigidbody (เผื่อกรณีมีแรงโน้มถ่วงหรือการตกจากที่สูง)
        rb.linearVelocity = new Vector3(input.x * moveSpeed, rb.linearVelocity.y, input.z * moveSpeed);
    }
    
    public void Animate()
    {
        if (animator == null) return;
        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveZ", lastMoveDirection.z);
        animator.SetFloat("Speed", currentSpeed);
    }

    public void StopVelocity()
    {
        input = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }
}