using UnityEngine;
// Opening controls remain readable over the forest lighting, using the game's own font.
public sealed class IntroControlHints : MonoBehaviour {
    public Font font;
    CatPlayerController player;
    GUIStyle textStyle;
    void Start(){
        player=FindObjectOfType<CatPlayerController>();
        foreach(var text in FindObjectsOfType<TextMesh>(true)){
            string value=text.text.ToLowerInvariant();
            if(value.Contains("move left / right") || value.Contains("climb up / down") || value.Contains("hold a") || value.Contains("use w and s") || value.TrimStart().StartsWith("press")){
                var renderer=text.GetComponent<Renderer>();if(renderer)renderer.enabled=false;
            }
        }
        foreach(var text in FindObjectsOfType<TMPro.TMP_Text>(true))if(text.text.StartsWith("Cutscene of cat climbing")){var renderer=text.GetComponent<Renderer>();if(renderer)renderer.enabled=false;}
    }
    void OnGUI(){
        if(Time.timeScale<=0 || !player || !player.gameObject.activeInHierarchy || player.IsFrozen || !Camera.main || !font)return;
        if(textStyle==null)textStyle=new GUIStyle{font=font,alignment=TextAnchor.MiddleLeft,wordWrap=false};
        var rect=Camera.main.pixelRect;
        float scale=Mathf.Clamp(rect.width/1100f,.75f,1.3f);
        float x=rect.x+18*scale,y=Screen.height-rect.yMax+18*scale,width=310*scale,height=142*scale;
        Color old=GUI.color;GUI.color=new Color(.025f,.04f,.05f,.9f);GUI.DrawTexture(new Rect(x,y,width,height),Texture2D.whiteTexture);
        GUI.color=new Color(.76f,.7f,.46f,1);GUI.DrawTexture(new Rect(x,y,3*scale,height),Texture2D.whiteTexture);
        textStyle.fontSize=Mathf.RoundToInt(12*scale);textStyle.normal.textColor=new Color(.96f,.94f,.85f);
        string[] lines=TimeCatWebBridge.TouchMode?new[]{"LEFT STICK    MOVE","STICK UP/DOWN    CLIMB","JUMP    UP / AWAY + JUMP","FULL STICK    RUN"}:new[]{"A / D    MOVE","W / S    CLIMB","SPACE    JUMP UP","AWAY + SPACE    PUSH OFF"};
        GUI.color=Color.white;for(int i=0;i<lines.Length;i++)GUI.Label(new Rect(x+16*scale,y+(10+i*30)*scale,width-24*scale,28*scale),lines[i],textStyle);
        GUI.color=old;
    }
}
