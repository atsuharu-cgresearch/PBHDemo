using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBH
{
    public class TargetPosSolver
    {
        private ComputeShader compute;
        private int kMain;
        private int threadGroups;

        private ComputeBuffer offsetBuffer;

        public readonly int MAX_OBJECTS = 128;

        public TargetPosSolver()
        {
            compute = Resources.Load<ComputeShader>("ComputeShader/Physics/TargetPositionConstraint");
            
            if (compute == null)
            {
                Debug.LogError("ComputeShaderがありません");
            }
            kMain = compute.FindKernel("CS_Main");

            offsetBuffer = ComputeHelper.CreateStructuredBuffer<Vector4>(MAX_OBJECTS);
        }

        public void SetOffsets(Transform2D[] offsets)
        {
            offsetBuffer.SetData(offsets, 0, 0, offsets.Length);
        }

        public void Bind(ComputeBuffer particles, ComputeBuffer localPositions, ComputeBuffer references)
        {
            compute.SetBuffer(kMain, "_Particles", particles);
            compute.SetBuffer(kMain, "_LocalPositions", localPositions);
            compute.SetBuffer(kMain, "_Offsets", offsetBuffer);
            compute.SetBuffer(kMain, "_References", references);

            compute.SetInt("_NumParticles", particles.count);

            threadGroups = Mathf.CeilToInt(particles.count / 64f);            
        }

        public void ConstrainPositions(float k, CommandBuffer cmd)
        {
            // 動的に変更されるパラメータは引数で受け取ってここで設定する
            compute.SetFloat("_K", k);

            // 拘束条件を解いて、パーティクルの位置を修正
            // compute.Dispatch(kMain, threadGroups, 1, 1);

            // PIX計測用
            cmd.BeginSample("PBD_TargetPosSolver");
            cmd.DispatchCompute(compute, kMain, threadGroups, 1, 1);
            cmd.EndSample("PBD_TargetPosSolver");
        }

        public virtual void ReleaseBuffers()
        {
            ComputeHelper.Release(offsetBuffer);
        }
    }
}
