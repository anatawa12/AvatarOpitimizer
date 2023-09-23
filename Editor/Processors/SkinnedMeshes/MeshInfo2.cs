#if true
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes.Blendshape;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class MeshInfo2
    {
        public int InitialVertexCount { get; private set; }
        public readonly Renderer SourceRenderer;
        public Transform RootBone;
        public Bounds Bounds;
        public readonly List<Vertex> Vertices = new List<Vertex>(0);
        public BlendshapeInfo BlendShapeData;

        // TexCoordStatus which is 3 bits x 8 = 24 bits
        private ushort _texCoordStatus;

        public readonly List<SubMesh> SubMeshes = new List<SubMesh>(0);

        public readonly List<(string name, float weight)> BlendShapes = new List<(string name, float weight)>(0);

        public readonly List<Bone> Bones = new List<Bone>();

        public bool HasColor { get; set; }
        public bool HasTangent { get; set; }

        public MeshInfo2(SkinnedMeshRenderer renderer)
        {
            SourceRenderer = renderer;
            var mesh = renderer.sharedMesh;
            if (mesh && !mesh.isReadable)
            {
                BuildReport.LogFatal("The Mesh is not readable. Please Check Read/Write")?.WithContext(mesh);
                return;
            }
            
            BuildReport.ReportingObject(renderer, true, () =>
            {
                if (mesh)
                    ReadSkinnedMesh(mesh);

                // if there's no bones: add one fake bone
                if (Bones.Count == 0)
                    SetIdentityBone(renderer.rootBone ? renderer.rootBone : renderer.transform);

                Bounds = renderer.localBounds;
                RootBone = renderer.rootBone ? renderer.rootBone : renderer.transform;

                if (mesh)
                {
                    for (var i = 0; i < mesh.blendShapeCount; i++)
                        BlendShapes[i] = (BlendShapes[i].name, renderer.GetBlendShapeWeight(i));
                }

                SetMaterials(renderer);

                var bones = renderer.bones;
                for (var i = 0; i < bones.Length && i < Bones.Count; i++) Bones[i].Transform = bones[i];

                RemoveUnusedBones();

                AssertInvariantContract("SkinnedMeshRenderer");
            });
        }

        public MeshInfo2(MeshRenderer renderer)
        {
            SourceRenderer = renderer;
            BuildReport.ReportingObject(renderer, true, () =>
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                if (mesh && !mesh.isReadable)
                {
                    BuildReport.LogFatal("The Mesh is not readable. Please Check Read/Write")?.WithContext(mesh);
                    return;
                }
                if (mesh)
                    ReadStaticMesh(mesh);

                SetIdentityBone(renderer.transform);

                if (mesh)
                    Bounds = mesh.bounds;
                RootBone = renderer.transform;

                SetMaterials(renderer);

                AssertInvariantContract("MeshRenderer");
            });
        }

        private void SetMaterials(Renderer renderer)
        {
            var sourceMaterials = renderer.sharedMaterials;

            if (sourceMaterials.Length < SubMeshes.Count)
                SubMeshes.RemoveRange(sourceMaterials.Length, SubMeshes.Count - sourceMaterials.Length);

            for (var i = 0; i < SubMeshes.Count; i++)
                SubMeshes[i].SharedMaterial = sourceMaterials[i];
            var verticesForLastSubMesh =
                SubMeshes.Count == 0 ? new List<Vertex>() : SubMeshes[SubMeshes.Count - 1].Triangles;
            for (var i = SubMeshes.Count; i < sourceMaterials.Length; i++)
                SubMeshes.Add(new SubMesh(verticesForLastSubMesh.ToList(), sourceMaterials[i]));
        }

        [Conditional("UNITY_ASSERTIONS")]
        public void AssertInvariantContract(string context)
        {
            var vertices = new HashSet<Vertex>(Vertices);
            Debug.Assert(SubMeshes.SelectMany(x => x.Triangles).All(vertices.Contains),
                $"{context}: some SubMesh has invalid triangles");
            var bones = new HashSet<Bone>(Bones);
            Debug.Assert(Vertices.SelectMany(x => x.BoneWeights).Select(x => x.bone).All(bones.Contains),
                $"{context}: some SubMesh has invalid bone weights");
        }

        private void SetIdentityBone(Transform transform)
        {
            Bones.Add(new Bone(Matrix4x4.identity, transform));

            foreach (var vertex in Vertices)
                vertex.BoneWeights.Add((Bones[0], 1f));
        }

        public void ReadSkinnedMesh([NotNull] Mesh mesh)
        {
            ReadStaticMesh(mesh);

            Bones.Clear();
            Bones.Capacity = Math.Max(Bones.Capacity, mesh.bindposes.Length);
            Bones.AddRange(mesh.bindposes.Select(x => new Bone(x)));

            var bonesPerVertex = mesh.GetBonesPerVertex();
            var allBoneWeights = mesh.GetAllBoneWeights();
            var bonesBase = 0;
            for (var i = 0; i < bonesPerVertex.Length; i++)
            {
                int count = bonesPerVertex[i];
                Vertices[i].BoneWeights.Capacity = count;
                foreach (var boneWeight1 in allBoneWeights.AsReadOnlySpan().Slice(bonesBase, count))
                    Vertices[i].BoneWeights.Add((Bones[boneWeight1.boneIndex], boneWeight1.weight));
                bonesBase += count;
            }

            BlendShapes.Clear();
            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                var shapeName = mesh.GetBlendShapeName(i);

                BlendShapes.Add((shapeName, 0.0f));
            }
        }

        public void ReadStaticMesh([NotNull] Mesh mesh)
        {
            InitialVertexCount = mesh.vertexCount;
            BlendShapeData = new BlendshapeInfo(mesh);
            
            Vertices.Capacity = Math.Max(Vertices.Capacity, mesh.vertexCount);
            Vertices.Clear();
            for (var i = 0; i < mesh.vertexCount; i++) Vertices.Add(new Vertex(BlendShapeData, i));

            CopyVertexAttr(mesh.vertices, (x, v) => x.Position = v);
            CopyVertexAttr(mesh.normals, (x, v) => x.Normal = v);
            if (mesh.GetVertexAttributeDimension(VertexAttribute.Tangent) != 0)
            {
                HasTangent = true;
                CopyVertexAttr(mesh.tangents, (x, v) => x.Tangent = v);
            }
            if (mesh.GetVertexAttributeDimension(VertexAttribute.Color) != 0)
            {
                HasColor = true;
                CopyVertexAttr(mesh.colors32, (x, v) => x.Color = v);
            }

            var uv2 = new List<Vector2>(0);
            var uv3 = new List<Vector3>(0);
            var uv4 = new List<Vector4>(0);
            for (var index = 0; index <= 7; index++)
            {
                // ReSharper disable AccessToModifiedClosure
                switch (mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0 + index))
                {
                    case 2:
                        SetTexCoordStatus(index, TexCoordStatus.Vector2);
                        mesh.GetUVs(index, uv2);
                        CopyVertexAttr(uv2, (x, v) => x.SetTexCoord(index, v));
                        break;
                    case 3:
                        SetTexCoordStatus(index, TexCoordStatus.Vector3);
                        mesh.GetUVs(index, uv3);
                        CopyVertexAttr(uv3, (x, v) => x.SetTexCoord(index, v));
                        break;
                    case 4:
                        SetTexCoordStatus(index, TexCoordStatus.Vector4);
                        mesh.GetUVs(index, uv4);
                        CopyVertexAttr(uv4, (x, v) => x.SetTexCoord(index, v));
                        break;
                }

                // ReSharper restore AccessToModifiedClosure
            }

            var triangles = mesh.triangles;
            SubMeshes.Clear();
            SubMeshes.Capacity = Math.Max(SubMeshes.Capacity, mesh.subMeshCount);
            for (var i = 0; i < mesh.subMeshCount; i++)
                SubMeshes.Add(new SubMesh(Vertices, triangles, mesh.GetSubMesh(i)));
        }

        void CopyVertexAttr<T>(T[] attributes, Action<Vertex, T> assign)
        {
            for (var i = 0; i < attributes.Length; i++)
                assign(Vertices[i], attributes[i]);
        }

        void CopyVertexAttr<T>(List<T> attributes, Action<Vertex, T> assign)
        {
            for (var i = 0; i < attributes.Count; i++)
                assign(Vertices[i], attributes[i]);
        }

        private const int BitsPerTexCoordStatus = 2;
        private const int TexCoordStatusMask = (1 << BitsPerTexCoordStatus) - 1;

        public TexCoordStatus GetTexCoordStatus(int index)
        {
            return (TexCoordStatus)((_texCoordStatus >> (index * BitsPerTexCoordStatus)) & TexCoordStatusMask);
        }

        public void SetTexCoordStatus(int index, TexCoordStatus value)
        {
            _texCoordStatus = (ushort)(
                (uint)_texCoordStatus & ~(TexCoordStatusMask << (BitsPerTexCoordStatus * index)) | 
                ((uint)value & TexCoordStatusMask) << (BitsPerTexCoordStatus * index));
        }

        public void Clear()
        {
            Bounds = default;
            Vertices.Clear();
            _texCoordStatus = default;
            SubMeshes.Clear();
            BlendShapes.Clear();
            Bones.Clear();
            HasColor = false;
            HasTangent = false;
            BlendShapeData = null;
        }

        public void Optimize()
        {
            RemoveUnusedBones();
        }

        private void RemoveUnusedBones()
        {
            // GC Bones
            var usedBones = new HashSet<Bone>();
            foreach (var meshInfo2Vertex in Vertices)
            foreach (var (bone, _) in meshInfo2Vertex.BoneWeights)
                usedBones.Add(bone);
            Bones.RemoveAll(x => !usedBones.Contains(x));
        }

        public void WriteToMesh(Mesh destMesh)
        {
            Optimize();
            destMesh.Clear();

            // Basic Vertex Attributes: vertices, normals
            {
                var vertices = new Vector3[Vertices.Count];
                var normals = new Vector3[Vertices.Count];
                for (var i = 0; i < Vertices.Count; i++)
                {
                    vertices[i] = Vertices[i].Position;
                    normals[i] = Vertices[i].Normal.normalized;
                }

                destMesh.vertices = vertices;
                destMesh.normals = normals;
            }

            // tangents
            if (HasTangent)
            {
                var tangents = new Vector4[Vertices.Count];
                for (var i = 0; i < Vertices.Count; i++)
                {
                    var tangent3 = (Vector3)Vertices[i].Tangent;
                    var tangentW = Vertices[i].Tangent.w;
                    tangent3.Normalize();
                    tangents[i] = new Vector4(tangent3.x, tangent3.y, tangent3.z, tangentW);
                }
                destMesh.tangents = tangents;
            }

            // UVs
            {
                var uv2 = new Vector2[Vertices.Count];
                var uv3 = new Vector3[Vertices.Count];
                var uv4 = new Vector4[Vertices.Count];
                for (var uvIndex = 0; uvIndex < 8; uvIndex++)
                {
                    switch (GetTexCoordStatus(uvIndex))
                    {
                        case TexCoordStatus.NotDefined:
                            // nothing to do
                            break;
                        case TexCoordStatus.Vector2:
                            for (var i = 0; i < Vertices.Count; i++)
                                uv2[i] = Vertices[i].GetTexCoord(uvIndex);
                            destMesh.SetUVs(uvIndex, uv2);
                            break;
                        case TexCoordStatus.Vector3:
                            for (var i = 0; i < Vertices.Count; i++)
                                uv3[i] = Vertices[i].GetTexCoord(uvIndex);
                            destMesh.SetUVs(uvIndex, uv3);
                            break;
                        case TexCoordStatus.Vector4:
                            for (var i = 0; i < Vertices.Count; i++)
                                uv4[i] = Vertices[i].GetTexCoord(uvIndex);
                            destMesh.SetUVs(uvIndex, uv4);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            // color
            if (HasColor)
            {
                var colors = new Color32[Vertices.Count];
                for (var i = 0; i < Vertices.Count; i++)
                    colors[i] = Vertices[i].Color;
                destMesh.colors32 = colors;
            }

            // bones
            destMesh.bindposes = Bones.Select(x => x.Bindpose.ToUnity()).ToArray();

            // triangles and SubMeshes
            {
                var vertexIndices = new Dictionary<Vertex, int>();
                // first, set vertex indices
                for (var i = 0; i < Vertices.Count; i++)
                    vertexIndices.Add(Vertices[i], i);

                var triangles = new int[SubMeshes.Sum(x => x.Triangles.Count)];
                var subMeshDescriptors = new SubMeshDescriptor[SubMeshes.Count];
                var trianglesIndex = 0;
                for (var i = 0; i < SubMeshes.Count; i++)
                {
                    var subMesh = SubMeshes[i];
                    var existingIndex = SubMeshes.FindIndex(0, i, sm => sm.Triangles.SequenceEqual(subMesh.Triangles));
                    if (existingIndex != -1)
                    {
                        subMeshDescriptors[i] = subMeshDescriptors[existingIndex];
                    }
                    else
                    {
                        subMeshDescriptors[i] = new SubMeshDescriptor(trianglesIndex, SubMeshes[i].Triangles.Count);
                        foreach (var triangle in SubMeshes[i].Triangles)
                            triangles[trianglesIndex++] = vertexIndices[triangle];
                    }
                }

                triangles = triangles.Length == trianglesIndex
                    ? triangles
                    : triangles.AsSpan().Slice(0, trianglesIndex).ToArray();

                destMesh.indexFormat = Vertices.Count <= ushort.MaxValue ? IndexFormat.UInt16 : IndexFormat.UInt32;
                destMesh.triangles = triangles;
                destMesh.subMeshCount = SubMeshes.Count;
                for (var i = 0; i < SubMeshes.Count; i++)
                    destMesh.SetSubMesh(i, subMeshDescriptors[i]);
            }

            // BoneWeights
            if (Vertices.Any(x => x.BoneWeights.Count != 0)){
                var boneIndices = new Dictionary<Bone, int>();
                for (var i = 0; i < Bones.Count; i++)
                    boneIndices.Add(Bones[i], i);

                var bonesPerVertex = new NativeArray<byte>(Vertices.Count, Allocator.Temp);
                var allBoneWeights =
                    new NativeArray<BoneWeight1>(Vertices.Sum(x => x.BoneWeights.Count), Allocator.Temp);
                var boneWeightsIndex = 0;
                for (var i = 0; i < Vertices.Count; i++)
                {
                    bonesPerVertex[i] = (byte)Vertices[i].BoneWeights.Count;
                    Vertices[i].BoneWeights.Sort((x, y) => -x.weight.CompareTo(y.weight));
                    foreach (var (bone, weight) in Vertices[i].BoneWeights)
                        allBoneWeights[boneWeightsIndex++] = new BoneWeight1
                            { boneIndex = boneIndices[bone], weight = weight };
                }

                destMesh.SetBoneWeights(bonesPerVertex, allBoneWeights);
            }

            // BlendShapes
            if (BlendShapes.Count != 0 && BlendShapeData != null)
            {
                BlendShapeData.SaveToMesh(destMesh, Vertices);
            }
        }

        public void WriteToSkinnedMeshRenderer(SkinnedMeshRenderer targetRenderer, OptimizerSession session)
        {
            BuildReport.ReportingObject(targetRenderer, () =>
            {
                var mesh = targetRenderer.sharedMesh
                    ? session.MayCreate(targetRenderer.sharedMesh)
                    : session.AddToAsset(new Mesh { name = $"AAOGeneratedMesh{targetRenderer.name}" });

                WriteToMesh(mesh);
                targetRenderer.sharedMesh = mesh;
                for (var i = 0; i < BlendShapes.Count; i++)
                {
                    try
                    {
                        if (i > targetRenderer.sharedMesh.blendShapeCount)
                        {
                            UnityEngine.Debug.Log($"Blendshape {BlendShapes[i].name} is out of range on {targetRenderer.gameObject.name}");
                        }
                        targetRenderer.SetBlendShapeWeight(i, BlendShapes[i].weight);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
                    
                targetRenderer.sharedMaterials = SubMeshes.Select(x => x.SharedMaterial).ToArray();
                targetRenderer.bones = Bones.Select(x => x.Transform).ToArray();

                targetRenderer.rootBone = RootBone;
                if (Bounds != default)
                    targetRenderer.localBounds = Bounds;
            });
        }
    }

    internal class SubMesh
    {
        // size of this must be 3 * n
        public readonly List<Vertex> Triangles = new List<Vertex>();
        public Material SharedMaterial;

        public SubMesh()
        {
        }

        public SubMesh(List<Vertex> vertices) => Triangles = vertices;
        public SubMesh(List<Vertex> vertices, Material sharedMaterial) => 
            (Triangles, SharedMaterial) = (vertices, sharedMaterial);
        public SubMesh(Material sharedMaterial) => 
            SharedMaterial = sharedMaterial;

        public SubMesh(List<Vertex> vertices, ReadOnlySpan<int> triangles, SubMeshDescriptor descriptor)
        {
            Assert.AreEqual(MeshTopology.Triangles, descriptor.topology);
            Triangles.Capacity = descriptor.indexCount;
            foreach (var i in triangles.Slice(descriptor.indexStart, descriptor.indexCount))
                Triangles.Add(vertices[i]);
        }
    }

    internal class Vertex
    {
        public int Index { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector4 Tangent { get; set; } = new Vector4(1, 0, 0, 1);
        public Vector4 TexCoord0 { get; set; }
        public Vector4 TexCoord1 { get; set; }
        public Vector4 TexCoord2 { get; set; }
        public Vector4 TexCoord3 { get; set; }
        public Vector4 TexCoord4 { get; set; }
        public Vector4 TexCoord5 { get; set; }
        public Vector4 TexCoord6 { get; set; }
        public Vector4 TexCoord7 { get; set; }

        public Color32 Color { get; set; } = new Color32(0xff, 0xff, 0xff, 0xff);

        // SkinnedMesh related
        public List<(Bone bone, float weight)> BoneWeights = new List<(Bone, float)>();

        public BlendshapeInfo BlendshapeInfo;

        public readonly struct BlendShapeFrame
        {
            public readonly float Weight;
            public readonly Vector3 Position;
            public readonly Vector3 Normal;
            public readonly Vector3 Tangent;

            public BlendShapeFrame(float weight, Vector3 position, Vector3 normal, Vector3 tangent)
            {
                Position = position;
                Normal = normal;
                Tangent = tangent;
                Weight = weight;
            }

            public void Deconstruct(out float weight, out Vector3 position, out Vector3 normal, out Vector3 tangent)
            {
                weight = Weight;
                position = Position;
                normal = Normal;
                tangent = Tangent;
            }
        }

        public Vector4 GetTexCoord(int index)
        {
            switch (index)
            {
                // @formatter off
                case 0: return TexCoord0;
                case 1: return TexCoord1;
                case 2: return TexCoord2;
                case 3: return TexCoord3;
                case 4: return TexCoord4;
                case 5: return TexCoord5;
                case 6: return TexCoord6;
                case 7: return TexCoord7;
                default: throw new IndexOutOfRangeException("TexCoord index");
                // @formatter on
            }
        }

        public void SetTexCoord(int index, Vector4 value)
        {
            switch (index)
            {
                // @formatter off
                case 0: TexCoord0 = value; break;
                case 1: TexCoord1 = value; break;
                case 2: TexCoord2 = value; break;
                case 3: TexCoord3 = value; break;
                case 4: TexCoord4 = value; break;
                case 5: TexCoord5 = value; break;
                case 6: TexCoord6 = value; break;
                case 7: TexCoord7 = value; break;
                default: throw new IndexOutOfRangeException("TexCoord index");
                // @formatter on
            }
        }

        public bool TryGetBlendShape(string name, float weight, out Vector3 position, out Vector3 normal, out Vector3 tangent)
        {
            return BlendshapeInfo.TryGetBlendshape(name, weight, Index, out position, out normal, out tangent);
        }

        public Vertex(BlendshapeInfo blendshapeInfo, int index)
        {
            this.BlendshapeInfo = blendshapeInfo;
            Index = index;
        }

        internal Vertex(Vertex vertex)
        {
            Index = vertex.Index;
            Position = vertex.Position;
            Normal = vertex.Normal;
            Tangent = vertex.Tangent;
            TexCoord0 = vertex.TexCoord0;
            TexCoord1 = vertex.TexCoord1;
            TexCoord2 = vertex.TexCoord2;
            TexCoord3 = vertex.TexCoord3;
            TexCoord4 = vertex.TexCoord4;
            TexCoord5 = vertex.TexCoord5;
            TexCoord6 = vertex.TexCoord6;
            TexCoord7 = vertex.TexCoord7;
            Color = vertex.Color;
            BoneWeights = vertex.BoneWeights.ToList();
            BlendshapeInfo = vertex.BlendshapeInfo;
        }

        public Vertex Clone() => new Vertex(this);

        public Vector3 ComputeActualPosition(MeshInfo2 meshInfo2, Matrix4x4 rendererWorldToLocalMatrix)
        {
            var position = Position;

            // first, apply blend shapes
            foreach (var (name, weight) in meshInfo2.BlendShapes)
                if (TryGetBlendShape(name, weight, out var posDelta, out _, out _))
                    position += posDelta;

            // then, apply bones
            var matrix = Matrix4x4.zero;
            foreach (var (bone, weight) in BoneWeights)
            {
                var transformMat = bone.Transform ? (Matrix4x4)bone.Transform.localToWorldMatrix : Matrix4x4.identity;
                var boneMat = transformMat * bone.Bindpose;
                matrix += boneMat * weight;
            }

            matrix = rendererWorldToLocalMatrix * matrix;
            return matrix * new Vector4(position.x, position.y, position.z, 1f);
        }
    }

    internal class Bone
    {
        public Matrix4x4 Bindpose;
        public Transform Transform;

        public Bone(Matrix4x4 bindPose) : this(bindPose, null) {}
        public Bone(Matrix4x4 bindPose, Transform transform) => (Bindpose, Transform) = (bindPose, transform);
    }

    internal enum TexCoordStatus
    {
        NotDefined = 0,
        Vector2 = 1,
        Vector3 = 2,
        Vector4 = 3,
    }
}
#endif
