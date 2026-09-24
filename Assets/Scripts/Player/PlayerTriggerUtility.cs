using UnityEngine;

public static class PlayerTriggerUtility
{
    public static Rigidbody GetPlayerRigidbody(Collider collider)
    {
        if (collider == null) return null;

        Rigidbody rb = collider.attachedRigidbody;
        if (rb == null) rb = collider.GetComponentInParent<Rigidbody>();

        return rb;
    }

    public static bool IsPlayer(Collider collider)
    {
        return GetPlayerRigidbody(collider) != null;
    }
}
