using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PixelStatic : MonoBehaviour
{
    [Header("Static Settings")]
    public int width = 64;          // Width of noise texture
    public int height = 64;         // Height of noise texture
    public float refreshRate = 0.05f; // How often static updates (seconds)
    public bool useLight2D = false;
    [Range(0f, 1f)] public float whiteProbability = 0.5f; // Chance of white pixel vs black

    private Texture2D noiseTexture;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Create a pixel-perfect noise texture
        noiseTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        noiseTexture.filterMode = FilterMode.Point; // Keep it pixelated
        noiseTexture.wrapMode = TextureWrapMode.Clamp;

        Sprite sprite = Sprite.Create(
            noiseTexture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            32f // pixels per unit (adjust as needed)
        );
        sr.sprite = sprite;
        if (useLight2D)
        {
            UnityEngine.Rendering.Universal.Light2D light2D = GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (light2D != null)
            {
                light2D.lightCookieSprite = sprite; // correct way to assign sprite to Light2D
            }
        }


        StartCoroutine(UpdateStatic());
    }

    private System.Collections.IEnumerator UpdateStatic()
    {
        while (true)
        {
            GenerateNoise();
            yield return new WaitForSeconds(refreshRate);
        }
    }

    void GenerateNoise()
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            bool white = Random.value < whiteProbability;
            pixels[i] = white ? Color.white : Color.black;
        }

        noiseTexture.SetPixels(pixels);
        noiseTexture.Apply();
    }
}
