using UnityEngine;
using System.Collections.Generic;

public class FloatingChildren : MonoBehaviour
{
    [Header("UI Floating Motion Settings")]
    [SerializeField] float amplitude = 20f; // pixels
    [SerializeField] float speed = 1f;
    [SerializeField] float randomOffsetRange = 1f;

    private List<RectTransform> rectChildren = new List<RectTransform>();
    private List<Vector2> baseAnchors = new List<Vector2>();
    private float[] randomOffsets;

void Start()
{
    foreach (Transform t in transform)
    {
        RectTransform rect = t as RectTransform;
        if (rect != null)
        {
            rectChildren.Add(rect);
            baseAnchors.Add(rect.anchoredPosition);
        }
        else
        {
            // World-space sprite children keep their authored pose; only UI floats here.
        }
    }

    randomOffsets = new float[rectChildren.Count];
    for (int i = 0; i < randomOffsets.Length; i++)
    {
        randomOffsets[i] = Random.Range(0f, randomOffsetRange);
    }
}


    void Update()
    {
        for (int i = 0; i < rectChildren.Count; i++)
        {
            float offset = Mathf.Sin(Time.time * speed + randomOffsets[i]) * amplitude;
            rectChildren[i].anchoredPosition = baseAnchors[i] + Vector2.up * offset;
        }
    }
}