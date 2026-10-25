#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class CatClimbingSetup {
 public static void Apply(){
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/Player Sprites/Player.controller");if(!controller)return;
  if(!controller.parameters.Any(p=>p.name=="isMantling"))controller.AddParameter("isMantling",AnimatorControllerParameterType.Bool);
  if(!controller.parameters.Any(p=>p.name=="climbAnimationSpeed"))controller.AddParameter(new AnimatorControllerParameter{name="climbAnimationSpeed",type=AnimatorControllerParameterType.Float,defaultFloat=1});
  var root=controller.layers[0].stateMachine;var machine=root.stateMachines.FirstOrDefault(s=>s.stateMachine.name=="Wall traversal").stateMachine;
  if(!machine)machine=root.AddStateMachine("Wall traversal",new Vector3(800,-150));
  foreach(var transition in root.anyStateTransitions.ToArray())if(machine.states.Any(s=>s.state==transition.destinationState))root.RemoveAnyStateTransition(transition);
  // Legacy Any State transitions must not steal the authored traversal poses.
  foreach(var transition in root.anyStateTransitions)if(!transition.conditions.Any(c=>c.parameter=="isOnWall" && c.mode==AnimatorConditionMode.IfNot))transition.AddCondition(AnimatorConditionMode.IfNot,0,"isOnWall");
  foreach(var child in machine.states.ToArray())machine.RemoveState(child.state);
  var hang=machine.AddState("Wall hang");hang.motion=CatTraversalArt.Clip("hang");
  var climb=machine.AddState("Wall climb");climb.motion=CatTraversalArt.Clip("climb");climb.speed=1f;climb.speedParameter="climbAnimationSpeed";climb.speedParameterActive=true;
  var mantle=machine.AddState("Wall mantle");mantle.motion=CatTraversalArt.Clip("mantle");mantle.speed=1f;mantle.speedParameter="climbAnimationSpeed";mantle.speedParameterActive=true;
  var look=machine.AddState("Wall look away");look.motion=CatTraversalArt.Clip("look");
  foreach(var state in new[]{mantle,look,climb,hang}){
   var enter=root.AddAnyStateTransition(state);enter.hasExitTime=false;enter.duration=0;enter.canTransitionToSelf=false;
   enter.AddCondition(state==mantle?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"isMantling");
   if(state!=mantle){enter.AddCondition(AnimatorConditionMode.If,0,"isOnWall");if(state!=look)enter.AddCondition(state==climb?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"isClimbingWall");enter.AddCondition(state==look?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"isLookingAwayFromWall");enter.AddCondition(AnimatorConditionMode.IfNot,0,"isAttacking");}
   var exit=state.AddTransition(root.defaultState);exit.hasExitTime=false;exit.duration=0;exit.AddCondition(AnimatorConditionMode.IfNot,0,"isOnWall");exit.AddCondition(AnimatorConditionMode.IfNot,0,"isMantling");
  }
  EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
 }
}
#endif
