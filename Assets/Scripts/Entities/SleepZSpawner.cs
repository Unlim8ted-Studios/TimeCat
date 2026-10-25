using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SleepZSpawner : MonoBehaviour
{
    public Font font;                     // Assign your regular TTF font in Inspector
    public float spawnInterval = 0.5f;    // Time between each "Z" spawn
    public float floatDuration = 1.5f;    // How long each Z lasts
    public Vector3 spawnOffset = new Vector3(0, 1f, 0);  // Offset above the spawner
    public Vector3 floatDirection = new Vector3(0.4f, 0.8f, 0); // Upward-right motion

    public int sortingLayer = 5;          // Sorting layer for rendering order

    public Color ZColor = Color.white;    // Color of the "Z"

    private Coroutine spawnRoutine;
    private List<GameObject> activeZs = new List<GameObject>();

    void Start()
    {
        spawnRoutine = StartCoroutine(SpawnZsLoop());
    }

    IEnumerator SpawnZsLoop()
    {
        while (true)
        {
            GameObject zObj = new GameObject("Zzz");
            zObj.transform.position = transform.position + spawnOffset;

            TextMesh textMesh = zObj.AddComponent<TextMesh>();
            textMesh.text = "Z";
            textMesh.font = font;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.1f;
            textMesh.color = ZColor;
            textMesh.anchor = TextAnchor.MiddleCenter;

            MeshRenderer renderer = zObj.GetComponent<MeshRenderer>();
            renderer.material = font.material;
            renderer.sortingOrder = sortingLayer;

            activeZs.Add(zObj); // track it
            StartCoroutine(AnimateZ(zObj, textMesh));

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    IEnumerator AnimateZ(GameObject zObj, TextMesh textMesh)
    {
        float t = 0f;

        while (t < floatDuration)
        {
            float progress = t / floatDuration;

            zObj.transform.position += floatDirection * Time.deltaTime;

            float scale = Mathf.Lerp(0.5f, 1.2f, progress);
            zObj.transform.localScale = new Vector3(scale, scale, scale);

            float alpha = Mathf.Sin(progress * Mathf.PI);
            Color color = textMesh.color;
            textMesh.color = new Color(color.r, color.g, color.b, alpha);

            t += Time.deltaTime;
            yield return null;
        }

        activeZs.Remove(zObj);
        Destroy(zObj);
    }

    public void StopAndClearZs()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        foreach (GameObject z in activeZs)
        {
            if (z != null)
                Destroy(z);
        }

        activeZs.Clear();
    }
}
