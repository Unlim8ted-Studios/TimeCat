using UnityEngine;

public class PrefabReplacer : MonoBehaviour
{
    [Header("Prefab to Instantiate")]
    public GameObject prefab;
    public Transform prefablocation;

    /// <summary>
    /// Deletes all children of this GameObject and instantiates the prefab as a child.
    /// Can be called from Unity Events.
    /// </summary>
    public void ReplaceChildrenWithPrefab()
    {
        // Delete all children
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Spawn new prefab as child
        if (prefab != null)
        {
            GameObject newObj = Instantiate(prefab, prefablocation);
        }
        else
        {
            Debug.LogWarning("PrefabReplacer: No prefab assigned.");
        }
    }
}
