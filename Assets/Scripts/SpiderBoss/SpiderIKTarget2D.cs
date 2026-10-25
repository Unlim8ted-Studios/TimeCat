using UnityEngine;

// Keyframe Weight and this transform's localPosition in Unity's Animation window.
public class SpiderIKTarget2D : MonoBehaviour {
    [Range(0,1)] public float weight;
    public Transform pole;
}
