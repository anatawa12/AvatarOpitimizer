using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.EditModePreview
{
    /// <summary>
    /// Preview Context for RemoveMesh with Box.
    /// RuntimePreview of Remove Mesh with Box needs BlendShape applied vertex position so
    /// this class holds that information and update if nesessary
    /// </summary>
    class RemoveMeshWithBoxPreviewContext : IDisposable
    {
        private readonly int _vertexCount;

        public NativeArray<Vector3> Vertices => _vertices;

        // not transformed vertices
        private NativeArray<Vector3> _originalVertices;
        // this should be blendShape transformed
        private NativeArray<Vector3> _vertices;
        // blendshape vertex transforms. _blendShapeVertices[vertexIndex + frameIndex * vertexCount]
        private NativeArray<Vector3> _blendShapeVertices;
        // frame info. _blendShapeFrameInfo[blendShapeIndex][frameIndexInShape]
        private readonly (float weight, int globalIndex)[][] _blendShapeFrameInfo;
        // configured BlendShape weights
        [NotNull] private readonly float[] _blendShapeWeights;

        public RemoveMeshWithBoxPreviewContext(Mesh originalMesh)
        {
            _vertexCount = originalMesh.vertexCount;
            _originalVertices = new NativeArray<Vector3>(_vertexCount, Allocator.Persistent);
            _originalVertices.CopyFrom(originalMesh.vertices);

            // initialize with original vertices
            _vertices = new NativeArray<Vector3>(_originalVertices, Allocator.Persistent);

            // BlendShapes
            var blendShapeFrameCount = 0;
            for (var i = 0; i < originalMesh.blendShapeCount; i++)
                blendShapeFrameCount += originalMesh.GetBlendShapeFrameCount(i);

            _blendShapeFrameInfo = new (float weight, int globalIndex)[originalMesh.blendShapeCount][];
            _blendShapeVertices = new NativeArray<Vector3>(_vertexCount * blendShapeFrameCount, Allocator.Persistent);
            var frameIndex = 0;
            var getterBuffer = new Vector3[_vertexCount];
            for (var i = 0; i < originalMesh.blendShapeCount; i++)
            {
                var frameCount = originalMesh.GetBlendShapeFrameCount(i);
                var thisShapeInfo = _blendShapeFrameInfo[i] = new (float weight, int globalIndex)[frameCount];
                for (var j = 0; j < frameCount; j++)
                {
                    originalMesh.GetBlendShapeFrameVertices(i, j, getterBuffer, null, null);
                    getterBuffer.AsSpan()
                        .CopyTo(_blendShapeVertices.AsSpan().Slice(frameIndex * _vertexCount, _vertexCount));
                    thisShapeInfo[j] = (originalMesh.GetBlendShapeFrameWeight(i, j), frameIndex);
                    frameIndex++;
                }
            }
            _blendShapeWeights = new float[originalMesh.blendShapeCount];
        }

        public void UpdateVertices(SkinnedMeshRenderer renderer)
        {
            var modified = false;
            for (var i = 0; i < _blendShapeWeights.Length; i++)
            {
                var currentWeight = renderer.GetBlendShapeWeight(i);
                if (Math.Abs(currentWeight - _blendShapeWeights[i]) > Mathf.Epsilon)
                {
                    _blendShapeWeights[i] = currentWeight;
                    modified = true;
                }
            }

            if (!modified) return;

            var frameIndices = new List<int>();
            var frameWeights = new List<float>();

            for (var i = 0; i < _blendShapeWeights.Length; i++)
                GetBlendShape(i, _blendShapeWeights[i], frameIndices, frameWeights);

            if (frameWeights.Count == 0)
            {
                _vertices.CopyFrom(_originalVertices);
                return;
            }

            using (var blendShapeFrameIndices = new NativeArray<int>(frameIndices.ToArray(), Allocator.TempJob))
            using (var blendShapeFrameWeights = new NativeArray<float>(frameWeights.ToArray(), Allocator.TempJob))
            {
                new ApplyBlendShapeJob
                {
                    OriginalVertices = _originalVertices,
                    BlendShapeVertices = _blendShapeVertices,
                    BlendShapeFrameIndices = blendShapeFrameIndices,
                    BlendShapeFrameWeights = blendShapeFrameWeights,
                    ResultVertices = _vertices
                }.Schedule(_vertexCount, 1).Complete();
            }
        }

        [BurstCompile]
        struct ApplyBlendShapeJob: IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Vector3> OriginalVertices;
            [ReadOnly]
            public NativeArray<Vector3> BlendShapeVertices;
            [ReadOnly]
            public NativeArray<int> BlendShapeFrameIndices;
            [ReadOnly]
            public NativeArray<float> BlendShapeFrameWeights;
            public NativeArray<Vector3> ResultVertices;

            public void Execute (int vertexIndex)
            {
                var vertexCount = OriginalVertices.Length;
                var original = OriginalVertices[vertexIndex];

                for (var indicesIndex = 0; indicesIndex < BlendShapeFrameIndices.Length; indicesIndex++)
                {
                    var frameIndex = BlendShapeFrameIndices[indicesIndex];
                    var weight = BlendShapeFrameWeights[indicesIndex];
                    var delta = BlendShapeVertices[frameIndex * vertexCount + vertexIndex];
                    original += delta * weight;
                }

                ResultVertices[vertexIndex] = original;
            }
        }

        public void Dispose()
        {
            _originalVertices.Dispose();
            _vertices.Dispose();
            _blendShapeVertices.Dispose();
        }

        public void GetBlendShape(int shapeIndex, float weight, List<int> frameIndices, List<float> frameWeights)
        {
            const float blendShapeEpsilon = 0.0001f;
            var frames = _blendShapeFrameInfo[shapeIndex];

            if (Mathf.Abs(weight) <= blendShapeEpsilon && ZeroForWeightZero())
            {
                // the blendShape is not active
                return;
            }

            bool ZeroForWeightZero()
            {
                if (frames.Length == 1) return true;
                var first = frames.First();
                var end = frames.Last();

                // both weight are same sign, zero for 0 weight
                if (first.weight <= 0 && end.weight <= 0) return true;
                if (first.weight >= 0 && end.weight >= 0) return true;

                return false;
            }

            if (frames.Length == 1)
            {
                // simplest and likely
                var frame = frames[0];
                frameIndices.Add(frame.globalIndex);
                frameWeights.Add(weight / frame.weight);
            }
            else
            {
                // multi frame
                
                var firstFrame = frames[0];
                var lastFrame = frames.Last();

                if (firstFrame.weight > 0 && weight < firstFrame.weight)
                {
                    // if all weights are positive and the weight is less than first weight: lerp 0..first
                    frameIndices.Add(firstFrame.globalIndex);
                    frameWeights.Add(weight / firstFrame.weight);
                }

                if (lastFrame.weight < 0 && weight > lastFrame.weight)
                {
                    // if all weights are negative and the weight is more than last weight: lerp last..0
                    frameIndices.Add(lastFrame.globalIndex);
                    frameWeights.Add(weight / lastFrame.weight);
                }

                // otherwise, lerp between two surrounding frames OR nearest two frames
                var (lessFrame, greaterFrame) = FindFrame();
                var weightDiff = greaterFrame.weight - lessFrame.weight;
                var lessFrameWeight = (weight - lessFrame.weight) / weightDiff;
                var graterFrameWeight = (greaterFrame.weight - weight) / weightDiff;

                if (!(Mathf.Abs(lessFrameWeight) < blendShapeEpsilon))
                {
                    frameIndices.Add(lessFrame.globalIndex);
                    frameWeights.Add(lessFrameWeight);
                }
                
                if (!(Mathf.Abs(graterFrameWeight) < blendShapeEpsilon))
                {
                    frameIndices.Add(greaterFrame.globalIndex);
                    frameWeights.Add(graterFrameWeight);
                }
            }

            return;

            // TODO: merge this logic with it in MeshInfo2
            ((float weight, int globalIndex), (float weight, int globalIndex)) FindFrame()
            {
                for (var i = 1; i < frames.Length; i++)
                {
                    if (weight <= frames[i].weight)
                        return (frames[i - 1], frames[i]);
                }

                return (frames[frames.Length - 2], frames[frames.Length - 1]);
            }
        }
    }
}