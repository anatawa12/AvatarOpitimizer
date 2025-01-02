using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer
{
    internal class ObjectMapping
    {
        private readonly IReadOnlyDictionary<int, BeforeGameObjectTree> _beforeTree;
        private readonly IReadOnlyDictionary<int, ComponentInfo> _componentMapping;

        public ObjectMapping(
            IReadOnlyDictionary<int, BeforeGameObjectTree> beforeTree, 
            IReadOnlyDictionary<int, ComponentInfo> componentMapping)
        {
            _beforeTree = beforeTree;
            _componentMapping = componentMapping;
        }

        public bool MapComponentInstance(int instanceId, out Component? component)
        {
            if (instanceId == 0)
            {
                component = null;
                return false;
            }
            var mergedInto = _componentMapping.TryGetValue(instanceId, out var info) ? info.MergedInto : instanceId;

            var found = EditorUtility.InstanceIDToObject(mergedInto);
            if (!found)
            {
                component = null;
                return true;
            }

            if (found is Component c)
            {
                component = c;
                return instanceId != mergedInto;
            }

            component = default;
            return false;
        }

        internal BeforeGameObjectTree? GetBeforeGameObjectTree(GameObject rootGameObject) =>
            _beforeTree.TryGetValue(rootGameObject.GetInstanceID(), out var tree) ? tree : null;

        // null means nothing to map
        public ComponentInfo? GetComponentMapping(int instanceId) =>
            _componentMapping.TryGetValue(instanceId, out var info) ? info : null;

        public AnimationObjectMapper CreateAnimationMapper(GameObject rootGameObject)
        {
            if (!_beforeTree.TryGetValue(rootGameObject.GetInstanceID(), out var beforeTree))
                throw new InvalidOperationException($"rootGameObject {rootGameObject} is not in the mapping");
            return new AnimationObjectMapper(rootGameObject, beforeTree, this);
        }
    }
    class BeforeGameObjectTree
    {
        public readonly int InstanceId;
        public readonly int ParentInstanceId;
        public readonly string Name;
        public readonly IReadOnlyDictionary<Type, int> ComponentInstanceIdByType;
        public readonly int[] ComponentInstanceIds;
        public readonly BeforeGameObjectTree[] Children;
        public bool HasSlashInNameInDirectChildren { get; private set; }
        public bool HasSlashInNameInChildren { get; private set; }

        public BeforeGameObjectTree(GameObject gameObject)
        {
            var parentTransform = gameObject.transform.parent;
            InstanceId = gameObject.GetInstanceID();
            Name = gameObject.name;
            ParentInstanceId = parentTransform ? parentTransform.gameObject.GetInstanceID() : 0;
            Children = new BeforeGameObjectTree[gameObject.transform.childCount];

            var components = gameObject.GetComponents<Component>();
            ComponentInstanceIds = components.Select(x => x.GetInstanceID()).ToArray();
            
            var componentByType = new Dictionary<Type, int>();
            foreach (var component in components)
            {
                // some animation may affects to base class. e.g.
                // To affect Animator component, we may animate Behaviour.m_Enabled.
                var type = component.GetType();
                while (type != typeof(Component))
                {
                    if (!componentByType.ContainsKey(type))
                        componentByType.Add(type, component.GetInstanceID());
                    type = type.BaseType;
                    if (type == null)
                        throw new InvalidOperationException("logic failure: component which doesn't extend Component");
                }
            }

            componentByType[typeof(GameObject)] = InstanceId;

            ComponentInstanceIdByType = componentByType;
        }

        public void InitializeRecursive()
        {
            foreach (var child in Children)
                child.InitializeRecursive();

            HasSlashInNameInDirectChildren = Children.Any(x => x.HasSlashInNameInChildren || x.Name.Contains('/'));
            HasSlashInNameInChildren = Children.Any(x => x.HasSlashInNameInChildren || x.Name.Contains('/'));
        }

        public BeforeGameObjectTree? ResolvePath(string relative) =>
            relative == "" ? this : ResolvePathAll(relative).FirstOrDefault();

        private IEnumerable<BeforeGameObjectTree> ResolvePathAll(string relative)
        {
            if (relative == "")
                return new[] { this };
            // otherwise, match as possible from start

            // simplest
            var slashIndex = relative.IndexOf('/');

            if (slashIndex == -1)
                return Children.Where(x => x.Name == relative);

            for (;slashIndex != -1; slashIndex = relative.IndexOf('/', slashIndex + 1))
            {
                var name = relative.Substring(0, slashIndex);

                if (Children.Any(x => x.Name == name))
                {
                    var remaining = relative.Substring(slashIndex + 1);

                    return Children.Where(x => x.Name == name).SelectMany(x => x.ResolvePathAll(remaining));
                }
            }

            return Array.Empty<BeforeGameObjectTree>();
        }
    }

    class ComponentInfo
    {
        public readonly int InstanceId;
        public readonly int MergedInto;
        public readonly Type Type;
        public readonly IReadOnlyDictionary<string, MappedPropertyInfo> PropertyMapping;
        public readonly VrmFirstPersonFlag? VrmFirstPersonFlag;

        public ComponentInfo(int instanceId, int mergedInto, Type type,
            IReadOnlyDictionary<string, MappedPropertyInfo> propertyMapping,
            VrmFirstPersonFlag? vrmFirstPersonFlag)
        {
            InstanceId = instanceId;
            MergedInto = mergedInto;
            Type = type;
            PropertyMapping = propertyMapping;
            VrmFirstPersonFlag = vrmFirstPersonFlag;
        }
    }

    readonly struct PropertyDescriptor : IEquatable<PropertyDescriptor>
    {
        public static readonly PropertyDescriptor Removed = default;
        public readonly int InstanceId;
        public readonly Type Type;
        public readonly string Name;

        public PropertyDescriptor(int instanceId, Type type, string name)
        {
            InstanceId = instanceId;
            Type = type;
            Name = name;
        }

        public override int GetHashCode() => HashCode.Combine(InstanceId, Type, Name);
        public bool Equals(PropertyDescriptor other) =>
            InstanceId == other.InstanceId && Type == other.Type && Name == other.Name;
        public override bool Equals(object? obj) => obj is PropertyDescriptor other && Equals(other);
        public static bool operator ==(PropertyDescriptor left, PropertyDescriptor right) => left.Equals(right);
        public static bool operator !=(PropertyDescriptor left, PropertyDescriptor right) => !left.Equals(right);
    }

    readonly struct MappedPropertyInfo
    {
        public static readonly MappedPropertyInfo Removed = default;
        public readonly PropertyDescriptor MappedProperty;
        private readonly PropertyDescriptor[] _copiedTo;

        public PropertyDescriptor[] AllCopiedTo => _copiedTo ?? Array.Empty<PropertyDescriptor>();

        public MappedPropertyInfo(PropertyDescriptor property, PropertyDescriptor[] copiedTo)
        {
            MappedProperty = property;
            _copiedTo = copiedTo;
        }

        public MappedPropertyInfo(int mappedInstanceId, Type mappedType, string mappedName) : this(
            new PropertyDescriptor(mappedInstanceId, mappedType, mappedName))
        {
        }

        public MappedPropertyInfo(PropertyDescriptor property)
        {
            MappedProperty = property;
            _copiedTo = new[] { property };
        }
    }

    static class VProp
    {
        private const string ExtraProps = "AvatarOptimizerExtraProps";
        public static string BlendShapeIndex(int index) => $"{ExtraProps}.BlendShapeIndex.{index}";
        public static bool IsBlendShapeIndex(string prop) => prop.StartsWith($"{ExtraProps}.BlendShapeIndex.");

        public static int ParseBlendShapeIndex(string prop)
        {
            if (!prop.StartsWith($"{ExtraProps}.BlendShapeIndex.", StringComparison.Ordinal))
                throw new ArgumentException($"The property {prop} is not BlendShapeIndex", nameof(prop));
            var indexStr = prop.Substring($"{ExtraProps}.BlendShapeIndex.".Length);
            if (!int.TryParse(indexStr, out var index))
                throw new ArgumentException($"The property {prop} is not BlendShapeIndex", nameof(prop));
            return index;
        }

        public static string AnimatorEnabledAsBehavior = $"{ExtraProps}.AnimatorEnabledAsBehavior";
    }

    static class Props
    {
        private const string Enabled = "m_Enabled";
        // enabled for behaviour-like components
        public static string EnabledFor(Object? obj) => obj == null ? Enabled : EnabledFor(obj.GetType());

        public static string EnabledFor(Type type) =>
            type == typeof(Animator) ? VProp.AnimatorEnabledAsBehavior : Enabled;

        // isActive for GameObjects
        public const string IsActive = "m_IsActive";
    }
}
