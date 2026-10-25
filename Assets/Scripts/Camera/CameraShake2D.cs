using UnityEngine;
[DefaultExecutionOrder(1001)]
public class CameraShake2D : MonoBehaviour {
 public float defaultIntensity=.1f,defaultDuration=.5f;
 [Range(.1f,20f)]public float smoothness=5;
 public bool isInfinite;
 Vector3 applied;float remaining,duration,strength,seed;
 void Awake(){seed=Random.value*1000;strength=defaultIntensity;if(!isInfinite){remaining=duration=defaultDuration;}}
 void Update(){RemoveOffset();}
 void LateUpdate(){
  if(!isInfinite && remaining<=0)return;
  float fade=isInfinite?1:Mathf.Clamp01(remaining/Mathf.Max(.01f,duration));remaining-=Time.deltaTime;float time=Time.time*smoothness;
  applied=new Vector3(Mathf.PerlinNoise(seed,time)-.5f,Mathf.PerlinNoise(seed+1,time)-.5f,0)*2*strength*fade;transform.position+=applied;
 }
 void RemoveOffset(){transform.position-=applied;applied=Vector3.zero;}
 public void Shake(float intensity,float seconds){RemoveOffset();isInfinite=false;strength=intensity;remaining=duration=Mathf.Max(0,seconds);}
 public void ShakeInfinite(float intensity){RemoveOffset();isInfinite=true;strength=intensity;}
 public void StopShake(){RemoveOffset();isInfinite=false;remaining=0;}
 public void SetIntensity(float intensity){strength=intensity;}
 public void SetSmoothness(float value){smoothness=value;}
 void OnDisable(){RemoveOffset();}
}
