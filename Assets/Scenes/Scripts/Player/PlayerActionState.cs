using UnityEngine;

public enum PlayerActionState
{
    Normal,     // เดิน, วิ่ง, ยืนปกติ
    Dashing,    // กำลังพุ่งตัว
    Climbing,   // กำลังปีนบันได
    Dead        // ตาย / รอเกิดใหม่
}
