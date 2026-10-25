using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class deleteChildren : MonoBehaviour
{
    // Call this function to delete all children of this GameObject
    public void DeleteAllChildren()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
