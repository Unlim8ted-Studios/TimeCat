using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
public class GooFluidController : MonoBehaviour
{
    [Header("Size / Layout")]
    public float width = 6f;
    public float height = 2f;
    public int surfaceResolution = 20;

    [Header("Wave Settings")]
    public float springStrength = 0.02f;
    public float damping = 0.04f;
    public float spread = 0.05f;

    [Header("Buoyancy")]
    public float buoyancyForce = 8f;
    public float drag = 2f;

    [Header("Visuals")]
    public Color gooColor = new Color(0.2f, 1f, 0.5f, 0.7f);
    public bool showGizmos = true;

    [Header("Contact damage")]
    public float damagePerSecond=15;
    public float damageInterval=.5f;
    readonly Dictionary<CatPlayerController,float> nextDamage=new Dictionary<CatPlayerController,float>();

    [Header("Events")]
    public UnityEvent<GameObject> onGooEnter;
    public UnityEvent<GameObject> onGooExit;
    public UnityEvent<GameObject> onGooCollide;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private PolygonCollider2D poly;
    private Mesh mesh;
    private List<float> velocities = new List<float>();
    private List<float> heights = new List<float>();
    private float baseY;
    readonly Collider2D[] damageContacts=new Collider2D[32];
    private HashSet<Rigidbody2D> inside = new HashSet<Rigidbody2D>();

    // ===============================================================
    void OnEnable()
    {
        if(damagePerSecond<=0)damagePerSecond=15;
        if(damageInterval<=0)damageInterval=.5f;
        nextDamage.Clear();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        poly = GetComponent<PolygonCollider2D>();
        if (mesh == null) mesh = new Mesh();
        meshFilter.sharedMesh = mesh;

        if (meshRenderer.sharedMaterial == null)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.sharedMaterial = mat;
        }
        meshRenderer.sharedMaterial.color = gooColor;

