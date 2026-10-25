using UnityEngine;

public class TimelineSync : MonoBehaviour
{
    public bool IsPlayerInPast { get; private set; }

    public void OnTimelineSwap(bool toPast)
    {
        if (IsPlayerInPast == toPast) return;
        IsPlayerInPast = toPast;
        Debug.Log("Timeline swapped. Player is now in " + (toPast ? "Past" : "Present"));
    }
}
