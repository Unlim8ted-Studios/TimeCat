using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnorePlayerCollider : MonoBehaviour
{
    [Header("Tag Settings")]
    public string tagToIgnore = "tagToIgnore"; // Tag of the objects to ignore

    private void Start()
    {
        GameObject[] objectsToIgnore = GameObject.FindGameObjectsWithTag(tagToIgnore);
        Collider2D myCollider = GetComponent<Collider2D>();

foreach (GameObject obj in objectsToIgnore)
{
    Collider2D[] otherColliders = obj.GetComponents<Collider2D>();
    Collider2D[] myColliders = GetComponents<Collider2D>();

    foreach (Collider2D other in otherColliders)
    {
        foreach (Collider2D mine in myColliders)
        {
            Physics2D.IgnoreCollision(other, mine);
        }
    }
}

    }
}
