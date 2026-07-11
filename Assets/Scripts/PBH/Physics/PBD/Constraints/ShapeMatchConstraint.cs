using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

namespace PBH
{
    public class ShapeMatchConstraint
    {
        private ComputeShader compute;
        private int kCalcDelta;
        private int kApplyDelta;

        private ComputeBuffer particleBuffer;
        private ComputeBuffer clusterBuffer;
        // WebGPUでビルドする時にバッファの数の制限があるのでまとめる
        private ComputeBuffer shapeMatchPointBuffer;
        // private ComputeBuffer indexBuffer;
        // private ComputeBuffer restPositionBuffer;
        private ComputeBuffer correctionBuffer;
        private ComputeBuffer referenceBuffer;
        private ComputeBuffer helperBuffer;

        private int threadGroupsCalc;
        private int threadGroupsApply;

        public struct Cluster
        {
            public int objPOffset;
            public int objPCount;
            public int pStart;
            public int pCount;
        }

        public struct ShapeMatchPoint
        {
            public int index;
            public Vector2 restPosition;
        }

        public struct Helper
        {
            public int start;
            public int count;
        }

        public ShapeMatchConstraint(ComputeBuffer particleBuffer, ParticleData[] particleDataArray, int[] indexArray, Cluster[] clusterArray)
        {
            this.particleBuffer = particleBuffer;

            Cluster[] clusters = CreateClustersAndRestPositions(particleDataArray, indexArray, clusterArray, out Vector2[] restPositions);

            CreateReferenceTables(indexArray, out int[] referenceArray, out Helper[] helperArray);
            
            LoadComputeShader();

            AllocateBuffers(clusters, indexArray, restPositions, referenceArray, helperArray);

            BindBuffersToShader();

            CalculateThreadGroups(clusters.Length);
        }

        /// <summary>
        /// ShapeMatchingのクラスタの作成・事前計算（初期位置と重心）
        /// </summary>
        private Cluster[] CreateClustersAndRestPositions(ParticleData[] particles, int[] indexArray, Cluster[] clusterArray, out Vector2[] restPositions)
        {
            restPositions = new Vector2[indexArray.Length];

            for (int i = 0; i < clusterArray.Length; i++)
            {
                Cluster cluster = clusterArray[i];

                if (cluster.pCount <= 0) continue;

                Vector2 restCenter = Vector2.zero;
                for (int j = 0; j < cluster.pCount; j++)
                {
                    restCenter += particles[indexArray[cluster.pStart + j]].position;
                }
                restCenter /= (float)cluster.pCount;

                for (int j = 0; j < cluster.pCount; j++)
                {
                    restPositions[cluster.pStart + j] = particles[indexArray[cluster.pStart + j]].position - restCenter;
                }
            }

            return clusterArray;
        }

        /// <summary>
        /// ソルバーの実行はクラスタごと、位置の修正の適用はパーティクルごとに並列で行うので、
        /// パーティクル → クラスタの参照が必要
        /// </summary>
        private void CreateReferenceTables(int[] indexArray, out int[] referenceArray, out Helper[] helperArray)
        {
            int numParticles = particleBuffer.count;
            List<int>[] myCorrectionList = new List<int>[numParticles];
            for (int i = 0; i < numParticles; i++) myCorrectionList[i] = new List<int>();

            for (int i = 0; i < indexArray.Length; i++)
            {
                myCorrectionList[indexArray[i]].Add(i);
            }

            List<int> myCorrectionList1D = new List<int>();
            helperArray = new Helper[numParticles];
            int count = 0;

            for (int i = 0; i < numParticles; i++)
            {
                Helper helper = new Helper();
                helper.start = count;
                helper.count = myCorrectionList[i].Count;
                helperArray[i] = helper;

                for (int j = 0; j < myCorrectionList[i].Count; j++)
                {
                    myCorrectionList1D.Add(myCorrectionList[i][j]);
                }
                count += myCorrectionList[i].Count;
            }
            referenceArray = myCorrectionList1D.ToArray();
        }

