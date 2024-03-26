using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.AnimatorParsersV2;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    // The class describes the location of the animation curve
    internal sealed class AnimationLocation : IErrorContext
    {
        [NotNull] public Animator Component { get; }
        public int PlayableLayerIndex { get; }
        public int AnimationLayerIndex { get; }
        [NotNull] public AnimatorState AnimatorState { get; }
        [NotNull] public int[] BlendTreeLocation { get; }
        [NotNull] public AnimationCurve Curve { get; }
        [NotNull] public AnimationClip Clip { get; set; }

        public AnimationLocation([NotNull] Animator component, int playableLayerIndex, int animationLayerIndex,
            [NotNull] AnimatorState state, int[] blendTreeLocation, [NotNull] AnimationCurve curve,
            [NotNull] AnimationClip clip)
        {
            if (!component) throw new ArgumentNullException(nameof(component));
            if (!state) throw new ArgumentNullException(nameof(state));

            Component = component;
            PlayableLayerIndex = playableLayerIndex;
            AnimationLayerIndex = animationLayerIndex;
            AnimatorState = state;
            BlendTreeLocation = blendTreeLocation ?? Array.Empty<int>();
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            Clip = clip;
        }

        public static IEnumerable<AnimationLocation> CollectActivenessAnimationLocations(
            BuildContext context, Component component)
        {
            {
                if (context.GetAnimationComponent(component).TryGetFloat("m_Enabled", out var p))
                    foreach (var location in CollectAnimationLocation(p))
                        yield return location;
            }
            foreach (var transform in component.transform.ParentEnumerable(context.AvatarRootTransform, includeMe: true))
                if (context.GetAnimationComponent(transform.gameObject).TryGetFloat("m_IsActive", out var p))
                    foreach (var location in CollectAnimationLocation(p))
                        yield return location;
        }

        public static IEnumerable<AnimationLocation> CollectAnimationLocation(RootPropModNode<float> node)
        {
            foreach (var animatorNode in node.ComponentNodes.OfType<AnimatorPropModNode<float>>())
            foreach (var playableNodeInfo in animatorNode.LayersReversed.WhileApplied())
            foreach (var animatorNodeInfo in playableNodeInfo.Node.LayersReversed.WhileApplied())
            foreach (var animatorStateNode in animatorNodeInfo.Node.Children)
            foreach (var animationLocation in CollectAnimationLocation(
                         animatorNode.Component, playableNodeInfo.LayerIndex, animatorNodeInfo.LayerIndex,
                         animatorStateNode.State, animatorStateNode.Node))
                yield return animationLocation;
        }

        public static IEnumerable<AnimationLocation> CollectAnimationLocation(Animator animator, int playableLayer,
            int animatorLayer,
            AnimatorState state, ImmutablePropModNode<float> node)
        {
            // fast path
            if (node is FloatAnimationCurveNode floatNode)
                return new[]
                {
                    new AnimationLocation(animator, playableLayer, animatorLayer,
                        state, null, floatNode.Curve, floatNode.Clip)
                };
            return CollectAnimationLocationSlow(animator, playableLayer, animatorLayer, state, node);
        }

        private static IEnumerable<AnimationLocation> CollectAnimationLocationSlow(Animator animator,
            int playableLayer, int animatorLayer, AnimatorState state, ImmutablePropModNode<float> node)
        {
            // slow path: recursively collect blend tree
            var queue = new Queue<(BlendTreeNode<float>, int[])>();
            queue.Enqueue(((BlendTreeNode<float>)node, Array.Empty<int>()));

            while (queue.Count != 0)
            {
                var (blendTree, location) = queue.Dequeue();

                for (var i = 0; i < blendTree.Children.Count; i++)
                {
                    var newLocation = location;
                    ArrayUtility.Add(ref newLocation, i);
                    switch (blendTree.Children[i])
                    {
                        case FloatAnimationCurveNode floatNode:
                            yield return new AnimationLocation(animator, playableLayer, animatorLayer, state,
                                newLocation, floatNode.Curve, floatNode.Clip);
                            break;
                        case BlendTreeNode<float> childBlendTree:
                            queue.Enqueue((childBlendTree, newLocation));
                            break;
                        default:
                            throw new InvalidOperationException(
                                "Unexpected node type: " +
                                blendTree.Children[i].GetType().FullName);
                    }
                }
            }
        }

        private bool Equals(AnimationLocation other) =>
            Equals(Component, other.Component) && PlayableLayerIndex == other.PlayableLayerIndex &&
            AnimationLayerIndex == other.AnimationLayerIndex && Equals(AnimatorState, other.AnimatorState) &&
            BlendTreeLocation.SequenceEqual(other.BlendTreeLocation) && Equals(Curve, other.Curve);

        public override bool Equals(object obj) =>
            ReferenceEquals(this, obj) || obj is AnimationLocation other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Component.GetHashCode();
                hashCode = (hashCode * 397) ^ PlayableLayerIndex;
                hashCode = (hashCode * 397) ^ AnimationLayerIndex;
                hashCode = (hashCode * 397) ^ AnimatorState.GetHashCode();
                hashCode = (hashCode * 397) ^ BlendTreeLocation.Length;
                foreach (var location in BlendTreeLocation)
                    hashCode = (hashCode * 397) ^ location;
                hashCode = (hashCode * 397) ^ Curve.GetHashCode2();
                return hashCode;
            }
        }

        public IEnumerable<ObjectReference> ContextReferences => new[]
            { ObjectRegistry.GetReference(Component), ObjectRegistry.GetReference(Clip) };
    }
}
