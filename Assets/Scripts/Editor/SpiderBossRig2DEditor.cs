#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpiderBossRig2D))]
public class SpiderBossRig2DEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        var rig=(SpiderBossRig2D)target;
        if(GUILayout.Button("Build / refresh IK targets")) {
            Undo.RegisterFullObjectHierarchyUndo(rig.gameObject,"Build spider IK rig");
            rig.BuildRig(); EditorUtility.SetDirty(rig);
        }
        EditorGUILayout.HelpBox("Move yellow target handles and set limb Weight to preview. Weight zero preserves authored clips. Pole handles control knee bend.",MessageType.Info);
    }
    void OnSceneGUI() {
        var rig=(SpiderBossRig2D)target;
        foreach(var limb in rig.limbs) {
            if(!limb.target) continue;
            Handles.color=Color.yellow;
            EditorGUI.BeginChangeCheck();
            Vector3 point=Handles.PositionHandle(limb.target.position,Quaternion.identity);
            if(EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(limb.target,"Move spider IK target"); limb.target.position=point;
                if(rig.previewInEditor && !Application.isPlaying) {
                    Undo.RecordObject(limb.root,"Preview spider IK"); if(limb.joint) Undo.RecordObject(limb.joint,"Preview spider IK"); rig.Evaluate();
                }
            }
            Handles.Label(limb.target.position,limb.root.name);
            if(limb.pole) {
                EditorGUI.BeginChangeCheck(); Vector3 pole=Handles.PositionHandle(limb.pole.position,Quaternion.identity);
                if(EditorGUI.EndChangeCheck()) { Undo.RecordObject(limb.pole,"Move spider pole");limb.pole.position=pole; }
            }
        }
    }
}
#endif