        private void LoadComputeShader()
        {
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Physics/ShapeMatching"));

            kCalcDelta = compute.FindKernel("CS_CalcDelta");
            kApplyDelta = compute.FindKernel("CS_ApplyDelta");
        }

        private void AllocateBuffers(Cluster[] clusters, int[] indexArray, Vector2[] restPositions, int[] referenceArray, Helper[] helperArray)
        {
            clusterBuffer = ComputeHelper.CreateStructuredBuffer(clusters);
            // indexBuffer = ComputeHelper.CreateStructuredBuffer(indexArray);
            // restPositionBuffer = ComputeHelper.CreateStructuredBuffer(restPositions);
            correctionBuffer = ComputeHelper.CreateStructuredBuffer<Vector2>(indexArray.Length);
            referenceBuffer = ComputeHelper.CreateStructuredBuffer(referenceArray);
            helperBuffer = ComputeHelper.CreateStructuredBuffer(helperArray);

            ShapeMatchPoint[] points = new ShapeMatchPoint[indexArray.Length];
            for (int i = 0; i < indexArray.Length; i++)
            {
                points[i] = new ShapeMatchPoint
                {
                    index = indexArray[i],
                    restPosition = restPositions[i],
                };
            }
            shapeMatchPointBuffer = ComputeHelper.CreateStructuredBuffer(points);
        }

        private void BindBuffersToShader()
        {
            // Calc Kernel
            compute.SetBuffer(kCalcDelta, "_Particles", particleBuffer);
            compute.SetBuffer(kCalcDelta, "_Clusters", clusterBuffer);
            compute.SetBuffer(kCalcDelta, "_ShapeMatchPoints", shapeMatchPointBuffer);
            // compute.SetBuffer(kCalcDelta, "_Indices", indexBuffer);
            // compute.SetBuffer(kCalcDelta, "_RestPositions", restPositionBuffer);
            compute.SetBuffer(kCalcDelta, "_Corrections", correctionBuffer);            

            // Apply Kernel
            compute.SetBuffer(kApplyDelta, "_Particles", particleBuffer);
            compute.SetBuffer(kApplyDelta, "_Corrections", correctionBuffer);
            compute.SetBuffer(kApplyDelta, "_References", referenceBuffer);
            compute.SetBuffer(kApplyDelta, "_Helpers", helperBuffer);

            compute.SetInt("_NumParticles", particleBuffer.count);
            compute.SetInt("_NumConstraints", clusterBuffer.count);
        }

        private void CalculateThreadGroups(int numClusters)
        {
            threadGroupsCalc = Mathf.CeilToInt(numClusters / 64f);
            threadGroupsApply = Mathf.CeilToInt(particleBuffer.count / 64f);
        }

        public void ConstrainPositions(float k, CommandBuffer cmd)
        {
            compute.SetFloat("_K", k);

            cmd.BeginSample("PBD_ShapeMatching_Calc");
            // compute.Dispatch(kCalcDelta, threadGroupsCalc, 1, 1);
            cmd.DispatchCompute(compute, kCalcDelta, threadGroupsCalc, 1, 1);
            cmd.EndSample("PBD_ShapeMatching_Calc");

            cmd.BeginSample("PBD_ShapeMatching_Apply");
            // compute.Dispatch(kApplyDelta, threadGroupsApply, 1, 1);
            cmd.DispatchCompute(compute, kApplyDelta, threadGroupsApply, 1, 1);
            cmd.EndSample("PBD_ShapeMatching_Apply");
        }

        public void Release()
        {
            ComputeHelper.Release(
                clusterBuffer,
                shapeMatchPointBuffer,
                // indexBuffer,
                // restPositionBuffer,
                correctionBuffer,
                referenceBuffer,
                helperBuffer
            );
        }
    }
}