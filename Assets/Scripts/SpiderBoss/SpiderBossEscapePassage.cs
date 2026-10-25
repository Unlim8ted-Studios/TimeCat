using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class SpiderBossEscapePassage : MonoBehaviour {
    public BoxCollider2D wall;
    public Tilemap wallArt;
    public float openingHeight=4, corridorLength=8;
    public string nextSceneName="Lab_7";
    public bool IsOpen { get; private set; }
    public Vector3 BreachPoint { get; private set; }
    public Vector3 ApproachPoint { get; private set; }
    public Vector3 ExitPoint { get; private set; }
    float floorY, bridgeStart;
    Bounds wallBounds;
    static readonly System.Collections.Generic.Dictionary<string,Sprite> passageSprites=new System.Collections.Generic.Dictionary<string,Sprite>();
    static Material passageMaterial;
    GameObject cracks;
    public GameObject hallwayRoot;
    public GateController exitDoor;
    SpiderBossController boss;

    public void Initialize(SpiderBossController owner) {
        boss=owner;
        float right=float.NegativeInfinity;
        foreach(var island in owner.islandAttacks) if(island.islandCollider && island.timeline!=IslandTimeline.Past && island.islandCollider.bounds.max.x>right) {
            right=island.islandCollider.bounds.max.x; floorY=island.islandCollider.bounds.max.y;
        }
        if(float.IsNegativeInfinity(right)) { right=transform.position.x; floorY=transform.position.y-6; }
        bridgeStart=right-.5f;
        if(!wall) {
            float closest=float.PositiveInfinity;
            foreach(var candidate in FindObjectsOfType<BoxCollider2D>()) {
                if(candidate.isTrigger || candidate.transform.IsChildOf(transform)) continue;
                var b=candidate.bounds;
                if(b.min.x<right-.5f || b.min.y>floorY || b.max.y<floorY+openingHeight || b.size.y<openingHeight) continue;
                float distance=b.min.x-right;
                if(distance<closest) { closest=distance; wall=candidate; }
            }
        }
        if(wall) { wallBounds=wall.bounds; if(!wallArt) wallArt=wall.GetComponent<Tilemap>(); }
        else wallBounds=new Bounds(new Vector3(right+1,floorY+2,0),new Vector3(2,10,1));
        BreachPoint=new Vector3(wallBounds.center.x,floorY+openingHeight*.5f,0);
        ApproachPoint=new Vector3(wallBounds.min.x-2,floorY+openingHeight*.5f,0);
        ExitPoint=new Vector3(wallBounds.max.x+corridorLength-1,floorY+1,0);
    }
    public void DamageWall() {
        if(IsOpen) return;
        if(cracks) return;
        cracks=new GameObject("Escape wall fractures");
        var fractures=SpiderCombatFX.Crack(BreachPoint+Vector3.up,2.5f);fractures.transform.SetParent(cracks.transform,true);
    }
    public void Open() {
        if(IsOpen) return;
        IsOpen=true;
        if(cracks) Destroy(cracks);
        float bottom=floorY,top=floorY+openingHeight;
        if(wall) {
            // Preserve solid wall above and below the opening, accounting for the scaled tilemap transform.
            Vector2 localBottom=wall.transform.InverseTransformPoint(new Vector3(wallBounds.center.x,bottom,0));
            Vector2 localTop=wall.transform.InverseTransformPoint(new Vector3(wallBounds.center.x,top,0));
            Rect original=new Rect(wall.offset-wall.size*.5f,wall.size);
            foreach(var section in SplitWall(original,localBottom.y,localTop.y)) AddWallSection(section.x,section.y,section.width,section.height);
            wall.enabled=false;
        }
        if(wallArt) {
            var from=wallArt.WorldToCell(new Vector3(wallBounds.min.x-.4f,bottom,0));
            var to=wallArt.WorldToCell(new Vector3(wallBounds.max.x+.4f,top,0));
            for(int x=from.x;x<=to.x;x++) for(int y=from.y;y<=to.y;y++) wallArt.SetTile(new Vector3Int(x,y,0),null);
            var collider=wallArt.GetComponent<TilemapCollider2D>(); if(collider) collider.ProcessTilemapChanges();
        }
        if(!hallwayRoot){Debug.LogError("Assign the editable Ending hallway object on SpiderBossEscapePassage.",this);return;}
        hallwayRoot.SetActive(true);
        ScatterHallwayRocks();
        SpiderCombatFX.Impact(BreachPoint,new Color(.65f,.7f,.6f),3);
        foreach(var shake in FindObjectsOfType<CameraShake2D>()) shake.Shake(.12f,.35f);
        float end=ExitPoint.x+3;
        foreach(var follow in FindObjectsOfType<CameraFollow>()) if(follow.boundsPoints!=null && follow.boundsPoints.Length>=3) {
            Vector3 min=follow.boundsPoints[0],max=min;
            foreach(var point in follow.boundsPoints) { min=Vector3.Min(min,point); max=Vector3.Max(max,point); }
            max.x=Mathf.Max(max.x,end+3); min.y=Mathf.Min(min.y,floorY-2);
            follow.boundsPoints=new[] { new Vector3(min.x,min.y,0),new Vector3(max.x,min.y,0),new Vector3(max.x,max.y,0),new Vector3(min.x,max.y,0) };
        }
        Physics2D.SyncTransforms();
    }
    void ScatterHallwayRocks(){
        foreach(var sprite in hallwayRoot.GetComponentsInChildren<SpriteRenderer>(true)){
            if(sprite.gameObject.name.IndexOf("rock",System.StringComparison.OrdinalIgnoreCase)<0)continue;
            var debris=sprite.GetComponent<HallwayRockDebris2D>();
            if(!debris)debris=sprite.gameObject.AddComponent<HallwayRockDebris2D>();
            debris.Launch(BreachPoint);
        }
    }
    [ContextMenu("Create editable ending hallway")]
    public void CreateEditableHallway(){
        if(hallwayRoot)return;
        if(!boss){boss=GetComponent<SpiderBossController>();if(!boss)return;Initialize(boss);}
        var root=new GameObject("Ending hallway (enable to edit)");
        SceneManager.MoveGameObjectToScene(root,gameObject.scene);hallwayRoot=root;
        var playerBody=boss.player?boss.player.GetComponent<Collider2D>():null;
        float catHeight=playerBody?playerBody.bounds.size.y:1.6f;
        float scale=ExitDoorScale(catHeight,2.47f,openingHeight);
        float end=ExitPoint.x+Mathf.Max(.7f,2.49f*scale), length=end-bridgeStart;
        int tiles=Mathf.CeilToInt(length/4);
        int playerOrder=0,playerLayer=0;
        var cat=boss.player?boss.player.GetComponentInChildren<SpriteRenderer>():null;
        if(cat){playerOrder=cat.sortingOrder;playerLayer=cat.sortingLayerID;}
        for(int i=0;i<tiles;i++) {
            float pieceLength=Mathf.Min(4,end-(bridgeStart+i*4));float x=bridgeStart+i*4+pieceLength*.5f;
            var panel=PassageSprite(root,"Recessed corridor wall","wall",new Vector3(x,floorY+2,1),playerLayer,playerOrder-6);
            var panelSprite=panel.GetComponent<SpriteRenderer>();panelSprite.drawMode=SpriteDrawMode.Tiled;panelSprite.size=new Vector2(pieceLength,4);
            var floor=PassageSprite(root,"Walkable service deck","floor",new Vector3(x,floorY-.375f,0),playerLayer,playerOrder-1);
            var floorSprite=floor.GetComponent<SpriteRenderer>();floorSprite.drawMode=SpriteDrawMode.Tiled;floorSprite.size=new Vector2(pieceLength,.75f);
            floor.layer=wall?wall.gameObject.layer:6;var shape=floor.AddComponent<BoxCollider2D>();shape.size=new Vector2(pieceLength,.75f);
            if(i==0)PassageSprite(root,"Reinforced passage jamb","brace",new Vector3(x-1.7f,floorY+2,0),playerLayer,playerOrder-3);
        }
        var doorPrefab=Resources.Load<GameObject>("BossExitDoor");
        if(doorPrefab) {
            GameObject door;
#if UNITY_EDITOR
            if(!Application.isPlaying){door=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(doorPrefab,root.transform);door.transform.SetPositionAndRotation(ExitPoint,Quaternion.identity);}
            else
#endif
            door=Instantiate(doorPrefab,ExitPoint,Quaternion.identity,root.transform);
            door.name="Chamber exit door";
            door.transform.localScale=Vector3.one*scale;exitDoor=door.GetComponent<GateController>();
            var art=exitDoor.background;door.transform.position+=Vector3.up*(floorY-art.bounds.min.y);
        }
        Physics2D.SyncTransforms();
        float crossing=exitDoor?exitDoor.blocker.bounds.extents.x+(playerBody?playerBody.bounds.extents.x*2:.4f)+.295f:.6f;
        var trigger=new GameObject("Leave chamber through exit door");trigger.transform.SetParent(root.transform,true);trigger.transform.position=ExitPoint+Vector3.right*crossing;
        var box=trigger.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(.35f,openingHeight);
        var exit=trigger.AddComponent<SpiderBossPassageExit>();exit.sceneName=nextSceneName;exit.owner=boss;exit.door=exitDoor;
        hallwayRoot.SetActive(false);
    }
    public static float ExitDoorScale(float catHeight,float localHeight,float opening) {
        return Mathf.Clamp(catHeight*1.45f,.35f,Mathf.Max(.35f,opening*.9f))/Mathf.Max(.01f,localHeight);
    }
    void Update() {
        if(!IsOpen || !exitDoor || !boss || boss.CurrentPhase!=SpiderBossController.Phase.Complete || !boss.player)return;
        if(Mathf.Abs(boss.player.position.x-exitDoor.transform.position.x)<4)exitDoor.OpenGate();
    }
    static GameObject PassageSprite(GameObject parent,string name,string asset,Vector3 position,int layer,int order) {
        var texture=Resources.Load<Texture2D>("BossPassage/"+asset);
        var go=new GameObject(name);go.transform.SetParent(parent.transform,true);go.transform.position=position;
        var sr=go.AddComponent<SpriteRenderer>();
        Sprite sprite;
        if(texture && (!passageSprites.TryGetValue(asset,out sprite) || !sprite)) {
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,128,0,SpriteMeshType.FullRect);passageSprites[asset]=sprite;
        }
        if(passageSprites.TryGetValue(asset,out sprite))sr.sprite=sprite;
        if(!passageMaterial){var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");if(!shader)shader=Shader.Find("Sprites/Default");passageMaterial=new Material(shader);}
        sr.sharedMaterial=passageMaterial;
        sr.sortingLayerID=layer;sr.sortingOrder=order;return go;
    }
    public static Rect[] SplitWall(Rect original,float openingBottom,float openingTop) {
        float bottom=Mathf.Clamp(openingBottom,original.yMin,original.yMax);
        float top=Mathf.Clamp(openingTop,bottom,original.yMax);
        return new[] { new Rect(original.xMin,original.yMin,original.width,bottom-original.yMin),new Rect(original.xMin,top,original.width,original.yMax-top) };
    }
    void AddWallSection(float x,float y,float width,float height) {
        if(height<.001f) return;
        var collider=wall.gameObject.AddComponent<BoxCollider2D>(); collider.offset=new Vector2(x+width*.5f,y+height*.5f); collider.size=new Vector2(width,height); collider.sharedMaterial=wall.sharedMaterial;
    }
    void OnDrawGizmosSelected() { if(wall) { Gizmos.color=Color.yellow; Gizmos.DrawWireCube(wall.bounds.center,wall.bounds.size); } }
}
