#if AAO_VRM0

using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using UnityEngine;
using VRM;

namespace Anatawa12.AvatarOptimizer.APIInternal
{

    // NOTE: VRM0 bones are not animated, therefore no need to configure ComponentDependencyInfo

    [ComponentInformation(typeof(VRMMeta))]
    internal class VRMMetaInformation : ComponentInformation<VRMMeta>
    {
        protected override void CollectDependency(VRMMeta component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
            if (component.TryGetComponent<Animator>(out var animator)) collector.AddDependency(animator);
        }
    }

    [ComponentInformation(typeof(VRMSpringBone))]
    internal class VRMSpringBoneInformation : ComponentInformation<VRMSpringBone>
    {
        protected override void CollectDependency(VRMSpringBone component, ComponentDependencyCollector collector)
        {
            collector.MarkHeavyBehaviour();
            foreach (var rootBone in component.RootBones)
            {
                foreach (var transform in rootBone.GetComponentsInChildren<Transform>())
                {
                    collector.AddDependency(transform, component);
                    collector.AddDependency(transform);
                }
            }

            foreach (var collider in component.ColliderGroups) collector.AddDependency(collider);
        }

        protected override void CollectMutations(VRMSpringBone component, ComponentMutationsCollector collector)
        {
            foreach (var transform in component.GetComponentsInChildren<Transform>())
                collector.TransformPositionAndRotation(transform);
        }
    }

    [ComponentInformation(typeof(VRMSpringBoneColliderGroup))]
    internal class VRMSpringBoneColliderGroupInformation : ComponentInformation<VRMSpringBoneColliderGroup>
    {
        protected override void CollectDependency(VRMSpringBoneColliderGroup component,
            ComponentDependencyCollector collector)
        {
        }
    }

    [ComponentInformation(typeof(VRMBlendShapeProxy))]
    internal class VRMBlendShapeProxyInformation : ComponentInformation<VRMBlendShapeProxy>
    {
        protected override void CollectDependency(VRMBlendShapeProxy component, ComponentDependencyCollector collector)
        {
            if (component.BlendShapeAvatar) collector.MarkEntrypoint();
        }

        // BlendShape / Material mutations are collected through AnimatorParser, once we start tracking material changes
    }

    [ComponentInformation(typeof(VRMLookAtHead))]
    internal class VRMLookAtHeadInformation : ComponentInformation<VRMLookAtHead>
    {
        protected override void CollectDependency(VRMLookAtHead component, ComponentDependencyCollector collector)
        {
            collector.MarkHeavyBehaviour();
            collector.AddDependency(component.Head, component);
            collector.AddDependency(component.Head);
        }
    }

    [ComponentInformation(typeof(VRMLookAtBoneApplyer))]
    internal class VRMLookAtBoneApplyerInformation : ComponentInformation<VRMLookAtBoneApplyer>
    {
        protected override void CollectDependency(VRMLookAtBoneApplyer component, ComponentDependencyCollector collector)
        {
            collector.MarkHeavyBehaviour();
            collector.AddDependency(component.GetComponent<VRMLookAtHead>());
            collector.AddDependency(component.LeftEye.Transform);
            collector.AddDependency(component.RightEye.Transform);
        }

        protected override void CollectMutations(VRMLookAtBoneApplyer component, ComponentMutationsCollector collector)
        {
            collector.TransformRotation(component.LeftEye.Transform);
            collector.TransformRotation(component.RightEye.Transform);
        }
    }


    [ComponentInformation(typeof(VRMLookAtBlendShapeApplyer))]
    internal class VRMLookAtBlendShapeApplyerInformation : ComponentInformation<VRMLookAtBlendShapeApplyer>
    {
        protected override void CollectDependency(VRMLookAtBlendShapeApplyer component, ComponentDependencyCollector collector)
        {
            collector.MarkHeavyBehaviour();
            collector.AddDependency(component.GetComponent<VRMLookAtHead>());
        }

    }


    [ComponentInformation(typeof(VRMFirstPerson))]
    internal class VRMFirstPersonInformation : ComponentInformation<VRMFirstPerson>
    {
        protected override void CollectDependency(VRMFirstPerson component, ComponentDependencyCollector collector)
        {
            collector.MarkHeavyBehaviour();
            collector.AddDependency(component.FirstPersonBone, component);
            collector.AddDependency(component.FirstPersonBone);
        }
        
        protected override void ApplySpecialMapping(VRMFirstPerson component, MappingSource mappingSource)
        {
            component.Renderers = component.Renderers
                .Select(r => new VRMFirstPerson.RendererFirstPersonFlags
                {
                    Renderer = mappingSource.GetMappedComponent(r.Renderer).MappedComponent,
                    FirstPersonFlag = r.FirstPersonFlag
                })
                .Where(r => r.Renderer)
                .GroupBy(r => r.Renderer, r => r.FirstPersonFlag)
                .Select(grouping =>
                {
                    FirstPersonFlag mergedFirstPersonFlag;
                    var firstPersonFlags = grouping.Distinct().ToArray();
                    if (firstPersonFlags.Length == 1)
                    {
                        mergedFirstPersonFlag = firstPersonFlags[0];
                    }
                    else
                    {
                        mergedFirstPersonFlag = firstPersonFlags.Contains(FirstPersonFlag.Both) ? FirstPersonFlag.Both : FirstPersonFlag.Auto;
                        BuildLog.LogWarning("MergeSkinnedMesh:warning:VRM:FirstPersonFlagsMismatch", mergedFirstPersonFlag.ToString());
                    }

                    return new VRMFirstPerson.RendererFirstPersonFlags
                    {
                        Renderer = grouping.Key,
                        FirstPersonFlag = mergedFirstPersonFlag
                    };
                }).ToList();
        }
    }
}

#endif
