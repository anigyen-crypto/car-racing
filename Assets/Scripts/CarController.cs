using UnityEngine;

/// <summary>
/// Arcade car physics based on 4 WheelColliders.
/// Input is written into the public fields `throttle`, `steerInput` and `handbrake`
/// by PlayerInput (human) or AICarController (AI), so both can share this controller.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel colliders (FL, FR, RL, RR)")]
    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelRL;
    public WheelCollider wheelRR;

    [Header("Wheel visuals")]
    public Transform meshFL;
    public Transform meshFR;
    public Transform meshRL;
    public Transform meshRR;

    [Header("Driving")]
    public float motorTorque = 1600f;
    public float brakeTorque = 5000f;
    public float maxSpeedKmh = 170f;
    public float downForce = 400f;

    [Header("Steering")]
    public float maxSteerAngle = 32f;
    public float minSteerAngleAtHighSpeed = 14f;
    public float speedForReducedSteerKmh = 130f;
    public float steerResponse = 150f; // degrees per second the wheels turn

    [Header("Setup")]
    public Vector3 centerOfMass = new Vector3(0f, 0.25f, 0.05f);

    // Controlled by PlayerInput / AICarController
    [HideInInspector] public float throttle;    // -1 (reverse/brake) .. 1 (accelerate)
    [HideInInspector] public float steerInput;  // -1 (left) .. 1 (right)
    [HideInInspector] public bool handbrake;

    Rigidbody rb;
    float currentSteer;

    public float SpeedKmh
    {
        get { return rb != null ? rb.velocity.magnitude * 3.6f : 0f; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
    }

    void FixedUpdate()
    {
        UpdateDrive();
        UpdateSteering();
        ApplyDownForce();
        UpdateVisuals();
    }

    float ForwardSpeed()
    {
        return transform.InverseTransformDirection(rb.velocity).z;
    }

    void UpdateDrive()
    {
        float fSpeed = ForwardSpeed();
        float motor = 0f;
        float brake = 0f;

        if (throttle > 0.01f)
        {
            if (fSpeed < -0.5f) brake = brakeTorque;      // rolling backwards, wants to go forward -> brake
            else motor = throttle * motorTorque;
        }
        else if (throttle < -0.01f)
        {
            if (fSpeed > 0.5f) brake = brakeTorque;       // rolling forwards, wants to reverse -> brake
            else motor = throttle * motorTorque * 0.5f;   // reverse gear
        }

        if (SpeedKmh > maxSpeedKmh) motor = 0f;

        float rearBrake = handbrake ? brakeTorque : brake;

        // Rear wheel drive
        wheelRL.motorTorque = motor;
        wheelRR.motorTorque = motor;
        wheelFL.motorTorque = 0f;
        wheelFR.motorTorque = 0f;

        wheelRL.brakeTorque = rearBrake;
        wheelRR.brakeTorque = rearBrake;
        wheelFL.brakeTorque = brake;
        wheelFR.brakeTorque = brake;
    }

    void UpdateSteering()
    {
        // Tighten steering at high speed so the car isn't twitchy
        float ratio = Mathf.Clamp01(SpeedKmh / Mathf.Max(1f, speedForReducedSteerKmh));
        float steerAngle = Mathf.Lerp(maxSteerAngle, minSteerAngleAtHighSpeed, ratio);

        float target = steerInput * steerAngle;
        currentSteer = Mathf.MoveTowards(currentSteer, target, steerResponse * Time.fixedDeltaTime);

        wheelFL.steerAngle = currentSteer;
        wheelFR.steerAngle = currentSteer;
    }

    void ApplyDownForce()
    {
        rb.AddForce(-transform.up * downForce * rb.velocity.sqrMagnitude * 0.01f);
    }

    void UpdateVisuals()
    {
        ApplyVisual(wheelFL, meshFL);
        ApplyVisual(wheelFR, meshFR);
        ApplyVisual(wheelRL, meshRL);
        ApplyVisual(wheelRR, meshRR);
    }

    static void ApplyVisual(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return;
        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}
