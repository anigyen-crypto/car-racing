using UnityEngine;

/// <summary>
/// Invisible trigger volume on the track. Reports its index to the RaceManager
/// when the player car drives through it.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    public int index;

    void OnTriggerEnter(Collider other)
    {
        if (RaceManager.Instance == null) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && RaceManager.Instance.playerCar != null &&
            rb.transform == RaceManager.Instance.playerCar)
        {
            RaceManager.Instance.OnCheckpointPassed(index);
        }
    }
}
