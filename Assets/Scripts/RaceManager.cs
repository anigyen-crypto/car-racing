using System.Collections;
using UnityEngine;

/// <summary>
/// Runs the race: 3-2-1-GO countdown, lap counting through ordered checkpoints,
/// lap/total timers, best lap tracking, finish detection and respawn (R key).
/// </summary>
public class RaceManager : MonoBehaviour
{
    public enum RaceState { Countdown, Racing, Finished }

    public static RaceManager Instance { get; private set; }
    public static bool ControlsEnabled
    {
        get { return Instance != null && Instance.state == RaceState.Racing; }
    }

    [Header("Race setup")]
    public int totalLaps = 3;
    public int checkpointCount = 5; // total checkpoints on the track (index 0 = start/finish)
    public float countdownSeconds = 3f;

    [Header("References")]
    public Transform playerCar;
    public Transform[] checkpoints; // in driving order, index 0 = start/finish line
    public HUD hud;

    public RaceState State { get { return state; } }
    public int TotalLaps { get { return totalLaps; } }
    public int LapsCompleted { get { return lapsCompleted; } }
    public float CurrentLapTime
    {
        get { return state == RaceState.Racing ? Time.time - lapStartTime : 0f; }
    }
    public float TotalRaceTime
    {
        get
        {
            if (raceFinishTime > 0f) return raceFinishTime - raceStartTime;
            if (state == RaceState.Racing) return Time.time - raceStartTime;
            return 0f;
        }
    }
    public float BestLapTime { get { return bestLapTime; } }
    public float CountdownRemaining { get { return Mathf.Max(0f, countdownEnd - Time.time); } }
    public float GoFlashUntil { get; private set; }

    RaceState state = RaceState.Countdown;
    int nextCheckpoint;
    int lapsCompleted;
    float raceStartTime;
    float lapStartTime;
    float bestLapTime = -1f;
    float raceFinishTime = -1f;
    float countdownEnd;

    void Awake()
    {
        if (Instance == null) Instance = this;
        nextCheckpoint = checkpointCount > 1 ? 1 : 0;
    }

    IEnumerator Start()
    {
        countdownEnd = Time.time + countdownSeconds;
        yield return new WaitForSeconds(countdownSeconds);

        state = RaceState.Racing;
        raceStartTime = Time.time;
        lapStartTime = Time.time;
        GoFlashUntil = Time.time + 1f;
    }

    public void OnCheckpointPassed(int index)
    {
        if (state != RaceState.Racing) return;
        if (index != nextCheckpoint) return;

        nextCheckpoint = (nextCheckpoint + 1) % checkpointCount;

        // Index 0 is the start/finish line: a full lap just completed
        if (index == 0)
        {
            float lapTime = Time.time - lapStartTime;
            if (bestLapTime < 0f || lapTime < bestLapTime) bestLapTime = lapTime;
            lapStartTime = Time.time;
            lapsCompleted++;

            if (lapsCompleted >= totalLaps)
            {
                state = RaceState.Finished;
                raceFinishTime = Time.time;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) RespawnPlayer();
    }

    /// <summary>
    /// Puts the (possibly flipped) player car back on track at the last checkpoint passed.
    /// </summary>
    void RespawnPlayer()
    {
        if (playerCar == null || checkpoints == null || checkpoints.Length == 0) return;

        int last = (nextCheckpoint - 1 + checkpoints.Length) % checkpoints.Length;
        Transform cp = checkpoints[last];

        playerCar.position = cp.position + Vector3.up * 0.8f;
        playerCar.rotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(cp.forward, Vector3.up).normalized, Vector3.up);

        Rigidbody rb = playerCar.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
