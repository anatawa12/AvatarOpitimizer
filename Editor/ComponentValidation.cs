using Anatawa12.AvatarOptimizer.Processors;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal static class ComponentValidation
    {
        public static void ValidateAll(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<AvatarTagComponent>(true))
            {
                switch (component)
                {
                    case MergePhysBone mergePhysBone:
                    {
                        MergePhysBoneValidator.Validate(mergePhysBone);
                        break;
                    }
                    case MergeSkinnedMesh mergeSkinnedMesh:
                    {
                        var smr = mergeSkinnedMesh.GetComponent<SkinnedMeshRenderer>();
                        if (smr.sharedMesh)
                            BuildLog.LogWarning("MergeSkinnedMesh:warning:MeshIsNotNone");

                        if (mergeSkinnedMesh.renderersSet.GetAsSet().Contains(smr))
                            BuildLog.LogError("MergeSkinnedMesh:validation:self-recursive");
                        break;
                    }
                    case MergeBone mergeBone:
                    {
                        MergeBoneProcessor.Validate(mergeBone, root);
                        break;
                    }
                    case AvatarGlobalComponent _:
                    {
                        if (component.transform != root.transform)
#if AAO_VRCSDK3_AVATARS
                            BuildLog.LogError("AvatarGlobalComponent:NotOnAvatarDescriptor");
#else
                            BuildReport.LogError("AvatarGlobalComponent:NotOnAvatarRoot");
#endif
                        break;
                    }
                }
            }
        }
    }
}