        poly.usedByEffector=false;poly.isTrigger=true;
        foreach(var effector in GetComponents<BuoyancyEffector2D>())effector.enabled=false;
        BuildSurface();
    }

    void Update()
    {
        SimulateWaves();
        RebuildMeshAndCollider();
    }

    void FixedUpdate()
    {
        ApplyBuoyancy();
        // Query the real liquid polygon as well as trigger events: layer collision matrices
        // and a moving/rebuilt surface must not silently disable damage.
        if(Application.isPlaying && Time.timeScale>0 && poly && poly.enabled){
            var filter=new ContactFilter2D();filter.SetLayerMask(~0);filter.useTriggers=false;
            int count=poly.OverlapCollider(filter,damageContacts);
            for(int i=0;i<count;i++)DamageContact(damageContacts[i]);
        }
    }

    // ===============================================================
    void BuildSurface()
    {
        velocities.Clear();
        heights.Clear();

        for (int i = 0; i <= surfaceResolution; i++)
        {
            velocities.Add(0);
            heights.Add(0);
        }
        baseY = transform.position.y;
    }

    void SimulateWaves()
    {
        // apply spring forces
        for (int i = 0; i <= surfaceResolution; i++)
        {
            float displacement = heights[i];
            float accel = -springStrength * displacement - velocities[i] * damping;
            velocities[i] += accel;
            heights[i] += velocities[i];
        }

        // spread waves
        float[] leftDeltas = new float[heights.Count];
        float[] rightDeltas = new float[heights.Count];
        for (int j = 0; j < 8; j++) // 8 passes for smoother spread
        {
            for (int i = 0; i <= surfaceResolution; i++)
            {
                if (i > 0)
                {
                    leftDeltas[i] = spread * (heights[i] - heights[i - 1]);
                    velocities[i - 1] += leftDeltas[i];
                }
                if (i < surfaceResolution)
                {
                    rightDeltas[i] = spread * (heights[i] - heights[i + 1]);
                    velocities[i + 1] += rightDeltas[i];
                }
            }
            for (int i = 0; i <= surfaceResolution; i++)
            {
                if (i > 0) heights[i - 1] += leftDeltas[i];
                if (i < surfaceResolution) heights[i + 1] += rightDeltas[i];
            }
        }
    }

    // ===============================================================
    void RebuildMeshAndCollider()
    {
        baseY=transform.position.y;
        float halfWidth = width * 0.5f;
        float segmentWidth = width / surfaceResolution;
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> colliderPoints = new List<Vector2>();

        // Build top surface points
        for (int i = 0; i <= surfaceResolution; i++)
        {
            float x = -halfWidth + i * segmentWidth;
            float y = heights[i];
            verts.Add(new Vector3(x, y, 0));
            colliderPoints.Add(new Vector2(x,y));
        }

        // Add bottom rectangle (flat bottom)
        verts.Add(new Vector3(halfWidth,-height,0));
        verts.Add(new Vector3(-halfWidth,-height,0));

        // Triangles
        for (int i = 0; i < surfaceResolution; i++)
        {
            tris.Add(i);
            tris.Add(i + 1);
            tris.Add(verts.Count - 2);

            tris.Add(verts.Count - 2);
            tris.Add(verts.Count - 1);
            tris.Add(i);
        }

        mesh.Clear();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();

        meshRenderer.sharedMaterial.color = gooColor;

        // Collider: top surface + sides + bottom
        List<Vector2> fullPath = new List<Vector2>(colliderPoints);
        fullPath.Add(new Vector2(width*.5f,-height));
        fullPath.Add(new Vector2(-width*.5f,-height));
        poly.pathCount = 1;
        poly.SetPath(0, fullPath);
        poly.isTrigger = true;
    }

    // ===============================================================
    void ApplyBuoyancy()
    {
        foreach (var rb in inside)
        {
            if (rb == null || rb.bodyType!=RigidbodyType2D.Dynamic || rb.GetComponentInParent<CatPlayerController>()) continue;
            Vector2 pos = rb.position;
            Vector2 local=transform.InverseTransformPoint(pos);

            float halfW = width * 0.5f;
            if (local.x < -halfW || local.x > halfW) continue;

            float percent = Mathf.InverseLerp(-halfW,halfW,local.x);
            int idx = Mathf.Clamp(Mathf.RoundToInt(percent * surfaceResolution), 0, surfaceResolution);
            float surfaceY = transform.TransformPoint(new Vector3(local.x,heights[idx],0)).y;
            float depth = surfaceY - pos.y;

            if (depth > 0)
            {
                Vector2 force = Vector2.up * (depth * buoyancyForce);
                rb.AddForce(force - rb.velocity * drag, ForceMode2D.Force);

                // disturb surface (create waves)
                velocities[idx] = Mathf.Clamp(velocities[idx] + depth * 0.02f, -1f, 1f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.attachedRigidbody && !inside.Contains(col.attachedRigidbody))
        {
            inside.Add(col.attachedRigidbody);
            onGooEnter?.Invoke(col.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.attachedRigidbody && inside.Contains(col.attachedRigidbody))
        {
            inside.Remove(col.attachedRigidbody);
            onGooExit?.Invoke(col.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        onGooCollide?.Invoke(col.gameObject);
        DamageContact(col);
    }
    void DamageContact(Collider2D col){
        AcidVolume2D.DamageCat(col,damagePerSecond,damageInterval,transform.position.y);
    }

    // ===============================================================
    public void ResetDamageAndSurface(){nextDamage.Clear();AcidVolume2D.ResetDamage();BuildSurface();}
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = gooColor;
        Vector3 pos = transform.position;
        Gizmos.DrawWireCube(pos, new Vector3(width, height, 0.1f));
        Gizmos.color = new Color(gooColor.r, gooColor.g, gooColor.b, 0.2f);
        Gizmos.DrawCube(pos, new Vector3(width, height, 0.1f));
    }
}
