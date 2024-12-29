using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [AddComponentMenu("Avatar Optimizer/AAO Remove Mesh By Material")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [AllowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/remove-mesh-by-material/")]
    [PublicAPI]
    public class RemoveMeshByMaterial : EditSkinnedMeshComponent, ISerializationCallbackReceiver
    {
        [SerializeField] internal PrefabSafeSet.PrefabSafeSet<Material> materials;

        internal RemoveMeshByMaterial()
        {
            materials = new PrefabSafeSet.PrefabSafeSet<Material>(this);
        }

        private void ValidatePSUC()
        {
            PrefabSafeSet.PrefabSafeSet.OnValidate(this, x => x.materials);
        }
        
        private void OnValidate() => ValidatePSUC();
        void ISerializationCallbackReceiver.OnBeforeSerialize() => ValidatePSUC();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }
        
        APIChecker _checker;
        
        /// <summary>
        /// Initializes the RemoveMEshByBlendShape with the specified default behavior version.
        ///
        /// As Described in the documentation, you have to call this method after `AddComponent` to make sure
        /// the default configuration is what you want.
        /// Without calling this method, the default configuration might be changed in the future.
        /// </summary>
        /// <param name="version">
        /// The default configuration version.
        /// Since 1.7.0, version 1 is supported.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Unsupported configuration version</exception>
        [PublicAPI]
        public void Initialize(int version)
        {
            switch (version)
            {
                case 1:
                    // nothing to do
                    break; 
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), $"unsupported version: {version}");
            }
            _checker.OnInitialize(version, this);
        }
        
        /// <summary>
        /// Gets the set of materials to delete from the target mesh.
        /// </summary>
        [PublicAPI]
        public API.PrefabSafeSetAccessor<Material> Materials =>
            _checker.OnAPIUsage(this, new API.PrefabSafeSetAccessor<Material>(materials));
    }
}
