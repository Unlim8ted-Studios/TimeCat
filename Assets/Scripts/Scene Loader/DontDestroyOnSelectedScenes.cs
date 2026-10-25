using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class DontDestroyOnSelectedScenes : MonoBehaviour {
 public List<string> sceneNames=new List<string>();
 public string instanceName;
 static readonly Dictionary<string,DontDestroyOnSelectedScenes> owners=new Dictionary<string,DontDestroyOnSelectedScenes>();
 string key;
 void Start(){
  key=string.IsNullOrEmpty(instanceName)?name:instanceName;
  DontDestroyOnSelectedScenes previous;
  if(owners.TryGetValue(key,out previous) && previous && previous!=this){Destroy(gameObject);return;}
  owners[key]=this;
  transform.SetParent(null,true);DontDestroyOnLoad(gameObject);
  SceneManager.sceneLoaded+=OnSceneLoaded;
 }
 void OnSceneLoaded(Scene scene,LoadSceneMode mode){
  if(sceneNames!=null && sceneNames.Contains(scene.name))return;
  Release();Destroy(gameObject);
 }
 void Release(){
  SceneManager.sceneLoaded-=OnSceneLoaded;
  DontDestroyOnSelectedScenes owner;if(key!=null && owners.TryGetValue(key,out owner) && owner==this)owners.Remove(key);
 }
 void OnDestroy(){Release();}
}
