using UnityEngine;

/// <summary>
/// Feeds input into the CarController.
/// - Desktop: WASD / Arrow keys to drive, Space for handbrake.
/// - Mobile (touch): auto-accelerates; hold LEFT half of the screen to steer
///   left, RIGHT half to steer right, hold bottom-middle zone to brake.
/// Input is ignored while the race hasn't started (countdown) or has finished,
/// so the game is fully playable with only this script + CarController.
/// </summary>
[RequireComponent(typeof(CarController))]
public class PlayerInput : MonoBehaviour
{
    [Header("Mobile settings")]
    public bool autoAccelerateOnTouch = true;
    public float mobileSteerSpeed = 4f; // how fast touch steering ramps up
    public float brakeZoneWidthScreenFrac = 0.25f; // bottom-centre fraction of screen

    CarController car;
    float touchSteer; // smoothed touch steering value

    void Awake()
    {
        car = GetComponent<CarController>();
    }

    void Update()
    {
        if (!RaceManager.ControlsEnabled)
        {
            car.steerInput = 0f;
            car.throttle = 0f;
            car.handbrake = false;
            return;
        }

        // Keyboard always works (desktop AND mobile with paired keyboard)
        float keyboardSteer = Input.GetAxis("Horizontal");
        float keyboardThrottle = Input.GetAxis("Vertical");
        bool keybrake = Input.GetAxis("Jump") > 0.5f;

        float touchThrottle = 0f;
        float touchBrake = 0f;
        float targetTouchSteer = 0f;
        bool touching = false;

#if !UNITY_EDITOR || UNITY_EDITOR_WIN
        // Only spend time on touch logic when a touch device is plausible
        if (Input.touchSupported && Input.touchCount > 0)
#endif
        {
            touching = true;
            float screenW = Screen.width;
            float screenH = Screen.height;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                float x = t.position.x / screenW;   // 0 = left edge, 1 = right edge
                float y = t.position.y / screenH;   // 0 = bottom edge

                bool brakeZone =
                    y < 0.22f &&
                    Mathf.Abs(x - 0.5f) < brakeZoneWidthScreenFrac * 0.5f;

                if (brakeZone)
                {
                    touchBrake = 1f;
                }
                else if (x < 0.5f)
                {
                    targetTouchSteer = -1f; // hold left side = steer left
                }
                else
                {
                    targetTouchSteer = 1f;  // hold right side = steer right
                }
            }
        }

        if (touching)
        {
            // Smooth ramp for touch steering so it feels like an analogue stick
            touchSteer = Mathf.MoveTowards(touchSteer, targetTouchSteer, mobileSteerSpeed * Time.deltaTime);

            car.steerInput = Mathf.Abs(keyboardSteer) > 0.01f ? keyboardSteer : touchSteer;

            if (touchBrake > 0f) car.throttle = -1f;                       // brake
            else if (autoAccelerateOnTouch && Mathf.Abs(keyboardThrottle) < 0.01f) car.throttle = 1f; // auto gas
            else car.throttle = keyboardThrottle;

            car.handbrake = keybrake;
        }
        else
        {
            touchSteer = 0f;
            car.steerInput = keyboardSteer;
            car.throttle = keyboardThrottle;
            car.handbrake = keybrake;
        }
    }
}
