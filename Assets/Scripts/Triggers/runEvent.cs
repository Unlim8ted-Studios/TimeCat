using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class runEvent : MonoBehaviour
{
    [Header("List of events to trigger")]
    public List<UnityEvent> events = new List<UnityEvent>();

    /// <summary>
    /// Runs a specific UnityEvent by its index.
    /// </summary>
    /// <param name="index">The index of the event in the list.</param>
    public void Run(int index)
    {
        if (index < 0 || index >= events.Count)
        {
            Debug.LogWarning($"RunEvent: Invalid index {index}. There are {events.Count} events in the list.");
            return;
        }

        if (events[index] != null)
        {
            events[index].Invoke();
        }
        else
        {
            Debug.LogWarning($"RunEvent: Event at index {index} is null.");
        }
    }
}
