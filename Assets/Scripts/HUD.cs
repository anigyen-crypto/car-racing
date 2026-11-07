using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On-screen HUD: speed, current lap, lap timer, best lap and race messages
/// (countdown / GO! / FINISH).
/// </summary>
public class HUD : MonoBehaviour
{
    public Text speedText;
    public Text lapText;
    public Text timeText;
    public Text bestText;
    public Text messageText;

    public CarController car;
    public RaceManager race;

    void Update()
    {
        if (car != null && speedText != null)
            speedText.text = Mathf.RoundToInt(car.SpeedKmh) + " km/h";

        if (race == null) return;

        if (lapText != null)
        {
            int lap = race.State == RaceManager.RaceState.Finished
                ? race.TotalLaps
                : Mathf.Clamp(race.LapsCompleted + 1, 1, race.TotalLaps);
            lapText.text = "LAP " + lap + " / " + race.TotalLaps;
        }

        if (timeText != null)
            timeText.text = "TIME " + FormatTime(race.CurrentLapTime);

        if (bestText != null)
            bestText.text = "BEST " + (race.BestLapTime > 0f ? FormatTime(race.BestLapTime) : "--:--");

        if (messageText != null)
        {
            if (race.State == RaceManager.RaceState.Countdown)
            {
                int n = Mathf.CeilToInt(race.CountdownRemaining);
                messageText.text = n > 0 ? n.ToString() : "GO!";
            }
            else if (Time.time < race.GoFlashUntil)
            {
                messageText.text = "GO!";
            }
            else if (race.State == RaceManager.RaceState.Finished)
            {
                messageText.text = "FINISH!\nTotal: " + FormatTime(race.TotalRaceTime);
            }
            else
            {
                messageText.text = "";
            }
        }
    }

    public static string FormatTime(float t)
    {
        if (t <= 0f) return "--:--";
        int minutes = (int)(t / 60f);
        float seconds = t - minutes * 60f;
        return string.Format("{0}:{1:00.00}", minutes, seconds);
    }
}
