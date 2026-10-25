#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine.Events;

public class EventJSONBinder : EditorWindow
{
    string jsonInput = "";
    ColliderEventTriggerDelayed targetComponent;
    Vector2 scroll;
    List<ParsedCall> calls = new();

    [MenuItem("Tools/TimeCat/Event JSON Binder")]
    static void Init() => GetWindow<EventJSONBinder>("Event JSON Binder");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Paste GenericPropertyJSON dump below:", EditorStyles.boldLabel);
        jsonInput = EditorGUILayout.TextArea(jsonInput, GUILayout.Height(160));

        targetComponent = (ColliderEventTriggerDelayed)EditorGUILayout.ObjectField("Target Component", targetComponent, typeof(ColliderEventTriggerDelayed), true);

        if (GUILayout.Button("Parse JSON"))
        {
            ParseJSON();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField($"Parsed Calls: {calls.Count}", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < calls.Count; i++)
        {
            var c = calls[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"#{i + 1} {c.targetType}.{c.method}", EditorStyles.boldLabel);
            c.boundObject = (GameObject)EditorGUILayout.ObjectField("Target Object", c.boundObject, typeof(GameObject), true);
            c.waitTime = EditorGUILayout.FloatField("Wait Time", c.waitTime);
            EditorGUILayout.LabelField($"Mode: {c.mode}  Args: {c.stringArg} {c.floatArg} {c.boolArg}");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(5);
        if (GUILayout.Button("Apply to Component"))
            ApplyToTarget();
    }

    void ParseJSON()
    {
        calls.Clear();
        string raw = jsonInput.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            Debug.LogWarning("No JSON text");
            return;
        }

        // auto-remove label
        if (raw.StartsWith("GenericPropertyJSON:"))
            raw = raw.Substring("GenericPropertyJSON:".Length);

        try
        {
            var root = JObject.Parse(raw);
            var methodNodes = root.SelectTokens("$..children[?(@.name=='m_MethodName')]");
            foreach (var m in methodNodes)
            {
                var parent = m.Parent?.Parent;
                if (parent == null) continue;

                var pc = new ParsedCall
                {
                    method = m["val"]?.ToString(),
                    targetType = parent.SelectToken("children[?(@.name=='m_TargetAssemblyTypeName')].val")?.ToString()?.Split(',')[0],
                    mode = parent.SelectToken("children[?(@.name=='m_Mode')].val")?.ToString(),
                    stringArg = parent.SelectToken("..children[?(@.name=='m_StringArgument')].val")?.ToString(),
                    floatArg = parent.SelectToken("..children[?(@.name=='m_FloatArgument')].val")?.ToObject<float>() ?? 0,
                    boolArg = parent.SelectToken("..children[?(@.name=='m_BoolArgument')].val")?.ToObject<bool>() ?? false
                };
                calls.Add(pc);
            }

            Debug.Log($"Parsed {calls.Count} event calls.");
        }
        catch (Exception ex)
        {
            Debug.LogError("JSON parse error: " + ex.Message);
        }
    }

    void ApplyToTarget()
    {
        if (targetComponent == null)
        {
            Debug.LogWarning("Assign target component first.");
            return;
        }

        Undo.RecordObject(targetComponent, "Apply JSON Binder");

        var list = new List<ColliderEventTriggerDelayed.TimedUnityEvent>();
        foreach (var c in calls)
        {
            if (c.boundObject == null)
            {
                Debug.LogWarning($"Call {c.method} has no bound object; skipping.");
                continue;
            }

            var newEvent = new ColliderEventTriggerDelayed.TimedUnityEvent();
            var unityEvent = new UnityEvent();

            var comp = c.boundObject.GetComponent(c.targetType);
            if (comp == null)
            {
                Debug.LogWarning($"Object {c.boundObject.name} missing component {c.targetType}");
                continue;
            }

            switch (c.mode)
            {
                case "Enum:Void":
                    AddAction(unityEvent, comp, c.method);
                    break;
                case "Enum:Float":
                    AddAction(unityEvent, comp, c.method, c.floatArg);
                    break;
                case "Enum:Bool":
                    AddAction(unityEvent, comp, c.method, c.boolArg);
                    break;
                case "Enum:String":
                    AddAction(unityEvent, comp, c.method, c.stringArg);
                    break;
                default:
                    AddAction(unityEvent, comp, c.method);
                    break;
            }

            newEvent.unityEvent = unityEvent;
            newEvent.waitTime = c.waitTime;
            list.Add(newEvent);
        }

        targetComponent.onPressedEvents = list;
        EditorUtility.SetDirty(targetComponent);
        Debug.Log($"✅ Applied {list.Count} bindings to {targetComponent.name}");
    }

    void AddAction(UnityEvent evt, Component comp, string method, object arg = null)
    {
        var mInfo = comp.GetType().GetMethod(method);
        if (mInfo == null) { Debug.LogWarning($"{comp.GetType().Name}.{method} not found"); return; }

        evt.AddListener(() =>
        {
            mInfo.Invoke(comp, arg != null ? new[] { arg } : null);
        });
    }

    class ParsedCall
    {
        public string targetType;
        public string method;
        public string mode;
        public float floatArg;
        public bool boolArg;
        public string stringArg;
        public GameObject boundObject;
        public float waitTime;
    }
}
#endif
