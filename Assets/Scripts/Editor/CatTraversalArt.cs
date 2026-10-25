#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class CatTraversalArt {
 public static AnimationClip Clip(string kind){
  const string sheet="Assets/Art/Player Sprites/Traversal/cat-traversal.png";
  var importer=(TextureImporter)AssetImporter.GetAtPath(sheet);
  if(importer.spriteImportMode!=SpriteImportMode.Multiple){
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
   importer.spritePixelsPerUnit=118.5f;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
   var cells=new System.Collections.Generic.List<SpriteMetaData>();string[] kinds={"climb","hang","look","mantle"};
   for(int row=0;row<4;row++)for(int i=0;i<8;i++)cells.Add(new SpriteMetaData{name=kinds[row]+"_"+i,rect=new Rect(i*128,(3-row)*192,128,192),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(.5f,.5f)});
   importer.spritesheet=cells.ToArray();importer.SaveAndReimport();
  }
  string path="Assets/Art/Player Sprites/Traversal/"+kind+".anim";
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
  clip.name="Traversal "+kind;clip.frameRate=12;
  var sprites=AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().Where(s=>s.name.StartsWith(kind+"_")).OrderBy(s=>s.name).ToArray();
  float duration=kind=="hang" || kind=="look"?1.6f:.8f;
  var keys=sprites.Select((sprite,index)=>new ObjectReferenceKeyframe{time=index*duration/8,value=sprite}).ToList();keys.Add(new ObjectReferenceKeyframe{time=duration,value=sprites[0]});
  AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys.ToArray());
  var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);return clip;
 }
}
#endif
