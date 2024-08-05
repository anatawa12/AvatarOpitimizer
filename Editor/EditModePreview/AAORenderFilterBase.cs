using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using nadena.dev.ndmf.preview;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal abstract class AAORenderFilterBase<T> : IRenderFilter
        where T : EditSkinnedMeshComponent
    {
        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext ctx)
        {
            // currently remove meshes are only supported
            var components = ctx.GetComponentsByType<T>();

            var groups = new HashSet<RenderGroup>();

            foreach (var component in components)
            {
                if (component.GetComponent<MergeSkinnedMesh>())
                {
                    // the component applies to MergeSkinnedMesh, which is not supported for now
                    // TODO: rollup the remove operation to source renderers of MergeSkinnedMesh
                    continue;
                }

                var renderer = component.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null) continue;
                if (renderer.sharedMesh == null) continue;

                groups.Add(RenderGroup.For(renderer).WithData<T[]>(new[] { component }));
            }

            return groups.ToImmutableList();
        }

        public async Task<IRenderFilterNode> Instantiate(RenderGroup group,
            IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var pair = proxyPairs.Single();
            if (!(pair.Item1 is SkinnedMeshRenderer original)) return null;
            if (!(pair.Item2 is SkinnedMeshRenderer proxy)) return null;

            // we modify the mesh so we need to clone the mesh

            var components = group.GetData<T[]>();

            foreach (var component in components)
                context.Observe(component);

            var node = CreateNode();

            await node.Process(original, proxy, components, context);

            return node;
        }

        protected abstract AAORenderFilterNodeBase<T> CreateNode();
        protected abstract bool SupportsMultiple();
    }

    internal abstract class AAORenderFilterNodeBase<T> : IRenderFilterNode
        where T : EditSkinnedMeshComponent
    {
        private Mesh _duplicated;

        RenderAspects IRenderFilterNode.WhatChanged => RenderAspects.Mesh | RenderAspects.Shapes;

        protected abstract ValueTask Process(
            SkinnedMeshRenderer original,
            SkinnedMeshRenderer proxy,
            [NotNull] T[] components,
            Mesh duplicated,
            ComputeContext context);

        internal async ValueTask Process(
            SkinnedMeshRenderer original,
            SkinnedMeshRenderer proxy,
            [NotNull] T[] components,
            ComputeContext context)
        {
            UnityEngine.Profiling.Profiler.BeginSample($"RemoveMeshByBlendShapeRendererNode.Process({original.name})");

            var duplicated = Object.Instantiate(proxy.sharedMesh);
            duplicated.name = proxy.sharedMesh.name + " (AAO Generated)";

            await Process(original, proxy, components, duplicated, context);

            proxy.sharedMesh = duplicated;
            _duplicated = duplicated;

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void IRenderFilterNode.OnFrame(Renderer original, Renderer proxy)
        {
            if (_duplicated == null) return;
            if (proxy is SkinnedMeshRenderer skinnedMeshProxy)
                skinnedMeshProxy.sharedMesh = _duplicated;
        }

        void IDisposable.Dispose()
        {
            if (_duplicated != null)
            {
                Object.DestroyImmediate(_duplicated);
                _duplicated = null;
            }
        }
    }
}