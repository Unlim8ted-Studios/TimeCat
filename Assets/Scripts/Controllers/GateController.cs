using UnityEngine;
using UnityEngine.SceneManagement;

// Existing event-facing component/GUID retained so all scene puzzle hookups keep working.
[DisallowMultipleComponent]
public class GateController : MonoBehaviour {
    public Animator gateAnimator;
    [Header("Layered cat door")]
    public SpriteRenderer background, foreground, leftPanel, rightPanel;
    public SpriteRenderer stateDisplay;
    public Sprite openState,closedState,movingState;
    public BoxCollider2D blocker;
    public bool initiallyOpen;
    [Min(.05f)] public float openingDuration=.6f, closingDuration=.5f;
    public LayerMask obstructionLayers=1<<8;
    public AudioSource audioSource;
    public AudioClip openingSound, closingSound;
    public float OpenFraction { get; private set; }
    public bool IsOpen => OpenFraction>=.98f;
    bool requestedOpen, initialized;
    Vector3 leftRest, rightRest, leftScale, rightScale;
    SpriteRenderer playerRenderer, leftFrame;
    Sprite splitLeft,splitRight;
    float nextPlayerSearch;
    readonly Collider2D[] occupants=new Collider2D[32];
    bool Layered => background && foreground && leftPanel && rightPanel;

