using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace PBH
{
    public class AreaConstraint
    {
        private ComputeShader compute;
        private int kCalcDelta;
        private int kApplyDelta;

        private ComputeBuffer particleBuffer;
        private ComputeBuffer clusterBuffer;
        private ComputeBuffer indexBuffer;
        private ComputeBuffer correctionBuffer;
        private ComputeBuffer referenceBuffer;
        private ComputeBuffer helperBuffer;

        private int threadGroupsCalc;
        private int threadGroupsApply;

        public struct Cluster
        {
            public float restArea;
            public int pStart;
        }

        public struct Helper
        {
            public int start;
            public int count;
        }

        public AreaConstraint(ComputeBuffer particleBuffer, int[] indexArray, Cluster[] clusters)
        {
            this.particleBuffer = particleBuffer;

            CreateReferenceTables(indexArray, out int[] referenceArray, out Helper[] helperArray);
            
            LoadComputeShader();

            AllocateBuffers(clusters, indexArray, referenceArray, helperArray);

            BindBuffersToShader();

            CalculateThreadGroups(clusters.Length);
        }

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
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Physics/AreaConstraint"));

            kCalcDelta = compute.FindKernel("CS_CalcDelta");
            kApplyDelta = compute.FindKernel("CS_ApplyDelta");
        }

        private void AllocateBuffers(Cluster[] clusters, int[] indexArray, int[] referenceArray, Helper[] helperArray)
        {
            clusterBuffer = ComputeHelper.CreateStructuredBuffer(clusters);
            indexBuffer = ComputeHelper.CreateStructuredBuffer(indexArray);
            correctionBuffer = ComputeHelper.CreateStructuredBuffer<Vector2>(indexArray.Length);
            referenceBuffer = ComputeHelper.CreateStructuredBuffer(referenceArray);
            helperBuffer = ComputeHelper.CreateStructuredBuffer(helperArray);
        }

        private void BindBuffersToShader()
        {
            // Calc Kernel
            compute.SetBuffer(kCalcDelta, "_Particles", particleBuffer);
            compute.SetBuffer(kCalcDelta, "_Clusters", clusterBuffer);
            compute.SetBuffer(kCalcDelta, "_Indices", indexBuffer);
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

            cmd.BeginSample("PBD_Area_Calc");
            // compute.Dispatch(kCalcDelta, threadGroupsCalc, 1, 1);
            cmd.DispatchCompute(compute, kCalcDelta, threadGroupsCalc, 1, 1);
            cmd.EndSample("PBD_Area_Calc");

            cmd.BeginSample("PBD_Area_Apply");
            // compute.Dispatch(kApplyDelta, threadGroupsApply, 1, 1);
            cmd.DispatchCompute(compute, kApplyDelta, threadGroupsApply, 1, 1);
            cmd.EndSample("PBD_Area_Apply");
        }

        public void Release()
        {
            ComputeHelper.Release(
                clusterBuffer,
                indexBuffer,
                correctionBuffer,
                referenceBuffer,
                helperBuffer
            );
        }
    }
}