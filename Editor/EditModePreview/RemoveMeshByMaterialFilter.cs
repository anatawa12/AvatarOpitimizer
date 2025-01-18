using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf;
using nadena.dev.ndmf.preview;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal class RemoveMeshByMaterialRenderFilter : AAORenderFilterBase<RemoveMeshByMaterial>
    {
        private RemoveMeshByMaterialRenderFilter() : base("Remove Mesh by Material", "remove-mesh-by-material")
        {
        }

        public static RemoveMeshByMaterialRenderFilter Instance { get; } = new();
        protected override AAORenderFilterNodeBase<RemoveMeshByMaterial> CreateNode() => new RemoveMeshByMaterialRendererNode();
        protected override bool SupportsMultiple() => false;
    }

    internal class RemoveMeshByMaterialRendererNode : AAORenderFilterNodeBase<RemoveMeshByMaterial>
    {
        protected override ValueTask Process(
            SkinnedMeshRenderer original,
            SkinnedMeshRenderer proxy,
            RemoveMeshByMaterial[] components,
            Mesh duplicated, 
            ComputeContext context
        )
        {
            HashSet<ObjectReference> toRemove = new();
            foreach (var component in components)
            {
                var materials = context.Observe(component, c => c.Materials.ToImmutableList());
                foreach (var material in materials)
                {
                    toRemove.Add(ObjectRegistry.GetReference(material));
                }
            }

            var actualMaterials = proxy.sharedMaterials ?? Array.Empty<Material>();
            for (int i = 0; i < actualMaterials.Length; i++)
            {
                var mat = actualMaterials[i];
                if (mat == null || !toRemove.Contains(ObjectRegistry.GetReference(mat))) continue;

                var submesh = duplicated.GetSubMesh(i);
                submesh.indexCount = 0;
                duplicated.SetSubMesh(i, submesh);
            }

            return default;
        }
    }
}
