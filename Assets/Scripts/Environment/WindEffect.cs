using UnityEngine;

public class WindEffect : MonoBehaviour
{
    [Header("Wind Settings")]
    public float minRotationAngle = 86.3f; // Minimum rotation angle
    public float maxRotationAngle = 91f;  // Maximum rotation angle
    public float windSpeed = 1f;          // Speed of the wind effect
    public float randomOffset = 0.5f;    // Adds random delay between child objects

    private Transform[] childSprites;

    void Start()
    {
        // Get all child transforms
        childSprites = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            childSprites[i] = transform.GetChild(i);
        }
    }

    void Update()
    {
        ApplyWindEffect();
    }

    void ApplyWindEffect()
    {
        // Loop through each child and apply rotation
        for (int i = 0; i < childSprites.Length; i++)
        {
            Transform child = childSprites[i];

            // Calculate a smooth sine wave for rotation
            float windOffset = i * randomOffset; // Offset for each child
            float t = (Mathf.Sin(Time.time * windSpeed + windOffset) + 1f) / 2f; // Normalize sine wave to 0-1
            float angle = Mathf.Lerp(minRotationAngle, maxRotationAngle, t); // Map 0-1 to min/max rotation

            // Apply the rotation
            child.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
