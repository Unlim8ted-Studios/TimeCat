using UnityEngine;

// Details stay in each sprite's own coordinates, so animated joints and hit shapes stay aligned.
[DefaultExecutionOrder(100)]
public sealed class PastLegDetails : MonoBehaviour {
    public SpriteRenderer source;
    Sprite lastSprite;
    static Material material;
    void LateUpdate() {
        if(!source || !source.enabled){foreach(var line in GetComponentsInChildren<LineRenderer>())line.enabled=false;return;}
        if(lastSprite!=source.sprite)Rebuild();
        foreach(var line in GetComponentsInChildren<LineRenderer>()) {
            line.enabled=true;line.sortingLayerID=source.sortingLayerID;line.sortingOrder=source.sortingOrder+1;
        }
    }
    void Rebuild() {
        foreach(Transform child in transform)Destroy(child.gameObject);
        lastSprite=source.sprite;if(!lastSprite)return;
        if(!material){var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");if(!shader)shader=Shader.Find("Sprites/Default");material=new Material(shader);}
        var vertices=lastSprite.vertices;var triangles=lastSprite.triangles;
        Vector2 axis=Vector2.right;foreach(var v in vertices)if(v.sqrMagnitude>axis.sqrMagnitude)axis=v;
        axis.Normalize();Vector2 side=new Vector2(-axis.y,axis.x);
        float length=Mathf.Min(lastSprite.bounds.size.x,lastSprite.bounds.size.y)*.3f;
        int count=0;
        for(int i=0;i<49 && count<9;i++) {
            var bounds=lastSprite.bounds;
            Vector2 center=new Vector2(Mathf.Lerp(bounds.min.x,bounds.max.x,(i%7+.5f)/7),Mathf.Lerp(bounds.min.y,bounds.max.y,(i/7+.5f)/7));
            Vector2 a=center-axis*length,b=center+side*length*.25f,c=center+axis*length*.8f;
            if(!Inside(a,vertices,triangles) || !Inside(b,vertices,triangles) || !Inside(c,vertices,triangles))continue;
            var child=new GameObject("Exposed cracked strut");child.transform.SetParent(transform,false);
            var line=child.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=3;line.SetPositions(new[]{(Vector3)a,(Vector3)b,(Vector3)c});
            line.sharedMaterial=material;line.widthMultiplier=Mathf.Clamp(length*.28f,.04f,.10f);line.numCapVertices=2;
            line.startColor=line.endColor=count%3==0?new Color(.85f,.64f,.32f):new Color(.10f,.065f,.035f);
            count++;
        }
    }
    static bool Inside(Vector2 p,Vector2[] vertices,ushort[] triangles) {
        for(int i=0;i<triangles.Length;i+=3){Vector2 a=vertices[triangles[i]],b=vertices[triangles[i+1]],c=vertices[triangles[i+2]];
            float x=Cross(b-a,p-a),y=Cross(c-b,p-b),z=Cross(a-c,p-c);if(x>=0 && y>=0 && z>=0 || x<=0 && y<=0 && z<=0)return true;}
        return false;
    }
    static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
}
