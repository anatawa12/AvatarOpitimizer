using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes;

internal class RemoveMeshByMaterialProcessor : EditSkinnedMeshProcessor<RemoveMeshByMaterial>
{
    public RemoveMeshByMaterialProcessor(RemoveMeshByMaterial component) : base(component)
    {
    }

    public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.RemovingMesh;
    public override void Process(BuildContext context, MeshInfo2 target)
    {
        var submeshes = target.SubMeshes;
        var toDeleteMats = new HashSet<ObjectReference>(Component.Materials.Select(m => ObjectRegistry.GetReference(m)));
        Renderer renderer = Component.GetComponent<Renderer>();
        var actualMats = renderer.sharedMaterials ?? Array.Empty<Material>();

        for (int i = 0; i < submeshes.Count; i++)
        {
            if (i < actualMats.Length && toDeleteMats.Contains(ObjectRegistry.GetReference(actualMats[i])))
            {
                submeshes[i].RemovePrimitives("RemoveMeshByMaterial", v => true);
            }
        }
        
        target.RemoveUnusedVertices();
    }

    public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => upstream;
}
