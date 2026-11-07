using UnityEngine;

/// <summary>
/// Smooth chase camera that follows the car from behind.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 7.5f;
    public float height = 3f;
    public float lookAhead = 4f;
    public float followSmoothing = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position - target.forward * distance + Vector3.up * height;
        transform.position = Vector3.Lerp(transform.position, desired, followSmoothing * Time.deltaTime);

        Vector3 lookAt = target.position + target.forward * lookAhead + Vector3.up * 0.8f;
        transform.LookAt(lookAt);
    }

    /// <summary>
    /// Instantly places the camera behind the car (used at spawn).
    /// </summary>
    public void SnapBehind()
    {
        if (target == null) return;
        transform.position = target.position - target.forward * distance + Vector3.up * height;
        transform.LookAt(target.position + target.forward * lookAhead + Vector3.up * 0.8f);
    }
}
