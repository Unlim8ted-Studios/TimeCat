using UnityEngine;

// Layered sparks, dust and branching energy strokes; no flat hitbox sprites.
public sealed class SpiderCombatEffect : MonoBehaviour {
    public Color tint;
    public float lifetime;
    float born;
    LineRenderer[] strokes;
    ParticleSystem particles;
    public void Initialize(Color color,float duration) {tint=color;lifetime=duration;born=Time.time;strokes=GetComponentsInChildren<LineRenderer>();particles=GetComponent<ParticleSystem>();}
    public void SetTint(Color color) {tint=color;if(particles){var main=particles.main;main.startColor=color;}}
    void Update() {
        float elapsed=Time.time-born;
        float alpha=lifetime>0?1-Mathf.Clamp01(elapsed/lifetime):.6f+.25f*Mathf.Sin(elapsed*9);
        if(strokes!=null)foreach(var line in strokes)if(line){var c=tint;c.a*=alpha;line.startColor=line.endColor=c;}
        if(lifetime>0 && elapsed>=lifetime)Destroy(gameObject);
    }
}
public static class SpiderCombatFX {
    static Material strokeMaterial,particleMaterial;
    static Material StrokeMaterial {
        get {if(!strokeMaterial){var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");if(!shader)shader=Shader.Find("Sprites/Default");strokeMaterial=new Material(shader);}return strokeMaterial;}
    }
    static Material ParticleMaterial {
        get {
            if(particleMaterial)return particleMaterial;
            var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<32;y++)for(int x=0;x<32;x++) {
                float u=(x+.5f)/32f,v=(y+.5f)/32f;
                float edge=Mathf.Min(Mathf.Min(u,v),Mathf.Min(1-u-v*.55f,1-v-u*.25f));
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(edge*15)*(.55f+.45f*Mathf.Sin(v*Mathf.PI))));
            }
            texture.Apply();particleMaterial=new Material(Shader.Find("Sprites/Default"));particleMaterial.mainTexture=texture;return particleMaterial;
        }
    }
    static void Sort(Renderer renderer) {
        var layers=SortingLayer.layers;int id=0,best=int.MinValue;
        foreach(var layer in layers)if(layer.value>best){best=layer.value;id=layer.id;}
        renderer.sortingLayerID=id;renderer.sortingOrder=32755;
    }
    static GameObject Root(Vector3 point,string name) {var root=new GameObject(name);root.transform.position=point;return root;}
    static void Stroke(GameObject root,Vector3[] points,Color color,float width) {
        var child=new GameObject("Energy stroke");child.transform.SetParent(root.transform,false);
        var line=child.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);
        line.sharedMaterial=StrokeMaterial;line.startColor=line.endColor=color;line.widthMultiplier=width;
        line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.15f,1),new Keyframe(.8f,.6f),new Keyframe(1,0));
        line.numCapVertices=3;Sort(line);
    }
    static void Particles(GameObject root,Color color,bool loop,float spread,int count) {
        var ps=root.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=loop;main.duration=.7f;main.startLifetime=new ParticleSystem.MinMaxCurve(.2f,.65f);
        main.startSpeed=new ParticleSystem.MinMaxCurve(loop?.1f:1,loop?.4f:4);main.startSize=new ParticleSystem.MinMaxCurve(.035f,.13f);
        main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);main.startColor=color;main.gravityModifier=loop?-.1f:.4f;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=96;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(spread,.08f,.01f);
        var emission=ps.emission;emission.rateOverTime=loop?12:0;if(!loop)emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
        var colors=ps.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.1f),new GradientAlphaKey(0,1)});colors.color=gradient;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=ParticleMaterial;Sort(renderer);ps.Play();
    }
    public static GameObject Telegraph(Vector3 point,float width,Color color) {
        var root=Root(point,"Spider ground energy warning");
        for(int i=0;i<3;i++) {
            var points=new Vector3[9];for(int j=0;j<9;j++)points[j]=new Vector3((j/8f-.5f)*width,Mathf.Sin(j*2.7f+i*3)*.08f+(i-1)*.1f,0);
            Stroke(root,points,color,.035f);
        }
        Particles(root,color,true,width,0);root.AddComponent<SpiderCombatEffect>().Initialize(color,0);return root;
    }
    public static GameObject Impact(Vector3 point,Color color,float scale=1) {
        var root=Root(point,"Spider impact sparks");Particles(root,color,false,.2f,24);
        for(int i=0;i<7;i++) {float angle=i*Mathf.PI*2/7;var direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);Stroke(root,new[]{direction*.05f,direction*.2f+Vector3.up*.07f,direction*.6f*scale},color,.04f);}
        root.AddComponent<SpiderCombatEffect>().Initialize(color,.65f);return root;
    }
    public static GameObject Slash(Vector3 point,float width,Color color) {
        var root=Root(point,"Spider claw trails");
        for(int k=0;k<3;k++) {var points=new Vector3[12];for(int i=0;i<12;i++){float t=i/11f;points[i]=new Vector3((t-.5f)*width,Mathf.Sin(t*Mathf.PI)*.4f+k*.09f,0);}Stroke(root,points,color,.055f);}
        root.AddComponent<SpiderCombatEffect>().Initialize(color,.22f);return root;
    }
    public static GameObject Crack(Vector3 point,float width) {
        Color color=new Color(.12f,.16f,.15f);var root=Root(point,"Fractured platform edge");
        for(int i=0;i<4;i++){float x=(i/3f-.5f)*width;Stroke(root,new[]{new Vector3(x-.25f,0,0),new Vector3(x+.12f,-.18f,0),new Vector3(x-.08f,-.38f,0),new Vector3(x+.14f,-.65f,0)},color,.035f);}
        return root;
    }
}
