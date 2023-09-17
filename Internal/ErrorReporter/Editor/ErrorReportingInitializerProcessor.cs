using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;
#if !NADEMOFU
using Anatawa12.ApplyOnPlay;
#endif

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    /// <summary>
    /// Initializes Error Reporting System
    /// </summary>
    internal class ErrorReportingInitializerProcessor : IVRCSDKPreprocessAvatarCallback
#if !NADEMOFU
        , IApplyOnPlayCallback, IManualBakeFinalizer
#endif
    {
        public int callbackOrder => int.MinValue;

        public string CallbackName => "Error Reporting Initialization";
        public string CallbackId => "com.anatawa12.error-reporting";

#if !NADEMOFU
        public bool ApplyOnPlay(GameObject avatarGameObject, ApplyReason reason) => OnPreprocessAvatar(avatarGameObject);
#endif

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            BuildReport.Clear();
            BuildReport.CurrentReport.Initialize(avatarGameObject.GetComponent<VRCAvatarDescriptor>());
            return true;
        }

        public void FinalizeManualBake(GameObject original, GameObject cloned)
        {
            var originalBasePath = GlobalPath(original);
            var clonedBasePath = GlobalPath(cloned);

            BuildReport.RemapPaths(originalBasePath, clonedBasePath);
        }

        private static string GlobalPath(GameObject child)
        {
            if (null == child) return "";

            var pathSegments = new List<string>();
            while (child != null)
            {
                pathSegments.Add(child.name);
                child = child.transform.parent != null ? child.transform.parent.gameObject : null;
            }

            pathSegments.Reverse();
            return string.Join("/", pathSegments);
        }
    }
}
