#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
public static class SpiderHallwaySetup {
 [MenuItem("Tools/TimeCat/Create editable boss hallway")]
 public static void Prepare(){
  var prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Lab_6.unity");bool alreadyLoaded=scene.IsValid() && scene.isLoaded;
  if(!alreadyLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Lab_6.unity",OpenSceneMode.Additive);
  SpiderBossController boss=null;foreach(var root in scene.GetRootGameObjects()){boss=root.GetComponentInChildren<SpiderBossController>(true);if(boss)break;}
  if(!boss)throw new System.InvalidOperationException("Lab 6 boss missing.");
  var passage=boss.GetComponent<SpiderBossEscapePassage>();if(!passage)passage=boss.gameObject.AddComponent<SpiderBossEscapePassage>();boss.escapePassage=passage;
  passage.Initialize(boss);passage.CreateEditableHallway();
  if(passage.hallwayRoot.scene!=scene)UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(passage.hallwayRoot,scene);
  if(passage.hallwayRoot.scene!=scene)throw new System.InvalidOperationException("Hallway must belong to Lab 6 before saving.");
  string folder="Assets/Resources/BossPassage/Editable";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  foreach(var renderer in passage.hallwayRoot.GetComponentsInChildren<SpriteRenderer>(true)){
   if(renderer.sprite && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(renderer.sprite))){
    string key=renderer.sprite.texture.name;string path=folder+"/"+key+".asset";
    var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!sprite){sprite=Object.Instantiate(renderer.sprite);AssetDatabase.CreateAsset(sprite,path);}renderer.sprite=sprite;
   }
   if(renderer.sharedMaterial && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(renderer.sharedMaterial))){
    string path=folder+"/Corridor.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=Object.Instantiate(renderer.sharedMaterial);AssetDatabase.CreateAsset(material,path);}renderer.sharedMaterial=material;
   }
  }
  EditorUtility.SetDirty(boss);EditorUtility.SetDirty(passage);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  if(alreadyLoaded){Selection.activeGameObject=passage.hallwayRoot;EditorGUIUtility.PingObject(passage.hallwayRoot);}
  File.WriteAllText("Temp/EditableHallway.saved.txt","Assets/Scenes/Lab_6.unity / "+passage.hallwayRoot.name);
  if(!alreadyLoaded){EditorSceneManager.CloseScene(scene,true);if(prior.IsValid() && prior.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(prior);}
  Debug.Log("Editable ending hallway saved as a disabled root with wall, deck, jamb, prefab door and exit trigger children.");
 }
 public static void PrepareBatch(){try{Prepare();EditorApplication.Exit(0);}catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
