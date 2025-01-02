using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

#if AAO_VRM0
using VRM;
#endif

#if AAO_VRM1
using UniVRM10;
using UniGLTF.Extensions.VRMC_vrm;
#endif

namespace Anatawa12.AvatarOptimizer
{
    /// <summary>
    /// The class manages Object location mapping
    /// </summary>
    internal class ObjectMappingBuilder<TPropInfo>
        where TPropInfo : struct, IPropertyInfo<TPropInfo>
    {
        // Responsibility of this class can be classified to the following parts
        // - Track moving GameObjects
        // - Track renaming Properties in Component
        // - Track merging Components
        // - Track VrmFirstPersonFlags

        private readonly IReadOnlyDictionary<int, BeforeGameObjectTree> _beforeGameObjectInfos;

        // key: instanceId
        private readonly Dictionary<int, BuildingComponentInfo> _originalComponentInfos = new();
        private readonly Dictionary<int, BuildingComponentInfo> _componentInfos = new();

        public ObjectMappingBuilder(GameObject rootObject)
        {
            if (!rootObject) throw new ArgumentNullException(nameof(rootObject));
            var transforms = rootObject.GetComponentsInChildren<Transform>(true);

            _beforeGameObjectInfos = transforms
                .ToDictionary(t => t.gameObject.GetInstanceID(), t => new BeforeGameObjectTree(t.gameObject));

            foreach (var transform in transforms)
            {
                if (!transform.parent) continue;
                if (!_beforeGameObjectInfos.TryGetValue(transform.parent.gameObject.GetInstanceID(),
                        out var parentInfo)) continue;
                var selfInfo = _beforeGameObjectInfos[transform.gameObject.GetInstanceID()];
                parentInfo.Children[transform.GetSiblingIndex()] = selfInfo;
            }

            _beforeGameObjectInfos[rootObject.GetInstanceID()].InitializeRecursive();
            
#if AAO_VRM0
            if (rootObject.TryGetComponent<VRMFirstPerson>(out var firstPerson))
            {
                foreach (var renderer in firstPerson.Renderers)
                {
                    GetComponentInfo(renderer.Renderer).VrmFirstPersonFlag = renderer.FirstPersonFlag switch
                    {
                        FirstPersonFlag.Auto => VrmFirstPersonFlag.Auto,
                        FirstPersonFlag.Both => VrmFirstPersonFlag.Both,
                        FirstPersonFlag.ThirdPersonOnly => VrmFirstPersonFlag.ThirdPersonOnly,
                        FirstPersonFlag.FirstPersonOnly => VrmFirstPersonFlag.FirstPersonOnly,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
            }
#endif

#if AAO_VRM1
            if (rootObject.TryGetComponent<Vrm10Instance>(out var vrm10Instance))
            {
                foreach (var renderer in vrm10Instance.Vrm.FirstPerson.Renderers)
                {
                    GetComponentInfo(renderer.GetRenderer(rootObject.transform)).VrmFirstPersonFlag = renderer.FirstPersonFlag switch
                    {
                        FirstPersonType.auto => VrmFirstPersonFlag.Auto,
                        FirstPersonType.both => VrmFirstPersonFlag.Both,
                        FirstPersonType.thirdPersonOnly => VrmFirstPersonFlag.ThirdPersonOnly,
                        FirstPersonType.firstPersonOnly => VrmFirstPersonFlag.FirstPersonOnly,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
            }
#endif
        }

        public void RecordMergeComponent<T>(T from, T mergeTo) where T: Component
        {
            if (!_componentInfos.TryGetValue(mergeTo.GetInstanceID(), out var mergeToInfo))
            {
                var newMergeToInfo = new BuildingComponentInfo(mergeTo);
                _originalComponentInfos.Add(mergeTo.GetInstanceID(), newMergeToInfo);
                _componentInfos.Add(mergeTo.GetInstanceID(), newMergeToInfo);
                GetComponentInfo(from).MergedTo(newMergeToInfo);
            }
            else
            {
                var newMergeToInfo = new BuildingComponentInfo(mergeTo);
                _componentInfos[mergeTo.GetInstanceID()]= newMergeToInfo;
                mergeToInfo.MergedTo(newMergeToInfo);
                GetComponentInfo(from).MergedTo(newMergeToInfo);
            }
        }

        static VrmFirstPersonFlag? MergeVrmFirstPersonFlags(VrmFirstPersonFlag? from, VrmFirstPersonFlag? to)
        {
            switch (from, to)
            {
                case (null, var other): return other;
                case (var other, null): return other; 
                case ({ } fromFlag, { } toFlag) when fromFlag == toFlag: return fromFlag;
                case ({ } fromFlag, { } toFlag):
                {
                    var mergedFirstPersonFlag = fromFlag == VrmFirstPersonFlag.Both || toFlag == VrmFirstPersonFlag.Both
                        ? VrmFirstPersonFlag.Both : VrmFirstPersonFlag.Auto; 
                    BuildLog.LogWarning("MergeSkinnedMesh:warning:VRM:FirstPersonFlagsMismatch", mergedFirstPersonFlag.ToString());
                    return mergedFirstPersonFlag;
                }
            }
        }

        public void RecordMoveProperties(ComponentOrGameObject from, params (string old, string @new)[] props) =>
            GetComponentInfo(from).MoveProperties(props);

        public void RecordMoveProperty(ComponentOrGameObject from, string oldProp, string newProp) =>
            GetComponentInfo(from).MoveProperties((oldProp, newProp));

        public void RecordMoveProperty(ComponentOrGameObject fromComponent, string oldProp, ComponentOrGameObject toComponent, string newProp) =>
            GetComponentInfo(fromComponent).MoveProperty(GetComponentInfo(toComponent), oldProp, newProp);

        public void RecordCopyProperty(ComponentOrGameObject fromComponent, string oldProp, ComponentOrGameObject toComponent, string newProp) =>
            GetComponentInfo(fromComponent).CopyProperty(GetComponentInfo(toComponent), oldProp, newProp);

        public void RecordRemoveProperty(ComponentOrGameObject from, string oldProp) =>
            GetComponentInfo(from).RemoveProperty(oldProp);

        public AnimationComponentInfo<TPropInfo> GetAnimationComponent(ComponentOrGameObject component)
            => GetComponentInfo(component);

        public IEnumerable<AnimationComponentInfo<TPropInfo>> GetAllAnimationComponents() =>
            _componentInfos.Values.Where(x => !x.IsMerged);

        public VrmFirstPersonFlag? GetVrmFirstPersonFlag(ComponentOrGameObject component)
            => _componentInfos.TryGetValue(component.GetInstanceID(), out var info) ? info.VrmFirstPersonFlag : null;

        private BuildingComponentInfo GetComponentInfo(ComponentOrGameObject component)
        {
            if (!_componentInfos.TryGetValue(component.GetInstanceID(), out var info))
            {
                info = new BuildingComponentInfo(component);
                _originalComponentInfos.Add(component.GetInstanceID(), info);
                _componentInfos.Add(component.GetInstanceID(), info);
            }
            return info;
        }

        public ObjectMapping BuildObjectMapping()
        {
            return new ObjectMapping(
                _beforeGameObjectInfos, 
                _originalComponentInfos.ToDictionary(p => p.Key, p => p.Value.Build()));
        }

        class AnimationPropertyInfo
        {
            public readonly BuildingComponentInfo? Component;
            public readonly string? Name;
            public AnimationPropertyInfo? MergedTo { get; private set; }
            private MappedPropertyInfo? _mappedPropertyInfo;

            public TPropInfo PropertyInfo;

            public List<AnimationPropertyInfo>? CopiedTo { get; private set; }

            public AnimationPropertyInfo(BuildingComponentInfo component, string name)
            {
                Component = component ?? throw new ArgumentNullException(nameof(component));
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            private AnimationPropertyInfo()
            {
            }

            // TODO: split type for removed marker?
            // If we do that, we can remove nullabile from Component and Name
            public static readonly AnimationPropertyInfo RemovedMarker = new();

            public void MergeTo(AnimationPropertyInfo property)
            {
                MergedTo = property;
                PropertyInfo.MergeTo(ref property.PropertyInfo);
            }

            public void CopyTo(AnimationPropertyInfo property)
            {
                if (CopiedTo == null)
                    CopiedTo = new List<AnimationPropertyInfo>();
                CopiedTo.Add(property);
                PropertyInfo.CopyTo(ref property.PropertyInfo);
            }

            public MappedPropertyInfo GetMappedInfo()
            {
                if (_mappedPropertyInfo is { } property) return property;
                property = ComputeMappedInfo();
                _mappedPropertyInfo = property;
                return property;
            }

            private MappedPropertyInfo ComputeMappedInfo()
            {
                if (this == RemovedMarker) return MappedPropertyInfo.Removed;

                if (Component == null) throw new InvalidOperationException("Component is null");

                if (MergedTo != null)
                {
                    var merged = MergedTo.GetMappedInfo();

                    if (CopiedTo == null || CopiedTo.Count == 0)
                        return merged;

                    var copied = new List<PropertyDescriptor>();
                    copied.AddRange(merged.AllCopiedTo);
                    foreach (var copiedTo in CopiedTo)
                        copied.AddRange(copiedTo.GetMappedInfo().AllCopiedTo);
                    
                    return new MappedPropertyInfo(merged.MappedProperty, copied.ToArray());
                }
                else
                {
                    // this is edge
                    if (CopiedTo == null || CopiedTo.Count == 0)
                        return new MappedPropertyInfo(Component.InstanceId, Component.Type, Name!);

                    var descriptor = new PropertyDescriptor(Component.InstanceId, Component.Type, Name!);

                    var copied = new List<PropertyDescriptor> { descriptor };
                    foreach (var copiedTo in CopiedTo)
                        copied.AddRange(copiedTo.GetMappedInfo().AllCopiedTo);

                    return new MappedPropertyInfo(descriptor, copied.ToArray());
                }
            }
        }

        class BuildingComponentInfo : AnimationComponentInfo<TPropInfo>
        {
            internal readonly int InstanceId;
            internal readonly Type Type;

            // id in this -> id in merged
            private BuildingComponentInfo? _mergedInto;

            private readonly Dictionary<string, AnimationPropertyInfo> _beforePropertyIds = new();

            private readonly Dictionary<string, AnimationPropertyInfo> _afterPropertyIds = new();

            internal VrmFirstPersonFlag? VrmFirstPersonFlag;

            public BuildingComponentInfo(ComponentOrGameObject component)
            {
                InstanceId = component.GetInstanceID();
                Type = component.Value.GetType();
            }

            internal bool IsMerged => _mergedInto != null;

            private AnimationPropertyInfo GetProperty(string name, bool remove = false)
            {
                if (_afterPropertyIds.TryGetValue(name, out var prop))
                {
                    if (remove) _afterPropertyIds.Remove(name);
                    return prop;
                }
                else
                {
                    var newProp = new AnimationPropertyInfo(this, name);
                    if (!remove) _afterPropertyIds.Add(name, newProp);
                    if (!_beforePropertyIds.ContainsKey(name))
                        _beforePropertyIds.Add(name, newProp);
                    return newProp;
                }
            }

            public void MergedTo(BuildingComponentInfo mergeTo)
            {
                if (Type == typeof(Transform)) throw new Exception("Merging Transform is not supported!");
                if (_mergedInto != null) throw new InvalidOperationException("Already merged");
                _mergedInto = mergeTo ?? throw new ArgumentNullException(nameof(mergeTo));
                foreach (var property in _afterPropertyIds.Values)
                    property.MergeTo(mergeTo.GetProperty(property.Name!));
                _afterPropertyIds.Clear();

                mergeTo.VrmFirstPersonFlag = MergeVrmFirstPersonFlags(VrmFirstPersonFlag, mergeTo.VrmFirstPersonFlag);
            }

            public void MoveProperties(params (string old, string @new)[] props)
            {
                if (Type == typeof(Transform)) throw new Exception("Move properties of Transform is not supported!");
                if (_mergedInto != null) throw new Exception("Already Merged");

                var propertyIds = new AnimationPropertyInfo[props.Length];
                for (var i = 0; i < props.Length; i++)
                    propertyIds[i] = GetProperty(props[i].old, remove: true);

                for (var i = 0; i < propertyIds.Length; i++)
                    propertyIds[i].MergeTo(GetProperty(props[i].@new));
            }

            public void MoveProperty(BuildingComponentInfo toComponent, string oldProp, string newProp)
            {
                if (Type == typeof(Transform)) throw new Exception("Move properties of Transform is not supported!");
                GetProperty(oldProp, remove: true).MergeTo(toComponent.GetProperty(newProp));
            }

            public void CopyProperty(BuildingComponentInfo toComponent, string oldProp, string newProp)
            {
                var prop = GetProperty(oldProp);
                prop.CopyTo(toComponent.GetProperty(newProp));
            }

            public override void RemoveProperty(string property)
            {
                if (Type == typeof(Transform)) throw new Exception("Removing properties of Transform is not supported!");
                if (_mergedInto != null) throw new Exception("Already Merged");

                GetProperty(property, remove: true).MergeTo(AnimationPropertyInfo.RemovedMarker);
            }

            public ComponentInfo Build()
            {
                var propertyMapping = _beforePropertyIds.ToDictionary(p => p.Key,
                    p => p.Value.GetMappedInfo());
                var mergedInfo = this;
                while (mergedInfo._mergedInto != null)
                {
                    mergedInfo = mergedInfo._mergedInto;
                    foreach (var (key, value) in mergedInfo._beforePropertyIds)
                        if (!propertyMapping.ContainsKey(key))
                            propertyMapping.Add(key, value.GetMappedInfo());
                }

                return new ComponentInfo(InstanceId, mergedInfo.InstanceId, Type, propertyMapping, mergedInfo.VrmFirstPersonFlag);
            }

            public override ComponentOrGameObject TargetComponent
            {
                get
                {
                    if (_mergedInto != null) throw new Exception("Already Merged");
                    var instance = EditorUtility.InstanceIDToObject(InstanceId);
                    return new ComponentOrGameObject(instance ? instance : null);
                }
            }

            public override string[] Properties => _afterPropertyIds.Keys.ToArray();

            public override ref TPropInfo GetPropertyInfo(string property) => ref GetProperty(property).PropertyInfo;
            public override TPropInfo TryGetPropertyInfo(string property)
            {
                if (_afterPropertyIds.TryGetValue(property, out var info))
                    return info.PropertyInfo;
                return default;
            }

            public override IEnumerable<(string name, TPropInfo info)> GetAllPropertyInfo =>
                _afterPropertyIds.Select((x) => (x.Key, x.Value.PropertyInfo));
        }
    }

    internal interface IPropertyInfo<T>
        where T : struct, IPropertyInfo<T>
    {
        void MergeTo(ref T property);
        void CopyTo(ref T property);
    }

    internal abstract class AnimationComponentInfo<TPropInfo>
        where TPropInfo : struct, IPropertyInfo<TPropInfo>
    {
        public abstract string[] Properties { get; }
        public abstract ComponentOrGameObject TargetComponent { get; }

        public abstract void RemoveProperty(string property);

        public abstract ref TPropInfo GetPropertyInfo(string property);
        public abstract TPropInfo TryGetPropertyInfo(string property);
        public abstract IEnumerable<(string name, TPropInfo info)> GetAllPropertyInfo { get; }
    }
}
