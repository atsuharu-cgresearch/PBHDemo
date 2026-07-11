using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    /// <summary>
    /// パーティクルを目標位置へ移動させる（力学ベース）
    /// 拘束条件の代わりにこれを使うことで、Live2Dのようにハイライトが揺れる感じになる
    /// </summary>
    public class TargetPosForce
    {
        private ComputeShader compute;
        private int kMain;
        private int threadGroups;

        private ComputeBuffer offsetBuffer;

        public readonly int MAX_OBJECTS = 128;

        public TargetPosForce()
        {
            compute = Resources.Load<ComputeShader>("ComputeShader/Physics/TargetPosForce");

            kMain = compute.FindKernel("CS_Main");

            offsetBuffer = ComputeHelper.CreateStructuredBuffer<Vector4>(MAX_OBJECTS);
        }

        public void SetOffsets(Transform2D[] offsets)
        {
            offsetBuffer.SetData(offsets, 0, 0, offsets.Length);
        }

        public void Bind(ComputeBuffer particles, ComputeBuffer localPositions, ComputeBuffer references, float dt)
        {
            compute.SetBuffer(kMain, "_Particles", particles);
            compute.SetBuffer(kMain, "_LocalPositions", localPositions);
            compute.SetBuffer(kMain, "_Offsets", offsetBuffer);
            compute.SetBuffer(kMain, "_References", references);

            compute.SetInt("_NumParticles", particles.count);
            compute.SetFloat("_Dt", dt);

            threadGroups = Mathf.CeilToInt(particles.count / 64f);
        }

        public void ApplyForce(float k)
        {
            compute.SetFloat("_K", k);

            compute.Dispatch(kMain, threadGroups, 1, 1);
        }

        public virtual void ReleaseBuffers()
        {
            ComputeHelper.Release(offsetBuffer);
        }
    }
}

