using System;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Anatawa12.AvatarOptimizer
{
    internal class AnimationObjectMapper
    {
        readonly GameObject _rootGameObject;
        readonly BeforeGameObjectTree _beforeGameObjectTree;
        readonly ObjectMapping _objectMapping;

        private readonly Dictionary<string, MappedGameObjectInfo?> _pathsCache = new();

        public AnimationObjectMapper(GameObject rootGameObject, BeforeGameObjectTree beforeGameObjectTree,
            ObjectMapping objectMapping)
        {
            _rootGameObject = rootGameObject;
            _beforeGameObjectTree = beforeGameObjectTree;
            _objectMapping = objectMapping;
        }

        // null means nothing to map
        private MappedGameObjectInfo? GetGameObjectInfo(string path)
        {
            if (_pathsCache.TryGetValue(path, out var info)) return info;

            var tree = _beforeGameObjectTree.ResolvePath(path);

            if (tree == null)
            {
                info = null;
            }
            else
            {
                var foundGameObject = EditorUtility.InstanceIDToObject(tree.InstanceId) as GameObject;
                var newPath = foundGameObject != null
                    ? Utils.RelativePath(_rootGameObject.transform, foundGameObject.transform)
                    : null;

                info = new MappedGameObjectInfo(_objectMapping, newPath, tree);
            }

            _pathsCache.Add(path, info);
            return info;
        }

        class MappedGameObjectInfo
        {
            private ObjectMapping _objectMapping;

            readonly BeforeGameObjectTree _tree;

            // null means removed gameObject
            public readonly string? NewPath;

            public MappedGameObjectInfo(ObjectMapping objectMapping, string? newPath, BeforeGameObjectTree tree)
            {
                _objectMapping = objectMapping;
                NewPath = newPath;
                _tree = tree;
            }

            public (int instanceId, ComponentInfo?) GetComponentByType(Type type)
            {
                if (!_tree.ComponentInstanceIdByType.TryGetValue(type, out var instanceId))
                    return (instanceId, null); // Nothing to map
                return (instanceId, _objectMapping.GetComponentMapping(instanceId));
            }
        }

        public string? MapPath(string srcPath, Type type)
        {
            var gameObjectInfo = GetGameObjectInfo(srcPath);
            if (gameObjectInfo == null)
                return null; // this means the original GameObject does not exists
            var (instanceId, componentInfo) = gameObjectInfo.GetComponentByType(type);

            if (componentInfo != null)
            {
                var component = new ComponentOrGameObject(EditorUtility.InstanceIDToObject(componentInfo.MergedInto));
                // there's mapping about component.
                // this means the component is merged or some prop has mapping
                if (!component) return null; // this means removed.

                var newPath = Utils.RelativePath(_rootGameObject.transform, component.transform);
                if (newPath == null) return null; // this means moved to out of the animator scope

                return newPath;
            }
            else
            {
                // The component is not merged & no prop mapping so process GameObject mapping
                var component = EditorUtility.InstanceIDToObject(instanceId);
                if (!component) return null; // this means removed

                if (gameObjectInfo.NewPath == null) return null;
                return gameObjectInfo.NewPath;
            }
        }

        private Dictionary<(string, Type, string), (string, Type, string)[]?> _bindingCache = new();

        public (string path, Type type, string propertyName)[]? MapBinding(string path, Type type, string propertyName)
        {
            if (_bindingCache.TryGetValue((path, type, propertyName), out var result))
                return result;
            Profiler.BeginSample("MapBindingImpl");
            result = MapBindingImpl(path, type, propertyName);
            Profiler.EndSample();
            _bindingCache.Add((path, type, propertyName), result);
            return result;
        }
        (string path, Type type, string propertyName)[]? MapBindingImpl(string path, Type type, string propertyName)
        {
            var gameObjectInfo = GetGameObjectInfo(path);
            if (gameObjectInfo == null)
                return Array.Empty<(string, Type, string)>(); // this means the original GameObject does not exists
            var (instanceId, componentInfo) = gameObjectInfo.GetComponentByType(type);

            if (componentInfo != null)
            {
                // there's mapping about component.
                // this means the component is merged or some prop has mapping

                if (componentInfo.PropertyMapping.TryGetValue(propertyName, out var newProp))
                {
                    // if mapped one is exactly same as original, return null
                    if (newProp.AllCopiedTo.Length == 1
                        && gameObjectInfo.NewPath == path
                        && newProp.AllCopiedTo[0].InstanceId == instanceId
                        && newProp.AllCopiedTo[0].Name == propertyName)
                        return null;

                    // there are mapping for property
                    var mappedBindings = new (string path, Type type, string propertyName)[newProp.AllCopiedTo.Length];
                    var copiedToIndex = 0;
                    foreach (var descriptor in newProp.AllCopiedTo)
                    {
                        var component =
                            new ComponentOrGameObject(EditorUtility.InstanceIDToObject(descriptor.InstanceId));
                        // this means removed.
                        if (!component)
                            continue;

                        var newPath = Utils.RelativePath(_rootGameObject.transform, component.transform);

                        // this means moved to out of the animator scope
                        // TODO: add warning
                        if (newPath == null) return Array.Empty<(string path, Type type, string propertyName)>();

                        var binding = (newPath, descriptor.Type, descriptor.Name);

                        // For Animator component, to toggle `m_Enabled` as a property of `Behavior`,
                        //   we need to toggle `Behaviour.m_Enabled`.
                        if (descriptor.Name == VProp.AnimatorEnabledAsBehavior)
                            binding = (newPath, typeof(Behaviour), "m_Enabled");
                        mappedBindings[copiedToIndex++] = binding;
                    }

                    if (copiedToIndex != mappedBindings.Length)
                        return mappedBindings.AsSpan().Slice(0, copiedToIndex).ToArray();
                    return mappedBindings;
                }
                else
                {
                    var component =
                        new ComponentOrGameObject(EditorUtility.InstanceIDToObject(componentInfo.MergedInto));
                    if (!component)
                        return Array.Empty<(string path, Type type, string propertyName)>(); // this means removed.

                    var newPath = Utils.RelativePath(_rootGameObject.transform, component.transform);
                    if (newPath == null)
                        return Array
                            .Empty<(string path, Type type, string propertyName
                                )>(); // this means moved to out of the animator scope
                    if (path == newPath) return null;
                    return new[] { (newPath, type, propertyName) };
                }
            }
            else
            {
                // The component is not merged & no prop mapping so process GameObject mapping

                var component = EditorUtility.InstanceIDToObject(instanceId);
                if (!component)
                {
#if AAO_VRCSDK3_AVATARS
                    // See https://github.com/anatawa12/AvatarOptimizer/issues/1330
                    // Some flying avatar gimmicks use VRCStation to make the Collider for flying.
                    // (Box Collider is not whitelisted for quest avatars)
                    // Therefore, we need to keep animation bindings for BoxCollider (and Collider) if 
                    // the GameObject has VRCStation component.
                    if (type == typeof(BoxCollider) || type == typeof(Collider))
                    {
                        var (stationInstanceId, _) = gameObjectInfo.GetComponentByType(typeof(VRC.SDK3.Avatars.Components.VRCStation));
                        if (EditorUtility.InstanceIDToObject(stationInstanceId) != null)
                        {
                            goto componentLive;
                        }
                    }
#endif

                    return Array.Empty<(string path, Type type, string propertyName)>(); // this means removed
                }
                componentLive:;

                if (gameObjectInfo.NewPath == null) return Array.Empty<(string path, Type type, string propertyName)>();
                if (path == gameObjectInfo.NewPath) return null;
                return new[] { (gameObjectInfo.NewPath, type, propertyName) };
            }
        }

        public bool TryGetMappedVrmFirstPersonFlag(string path, out VrmFirstPersonFlag vrmFirstPersonFlag)
        {
            vrmFirstPersonFlag = default;
            var gameObjectInfo = GetGameObjectInfo(path);
            if (gameObjectInfo == null) return false;
            var (_, componentInfo) = gameObjectInfo.GetComponentByType(typeof(Renderer));
            if (componentInfo?.VrmFirstPersonFlag is not { } firstPersonFlag) return false;
            vrmFirstPersonFlag = firstPersonFlag;
            return true;
        }
    }
}
