using UnityEngine;
[DefaultExecutionOrder(1000)]
public class CameraShake : MonoBehaviour {
 public static CameraShake Instance;
 public float defaultDuration=.5f,intensity=.5f,smoothness=5;
 Vector3 applied;float remaining;
 void OnEnable(){Instance=this;}
 void Update(){RemoveOffset();if(Input.GetKeyDown(KeyCode.K))Shake(.4f);}
 void LateUpdate(){if(remaining<=0)return;remaining-=Time.deltaTime;float fade=Mathf.Clamp01(remaining/Mathf.Max(.01f,defaultDuration));applied=(Vector3)Random.insideUnitCircle*intensity*fade;transform.position+=applied;}
 void RemoveOffset(){transform.position-=applied;applied=Vector3.zero;}
 public void Shake(float duration=-1){remaining=duration>0?duration:defaultDuration;}
 void OnDisable(){RemoveOffset();remaining=0;if(Instance==this)Instance=null;}
}
