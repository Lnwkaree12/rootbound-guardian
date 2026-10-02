using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class SlidingTrap : MonoBehaviour
{
    [Header("การตั้งค่าระยะทางและการเคลื่อนที่")]
    [SerializeField] private Vector3 moveDirection = Vector3.right;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float speed = 3f;

    private Vector3 startPosition;
    private Vector3 lastPosition;
    private Rigidbody rb;

    // เก็บรายชื่อ Rigidbody ของตัวละครที่กำลังยืนอยู่บนแท่น
    private HashSet<Rigidbody> passengers = new HashSet<Rigidbody>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void Start()
    {
        startPosition = transform.position;
        lastPosition = startPosition;
    }

    private void FixedUpdate()
    {
        // 1. คำนวณตำแหน่งเป้าหมายใหม่ของแท่น
        float pingPongValue = Mathf.PingPong(Time.time * speed, distance);
        Vector3 targetPosition = startPosition + (moveDirection.normalized * pingPongValue);

        // 2. คำนวณระยะที่แท่นเคลื่อนที่ไปในเฟรมนี้ (Delta)
        Vector3 platformDelta = targetPosition - transform.position;

        // 3. ขยับ Rigidbody ของตัวละครตามระยะ platformDelta ทันที!
        foreach (Rigidbody passengerRb in passengers)
        {
            if (passengerRb != null)
            {
                passengerRb.MovePosition(passengerRb.position + platformDelta);
            }
        }

        // 4. ขยับตัวแท่นเลื่อน
        rb.MovePosition(targetPosition);
        lastPosition = targetPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null && !passengers.Contains(playerRb))
            {
                passengers.Add(playerRb);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null && passengers.Contains(playerRb))
            {
                passengers.Remove(playerRb);
            }
        }
    }
}