    void Awake() {
        if(initialized)return;
        if(!gateAnimator) gateAnimator=GetComponent<Animator>();
        if(!blocker) blocker=GetComponent<BoxCollider2D>();
        if(!Layered) return;
        FitArtToCollider();SplitFrame();
        if(!audioSource && (openingSound || closingSound)) {
            audioSource=gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake=false; audioSource.spatialBlend=0;
        }
        // Old clips key the root sprite and collider into a trigger. The layered mechanism
        // owns these properties now, including on prefab instances with Animator overrides.
        if(gateAnimator) gateAnimator.enabled=false;
        var legacy=GetComponent<SpriteRenderer>(); if(legacy) legacy.enabled=false;
        leftRest=leftPanel.transform.localPosition; rightRest=rightPanel.transform.localPosition;
        leftScale=leftPanel.transform.localScale; rightScale=rightPanel.transform.localScale;
        requestedOpen=initiallyOpen || blocker && (blocker.isTrigger || !blocker.enabled);
        OpenFraction=requestedOpen?1:0; initialized=true; UpdateSorting(); ApplyPose();
    }
    public void OpenGate() {
        requestedOpen=true;
        if(!Layered) { SetLegacy(true); return; }
        if(audioSource && openingSound && OpenFraction<.98f) audioSource.PlayOneShot(openingSound);
    }
    public void CloseGate() {
        requestedOpen=false;
        if(!Layered) { SetLegacy(false); return; }
        if(audioSource && closingSound && OpenFraction>.01f) audioSource.PlayOneShot(closingSound);
    }
    void SetLegacy(bool open) {
        if(!gateAnimator || !gateAnimator.runtimeAnimatorController) return;
        foreach(var parameter in gateAnimator.parameters) if(parameter.name=="GateOpen" && parameter.type==AnimatorControllerParameterType.Bool) {
            gateAnimator.SetBool("GateOpen",open); return;
        }
    }
    void LateUpdate() {
        if(!Layered || !initialized) return;
        UpdateSorting();
        // Reopen/wait if anything occupies the aperture while closing. A closed, locked door
        // does not open merely because a cat walks into its outside collision surface.
        bool obstructed=!requestedOpen && OpenFraction>.001f && IsObstructed();
        float target=requestedOpen || obstructed?1:0;
        float duration=target>OpenFraction?openingDuration:closingDuration;
        OpenFraction=Mathf.MoveTowards(OpenFraction,target,Time.deltaTime/Mathf.Max(.05f,duration));
        ApplyPose();
    }
    void UpdateSorting() {
        if((!playerRenderer || !playerRenderer.gameObject.activeInHierarchy) && Time.unscaledTime>=nextPlayerSearch) {
            nextPlayerSearch=Time.unscaledTime+.5f;
            var cat=FindObjectOfType<CatPlayerController>();
            if(cat) playerRenderer=cat.GetComponentInChildren<SpriteRenderer>();
        }
        if(!playerRenderer) return;
        int layer=playerRenderer.sortingLayerID, order=playerRenderer.sortingOrder;
        var group=playerRenderer.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
        if(group && group.enabled) { layer=group.sortingLayerID; order=group.sortingOrder; }
        background.sortingLayerID=foreground.sortingLayerID=leftPanel.sortingLayerID=rightPanel.sortingLayerID=layer;
        background.sortingOrder=order-2;
        leftPanel.sortingOrder=order-1;rightPanel.sortingOrder=order+1;
        foreground.sortingOrder=order+2;
        if(leftFrame){leftFrame.sortingLayerID=layer;leftFrame.sortingOrder=order-2;}
        // Physical depth agrees with the sprite ordering as the cat crosses sideways.
        SetDepth(background,.12f);SetDepth(leftPanel,.06f);SetDepth(leftFrame,.08f);
        SetDepth(rightPanel,-.06f);SetDepth(foreground,-.08f);
        if(stateDisplay) { stateDisplay.sortingLayerID=layer;stateDisplay.sortingOrder=order+3; }
    }
    static void SetDepth(SpriteRenderer renderer,float offset) {
        if(!renderer)return;var position=renderer.transform.localPosition;position.z=offset;renderer.transform.localPosition=position;
    }
    void FitArtToCollider() {
        if(!blocker || !foreground || !foreground.sprite)return;
        Vector2 size=foreground.sprite.bounds.size;
        if(size.x<.01f || size.y<.01f)return;
        Vector3 fit=new Vector3(blocker.size.x/size.x,blocker.size.y/size.y,1);
        foreach(var renderer in new[]{background,foreground,leftPanel,rightPanel,stateDisplay})if(renderer){
            renderer.transform.localScale=fit;
            var position=renderer.transform.localPosition;position.x=blocker.offset.x;position.y=blocker.offset.y;renderer.transform.localPosition=position;
        }
    }
    void SplitFrame() {
        if(leftFrame || !foreground || !foreground.sprite)return;
        Sprite original=foreground.sprite;Rect rect=original.rect;float half=rect.width*.5f;
        Vector2 pivot=new Vector2(.5f,original.pivot.y/rect.height);
        splitLeft=Sprite.Create(original.texture,new Rect(rect.x,rect.y,half,rect.height),pivot,original.pixelsPerUnit);
        splitRight=Sprite.Create(original.texture,new Rect(rect.x+half,rect.y,rect.width-half,rect.height),pivot,original.pixelsPerUnit);
        var left=new GameObject("Left frame behind cat");left.transform.SetParent(foreground.transform.parent,false);
        left.transform.localPosition=foreground.transform.localPosition+Vector3.left*(original.bounds.size.x*.25f*foreground.transform.localScale.x);
        left.transform.localRotation=foreground.transform.localRotation;left.transform.localScale=foreground.transform.localScale;
        leftFrame=left.AddComponent<SpriteRenderer>();leftFrame.sprite=splitLeft;leftFrame.sharedMaterial=foreground.sharedMaterial;leftFrame.color=foreground.color;
        foreground.sprite=splitRight;foreground.transform.localPosition+=Vector3.right*(original.bounds.size.x*.25f*foreground.transform.localScale.x);
    }
    void OnDestroy(){if(splitLeft){if(Application.isPlaying)Destroy(splitLeft);else DestroyImmediate(splitLeft);}if(splitRight){if(Application.isPlaying)Destroy(splitRight);else DestroyImmediate(splitRight);}}
    bool IsObstructed() {
        if(!blocker) return false;
        Vector3 scale=transform.lossyScale;
        Vector2 size=Vector2.Scale(blocker.size,new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y)));
        // Calculate bounds from the shape: Collider.bounds is empty when the open blocker is disabled.
        Vector2 center=transform.TransformPoint(blocker.offset);
        var filter=new ContactFilter2D { useLayerMask=true, layerMask=obstructionLayers, useTriggers=false };
        int count=gameObject.scene.GetPhysicsScene2D().OverlapBox(center,size,transform.eulerAngles.z,filter,occupants);
        for(int i=0;i<count;i++) if(occupants[i] && occupants[i].enabled && !occupants[i].isTrigger && !occupants[i].transform.IsChildOf(transform)) return true;
        return count==occupants.Length; // A full query buffer must not be treated as an empty opening.
    }
    void ApplyPose() {
        if(stateDisplay) {
            bool moving=OpenFraction>.001f && OpenFraction<.98f;
            stateDisplay.sprite=moving?movingState:IsOpen?openState:closedState;
            stateDisplay.enabled=stateDisplay.sprite;
        }
        float eased=OpenFraction*OpenFraction*(3-2*OpenFraction);
        float shift=background.sprite?background.sprite.bounds.size.x*.345f: .86f;
        // Shrink/retract into opaque jamb pockets rather than drawing sliding leaves through adjacent walls.
        leftPanel.transform.localPosition=leftRest+Vector3.left*shift*eased;
        rightPanel.transform.localPosition=rightRest+Vector3.right*shift*eased;
        SetDepth(leftPanel,.06f);SetDepth(rightPanel,-.06f);
        leftPanel.transform.localScale=new Vector3(leftScale.x*(1-eased),leftScale.y,leftScale.z);
        rightPanel.transform.localScale=new Vector3(rightScale.x*(1-eased),rightScale.y,rightScale.z);
        leftPanel.enabled=rightPanel.enabled=eased<.9999f;
        if(blocker) { blocker.isTrigger=false; blocker.enabled=OpenFraction<.98f; }
    }
}
