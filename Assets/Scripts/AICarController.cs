using UnityEngine;

/// <summary>
/// Simple waypoint-following AI driver that feeds the same CarController
/// used by the player. Assign a loop of waypoints (in driving order).
/// </summary>
[RequireComponent(typeof(CarController))]
public class AICarController : MonoBehaviour
{
    public Transform[] waypoints;
    public float waypointReachDistance = 8f;
    public float cruiseSpeedKmh = 110f;
    public float maxSteerLookahead = 35f;

    CarController car;
    int index;

    void Awake()
    {
        car = GetComponent<CarController>();
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0 || !RaceManager.ControlsEnabled)
        {
            car.steerInput = 0f;
            car.throttle = 0f;
            return;
        }

        // Advance to the next waypoint when we get close to the current one
        int guard = 0;
        while (guard++ < waypoints.Length &&
               Vector3.Distance(transform.position, waypoints[index].position) < waypointReachDistance)
        {
            index = (index + 1) % waypoints.Length;
        }

        Vector3 target = waypoints[index].position;
        target.y = transform.position.y;
        Vector3 to = target - transform.position;

        float angle = Vector3.SignedAngle(transform.forward, to, Vector3.up);
        car.steerInput = Mathf.Clamp(angle / maxSteerLookahead, -1f, 1f);

        // Ease off / brake for corners and near cruise speed
        float speedPressure = car.SpeedKmh / Mathf.Max(1f, cruiseSpeedKmh);
        float cornerPressure = Mathf.Abs(angle) / 90f;
        float pressure = Mathf.Max(speedPressure, cornerPressure * 0.75f);
        car.throttle = Mathf.Clamp(Mathf.Lerp(1.2f, -0.4f, pressure), -0.6f, 1f);
    }
}
