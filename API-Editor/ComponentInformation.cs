#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.API
{
    /// <summary>
    /// This attribute declares the class provides information about the type.
    /// Your class MUST derive <see cref="ComponentInformation{TComponent}"/> and MUST have constructor without parameters.
    /// The <see cref="TargetType"/> must be assignable to <c>TComponent</c> of <see cref="ComponentInformation{TComponent}"/>
    ///
    /// Your class MAY have one type parameter.
    /// When your class have type parameter, the type parameter MUST be passed to <see cref="ComponentInformation{TComponent}"/>
    /// and must be assignable from <see cref="TargetType"/>.
    ///
    /// It's REQUIRED to be exists only one ComponentInformation for one type.
    /// So, you SHOULD NOT declare ComponentInformation for external components.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [MeansImplicitUse]
    [BaseTypeRequired(typeof(ComponentInformation<>))]
    [PublicAPI]
    public sealed class ComponentInformationAttribute : APIInternal.ComponentInformationAttributeBase
    {
        [PublicAPI]
        public Type TargetType { get; }

        [PublicAPI]
        public ComponentInformationAttribute(Type targetType)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }

        internal override Type GetTargetType() => TargetType;
    }

    [PublicAPI]
    [APIInternal.AllowInherit]
    public abstract class ComponentInformation<TComponent> :
        APIInternal.ComponentInformation,
        APIInternal.IComponentInformation<TComponent>
        where TComponent : Component
    {
        [PublicAPI]
        protected ComponentInformation()
        {
        }

        internal sealed override void CollectDependencyInternal(Component component,
            ComponentDependencyCollector collector) =>
            CollectDependency((TComponent)component, collector);

        internal sealed override void CollectMutationsInternal(Component component,
            ComponentMutationsCollector collector) =>
            CollectMutations((TComponent)component, collector);

        internal sealed override void ApplySpecialMappingInternal(Component component, MappingSource collector) =>
            ApplySpecialMapping((TComponent)component, collector);

        /// <summary>
        /// Collects runtime dependencies and runtime behavior information of <see cref="component"/>.
        ///
        /// This information is used for Garage Collection system and automatic activeness toggle system in Avatar Optimizer.
        /// You have to call some methods on <see cref="ComponentDependencyCollector"/> to declare dependencies
        /// and runtime behavior of the component.
        ///
        /// Please refer <see cref="ComponentDependencyCollector"/> for more information about GC system and collecting dependencies.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="collector">The callback.</param>
        [PublicAPI]
        protected abstract void CollectDependency(TComponent component, ComponentDependencyCollector collector);

        /// <summary>
        /// Collects runtime mutations by <see cref="component"/>.
        /// You have to call <see cref="collector"/>.<see cref="ComponentMutationsCollector.ModifyProperties"/>
        /// for all property mutations by the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="collector">The callback.</param>
        [PublicAPI]
        protected virtual void CollectMutations(TComponent component, ComponentMutationsCollector collector)
        {
        }

        /// <summary>
        /// Applies some property mapping to your component.
        /// For object replacements, AAO processes automatically so you don't have to implement that in this method.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="mappingSource">The mapping source</param>
        [PublicAPI]
        protected virtual void ApplySpecialMapping(TComponent component, MappingSource mappingSource)
        {
        }

        // Important note for future AAO developer
        // 1. You MUST NOT add abstract method onto this class
        // 2. When you added some virtual methods onto this class, implementer MAY NOT implement that method
        //    so we have to add some way to detect if implemented AND must fallback to default implementation.
        //    One way I can implement is add some internal method to the collector, call it in default implementation,
        //    and do fallback process in that method.
    }

    /// <summary>
    /// Callback for collecting dependencies information of the component.
    /// This information is currently used by Garbage Collection system and automatic component disable.
    ///
    /// # Components Garbage Collection
    /// Avatar Optimizer has a Garbage Collection system for components (GC components).
    /// `AddDependency` and `MarkEntryPoint` methods are used to declare the dependency relationship used by the GC system.
    ///
    /// The GC system is based on tracing garbage collection or mark-and-sweep algorithm.
    ///
    /// In Avatar Optimizer, root objects are the components that are enabled, gameObject is active,
    /// and the component is marked as EntryPoint with `MarkEntryPoint`.
    /// The dependency relationship is declared by `AddDependency` methods on this callback.
    /// There is option to declare condition of the dependency relationship based on the enabled / active state
    /// of the dependant and / or dependency.
    /// Please refer <see cref="ComponentDependencyInfo"/> and `AddDependency` methods for more information.
    ///
    /// # Automatic Component Disable
    /// Avatar Optimizer has runtime-load optimization feature that disables components that are not needed.
    /// In addition to the information provided for GC Components,
    /// `HeavyBehaviour` and `Behaviour` methods are used to declare the activeness of the component.
    ///
    /// Avatar Optimizer tries to disable `HeavyBehaviour` as long as possible and enable it when needed.
    /// Avatar Optimizer thinks a component is needed when any of the EntryPoint component that
    /// depends on the component is active and enabled.
    ///
    /// In current Avatar Optimizer, the optimization does animate enabled of the HeavyBehavior component,
    /// and does not change the enabled / active state of any other components.
    /// In the future, to reduce animation property count, Avatar Optimizer may change the active state of game object
    /// and this may affects to other components.
    /// However, some components are not heavy but enabled state is important.
    /// Please mark such components as Behavior with `MarkBehaviour`.
    /// Active / Enabled of components marked as Behaviour will not be changed by Avatar Optimizer.
    /// Please note that this does not prevent Avatar Optimizer to remove the component.
    /// </summary>
    [PublicAPI]
    public abstract class ComponentDependencyCollector
    {
        internal ComponentDependencyCollector()
        {
        }

        /// <summary>
        /// Marks this component as EntryPoint component.
        ///
        /// Avatar Optimizer will recognize EntryPoint compponents as components that have side effects to outside the avatar. 
        /// Therefore, Avatar Optimizer will not remove such a components if the component can be active and enabled.
        /// Please note that Avatar Optimizer may remove the component if the component is not active or enabled.
        /// 
        /// For example, Renderer components have side effects because it renders something.
        /// VRC Contacts have some effects to other avatars in the instance, so they have side effects.
        /// 
        /// If your component has some effects only for some specific component(s) in the avatar,
        /// you should not mark your component as EntryPoint.
        /// You should declare dependency relationship from affecting component to your component instead.
        /// This allows Avatar Optimizer to remove your component if the affecting component is removed.
        /// 
        /// One of components that is not EntryPoint is VRCPhysBone without animating parameters.
        /// VRCPhysBone only affects a specific subset of the transforms in the avatar.
        /// Therefore, VRCPhysBone has no effects if the affected transforms are not used by any Renderers or some other EntryPoint components.
        /// So, VRCPhysBone are not marked as EntryPoint and they declare bidirectional dependency relationship between
        /// PhysBone and PhysBone-affected transforms.
        ///
        /// With same reasons, Constraints are not treated as EntryPoint, they have bidirectional
        /// dependency relationship between Constraints and transform instead.
        /// </summary>
        [PublicAPI]
        public abstract void MarkEntrypoint();

        /// <summary>
        /// Marks this component as HeavyBehaviour component, which means the component uses some resources while
        /// enabled but doesn't eat if not enabled and AAO can (almost) freely change enablement of the component.
        /// When you mark some components HeavyBehaviour component, Avatar Optimizer will generate animation that disables
        /// the component when all entrypoints that using this component is not active / enabled.
        ///
        /// If your component have some runtime load and can be skipped if your component is not needed by all
        /// enabled EntryPoint components, you should mark your component as Behaviour for runtime-load optimization.
        ///
        /// For example, VRCPhysBone is marked as HeavyBehaviour.
        /// </summary>
        [PublicAPI]
        public abstract void MarkHeavyBehaviour();

        /// <summary>
        /// Marks this component as Behaviour component, which means the activeness of the component has meaning.
        /// When you mark your some components Behaviour component, Avatar Optimizer will never change
        /// `isActive` and `enabled` of the component, but this doesn't prevent AAO to remove the component.
        ///
        /// If your component is not a Behaviour component, AAO may change enablement of the component.
        ///
        /// NOTE: In AAO 1.6.0, AAO will not change enablement of non-Behaviour components but in the feature releases,
        /// AAO may change enablement of non-Behaviour components to change enablement of HeavyBehaviour components
        /// effectively.
        ///
        /// For example Constraints are marked as Behaviour.
        /// </summary>
        [PublicAPI]
        public abstract void MarkBehaviour();

        /// <summary>
        /// Adds <see cref="dependency"/> as dependencies of <see cref="dependant"/>.
        /// The dependency will be assumed as the dependant will have dependency if dependant can be enabled and
        /// even if dependency can be disabled. You can change the settings by <see cref="ComponentDependencyInfo"/>.
        ///
        /// WARNING: the <see cref="ComponentDependencyInfo"/> instance can be shared between multiple AddDependency
        /// invocation so you MUST NOT call methods on IComponentDependencyInfo after calling AddDependency other time.
        /// </summary>
        /// <param name="dependant">The dependant</param>
        /// <param name="dependency">The dependency</param>
        /// <returns>The object to configure the dependency</returns>
        [PublicAPI]
        public abstract ComponentDependencyInfo AddDependency(Component? dependant, Component? dependency);

        /// <summary>
        /// Adds <see cref="dependency"/> as dependencies of current component.
        /// Same as calling <see cref="AddDependency(UnityEngine.Component,UnityEngine.Component)"/>
        /// with current component but this might be optimized more.
        /// </summary>
        /// <seealso cref="AddDependency(UnityEngine.Component,UnityEngine.Component)"/>
        /// <param name="dependency">The dependency</param>
        /// <returns>The object to configure the dependency</returns>
        [PublicAPI]
        public abstract ComponentDependencyInfo AddDependency(Component? dependency);

        /// <summary>
        /// Adds relative path from <see cref="root"/> to <see cref="dependency"/> as dependencies of current component.
        ///
        /// This API is added in AvatarOptimizer 1.7.0.
        /// </summary>
        /// <remarks>
        /// This method is used to add dependencies that are referenced by relative path from
        /// the <see cref="root"/> GameObject. This can be use for Avatar definition of Animator component.
        ///
        /// This does not try to preserve the name of the <see cref="root"/> GameObject.
        ///
        /// AvatarOptimizer will work as possible to not change the relative path of the dependency,
        /// but the path of the component may be changed by the request of the user or some other reasons
        /// so it's highly recommended to re-compute the relative path on ApplySpecialMapping.
        /// In addition, for other NDMF plugin-compatibility, it might be better to resolve / replace 
        /// the path reference with UnityEngine.Object reference in Resolving phase.
        /// </remarks>
        /// <remarks>
        /// This method is added in AvatarOptimizer 1.7.0.
        /// If you want to use this API in previous versions of Avatar Optimizer,
        /// you can use the following extension method as a fallback.
        /// Thanks to the method resolution rules with extension methods and actual methods in C#,
        /// You can stay the extension method in the scope even in AvatarOptimizer 1.7.0 or later.
        ///
        /// Please note that this implementation is only for AvatarOptimizer 1.6.x or older.
        /// This may not work as expected in the future versions.
        ///
        /// <code><![CDATA[
        /// static class Extensions
        /// {
        ///     public static void AddPathDependency([NotNull] this ComponentDependencyCollector collector,
        ///         [NotNull] Transform dependency, [NotNull] Transform root)
        ///     {
        ///         for (var current = dependency; current != null && current != root; current = current.parent)
        ///             collector.AddDependency(current);
        ///     }
        /// }
        /// ]]></code>
        /// </remarks>
        /// <param name="dependency">The GameObject referenced with relative path</param>
        /// <param name="root">The GameObject the relative path starts from</param>
        /// <exception cref="ArgumentException">If the <see cref="dependency"/> is not child of <see cref="root"/>.</exception>
        [PublicAPI]
        public abstract PathDependencyInfo AddPathDependency(Transform dependency, Transform root);

        // TODO: rename to better name and make public
        // NOTE for external users: this is API Proposal to compute value of animatable bool property 
        // such as ParticleSystem.trigger.enabled.
        /// <summary>
        /// Returns whether if <paramref name="animationProperty"/> is always true, false, or animated by some animation.
        /// Returns true if initially true and not animated OR always animated to true.
        /// Returns false if initially false and not animated OR always animated to false.
        /// Returns null if the property is animated to both true and false.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="animationProperty"></param>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        internal abstract bool? GetAnimatedFlag(Component component, string animationProperty, bool currentValue);

        // TODO: rename to better name and make public
        // NOTE for external users: this is API Proposal to determine whether the parameter is
        // used by the animator or not. For example, phys bones.
        /// <summary>
        /// Returns whether if <paramref name="parameterName"/> is used by the animator.
        /// </summary>
        /// <param name="parameterName">The animator parameter name</param>
        /// <returns></returns>
        internal abstract bool IsParameterUsed(string parameterName);
    }

    [PublicAPI]
    public abstract class PathDependencyInfo
    {
        internal PathDependencyInfo()
        {
        }

        /// <summary>
        /// Indicates the dependency is required even if dependant component is disabled.
        /// </summary>
        /// <returns>this object for method chain</returns>
        [PublicAPI]
        public abstract PathDependencyInfo EvenIfDependantDisabled();
    }

    [PublicAPI]
    public abstract class ComponentDependencyInfo
    {
        internal ComponentDependencyInfo()
        {
        }

        /// <summary>
        /// Indicates the dependency is required even if dependant component is disabled.
        /// </summary>
        /// <returns>this object for method chain</returns>
        [PublicAPI]
        public abstract ComponentDependencyInfo EvenIfDependantDisabled();

        /// <summary>
        /// Indicates the dependency is not required if dependency component is disabled.
        /// </summary>
        /// <returns>this object for method chain</returns>
        [PublicAPI]
        public abstract ComponentDependencyInfo OnlyIfTargetCanBeEnable();
    }

    /// <example>
    /// <code>
    /// protected override void CollectMutations(AimConstraint component, IComponentMutationsCollector collector)
    /// {
    ///     collector.ModifyProperties(component.transform, new [] { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" });
    /// }
    /// </code>
    /// </example>
    [PublicAPI]
    public abstract class ComponentMutationsCollector
    {
        internal ComponentMutationsCollector()
        {
        }

        /// <summary>
        /// Registers <paramref name="properties"/> of the <paramref name="component"/> will be changed by current component.
        /// </summary>
        /// <param name="component">The component current component will modifies</param>
        /// <param name="properties">The list of properties current component will modifies</param>
        [PublicAPI]
        public abstract void ModifyProperties(Component component, IEnumerable<string> properties);

        /// <inheritdoc cref="ModifyProperties(UnityEngine.Component,System.Collections.Generic.IEnumerable{string})"/>
        [PublicAPI]
        public void ModifyProperties(Component component, params string[] properties) =>
            ModifyProperties(component, (IEnumerable<string>)properties);
    }

    /// <summary>
    /// The class provides object and property replaced by Avatar Optimizer.
    ///
    /// Avatar Optimizer may replaces or merges component to another component.
    /// This class provide the information about the replacement.
    /// In addition, Avatar Optimizer may replace or merges some properties of the component.
    /// This class also provide the information about the property replacement.
    /// </summary>
    [PublicAPI]
    public abstract class MappingSource
    {
        internal MappingSource()
        {
        }

        /// <summary>
        /// Returns <see cref="MappedComponentInfo{T}"/> about the component instance.
        /// The instance can be a missing component.
        /// </summary>
        /// <param name="component">The component to get information about</param>
        /// <typeparam name="T">The type of component</typeparam>
        [PublicAPI]
        public abstract MappedComponentInfo<T> GetMappedComponent<T>(T component) where T : Component;

        /// <summary>
        /// Returns <see cref="MappedComponentInfo{T}"/> about the GameObject instance.
        /// The instance can be a missing component.
        /// </summary>
        /// <param name="component">The component to get information about</param>
        /// <typeparam name="T">The type of component</typeparam>
        [PublicAPI]
        public abstract MappedComponentInfo<GameObject> GetMappedGameObject(GameObject component);
    }

    [PublicAPI]
    public abstract class MappedComponentInfo<T> where T : Object
    {
        internal MappedComponentInfo()
        {
        }

        /// <summary>
        /// The mapped component (or GameObject).
        /// The component may be removed without mapped component.
        /// If there are no mapped component, this will be null.
        ///
        /// Even if the component is removed without mapped component, some animation property can be mapped
        /// to a property on another component so you should use <see cref="TryMapProperty"/> if the component is highly related
        /// to animation property, for example, blendShape related SkinnedMeshRenderer.
        /// </summary>
        [PublicAPI]
        public abstract T? MappedComponent { get; }

        /// <summary>
        /// Maps animation property name to component and MappedPropertyInfo.
        /// If the property is not removed, returns true and <paramref name="found"/> is set.
        /// If the property is removed, returns false and <paramref name="found"/> will be default.
        ///
        /// To get mapped property probably, you must register the property as modified property by
        /// <see cref="ComponentMutationsCollector.ModifyProperties(UnityEngine.Component,System.Collections.Generic.IEnumerable{string})"/>.
        /// Unless that, renaming or moving the property may not be tracked by Avatar Optimizer.
        /// </summary>
        /// <param name="property">The name of property will be mapped</param>
        /// <param name="found">The result parameter</param>
        /// <returns>Whether if the property is successfully mapped or removed</returns>
        [PublicAPI]
        public abstract bool TryMapProperty(string property, out MappedPropertyInfo found);

        /// <summary>
        /// Gets the VRM FirstPersonFlag of the component, following component mappings.
        /// NOTE for external users: this API is subject to change in future releases.
        /// </summary>
        internal abstract bool TryGetMappedVrmFirstPersonFlag(out VrmFirstPersonFlag vrmFirstPersonFlag);
    }

    [PublicAPI]
    public readonly struct MappedPropertyInfo
    {
        /// <summary>
        /// The Component or GameObject the property is on.
        /// </summary>
        [PublicAPI]
        public Object Component { get; }

        /// <summary>
        /// The name of the mapped property.
        /// </summary>
        [PublicAPI]
        public string Property { get; }

        internal MappedPropertyInfo(Object component, string property)
        {
            Component = component;
            Property = property;
        }
    }
    
    /// <summary>
    /// The VRM FirstPersonFlag settings for renderers of a VRM0/VRM1 avatar, respective to either VRMFirstPerson.RendererFirstPersonFlags (VRM0) or FirstPersonType (VRM1).
    /// NOTE for external users: this API is subject to change in future releases.
    /// </summary>
    internal enum VrmFirstPersonFlag
    {
        Auto,
        Both,
        ThirdPersonOnly,
        FirstPersonOnly,
    }
}
