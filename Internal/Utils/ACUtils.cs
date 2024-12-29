using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    public static partial class ACUtils
    {
        public static IEnumerable<AnimatorStateMachine> AllStateMachines(AnimatorStateMachine? stateMachine)
        {
            if (stateMachine == null) yield break;
            yield return stateMachine;

            foreach (var child in stateMachine.stateMachines)
            foreach (var machine in AllStateMachines(child.stateMachine))
                yield return machine;
        }

        public static IEnumerable<AnimatorState> AllStates(AnimatorStateMachine? stateMachine)
        {
            if (stateMachine == null) yield break;
            foreach (var state in stateMachine.states)
                yield return state.state;

            foreach (var child in stateMachine.stateMachines)
            foreach (var state in AllStates(child.stateMachine))
                yield return state;
        }

        public static IEnumerable<AnimatorTransitionBase> AllTransitions(AnimatorStateMachine? stateMachine)
        {
            if (stateMachine == null) yield break;

            foreach (var transition in stateMachine.entryTransitions)
                yield return transition;
            foreach (var transition in stateMachine.anyStateTransitions)
                yield return transition;

            foreach (var state in stateMachine.states)
            foreach (var transition in state.state.transitions)
                yield return transition;

            foreach (var child in stateMachine.stateMachines)
            {
                foreach (var transition in stateMachine.GetStateMachineTransitions(child.stateMachine))
                    yield return transition;
                foreach (var transition in AllTransitions(child.stateMachine))
                    yield return transition;
            }
        }

        public static IEnumerable<AnimationClip> AllClips(Motion? motion)
        {
            switch (motion)
            {
                case null:
                    yield break;
                case AnimationClip clip:
                    yield return clip;
                    break;
                case BlendTree blendTree:
                    foreach (var child in blendTree.children)
                    foreach (var clip in AllClips(child.motion))
                        yield return clip;
                    break;
            }
        }

        public static IEnumerable<AnimationClip?> AllClipsMayNull(Motion? motion)
        {
            switch (motion)
            {
                case null:
                    yield return null;
                    break;
                case AnimationClip clip:
                    yield return clip;
                    break;
                case BlendTree blendTree:
                    foreach (var child in blendTree.children)
                    foreach (var clip in AllClips(child.motion))
                        yield return clip;
                    break;
            }
        }

        public static IEnumerable<BlendTree> AllBlendTrees(Motion? motion)
        {
            switch (motion)
            {
                case null:
                    yield break;
                case AnimationClip _:
                    break;
                case BlendTree blendTree:
                    yield return blendTree;
                    foreach (var child in blendTree.children)
                    foreach (var tree in AllBlendTrees(child.motion))
                        yield return tree;
                    break;
            }
        }

        public static IEnumerable<StateMachineBehaviour> StateMachineBehaviours(
            RuntimeAnimatorController runtimeController)
        {
            if (runtimeController == null) throw new ArgumentNullException(nameof(runtimeController));
            var (controller, _) = GetControllerAndOverrides(runtimeController);

            foreach (var layer in controller.layers)
            {
                if (layer.syncedLayerIndex == -1)
                    foreach (var behaviour in StateMachineBehaviours(layer.stateMachine))
                        yield return behaviour;
                else
                    foreach (var state in AllStates(controller.layers[layer.syncedLayerIndex].stateMachine))
                    foreach (var behaviour in layer.GetOverrideBehaviours(state))
                        yield return behaviour;
            }
        }

        public static IEnumerable<StateMachineBehaviour> StateMachineBehaviours(
            AnimatorStateMachine stateMachineIn)
        {
            if (stateMachineIn == null) throw new ArgumentNullException(nameof(stateMachineIn));
            var queue = new Queue<AnimatorStateMachine>();
            queue.Enqueue(stateMachineIn);

            while (queue.Count != 0)
            {
                var stateMachine = queue.Dequeue();
                foreach (var behaviour in stateMachine.behaviours)
                    if (behaviour != null)
                        yield return behaviour;
                foreach (var state in stateMachine.states)
                foreach (var behaviour in state.state.behaviours)
                    if (behaviour != null)
                        yield return behaviour;

                foreach (var childStateMachine in stateMachine.stateMachines)
                    queue.Enqueue(childStateMachine.stateMachine);
            }
        }

        public static int ComputeLayerCount(this RuntimeAnimatorController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            while (controller is AnimatorOverrideController overrideController)
                controller = overrideController.runtimeAnimatorController;
            return ((AnimatorController)controller).layers.Length;
        }

        public static bool? SatisfiesInt(this AnimatorCondition condition, int value) =>
            condition.mode switch
            {
                AnimatorConditionMode.Equals => value == (int)condition.threshold,
                AnimatorConditionMode.NotEqual => value != (int)condition.threshold,
                AnimatorConditionMode.Greater => value > condition.threshold,
                AnimatorConditionMode.Less => value < condition.threshold,
                _ => null
            };

        public static bool? SatisfiesBool(this AnimatorCondition condition, bool value) =>
            condition.mode switch
            {
                AnimatorConditionMode.If => value,
                AnimatorConditionMode.IfNot => !value,
                _ => null
            };

        public static HashSet<EditorCurveBinding> GetBindings(this AnimationClip clip)
        {
            var bindings = new HashSet<EditorCurveBinding>();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                bindings.Add(binding);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                bindings.Add(binding);
            return bindings;
        }

        public static HashSet<EditorCurveBinding> GetAllBindings(this IEnumerable<AnimationClip?> clips)
        {
            var bindings = new HashSet<EditorCurveBinding>();
            foreach (var clip in clips)
                if (clip != null)
                    bindings.UnionWith(clip.GetBindings());
            return bindings;
        }

        public static HashSet<EditorCurveBinding> GetAllBindings(this Motion? blendTree) =>
            AllClips(blendTree).GetAllBindings();
    }
}
