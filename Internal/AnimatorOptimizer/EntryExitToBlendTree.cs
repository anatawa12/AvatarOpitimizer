using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    /// <summary>
    /// converts entry-exit state machine to 1d blend tree
    ///
    /// Currently this class only supports entry transition with 1 parameter equals condition. 
    /// </summary>

    // detailed explanation of current limitations
    // This optimization expects
    // (about state machine)
    // - there are no child state machine
    // - there are no state machine behaviour
    // (about transitions)
    // - all transitions are associated with same single parameter
    // - all states are connected from entry transition
    // - all states are connected to exit
    // - there are no other transitions except entry and exit
    // - all states will leave state to exit when parameter value become values not listed in entry transitions
    // (semantics)
    // - each state has corresponding parameter value for the parameter
    // (motion / state)
    // - all states have same write defaults value
    // - if write defaults is off, all states have same animating properties
    // - all states must not have motion time. you have to use 1d blend tree for gesture weight.
    public class EntryExitToBlendTree : AnimOptPassBase<EntryExitToBlendTree>
    {
        private static CachedGuidLoader<AnimationClip> _emptyClip = "ce6c609e7fd58444d9d59e98296eed35";

        private protected override void Execute(BuildContext context, AOAnimatorController controller,
            TraceAndOptimizeState settings)
        {
            if (settings.SkipEntryExitToBlendTree) return; // feature disabled

            var state = context.GetState<AnimatorOptimizerState>();
            Execute(state, controller);
        }

        public static void Execute(AnimatorOptimizerState state, AOAnimatorController controller)
        {
            var intOrBoolParameters = new HashSet<string>(controller.parameters
                .Where(x => x.type is AnimatorControllerParameterType.Int or AnimatorControllerParameterType.Bool)
                .Select(x => x.name));

            // first, collect transformable layers
            var layers = controller.layers;
            var convertInfos = new ConvertibleLayerInfo?[layers.Length];
            var layerByParameter = new Dictionary<string, List<int>>();
            for (var i = 0; i < layers.Length; i++)
            {
                var info = convertInfos[i] = TryParseLayer(layers[i], state, intOrBoolParameters);
                if (info != null)
                {
                    foreach (var parameter in info.Parameters)
                    {
                        if (!layerByParameter.TryGetValue(parameter, out var list))
                            layerByParameter.Add(parameter, list = new List<int>());
                        list.Add(i);
                    }
                }
            }

            // finally, convert layers & change type of parameters

            for (var i = 0; i < layers.Length; i++)
            {
                if (convertInfos[i] is not { } info) continue;
                var layer = layers[i];
                DoConvert(info, layer);
            }

            var parameters = controller.parameters;
            foreach (ref var parameter in parameters.AsSpan())
                if (layerByParameter.ContainsKey(parameter.name))
                    parameter.type = AnimatorControllerParameterType.Float;

            Predicate<string> needsConvert = parameter => layerByParameter.ContainsKey(parameter);

            T[] ConvertTransitions<T>(T[] transitions, Func<T, AnimatorCondition[], T> clone)
                where T : AnimatorTransitionBase
            {
                if (transitions.Length == 0) return transitions;

                var entryTransitions = new LinkedList<T>(transitions);
                for (var cursor = entryTransitions.First; cursor != null; cursor = cursor.Next)
                {
                    if (!NeedsConversion(cursor.Value.conditions, needsConvert)) continue;
                    var conditions = cursor.Value.conditions;
                    var newConditions = ConvertIntOrBoolConditionsToFloat(conditions, needsConvert);
                    var toRemove = cursor;
                    foreach (var animatorConditions in FlattenConditions(newConditions))
                        cursor = entryTransitions.AddAfter(cursor, clone(toRemove.Value, animatorConditions));
                    entryTransitions.Remove(toRemove);
                }

                return entryTransitions.ToArray();
            }

            AnimatorTransition CloneTransition(AnimatorTransition transition, AnimatorCondition[] conditions) => new()
            {
                name = transition.name,
                conditions = conditions,
                destinationStateMachine = transition.destinationStateMachine,
                destinationState = transition.destinationState,
                solo = transition.solo,
                mute = transition.mute,
                isExit = transition.isExit,
            };

            AnimatorStateTransition CloneStateTransition(AnimatorStateTransition transition,
                AnimatorCondition[] conditions) => new()
            {
                name = transition.name,
                conditions = conditions,
                destinationStateMachine = transition.destinationStateMachine,
                destinationState = transition.destinationState,
                solo = transition.solo,
                mute = transition.mute,
                isExit = transition.isExit,
                duration = transition.duration,
                offset = transition.offset,
                exitTime = transition.exitTime,
                hasExitTime = transition.hasExitTime,
                hasFixedDuration = transition.hasFixedDuration,
                interruptionSource = transition.interruptionSource,
                orderedInterruption = transition.orderedInterruption,
                canTransitionToSelf = transition.canTransitionToSelf,
            };

            for (var layerI = 0; layerI < layers.Length; layerI++)
            {
                if (convertInfos[layerI] != null) continue;
                var layer = layers[layerI];
                if (layer.IsSynced) continue;

                foreach (var stateMachine in ACUtils.AllStateMachines(layer.stateMachine))
                {
                    stateMachine.entryTransitions = ConvertTransitions(stateMachine.entryTransitions, CloneTransition);
                    stateMachine.anyStateTransitions =
                        ConvertTransitions(stateMachine.anyStateTransitions, CloneStateTransition);
                    foreach (var animatorState in stateMachine.states)
                        animatorState.state.transitions =
                            ConvertTransitions(animatorState.state.transitions, CloneStateTransition);

                    foreach (var child in stateMachine.stateMachines)
                    {
                        var transitions = stateMachine.GetStateMachineTransitions(child.stateMachine);
                        if (transitions.Length == 0) continue;
                        stateMachine.SetStateMachineTransitions(child.stateMachine,
                            ConvertTransitions(transitions, CloneTransition));
                    }
                }
            }

            controller.parameters = parameters;
        }

        private static ConvertibleLayerInfo? TryParseLayer(AOAnimatorControllerLayer layer,
            AnimatorOptimizerState optimizerState, HashSet<string> intOrBoolParameters)
        {
            if (layer is not
                {
                    IsSynced: false,
                    IsSyncedToOtherLayer: false,
                    stateMachine:
                    {
                        anyStateTransitions: { Length: 0 },
                        stateMachines: { Length: 0 },
                        defaultState: { } defaultState,
                        states: { Length: >= 2 } states,
                        entryTransitions: { Length: >= 1 } entryTransitions,
                    }
                })
                return null;

            // check for conditions of entry transitions

            string conditionParameter;
            var stateValues = new Dictionary<AnimatorState, HashSet<IntOrBool>>();
            var allValues = new HashSet<IntOrBool>();

            {
                var entryTransition = entryTransitions[0];

                if (entryTransition is not
                    {
                        isExit: false,
                        destinationStateMachine: null,
                        destinationState: { } dest,
                        conditions: { Length: 1 } conditions
                    })
                    return null;

                conditionParameter = conditions[0].parameter;

                if (!intOrBoolParameters.Contains(conditionParameter)) return null; // neither int nor bool parameter

                if (CheckIntOrBoolCondition(conditions[0]) is not { } value) return null;
                if (!AddToStateValues(dest, value)) return null; // duplicated value
            }

            for (var index = 1; index < entryTransitions.Length - 1; index++)
            {
                var entryTransition = entryTransitions[index];
                if (entryTransition is not
                    {
                        isExit: false,
                        destinationStateMachine: null,
                        destinationState: { } dest,
                        conditions: { Length: 1 } conditions
                    }) return null;

                if (CheckIntOrBoolCondition(conditions[0]) is not { } value) return null;
                if (!AddToStateValues(dest, value)) return null; // duplicated value
            }

            // allow transition to default state without conditions for last entry transition
            if (entryTransitions.Length >= 2) {
                var entryTransition = entryTransitions[^1];

                if (entryTransition is not
                    {
                        isExit: false,
                        destinationStateMachine: null,
                        destinationState: { } dest,
                        conditions: { } conditions,
                    }) return null;

                switch (conditions.Length)
                {
                    case 1:
                    {
                        if (CheckIntOrBoolCondition(conditions[0]) is not { } value) return null;
                        if (!AddToStateValues(dest, value)) return null; // duplicated value
                        break;
                    }
                    case 0 when dest == defaultState:
                        // no condition for default state is allowed
                        break;
                    default:
                        return null;
                }
            }

            IntOrBool? CheckIntOrBoolCondition(AnimatorCondition condition)
            {
                if (condition is not
                    {
                        mode: var mode,
                        parameter: { } parameter,
                        threshold: var threshold,
                    }) return null;

                if (parameter != conditionParameter) return null;

                return mode switch
                {
                    // not finite makes casting to int undefined
                    AnimatorConditionMode.Equals when float.IsFinite(threshold) => (int)threshold,
                    AnimatorConditionMode.If => true,
                    AnimatorConditionMode.IfNot => false,
                    _ => null,
                };
            }

            bool AddToStateValues(AnimatorState state, IntOrBool value)
            {
                if (!stateValues.TryGetValue(state, out var values))
                    stateValues.Add(state, values = new HashSet<IntOrBool>());
                if (allValues.Contains(value)) return false; // duplicated value
                values.Add(value);
                allValues.Add(value);
                return true;
            }

            // check there are no states without entry transition.
            if (stateValues.ContainsKey(defaultState))
            {
                if (stateValues.Count != states.Length) return null;
            }
            else
            {
                if (stateValues.Count != states.Length - 1) return null;
            }

            // check for each states
            // we have to check
            // - write defaults are same in the layer
            // - if write defaults is off, check animating properties are same between layers
            // - exit transitions are correct for that state
            // - there are no other transitions
            // - there are no behaviors

            // check write defaults and animating properties
            if (states[0].state.writeDefaultValues)
            {
                for (var index = 1; index < states.Length; index++)
                {
                    // check WD
                    if (states[index] is not { state: { writeDefaultValues: true } }) return null;
                }
            }
            else
            {
                if (states[0].state.motion == null) return null; // with WD=off, motion=None will cause broken animator
                var expectAnimatingProperties = CollectAnimatingProperties(states[0].state.motion);
                if (expectAnimatingProperties == null) return null; // we found unsupported motion

                for (var index = 1; index < states.Length; index++)
                {
                    // check WD and animating properties
                    var childStateInfo = states[index];
                    if (childStateInfo is not { state: { motion: var motion, writeDefaultValues: false } })
                        return null;

                    if (motion == null) return null; // with WD=off, motion=None will cause broken animator
                    var newAnimatingProperties = CollectAnimatingProperties(motion);
                    if (newAnimatingProperties == null) return null; // we found unsupported motion
                    if (!newAnimatingProperties.SetEquals(expectAnimatingProperties)) return null;
                }
            }

            foreach (var childStateInfo in states)
            {
                // TODO: for linear animation, we can simulate motion time with 1d blend tree
                // https://github.com/anatawa12/AvatarOptimizer/issues/861

                if (childStateInfo is not
                    {
                        state:
                        {
                            behaviours: { Length: 0 },
                            timeParameterActive: false,
                            motion: var motion,
                            transitions: var transitions,
                        } state,
                    }) return null;

                // the clip is time dependant, we cannot convert it to blend tree
                foreach (var clip in ACUtils.AllClips(motion))
                    if (optimizerState.IsTimeDependentClip(clip))
                        return null;

                // check for transitions

                // basic transition check: all transitions are exit transitions without blending
                var allConditions = new AnimatorCondition[transitions.Length][];
                for (var i = 0; i < transitions.Length; i++)
                {
                    var transition = transitions[i];
                    if (transition is not
                        {
                            isExit: true,
                            solo: false,
                            mute: false,
                            destinationState: null,
                            destinationStateMachine: null,
                            conditions: { } conditions,

                            hasExitTime: false,
                            duration: 0,
                            offset: 0,
                            // since duration is zero, interruption should not be happened
                        }) return null;
                    allConditions[i] = conditions;
                }

                // transition condition check.
                if (defaultState == state)
                {
                    // for default state, we check if we exit the default state if the value is any other states value.
                    // We allow too relaxed condition for exiting default state since it will re-enter the default state.
                    HashSet<IntOrBool> exitValues;
                    if (stateValues.TryGetValue(state, out var values))
                    {
                        exitValues = new HashSet<IntOrBool>(allValues);
                        exitValues.ExceptWith(values);
                    }
                    else
                    {
                        exitValues = new HashSet<IntOrBool>(allValues);
                    }

                    foreach (var conditions in allConditions)
                    {
                        // conditions with parameters other than conditionParameter can be false
                        if (conditions.Any(c => c.parameter != conditionParameter)) continue;

                        exitValues.RemoveWhere(value => value.IntValue.HasValue ?
                            conditions.All(c => c.SatisfiesInt(value.IntValue.Value) == true) :
                            conditions.All(c => c.SatisfiesBool(value.BoolValue!.Value) == true));
                    }

                    if (exitValues.Count != 0) return null;
                }
                else
                {
                    // for other states, it have to leave state if value is not any of current value
                    // TODO: users can create condition like `< minValue` or `> maxValue` to leave state
                    // TODO: users can exit state and immediately enter to same state infinitely
                    // https://github.com/anatawa12/AvatarOptimizer/issues/862
                    var values = stateValues[state];
                    if (!PossibleValuesExitTransitionCheck(values)) return null;
                }

                bool PossibleValuesExitTransitionCheck(HashSet<IntOrBool> values)
                {
                    if (allConditions.Length != 1) return false;
                    var conditions = allConditions[0];
                    if (conditions.Length != values.Count) return false;

                    values = new HashSet<IntOrBool>(values);
                    foreach (var condition in conditions)
                    {
                        if (condition.mode != AnimatorConditionMode.NotEqual &&
                            condition.mode != AnimatorConditionMode.IfNot &&
                            condition.mode != AnimatorConditionMode.If) return false;
                        if (condition.parameter != conditionParameter) return false;
                        IntOrBool value =
                            condition.mode == AnimatorConditionMode.NotEqual ? (int)condition.threshold :
                            condition.mode == AnimatorConditionMode.IfNot ? true : false;
                        if (!values.Remove(value)) return false;
                    }

                    return true;
                }
            }

            return new ConvertibleLayerInfo(conditionParameter, defaultState, stateValues);
        }

        readonly struct IntOrBool : IEquatable<IntOrBool>
        {
            public readonly int? IntValue;
            public readonly bool? BoolValue;

            public IntOrBool(int value)
            {
                IntValue = value;
                BoolValue = null;
            }

            public IntOrBool(bool value)
            {
                IntValue = null;
                BoolValue = value;
            }

            public override int GetHashCode() => HashCode.Combine(IntValue, BoolValue);

            public bool Equals(IntOrBool other) => IntValue == other.IntValue && BoolValue == other.BoolValue;
            public override bool Equals(object? obj) => obj is IntOrBool other && Equals(other);
            public static bool operator ==(IntOrBool left, IntOrBool right) => left.Equals(right);
            public static bool operator !=(IntOrBool left, IntOrBool right) => !left.Equals(right);

            public static implicit operator IntOrBool(int value) => new(value);
            public static implicit operator IntOrBool(bool value) => new(value);
        }

        class ConvertibleLayerInfo
        {
            public readonly string ParameterName;
            public readonly AnimatorState DefaultState;
            public readonly Dictionary<AnimatorState, HashSet<IntOrBool>> ValueForStates;

            public ConvertibleLayerInfo(string parameterName, AnimatorState defaultState,
                Dictionary<AnimatorState, HashSet<IntOrBool>> valueForStates)
            {
                ParameterName = parameterName;
                DefaultState = defaultState;
                ValueForStates = valueForStates;
            }

            public IEnumerable<string> Parameters => new[] { ParameterName };
        }

        private static HashSet<EditorCurveBinding>? CollectAnimatingProperties(Motion? motion)
        {
            switch (motion)
            {
                case AnimationClip clip:
                    return clip.GetBindings();
                case BlendTree tree:
                    if (tree.blendType == BlendTreeType.Direct) return null;
                    if (tree.children.Length == 0) return null; // unknown
                    var props = CollectAnimatingProperties(tree.children[0].motion);
                    if (props == null) return null;

                    for (var i = 1; i < tree.children.Length; i++)
                    {
                        var childProps = CollectAnimatingProperties(tree.children[i].motion);
                        if (childProps == null) return null;
                        if (!props.SetEquals(childProps)) return null;
                    }

                    return props;
                default:
                    return null;
            }
        }

        private static void DoConvert(ConvertibleLayerInfo info, AOAnimatorControllerLayer layer)
        {
            var valueForStates = info.ValueForStates;
            valueForStates.Remove(info.DefaultState); // default states are proceed always so remove from this list
            var defaultMotion = info.DefaultState.motion ? info.DefaultState.motion : _emptyClip.Value;

            var states = new List<(IntOrBool value, Motion motion)>();

            foreach (var (state, values) in valueForStates)
            foreach (var value in values)
                states.Add((value, state.motion ? state.motion : _emptyClip.Value));

            var children = new List<ChildMotion>();

            void AddFrames(int before, Motion beforeMotion, int after, Motion afterMotion)
            {
                var threshold = before + 0.5f;
                var rounded = Mathf.Round(threshold);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (rounded == after)
                {
                    Utils.Assert((int)Mathf.Round(Utils.PreviousFloat(threshold)) == before);
                    // in this case, .5 will go the motion so default is one before .5
                    children.Add(CreateChild(Utils.PreviousFloat(threshold), beforeMotion));
                    children.Add(CreateChild(threshold, afterMotion));
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (rounded == before)
                {
                    Utils.Assert((int)Mathf.Round(Utils.NextFloat(threshold)) == after);
                    // in this case, .5 will go to default motion so default is .5
                    children.Add(CreateChild(threshold, beforeMotion));
                    children.Add(CreateChild(Utils.NextFloat(threshold), afterMotion));
                }
                else
                {
                    throw new InvalidOperationException("unexpected: rounding x - 0.5 is not x - 1 or x");
                }
            }

            if (states.All(x => x.value.IntValue.HasValue))
            {
                // sort increasing order
                states.Sort((x, y) => x.value.IntValue!.Value.CompareTo(y.value.IntValue!.Value));

                {
                    // first frame: add defaultMotion before first state
                    var (value, motion) = states[0];
                    AddFrames(value.IntValue!.Value - 1, defaultMotion,
                        value.IntValue.Value, motion);
                }

                for (var i = 1; i < states.Count; i++)
                {
                    // other frames: add motions if needed
                    var (prevValue, prevMotion) = states[i - 1];
                    var (currentValue, currentMotion) = states[i];

                    if (currentValue.IntValue!.Value - prevValue.IntValue!.Value > 1)
                    {
                        AddFrames(prevValue.IntValue.Value, prevMotion,
                            prevValue.IntValue.Value + 1, defaultMotion);
                        AddFrames(currentValue.IntValue.Value - 1, defaultMotion,
                            currentValue.IntValue.Value, currentMotion);
                    }
                    else if (prevMotion != currentMotion)
                    {
                        AddFrames(prevValue.IntValue.Value, prevMotion,
                            currentValue.IntValue.Value, currentMotion);
                    }
                }

                {
                    // last frame: add last state to defaultMotion
                    var (value, motion) = states[^1];
                    AddFrames(value.IntValue!.Value, motion,
                        value.IntValue.Value + 1, defaultMotion);
                }
            }
            else if (states.All(x => x.value.BoolValue.HasValue))
            {
                var (_, trueMotion) = states.SingleOrDefault(x => x.value.BoolValue is true);
                if (trueMotion == null) trueMotion = defaultMotion;
                var (_, falseMotion) = states.SingleOrDefault(x => x.value.BoolValue is false);
                if (falseMotion == null) falseMotion = defaultMotion;

                // add true motion for negative
                children.Add(CreateChild(Utils.PreviousFloat(0), trueMotion));

                // add false motion for zero
                children.Add(CreateChild(0, falseMotion));

                // add true motion for positive
                children.Add(CreateChild(Utils.NextFloat(0), trueMotion));
            }
            else
            {
                throw new InvalidOperationException("unexpected: mixed condition types");
            }

            var blendTree = new BlendTree
            {
                blendType = BlendTreeType.Simple1D,
                blendParameter = info.ParameterName,
                useAutomaticThresholds = false,
                minThreshold = children.First().threshold,
                maxThreshold = children.Last().threshold,
                name = $"EntryExit to BlendTree by AAO for {layer.name}",
                children = children.ToArray(),
            };

            var newState = new AnimatorState()
            {
                name = $"EntryExit to BlendTree by AAO for {layer.name}",
                motion = blendTree,
                // since it's 1d BlendTree, it's always be normalized wo WD off will not cause weird behavior.
                // changing to WD on will cause problem with WD off-based animators
                writeDefaultValues = info.DefaultState.writeDefaultValues,
            };

            layer.stateMachine!.states = new[]
            {
                new ChildAnimatorState()
                {
                    position = Vector3.zero,
                    state = newState,
                }
            };

            layer.stateMachine.entryTransitions = Array.Empty<AnimatorTransition>();
            layer.stateMachine.defaultState = newState;

            ChildMotion CreateChild(float value, Motion motion) => new()
            {
                motion = motion,
                timeScale = 1,
                threshold = value,
                directBlendParameter = "",
            };
        }

        public static IEnumerable<AnimatorCondition[]> FlattenConditions(AnimatorCondition[][] conditions)
        {
            var indices = new int[conditions.Length];

            while (true)
            {
                var result = new AnimatorCondition[conditions.Length];
                for (var i = 0; i < conditions.Length; i++)
                    result[i] = conditions[i][indices[i]];

                yield return result;

                for (var i = 0; i < conditions.Length; i++)
                {
                    indices[i]++;
                    if (indices[i] < conditions[i].Length) break;
                    indices[i] = 0;
                    if (i == conditions.Length - 1) yield break;
                }
            }
        }

        public static bool NeedsConversion(AnimatorCondition[] conditions, Predicate<string> shouldConvert)
        {
            foreach (var condition in conditions)
                if (shouldConvert(condition.parameter))
                    return true;
            return false;
        }

        // AnimatorCondition[and][or]
        public static AnimatorCondition[][] ConvertIntOrBoolConditionsToFloat(AnimatorCondition[] condition,
            Predicate<string> shouldConvert)
        {
            var result = new List<AnimatorCondition[]>();
            foreach (var cond in condition)
                if (shouldConvert(cond.parameter))
                    result.AddRange(ConvertIntOrBoolConditionToFloat(cond));
                else
                    result.Add(new[] { cond });
            return result.ToArray();
        }

        // AnimatorCondition[and][or]
        public static AnimatorCondition[][] ConvertIntOrBoolConditionToFloat(AnimatorCondition condition)
        {
            switch (condition.mode)
            {
                // for int
                case AnimatorConditionMode.Greater:
                {
                    var thresholdRound = Mathf.Round(condition.threshold);
                    var threshold = thresholdRound - 0.5f;
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(threshold) == thresholdRound)
                        threshold = Utils.PreviousFloat(threshold);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(threshold) == thresholdRound - 1);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(Utils.NextFloat(threshold)) == thresholdRound);

                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Greater,
                                parameter = condition.parameter,
                                threshold = threshold,
                            },
                        },
                    };
                }
                case AnimatorConditionMode.Less:
                {
                    var thresholdRound = Mathf.Round(condition.threshold);
                    var threshold = thresholdRound + 0.5f;
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(threshold) == thresholdRound)
                        threshold = Utils.NextFloat(threshold);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(threshold) == thresholdRound + 1);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(Utils.PreviousFloat(threshold)) == thresholdRound);

                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Less,
                                parameter = condition.parameter,
                                threshold = threshold,
                            },
                        },
                    };
                }
                case AnimatorConditionMode.Equals:
                {
                    var thresholdRound = Mathf.Round(condition.threshold);
                    var lowerThreshold = thresholdRound - 0.5f;
                    var upperThreshold = thresholdRound + 0.5f;

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(lowerThreshold) == thresholdRound)
                        lowerThreshold = Utils.PreviousFloat(lowerThreshold);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(upperThreshold) == thresholdRound)
                        upperThreshold = Utils.NextFloat(upperThreshold);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(lowerThreshold) == thresholdRound - 1);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(Utils.NextFloat(lowerThreshold)) == thresholdRound);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(Utils.PreviousFloat(upperThreshold)) == thresholdRound);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(upperThreshold) == thresholdRound + 1);

                    // AND condition
                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Greater,
                                parameter = condition.parameter,
                                threshold = lowerThreshold,
                            },
                        },
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Less,
                                parameter = condition.parameter,
                                threshold = upperThreshold,
                            },
                        },
                    };
                }
                case AnimatorConditionMode.NotEqual:
                {
                    var thresholdRound = Mathf.Round(condition.threshold);
                    var lowerThreshold = thresholdRound - 0.5f;
                    var upperThreshold = thresholdRound + 0.5f;

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(lowerThreshold) != thresholdRound)
                        lowerThreshold = Utils.NextFloat(lowerThreshold);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Mathf.Round(upperThreshold) != thresholdRound)
                        upperThreshold = Utils.PreviousFloat(upperThreshold);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(Utils.PreviousFloat(lowerThreshold)) == thresholdRound - 1);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(lowerThreshold) == thresholdRound);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(upperThreshold) == thresholdRound);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Utils.Assert(Mathf.Round(Utils.NextFloat(upperThreshold)) == thresholdRound + 1);

                    // OR condition
                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Less,
                                parameter = condition.parameter,
                                threshold = lowerThreshold,
                            },
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Greater,
                                parameter = condition.parameter,
                                threshold = upperThreshold,
                            },
                        },
                    };
                }

                // for bool
                case AnimatorConditionMode.If:
                {
                    // OR condition
                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Less,
                                parameter = condition.parameter,
                                threshold = 0,
                            },
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Greater,
                                parameter = condition.parameter,
                                threshold = 0,
                            },
                        },
                    };
                }
                case AnimatorConditionMode.IfNot:
                {
                    // AND condition
                    return new[]
                    {
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Greater,
                                parameter = condition.parameter,
                                threshold = Utils.PreviousFloat(0),
                            },
                        },
                        new[]
                        {
                            new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Less,
                                parameter = condition.parameter,
                                threshold = Utils.NextFloat(0),
                            },
                        },
                    };
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
