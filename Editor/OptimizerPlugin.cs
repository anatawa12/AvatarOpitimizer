using System;
using Anatawa12.AvatarOptimizer.ndmf;
using nadena.dev.ndmf;
using nadena.dev.ndmf.builtin;

[assembly: ExportsPlugin(typeof(OptimizerPlugin))]

namespace Anatawa12.AvatarOptimizer.ndmf
{
    internal class OptimizerPlugin : Plugin<OptimizerPlugin>
    {
        public override string DisplayName => $"AAO: Avatar Optimizer ({CheckForUpdate.Checker.CurrentVersionName})";

        public override string QualifiedName => "com.anatawa12.avatar-optimizer";

        protected override void Configure()
        {
            // Run early steps before EditorOnly objects are purged
            InPhase(BuildPhase.Resolving)
                .Run("Info if AAO is Out of Date", ctx =>
                {
                    // we skip check for update 
                    var components = ctx.AvatarRootObject.GetComponentInChildren<AvatarTagComponent>(true);
                    if (components && CheckForUpdate.Checker.OutOfDate && CheckForUpdate.MenuItems.CheckForUpdateEnabled)
                        BuildLog.LogInfo("CheckForUpdate:out-of-date",
                            CheckForUpdate.Checker.LatestVersionName, CheckForUpdate.Checker.CurrentVersionName);
                })
                .Then.Run(Processors.UnusedBonesByReferencesToolEarlyProcessor.Instance)
                .Then.Run("Early: MakeChildren",
                    ctx => new Processors.MakeChildrenProcessor(early: true).Process(ctx)
                )
                .BeforePass(RemoveEditorOnlyPass.Instance);

            InPhase(BuildPhase.Resolving).Run(Processors.FetchOriginalStatePass.Instance);
            ;

            // Run everything else in the optimize phase
            var mainSequence = InPhase(BuildPhase.Optimizing);
            mainSequence
                .WithRequiredExtensions(new[]
                {
                    typeof(Processors.MeshInfo2Context),
                    typeof(ObjectMappingContext),
                    typeof(DestroyTracker.ExtensionContext),
                }, seq =>
                {
                    seq.Run("Initial Step for Avatar Optimizer",
                            ctx =>
                            {
                                ctx.GetState<AAOEnabled>().Enabled =
                                    ctx.AvatarRootObject.GetComponentInChildren<AvatarTagComponent>(true);
                                // invalidate ComponentInfoRegistry cache to support newly added assets
                                AssetDescription.Reload();
                            })
                        .Then.Run("Validation", (ctx) => ComponentValidation.ValidateAll(ctx.AvatarRootObject))
                        .Then.Run(Processors.TraceAndOptimizes.LoadTraceAndOptimizeConfiguration.Instance)
                        .Then.Run(Processors.DupliacteAssets.Instance)
                        .Then.Run(Processors.ParseAnimator.Instance)
                        .Then.Run(Processors.GatherShaderMaterialInformation.Instance)
                        .Then.Run(Processors.TraceAndOptimizes.AddRemoveEmptySubMesh.Instance)
                        .Then.Run(Processors.TraceAndOptimizes.AutoFreezeBlendShape.Instance)
#if AAO_VRCSDK3_AVATARS
                        .Then.Run(Processors.ClearEndpointPositionProcessor.Instance)
                        .Then.Run(Processors.MergePhysBoneProcessor.Instance)
#endif
                        .Then.Run(Processors.EditSkinnedMeshComponentProcessor.Instance)
                        .PreviewingWith(EditModePreview.RemoveMeshByMaterialRenderFilter.Instance)
                        .PreviewingWith(EditModePreview.RemoveMeshByMaskRenderFilter.Instance)
                        .PreviewingWith(EditModePreview.RemoveMeshByBlendShapeRenderFilter.Instance)
                        .PreviewingWith(EditModePreview.RemoveMeshInBoxRenderFilter.Instance)
                        .PreviewingWith(EditModePreview.RemoveMeshByUVTileRenderFilter.Instance)
                        .Then.Run("MakeChildrenProcessor",
                            ctx => new Processors.MakeChildrenProcessor(early: false).Process(ctx)
                        )
#if AAO_VRCSDK3_AVATARS
                        .Then.Run(Processors.TraceAndOptimizes.OptimizePhysBone.Instance)
#endif
                        .Then.Run(Processors.TraceAndOptimizes.AutoMergeSkinnedMesh.Instance)
                        .Then.Run(Processors.TraceAndOptimizes.FindUnusedObjects.Instance)
                        .Then.Run(Processors.TraceAndOptimizes.ConfigureRemoveZeroSizedPolygon.Instance)
                        .Then.Run(Processors.TraceAndOptimizes.RemoveUnusedMaterialProperties.Instance)
                        .Then.Run(Processors.TraceAndOptimizes.AutoMergeBlendShape.Instance)
                        .Then.Run(Processors.MergeBoneProcessor.Instance)
                        .Then.Run(Processors.RemoveZeroSizedPolygonProcessor.Instance)
                        .Then.Run(Processors.TraceAndOptimizes.OptimizeTexture.Instance)
                        .Then.Run(Processors.AnimatorOptimizer.RemoveInvalidProperties.Instance)
                        ;
                });

            // animator optimizer is written in newer C# so requires 2021.3 or newer 
            mainSequence.Run(Processors.AnimatorOptimizer.InitializeAnimatorOptimizer.Instance)
                .Then.Run(Processors.AnimatorOptimizer.AnyStateToEntryExit.Instance)
#if AAO_VRCSDK3_AVATARS
                // EntryExit to BlendTree optimization heavily depends on VRChat's behavior
                .Then.Run(Processors.AnimatorOptimizer.EntryExitToBlendTree.Instance)
#endif
                .Then.Run(Processors.AnimatorOptimizer.MergeBlendTreeLayer.Instance)
                .Then.Run(Processors.AnimatorOptimizer.RemoveMeaninglessLayer.Instance)
                ;
        }

        protected override void OnUnhandledException(Exception e)
        {
            ErrorReport.ReportException(e);
        }
    }

    internal class AAOEnabled
    {
        public bool Enabled { get; set; }
    }
}
