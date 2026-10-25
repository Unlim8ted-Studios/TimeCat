using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class CropSpriteEditor : MonoBehaviour
{
    [Range(0f, 1f)] public float xMin = 0f;
    [Range(0f, 1f)] public float yMin = 0f;
    [Range(0f, 1f)] public float xMax = 1f;
    [Range(0f, 1f)] public float yMax = 1f;

    private SpriteRenderer sr;
    private MaterialPropertyBlock block;

    void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();

        // Make sure a proper material is assigned
        if (sr.sharedMaterial == null || sr.sharedMaterial.shader.name != "Universal Render Pipeline/2D/Sprite-Lit-Crop")
        {
            sr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Crop"));
        }

        UpdateCrop();
    }

    void OnValidate()
    {
        UpdateCrop();
    }

    void UpdateCrop()
    {
        if (sr == null) return;
        if (block == null) block = new MaterialPropertyBlock();

        Vector4 crop = new Vector4(xMin, yMin, xMax, yMax);

        // Update per-renderer property block (runtime safe)
        sr.GetPropertyBlock(block);
        block.SetVector("_CropRect", crop);
        sr.SetPropertyBlock(block);

        // Also update the shared material (so you see changes in Inspector/Material asset)
        if (sr.sharedMaterial != null && sr.sharedMaterial.HasProperty("_CropRect"))
        {
            sr.sharedMaterial.SetVector("_CropRect", crop);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (sr == null || sr.sprite == null) return;

        Gizmos.color = Color.green;

        Bounds b = sr.sprite.bounds;
        Vector3 pos = transform.position;

        Vector3 bl = pos + new Vector3(Mathf.Lerp(b.min.x, b.max.x, xMin), Mathf.Lerp(b.min.y, b.max.y, yMin), 0);
        Vector3 tr = pos + new Vector3(Mathf.Lerp(b.min.x, b.max.x, xMax), Mathf.Lerp(b.min.y, b.max.y, yMax), 0);

        Vector3 tl = new Vector3(bl.x, tr.y, 0);
        Vector3 br = new Vector3(tr.x, bl.y, 0);

        Gizmos.DrawLine(bl, tl);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);

        Handles.color = Color.green;

        EditorGUI.BeginChangeCheck();
        Vector3 newBL = Handles.FreeMoveHandle(bl, Quaternion.identity, 0.05f, Vector3.zero, Handles.SphereHandleCap);
        Vector3 newTR = Handles.FreeMoveHandle(tr, Quaternion.identity, 0.05f, Vector3.zero, Handles.SphereHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Crop Changed");

            // Convert world back to normalized UV
            xMin = Mathf.InverseLerp(b.min.x, b.max.x, newBL.x - pos.x);
            yMin = Mathf.InverseLerp(b.min.y, b.max.y, newBL.y - pos.y);
            xMax = Mathf.InverseLerp(b.min.x, b.max.x, newTR.x - pos.x);
            yMax = Mathf.InverseLerp(b.min.y, b.max.y, newTR.y - pos.y);

            UpdateCrop();
        }
    }
#endif
}
