using UnityEngine;

public sealed class HallwayRockDebris2D : MonoBehaviour {
    float expiresAt, destroyBelow;
    bool launched;

    public void Launch(Vector2 impact) {
        if(launched)return;
        launched=true;
        foreach(var shape in GetComponentsInChildren<Collider2D>(true))shape.enabled=false;
        foreach(var joint in GetComponentsInChildren<Joint2D>(true))joint.enabled=false;

        var body=GetComponent<Rigidbody2D>();
        if(!body)body=gameObject.AddComponent<Rigidbody2D>();
        body.simulated=true;
        body.bodyType=RigidbodyType2D.Dynamic;
        body.constraints=RigidbodyConstraints2D.None;
        body.useAutoMass=false;
        body.gravityScale=1.3f;
        body.drag=.05f;
        body.angularDrag=.1f;
        Vector2 outward=(Vector2)transform.position-impact;
        Vector2 direction=(Vector2.right+outward.normalized*.45f).normalized;
        body.velocity=direction*Random.Range(4f,8f)+Vector2.up*Random.Range(2f,4f);
        body.angularVelocity=Random.Range(-240f,240f);
        body.WakeUp();
        expiresAt=Time.time+Random.Range(4.5f,6f);
        destroyBelow=transform.position.y-25;
    }

    void Update(){
        if(launched && (Time.time>=expiresAt || transform.position.y<destroyBelow))Destroy(gameObject);
    }
}
