using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    internal class RemoveMeshByMaskRenderFilter : AAORenderFilterBase<RemoveMeshByMask>
    {
        private RemoveMeshByMaskRenderFilter() : base("Remove Mesh by Mask", "remove-mesh-by-mask")
        {
        }

        public static RemoveMeshByMaskRenderFilter Instance { get; } = new();
        protected override AAORenderFilterNodeBase<RemoveMeshByMask> CreateNode() => new RemoveMeshByMaskRendererNode();
        protected override bool SupportsMultiple() => false;
    }

    internal class RemoveMeshByMaskRendererNode : AAORenderFilterNodeBase<RemoveMeshByMask>
    {
        protected override ValueTask Process(SkinnedMeshRenderer original, SkinnedMeshRenderer proxy,
            RemoveMeshByMask[] components,
            Mesh duplicated, ComputeContext context)
        {
            var component = components[0];

            var uv = duplicated.uv;
            using var uvJob = new NativeArray<Vector2>(uv, Allocator.TempJob);

            var materialSettings = context.Observe(component, c => c.materials.ToArray(), (a, b) => a.SequenceEqual(b));
            for (var subMeshI = 0; subMeshI < duplicated.subMeshCount; subMeshI++)
            {
                if (subMeshI < materialSettings.Length)
                {
                    var materialSetting = materialSettings[subMeshI];
                    if (!materialSetting.enabled) continue;
                    if (materialSetting.mask == null) continue;
                    if (!materialSetting.mask.isReadable) continue;

                    var editingTexture = MaskTextureEditor.Window.ObservePreviewTextureFor(original, subMeshI, context);
                    int textureWidth;
                    int textureHeight;
                    Color32[] pixels;
                    if (editingTexture != null)
                    {
                        textureWidth = editingTexture.width;
                        textureHeight = editingTexture.height;
                        pixels = editingTexture.GetPixels32();
                    }
                    else
                    {
                        // Register to be re-evaluted when the texture changes. We avoid using GetPixels32 here for
                        // performance reasons, as this is frequently re-invoked by NDMF's PropertyMonitor.
                        context.Observe(materialSetting.mask, m => (m.width, m.height, m.imageContentsHash));

                        textureWidth = materialSetting.mask.width;
                        textureHeight = materialSetting.mask.height;
                        pixels = materialSetting.mask.GetPixels32();
                    }
                    using var pixelsJob = new NativeArray<Color32>(pixels, Allocator.TempJob);

                    bool removeWhite;
                    switch (materialSetting.mode)
                    {
                        case RemoveMeshByMask.RemoveMode.RemoveWhite:
                            removeWhite = true;
                            break;
                        case RemoveMeshByMask.RemoveMode.RemoveBlack:
                            removeWhite = false;
                            break;
                        default:
                            BuildLog.LogError("RemoveMeshByMask:error:unknownMode");
                            continue;
                    }

                    var subMesh = duplicated.GetSubMesh(subMeshI);
                    int vertexPerPrimitive;
                    switch (subMesh.topology)
                    {
                        case MeshTopology.Triangles:
                            vertexPerPrimitive = 3;
                            break;
                        case MeshTopology.Quads:
                            vertexPerPrimitive = 4;
                            break;
                        case MeshTopology.Lines:
                            vertexPerPrimitive = 2;
                            break;
                        case MeshTopology.Points:
                            vertexPerPrimitive = 1;
                            break;
                        case MeshTopology.LineStrip:
                        default:
                            // unsupported topology
                            continue;
                    }

                    var triangles = duplicated.GetTriangles(subMeshI);
                    var primitiveCount = triangles.Length / vertexPerPrimitive;

                    using var trianglesJob = new NativeArray<int>(triangles, Allocator.TempJob);
                    using var shouldRemove = new NativeArray<bool>(primitiveCount, Allocator.TempJob);
                    UnityEngine.Profiling.Profiler.BeginSample("JobLoop");
                    var job = new CheckRemovePolygonJob
                    {
                        vertexPerPrimitive = vertexPerPrimitive,
                        textureWidth = textureWidth,
                        textureHeight = textureHeight,
                        removeWhite = removeWhite,
                        triangles = trianglesJob,
                        uv = uvJob,
                        pixels = pixelsJob,
                        shouldRemove = shouldRemove,
                    };
                    job.Schedule(primitiveCount, 32).Complete();
                    UnityEngine.Profiling.Profiler.EndSample();

                    var modifiedTriangles = new List<int>(triangles.Length);

                    UnityEngine.Profiling.Profiler.BeginSample("Inner Main Loop");
                    for (var primitiveI = 0; primitiveI < triangles.Length; primitiveI += vertexPerPrimitive)
                    {
                        if (!shouldRemove[primitiveI / vertexPerPrimitive])
                        {
                            for (var vertexI = 0; vertexI < vertexPerPrimitive; vertexI++)
                                modifiedTriangles.Add(triangles[primitiveI + vertexI]);
                        }
                    }

                    UnityEngine.Profiling.Profiler.EndSample();

                    duplicated.SetTriangles(modifiedTriangles, subMeshI);
                }
            }

            return default;
        }

        private class Color32Comparator : IEqualityComparer<Color32>
        {
            public static Color32Comparator Instance = new();
            public bool Equals(Color32 x, Color32 y) => x.r == y.r && x.g == y.g && x.b == y.b && x.a == y.a;
            public int GetHashCode(Color32 obj) => HashCode.Combine(obj.r, obj.g, obj.b, obj.a);
        }

        [BurstCompile]
        struct CheckRemovePolygonJob : IJobParallelFor
        {
            // ReSharper disable InconsistentNaming
            public int vertexPerPrimitive;
            public int textureWidth;
            public int textureHeight;
            public bool removeWhite;
            [ReadOnly] public NativeArray<int> triangles;
            [ReadOnly] public NativeArray<Vector2> uv;
            [ReadOnly] public NativeArray<Color32> pixels;
            [WriteOnly] public NativeArray<bool> shouldRemove;
            // ReSharper restore InconsistentNaming

            public void Execute(int primitiveIndex)
            {
                var baseIndex = primitiveIndex * vertexPerPrimitive;
                var indices = triangles.Slice(baseIndex, vertexPerPrimitive);

                var result = true;
                foreach (var index in indices)
                {
                    var isWhite = GetValue(uv[index].x, uv[index].y) > 127;
                    if (isWhite != removeWhite)
                    {
                        result = false;
                        break;
                    }
                }

                shouldRemove[primitiveIndex] = result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int GetValue(float u, float v)
            {
                var x = Mathf.FloorToInt(Utils.Modulo(v, 1) * textureHeight);
                var y = Mathf.FloorToInt(Utils.Modulo(u, 1) * textureWidth);
                var pixel = pixels[x * textureWidth + y];
                return Mathf.Max(Mathf.Max(pixel.r, pixel.g), pixel.b);
            }
        }
    }
}
